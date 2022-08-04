namespace Zooyard;

public interface IMerger
{
}
public interface IMerger<T> : IMerger
{
    T? Merge(params T[] items);
}
