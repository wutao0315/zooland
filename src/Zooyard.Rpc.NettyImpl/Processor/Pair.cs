namespace Zooyard.Rpc.NettyImpl.Processor;

public sealed class Pair<T1, T2>
{
    public Pair(T1 first, T2 second)
    {
        First = first;
        Second = second;
    }

    public T1 First { get; init; }

    public T2 Second { get; init; }
}
