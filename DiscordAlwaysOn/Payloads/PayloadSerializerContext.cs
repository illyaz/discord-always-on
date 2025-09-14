using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IdentifyPayload))]
[JsonSerializable(typeof(HelloPayload))]
[JsonSerializable(typeof(HeartbeatPayload))]
[JsonSerializable(typeof(HeartbeatAckPayload))]
[JsonSerializable(typeof(Payload))]
[JsonSerializable(typeof(DispatchPayload))]
[JsonSerializable(typeof(DispatchPayload<Ready>))]
public partial class PayloadSerializerContext : JsonSerializerContext;