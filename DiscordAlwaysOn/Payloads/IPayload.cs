namespace DiscordAlwaysOn.Payloads;

public interface IPayload
{
    public OpCode Op { get; init; }
}

public interface IPayload<T> : IPayload
{
    public T Data { get; init; }
}