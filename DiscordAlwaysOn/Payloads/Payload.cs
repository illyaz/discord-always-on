using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record Payload(
    [property: JsonPropertyName("op")] OpCode OpCode,
    [property: JsonPropertyName("s")] int? Sequence = default,
    [property: JsonPropertyName("t")] string? Event = default,
    [property: JsonPropertyName("d")] JsonElement? Data = default);