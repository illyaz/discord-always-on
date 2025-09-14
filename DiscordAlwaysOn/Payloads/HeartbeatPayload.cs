namespace DiscordAlwaysOn.Payloads;

public record HeartbeatPayload() : Payload<object?>(OpCode.Heartbeat, null);