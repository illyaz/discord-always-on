using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance.Buffers;
using DiscordAlwaysOn.Payloads;
using Microsoft.Extensions.Options;

namespace DiscordAlwaysOn;

public class AlwaysOnService(
    ILogger<AlwaysOnService> logger,
    IOptions<AlwaysOnOptions> options,
    IHostApplicationLifetime application)
    : BackgroundService
{
    private ClientWebSocket _client = new();

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_client.State is not WebSocketState.None)
        {
            _client.Dispose();
            _client = new ClientWebSocket();
        }

        await _client.ConnectAsync(new Uri("wss://gateway.discord.gg/?v=9&encoding=json"), cancellationToken);
    }

    private async Task<IPayload?> ReceiveAsync(CancellationToken cancellationToken)
    {
        ValueWebSocketReceiveResult result;
        using var buffer = new ArrayPoolBufferWriter<byte>();
        var probeInfo = default(PayloadProbeInfo?);
        var isProbe = false;

        do
        {
            result = await _client.ReceiveAsync(buffer.GetMemory(), cancellationToken);
            buffer.Advance(result.Count);

            if (buffer.WrittenCount < 256 && !result.EndOfMessage)
                continue;

            isProbe = true;
            probeInfo = Probe(buffer.WrittenSpan);
        } while (!isProbe);

        if (result.MessageType == WebSocketMessageType.Close
            && _client.CloseStatus is { } closeStatus
            && closeStatus != WebSocketCloseStatus.NormalClosure)
        {
            if ((int)_client.CloseStatus is 4004)
            {
                application.StopApplication();
                throw new AuthenticationException("Authentication failed");
            }

            throw new Exception($"Gateway connection closed with error {_client.CloseStatus}");
        }

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Received: {Probe}", probeInfo);

        IPayload? payload = null;
        if (probeInfo?.PayloadType is { } payloadType)
        {
            if (!result.EndOfMessage)
            {
                await using var reader = new ClientWebSocketStreamReader(buffer.WrittenMemory, _client);
                payload = (IPayload?)await JsonSerializer.DeserializeAsync(reader, payloadType,
                    PayloadSerializerContext.Default, cancellationToken);
            }
            else
                payload = (IPayload?)JsonSerializer.Deserialize(buffer.WrittenSpan, payloadType,
                    PayloadSerializerContext.Default);
        }
        else
            do
            {
                // Drain
                result = await _client.ReceiveAsync(buffer.GetMemory(), cancellationToken);
            } while (!result.EndOfMessage);

        return payload;
    }

    private ValueTask SendAsync<T>(T payload, CancellationToken cancellationToken)
        where T : IPayload
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(payload, typeof(T), PayloadSerializerContext.Default);

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Sending payload {Payload}", Encoding.UTF8.GetString(data));

        return _client.SendAsync(data, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage,
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        GC.Collect();
        logger.LogInformation(
            "Configuration:\n\tToken: {Token}\n\tStatus: {Status}\n\tAfk: {Afk}\n\tActivities: {Activities}",
            $"{(options.Value.Token.Length >= 8 ? options.Value.Token[..8] : options.Value.Token)}...",
            options.Value.Status,
            options.Value.Afk,
            options.Value.Activities);

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                logger.LogInformation("Connecting to gateway ...");
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromMinutes(1));

                var token = cts.Token;
                await ConnectAsync(token);

                if (await ReceiveAsync(token) is not HelloPayload { Data: { } hello })
                    throw new InvalidOperationException("First payload should be hello");

                var heartbeatTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(hello.HeartbeatInterval));
                cts.CancelAfter(hello.HeartbeatInterval * 2);

                logger.LogInformation("Logging in to Discord ...");

                await SendAsync(new IdentifyPayload(new IdentifyPayloadData(
                    options.Value.Token,
                    new IdentifyProperties("Windows 10", "Google Chrome", "Windows"),
                    new Presence(
                        JsonSerializer.Deserialize(options.Value.Activities ?? "[]",
                            PayloadSerializerContext.Default.JsonElement),
                        options.Value.Status, options.Value.Afk,
                        0))), token);

                var heartbeatTask = heartbeatTimer.WaitForNextTickAsync(token).AsTask();
                var receiveTask = ReceiveAsync(token);

                while (!token.IsCancellationRequested)
                {
                    var when = await Task.WhenAny(receiveTask, heartbeatTask);
                    if (when == receiveTask)
                    {
                        switch (receiveTask.Result)
                        {
                            case HeartbeatAckPayload:
                                cts.CancelAfter(hello.HeartbeatInterval * 2);
                                logger.LogTrace("Receive Heartbeat");
                                break;
                            case DispatchPayload<Ready> { Data: { } ready }:
                            {
                                logger.LogInformation(
                                    "Logged as {Username} ({UserId}) session id {SessionId}", ready.User.GlobalName,
                                    ready.User.Id, ready.SessionId);

                                break;
                            }
                        }

                        receiveTask = ReceiveAsync(token);
                    }
                    else if (when == heartbeatTask)
                    {
                        logger.LogTrace("Sending Heartbeat");
                        await SendAsync(new HeartbeatPayload(), token);

                        heartbeatTask = heartbeatTimer.WaitForNextTickAsync(token).AsTask();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Discord gateway error");
                await Task.Delay(3000, stoppingToken);
            }
    }


    private static PayloadProbeInfo? Probe(ReadOnlySpan<byte> buffer)
    {
        Type? payloadType = null;
        string? type = null;
        int? sequence = null;
        OpCode? opCode = null;

        // Allow read incomplete json data
        var reader = new Utf8JsonReader(buffer, false, default);

        while (reader.Read() && reader.CurrentDepth <= 1)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName
                    when reader.ValueTextEquals("t")
                         && reader.Read():
                    type = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                    break;
                case JsonTokenType.PropertyName
                    when reader.ValueTextEquals("s")
                         && reader.Read():
                    sequence = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
                    break;
                case JsonTokenType.PropertyName
                    when reader.ValueTextEquals("op")
                         && reader.Read():
                    opCode = reader.TokenType == JsonTokenType.Null ? null : (OpCode)reader.GetInt32();
                    break;
            }
        }

        if (opCode is null)
            return null;

        payloadType = opCode switch
        {
            OpCode.Dispatch when string.IsNullOrEmpty(type) && sequence is null
                => throw new FormatException("Invalid dispatch payload"),
            OpCode.Dispatch => type switch
            {
                "READY" => typeof(DispatchPayload<Ready>),
                _ => typeof(DispatchPayload)
            },
            OpCode.Hello => typeof(HelloPayload),
            OpCode.HeartbeatAck => typeof(HeartbeatAckPayload),
            _ => payloadType
        };

        return new PayloadProbeInfo(opCode.Value, type, sequence, payloadType);
    }

    private readonly record struct PayloadProbeInfo(OpCode OpCode, string? Type, int? Sequence, Type? PayloadType);
}