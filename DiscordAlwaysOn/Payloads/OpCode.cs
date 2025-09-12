namespace DiscordAlwaysOn.Payloads;

public enum OpCode : byte
{
    Dispatch = 0,
    Heartbeat = 1,
    Identify = 2,
    Hello = 10,
    HeartbeatAck = 11
}