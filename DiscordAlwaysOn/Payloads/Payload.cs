using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record Payload(
    [property: JsonPropertyName("op")] OpCode Op) : IPayload;

public record Payload<T>(
    [property: JsonPropertyName("op")] OpCode Op,
    [property: JsonPropertyName("d")] T Data)
    : IPayload<T>;