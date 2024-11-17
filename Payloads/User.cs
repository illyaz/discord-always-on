using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record User(
    [property: JsonPropertyName("id")]
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    ulong Id,
    [property: JsonPropertyName("global_name")]
    string GlobalName);