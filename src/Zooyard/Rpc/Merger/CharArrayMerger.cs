namespace Zooyard.Rpc.Merger;

public class CharArrayMerger : IMerger<char[]>
{
    public string Name => "char";
    public Type Type => typeof(char);
    public char[] Merge(params char[][] items)
    {
        int total = 0;
        foreach (char[] array in items)
        {
            total += array.Length;
        }
        char[] result = new char[total];
        int index = 0;
        foreach (char[] array in items)
        {
            foreach (char item in array)
            {
                result[index++] = item;
            }
        }
        return result;
    }
}
