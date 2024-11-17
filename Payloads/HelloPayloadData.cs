using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record HelloPayloadData(
    [property: JsonPropertyName("heartbeat_interval")]
    int HeartbeatInterval);