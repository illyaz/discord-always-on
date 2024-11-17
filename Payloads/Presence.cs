using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record Presence(
    [property: JsonPropertyName("activities")]
    JsonElement Activities,
    [property: JsonPropertyName("status")]
    [property: JsonConverter(typeof(PresenceStatusConverter))]
    PresenceStatus Status,
    [property: JsonPropertyName("afk")] bool Afk,
    [property: JsonPropertyName("since")] int? Since = default);

internal class PresenceStatusConverter() : JsonStringEnumConverter<PresenceStatus>(JsonNamingPolicy.CamelCase);