using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record IdentifyPayloadData(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("properties")]
    IdentifyProperties Properties,
    [property: JsonPropertyName("presence")]
    Presence? Presence = null);