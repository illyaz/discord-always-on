using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

public record DispatchPayload<T>(
    OpCode Op,
    int Sequence,
    string Event,
    [property: JsonPropertyName("d")] T Data) : DispatchPayload(Op, Sequence, Event);

public record DispatchPayload(
    [property: JsonPropertyName("op")] OpCode Op,
    [property: JsonPropertyName("s")] int Sequence,
    [property: JsonPropertyName("t")] string Event) : IDispatchPayload;