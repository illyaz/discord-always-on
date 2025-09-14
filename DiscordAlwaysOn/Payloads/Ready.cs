using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record Ready(
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("session_id")]
    string SessionId) : IDispatchData;