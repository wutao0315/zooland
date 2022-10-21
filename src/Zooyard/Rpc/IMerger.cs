namespace Zooyard.Rpc;

public interface IMerger
{
    Type Type { get; }
    string Name { get; }
}
public interface IMerger<T> : IMerger
{
    T? Merge(params T[] items);
}
