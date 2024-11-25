using System.Text;

namespace Zooyard.Rpc.Route;

public class RouterSnapshotNode
{
    private readonly List<URL> _inputInvokers;
    public RouterSnapshotNode(string name, IList<URL> inputInvokers)
    {
        Name = name;
        BeforeSize = inputInvokers.Count;
        _inputInvokers = new List<URL>(5);

        for (int i = 0; i < Math.Min(5, BeforeSize); i++)
        {
            _inputInvokers.Add(inputInvokers[i]);
        }
    }

    public string Name { get; init; }

    public int BeforeSize { get; init; }

    public string? RouterMessage { get; set; }

    public IList<URL> NodeOutputInvokers { get; set; } = [];

    public IList<URL> ChainOutputInvokers { get; set; } = [];

    public List<RouterSnapshotNode> NextNode { get; private set; } = [];

    public RouterSnapshotNode? ParentNode { get; private set; }

    public void AppendNode(RouterSnapshotNode nextNode)
    {
        this.NextNode.Add(nextNode);
        nextNode.ParentNode = this;
    }

    public override string ToString()
    {
        return toString(1);
    }

    private string toString(int level)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("[ ")
            .Append(Name)
            .Append(' ')
            .Append("(Input: ").Append(BeforeSize).Append(") ")
            .Append("(Current Node Output: ").Append(NodeOutputInvokers.Count).Append(") ")
            .Append("(Chain Node Output: ").Append(ChainOutputInvokers.Count).Append(')')
            .Append(RouterMessage == null ? "" : " Router message: ")
            .Append(RouterMessage ?? "")
            .Append(" ] ");
        if (level == 1)
        {
            stringBuilder.Append("Input: ")
                .Append(_inputInvokers.Count==0 ? "Empty" :
                        string.Join(",", _inputInvokers.Take(Math.Min(5, _inputInvokers.Count))
                        .Select(w=>w.Address)))
            .Append(" -> ");

            stringBuilder.Append("Chain Node Output: ")
                .Append(ChainOutputInvokers.Count == 0 ? "Empty" :
                    string.Join(",", ChainOutputInvokers.Take(Math.Min(5, ChainOutputInvokers.Count)).Select(w=>w.Address)));
        }
        else
        {
            stringBuilder.Append("Current Node Output: ")
                .Append(NodeOutputInvokers.Count==0 ? "Empty" :
                    string.Join(",", NodeOutputInvokers.Take(Math.Min(5, NodeOutputInvokers.Count)).Select(w=>w.Address)));
        }


        if (NodeOutputInvokers != null && NodeOutputInvokers.Count > 5)
        {
            stringBuilder.Append("...");
        }
        foreach (RouterSnapshotNode node in NextNode)
        {
            stringBuilder.Append('\n');
            for (int i = 0; i < level; i++)
            {
                stringBuilder.Append("  ");
            }
            stringBuilder.Append(node.toString(level + 1));
        }
        return stringBuilder.ToString();
    }
}
