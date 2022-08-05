using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Mock;

public class MockInvokersSelector<T> : AbstractStateRouter<T>
{
    public const string NAME = "MOCK_ROUTER";

    private volatile BitList<IInvoker> normalInvokers = new();
    private volatile BitList<IInvoker> mockedInvokers = new();

    public MockInvokersSelector(URL url) : base(url)
    {
    }

    protected override BitList<IInvoker> DoRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation,
                                          bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder,
                                          Holder<String> messageHolder)
    {
        if (invokers.Count == 0)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "Empty invokers. Directly return.";
            }
            return invokers;
        }

        if (invocation.Arguments == null)
        {
            if (needToPrintMessage)
            {
                messageHolder.Value = "ObjectAttachments from invocation are null. Return normal Invokers.";
            }
            return invokers.and(normalInvokers);
        }
        else
        {
            //string value = (string)invocation.GetObjectAttachmentWithoutConvert(Constants.INVOCATION_NEED_MOCK);
            string value = (string)invocation.getAttachment(Constants.INVOCATION_NEED_MOCK);
            if (value == null)
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = "invocation.need.mock not set. Return normal Invokers.";
                }
                return invokers.and(normalInvokers);
            }
            else if (bool.TrueString.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = "invocation.need.mock is true. Return mocked Invokers.";
                }
                return invokers.and(mockedInvokers);
            }
        }
        if (needToPrintMessage)
        {
            messageHolder.Value = "Directly Return. Reason: invocation.need.mock is set but not match true";
        }
        return invokers;
    }

    public void Notify(BitList<IInvoker> invokers)
    {
        cacheMockedInvokers(invokers);
        cacheNormalInvokers(invokers);
    }

    private void cacheMockedInvokers(BitList<IInvoker> invokers)
    {
        BitList<IInvoker> clonedInvokers = invokers;//.clone();
        //clonedInvokers.removeIf((invoker) => !invoker.getUrl().getProtocol().equals(Constants.MOCK_PROTOCOL));
        mockedInvokers = clonedInvokers;
    }

    //@SuppressWarnings("rawtypes")
    private void cacheNormalInvokers(BitList<IInvoker> invokers)
    {
        BitList<IInvoker> clonedInvokers = invokers;//.clone();
        //clonedInvokers.removeIf((invoker)->invoker.getUrl().getProtocol().Equals(Constants.MOCK_PROTOCOL));
        normalInvokers = clonedInvokers;
    }

    protected string doBuildSnapshot()
    {
        Dictionary<String, BitList<IInvoker>> grouping = new();
        grouping.Add("Mocked", mockedInvokers);
        grouping.Add("Normal", normalInvokers);
        return new RouterGroupingState(this.GetType().Name, mockedInvokers.Count + normalInvokers.Count, grouping).ToString();
    }
}
