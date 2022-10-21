namespace Zooyard.Rpc.Merger;

public class IntArrayMerger : IMerger<int[]>
{
    public string Name => "int";
    public Type Type => typeof(int);
    public int[] Merge(params int[][] items)
    {
        int totalLen = 0;
        foreach (int[] item in items)
        {
            totalLen += item.Length;
        }
        int[] result = new int[totalLen];
        int index = 0;
        foreach (int[] item in items)
        {
            foreach (int i in item)
            {
                result[index++] = i;
            }
        }
        return result;
    }
}
