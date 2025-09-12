using System.Text.Json.Serialization;

namespace DiscordAlwaysOn.Payloads;

[JsonSerializable(typeof(HelloPayloadData))]
[JsonSerializable(typeof(IdentifyPayloadData))]
[JsonSerializable(typeof(IdentifyProperties))]
[JsonSerializable(typeof(Payload))]
[JsonSerializable(typeof(Presence))]
[JsonSerializable(typeof(ReadyPayloadData))]
[JsonSerializable(typeof(User))]
public partial class PayloadSerializerContext : JsonSerializerContext;