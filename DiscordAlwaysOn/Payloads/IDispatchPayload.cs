namespace DiscordAlwaysOn.Payloads;

public interface IDispatchData;
public interface IDispatchPayload : IPayload
{
    public int Sequence { get; init; }
    public string Event { get; init; }
}

public interface IDispatchPayload<T> : IDispatchPayload
    where T : IDispatchData
{
    public T Data { get; init; }
}
