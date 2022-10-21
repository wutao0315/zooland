using Newtonsoft.Json.Linq;

namespace Zooyard.Rpc.Merger;

public class DoubleArrayMerger : IMerger<double[]>
{
    public string Name => "double";
    public Type Type => typeof(double);
    public double[] Merge(params double[][] items)
    {
        int total = 0;
        foreach (double[] array in items)
        {
            total += array.Length;
        }
        double[] result = new double[total];
        int index = 0;
        foreach (double[] array in items)
        {
            foreach (double item in array)
            {
                result[index++] = item;
            }
        }
        return result;
    }
}
