namespace DiscordAlwaysOn.Payloads;

public record HeartbeatAckPayload() : Payload(OpCode.HeartbeatAck);