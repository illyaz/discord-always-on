using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record HelloPayload(OpCode Op, HelloPayloadData Data)
    : Payload<HelloPayloadData>(Op, Data);

public record HelloPayloadData(
    [property: JsonPropertyName("heartbeat_interval")]
    int HeartbeatInterval);