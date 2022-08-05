namespace Zooyard.Rpc;

public interface IMerger
{
}
public interface IMerger<T> : IMerger
{
    T? Merge(params T[] items);
}
