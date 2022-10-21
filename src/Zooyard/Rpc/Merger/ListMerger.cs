namespace Zooyard.Rpc.Merger;

public class ListMerger<T> : IMerger<IEnumerable<T>>
{
    public string Name => "list";
    public Type Type => typeof(IEnumerable<>);
    public IEnumerable<T> Merge(params IEnumerable<T>[] items)
    {
        var result = new List<T>();
        foreach (var item in items)
        {
            if (item != null)
            {
                result.AddRange(item);
            }
        }
        return result;
    }
}
