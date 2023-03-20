using System.Text;

namespace Zooyard.Rpc.Route.State;

public record RouterGroupingState
{
    public RouterGroupingState(string routerName, int total, Dictionary<String, IList<URL>> grouping)
    {
        RouterName = routerName;
        Total = total;
        Grouping = grouping;
    }

    public string RouterName { get; init; }

    public int Total { get; init; }

    public Dictionary<String, IList<URL>> Grouping { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(RouterName)
            .Append(' ')
            .Append(" Total: ")
            .Append(Total)
            .Append('\n');

        foreach (var entry in Grouping)
        {
            IList<URL> invokers = entry.Value;
            stringBuilder.Append("[ ")
                .Append(entry.Key)
                .Append(" -> ")
                .Append(invokers.Count == 0 ? "Empty" : string.Join(",", invokers.Take(5)))
                .Append(invokers.Count > 5 ? "..." : "")
                .Append(" (Total: ")
                .Append(invokers.Count)
                .Append(") ]")
                .Append('\n');
        }
        return stringBuilder.ToString();
    }
}
