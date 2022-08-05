using System.Text;

namespace Zooyard.Rpc.Route.State;

public class RouterGroupingState
{
    private readonly string routerName;
    private readonly int total;
    private readonly Dictionary<string, BitList<IInvoker>> grouping;

    public RouterGroupingState(String routerName, int total, Dictionary<String, BitList<IInvoker>> grouping)
    {
        this.routerName = routerName;
        this.total = total;
        this.grouping = grouping;
    }

    public String getRouterName()
    {
        return routerName;
    }

    public int getTotal()
    {
        return total;
    }

    public Dictionary<String, BitList<IInvoker>> getGrouping()
    {
        return grouping;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(routerName)
            .Append(' ')
            .Append(" Total: ")
            .Append(total)
            .Append('\n');

        foreach (var entry in grouping)
        {
            BitList<IInvoker> invokers = entry.Value;
            stringBuilder.Append("[ ")
                .Append(entry.Key)
                .Append(" -> ")
                .Append(invokers.Count == 0 ? "Empty" : string.Join(",", invokers.Take(5)))
                //invokers.stream()
                //    .limit(5)
                //    .map(Invoker::getUrl)
                //    .map(URL::getAddress)
                //    .collect(Collectors.joining(",")))
                .Append(invokers.Count > 5 ? "..." : "")
                .Append(" (Total: ")
                .Append(invokers.Count)
                .Append(") ]")
                .Append('\n');
        }
        return stringBuilder.ToString();
    }
}
