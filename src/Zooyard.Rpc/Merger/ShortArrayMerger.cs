namespace Zooyard.Rpc.Merger;

public class ShortArrayMerger : IMerger<short[]>
{
    public short[] Merge(params short[][] items)
    {
        int total = 0;
        foreach (short[] array in items)
        {
            total += array.Length;
        }
        short[] result = new short[total];
        int index = 0;
        foreach (short[] array in items)
        {
            foreach (short item in array)
            {
                result[index++] = item;
            }
        }
        return result;
    }
}
