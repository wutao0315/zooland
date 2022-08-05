using System.Text;
using Zooyard.Rpc.Route.State;

namespace Zooyard.Rpc.Route;

public class RouterSnapshotNode<T>
{
    private readonly string name;
    private readonly int beforeSize;
    private int nodeOutputSize;
    private int chainOutputSize;
    private String routerMessage;
    private readonly IList<IInvoker> inputInvokers;
    private IList<IInvoker> nodeOutputInvokers;
    private IList<IInvoker> chainOutputInvokers;
    private readonly ICollection<RouterSnapshotNode<T>> nextNode = new LinkedList<RouterSnapshotNode<T>>();
    private RouterSnapshotNode<T> parentNode;

    public RouterSnapshotNode(string name, IList<IInvoker> inputInvokers)
    {
        this.name = name;
        this.beforeSize = inputInvokers.Count();
        if (inputInvokers is BitList<IInvoker> inputList) {
            this.inputInvokers = inputList;
        } else
        {
            this.inputInvokers = new List<IInvoker>(5);
            for (int i = 0; i < Math.Min(5, beforeSize); i++)
            {
                this.inputInvokers.Add(inputInvokers[i]);
            }
        }
        this.nodeOutputSize = 0;
    }

    public String getName()
    {
        return name;
    }

    public int getBeforeSize()
    {
        return beforeSize;
    }

    public int getNodeOutputSize()
    {
        return nodeOutputSize;
    }

    public String getRouterMessage()
    {
        return routerMessage;
    }

    public void setRouterMessage(String routerMessage)
    {
        this.routerMessage = routerMessage;
    }

    public IList<IInvoker> getNodeOutputInvokers()
    {
        return nodeOutputInvokers;
    }

    public void setNodeOutputInvokers(IList<IInvoker> outputInvokers)
    {
        this.nodeOutputInvokers = outputInvokers;
        this.nodeOutputSize = outputInvokers == null ? 0 : outputInvokers.Count;
    }

    public void setChainOutputInvokers(IList<IInvoker> outputInvokers)
    {
        this.chainOutputInvokers = outputInvokers;
        this.chainOutputSize = outputInvokers == null ? 0 : outputInvokers.Count;
    }

    public int getChainOutputSize()
    {
        return chainOutputSize;
    }

    public IList<IInvoker> getChainOutputInvokers()
    {
        return chainOutputInvokers;
    }

    public ICollection<RouterSnapshotNode<T>> getNextNode()
    {
        return nextNode;
    }

    public RouterSnapshotNode<T> getParentNode()
    {
        return parentNode;
    }

    public void appendNode(RouterSnapshotNode<T> nextNode)
    {
        this.nextNode.Add(nextNode);
        nextNode.parentNode = this;
    }

    public override string ToString()
    {
        return ToString(1);
    }

    public string ToString(int level)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("[ ")
            .Append(name)
            .Append(' ')
            .Append("(Input: ").Append(beforeSize).Append(") ")
            .Append("(Current Node Output: ").Append(nodeOutputSize).Append(") ")
            .Append("(Chain Node Output: ").Append(chainOutputSize).Append(')')
            .Append(routerMessage == null ? "" : " Router message: ")
            .Append(routerMessage == null ? "" : routerMessage)
            .Append(" ] ");
        if (level == 1)
        {
            stringBuilder.Append("Input: ")
                .Append(inputInvokers.Count<=0 ? "Empty" :
               string.Join(",", inputInvokers.Take(Math.Min(5, inputInvokers.Count))))
                        //inputInvokers.subList(0, 
                        //    .stream()
                        //    .map(Invoker::getUrl)
                        //    .map(URL::getAddress)
                        //    .collect(Collectors.joining(",")))
                .Append(" -> ");

            stringBuilder.Append("Chain Node Output: ")
                .Append(chainOutputInvokers.Count <= 0 ? "Empty" : string.Join(",", chainOutputInvokers.Take(Math.Min(5, inputInvokers.Count))));
                    //chainOutputInvokers.subList(0, Math.min(5, chainOutputInvokers.size()))
                    //    .stream()
                    //    .map(Invoker::getUrl)
                    //    .map(URL::getAddress)
                    //    .collect(Collectors.joining(",")));
        }
        else
        {
            stringBuilder.Append("Current Node Output: ")
                .Append(nodeOutputInvokers.Count <= 0 ? "Empty" : string.Join(",", nodeOutputInvokers.Take(Math.Min(5, inputInvokers.Count))));
            //nodeOutputInvokers.subList(0, Math.min(5, nodeOutputInvokers.size()))
            //            .stream()
            //            .map(Invoker::getUrl)
            //            .map(URL::getAddress)
            //            .collect(Collectors.joining(",")));
        }


        if (nodeOutputInvokers != null && nodeOutputInvokers.Count > 5)
        {
            stringBuilder.Append("...");
        }
        foreach (RouterSnapshotNode<T> node in nextNode)
        {
            stringBuilder.Append("\n");
            for (int i = 0; i < level; i++)
            {
                stringBuilder.Append("  ");
            }
            stringBuilder.Append(node.ToString(level + 1));
        }
        return stringBuilder.ToString();
    }
}
