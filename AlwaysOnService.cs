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

    private async Task<Payload?> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var receiveBuffer = new ArrayPoolBufferWriter<byte>();
        ValueWebSocketReceiveResult result;
        do
        {
            result = await _client.ReceiveAsync(receiveBuffer.GetMemory(), cancellationToken);
            receiveBuffer.Advance(result.Count);
        } while (!result.EndOfMessage);

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

        return result.MessageType == WebSocketMessageType.Close
            ? default
            : JsonSerializer.Deserialize<Payload>(receiveBuffer.WrittenSpan);
    }

    private ValueTask SendAsync(Payload payload, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(payload);

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Sending payload {Payload}", Encoding.UTF8.GetString(data));

        return _client.SendAsync(data, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage,
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

                if (await ReceiveAsync(token) is not { OpCode: OpCode.Hello, Data: { } helloData })
                    throw new InvalidOperationException("First payload should be hello");

                var hello = helloData.Deserialize<HelloPayloadData>()!;
                var heartbeatTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(hello.HeartbeatInterval));
                cts.CancelAfter(hello.HeartbeatInterval * 2);

                logger.LogInformation("Logging in to Discord ...");

                await SendAsync(new Payload(OpCode.Identify)
                {
                    Data = JsonSerializer.SerializeToElement(
                        new IdentifyPayloadData(
                            options.Value.Token,
                            new IdentifyProperties("Windows 10", "Google Chrome", "Windows"),
                            new Presence(
                                JsonSerializer.Deserialize<JsonElement>(options.Value.Activities ?? "[]"),
                                options.Value.Status, options.Value.Afk,
                                0)))
                }, token);

                var heartbeatTask = heartbeatTimer.WaitForNextTickAsync(token).AsTask();
                var receiveTask = ReceiveAsync(token);

                while (!token.IsCancellationRequested)
                {
                    var when = await Task.WhenAny(receiveTask, heartbeatTask);
                    if (when == receiveTask)
                    {
                        switch (receiveTask.Result)
                        {
                            case { OpCode: OpCode.HeartbeatAck }:
                                cts.CancelAfter(hello.HeartbeatInterval * 2);
                                logger.LogTrace("Receive Heartbeat");
                                break;
                            case { OpCode: OpCode.Dispatch, Event: "READY", Data: { } dispatchData }:
                            {
                                var ready = dispatchData.Deserialize<ReadyPayloadData>()!;
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
                        await SendAsync(new Payload(OpCode.Heartbeat), token);

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
}