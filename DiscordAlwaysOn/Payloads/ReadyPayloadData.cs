using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record ReadyPayloadData(
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("session_id")]
    string SessionId);