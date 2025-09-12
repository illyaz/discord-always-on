using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record IdentifyProperties(
    [property: JsonPropertyName("os")] string OperatingSystem,
    [property: JsonPropertyName("browser")]
    string Browser,
    [property: JsonPropertyName("device")] string Device);