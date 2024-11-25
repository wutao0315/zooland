using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Mock;

public class MockStateRouter : AbstractStateRouter
{
    public const string NAME = "MOCK_ROUTER";

    private volatile List<URL> normalInvokers = new();
    private volatile List<URL> mockedInvokers = new();

    public MockStateRouter(URL address) : base(address) { }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode> nodeHolder, Holder<String> messageHolder)
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
            return invokers.Intersect(normalInvokers).ToList();
            //交集
            //return invokers.and(normalInvokers);
        }
        else
        {
            //string value = (string)invocation.GetObjectAttachmentWithoutConvert(Constants.INVOCATION_NEED_MOCK);
            string? value = invocation.GetAttachment(Constants.INVOCATION_NEED_MOCK);
            if (value == null)
            {
                if (needToPrintMessage)
                {
                   messageHolder.Value = "invocation.need.mock not set. Return normal Invokers.";
                }
                return invokers.Intersect(normalInvokers).ToList();
                //return invokers.and(normalInvokers);
            }
            else if (bool.TrueString.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                if (needToPrintMessage)
                {
                    messageHolder.Value = "invocation.need.mock is true. Return mocked Invokers.";
                }
                return invokers.Intersect(mockedInvokers).ToList();
                //return invokers.and(mockedInvokers);
            }
        }
        if (needToPrintMessage)
        {
            messageHolder.Value = "Directly Return. Reason: invocation.need.mock is set but not match true";
        }
        return invokers;
    }

    //public void Notify(BitList<IInvoker> invokers)
    //{
    //    cacheMockedInvokers(invokers);
    //    cacheNormalInvokers(invokers);
    //}

    private void CacheMockedInvokers(IList<URL> invokers)
    {
        var clonedInvokers = invokers.Where(w=>!w.Protocol.Equals(Constants.MOCK_PROTOCOL, StringComparison.OrdinalIgnoreCase)).ToList();
        mockedInvokers = clonedInvokers;
    }

    private void CacheNormalInvokers(IList<URL> invokers)
    {
        var clonedInvokers = invokers.Where(w => w.Protocol.Equals(Constants.MOCK_PROTOCOL, StringComparison.OrdinalIgnoreCase)).ToList();
        normalInvokers = clonedInvokers;
    }

    protected string DoBuildSnapshot()
    {
        var  grouping = new Dictionary<string, IList<URL>>
        {
            { "Mocked", mockedInvokers },
            { "Normal", normalInvokers }
        };
        return new RouterGroupingState(this.GetType().Name, mockedInvokers.Count + normalInvokers.Count, grouping).ToString();
    }
}
