namespace Zooyard.Rpc.Merger;

public class SetMerger<T> : IMerger<ISet<T>>
{
    public string Name => "set";
    public Type Type => typeof(ISet<>);
    public ISet<T> Merge(params ISet<T>[] items)
    {
        var result = new HashSet<T>();
        
        foreach (var item in items)
        {
            if (item != null)
            {
                foreach (var i in item)
                {
                    result.Add(i);
                }
            }
        }

        return result;
    }
}
