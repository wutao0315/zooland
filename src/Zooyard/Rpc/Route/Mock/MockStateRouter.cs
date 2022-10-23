using System.Collections.Generic;
using Zooyard.Rpc.Route.State;
using Zooyard.Utils;

namespace Zooyard.Rpc.Route.Mock;

public class MockStateRouter : AbstractStateRouter
{
    public const string NAME = "MOCK_ROUTER";

    private volatile IList<URL> normalInvokers = new List<URL>();
    private volatile IList<URL> mockedInvokers = new List<URL>();

    public MockStateRouter(URL address) : base(address)
    {
    }

    protected override IList<URL> DoRoute(IList<URL> invokers, URL url, IInvocation invocation,
                                          bool needToPrintMessage)//, Holder<RouterSnapshotNode> nodeHolder,Holder<String> messageHolder)
    {
        if (invokers.Count == 0)
        {
            if (needToPrintMessage)
            {
                //messageHolder.Value = "Empty invokers. Directly return.";
            }
            return invokers;
        }

        if (invocation.Arguments == null)
        {
            if (needToPrintMessage)
            {
                //messageHolder.Value = "ObjectAttachments from invocation are null. Return normal Invokers.";
            }
            return invokers.Intersect(normalInvokers).ToList();
            //交集
            //return invokers.and(normalInvokers);
        }
        else
        {
            //string value = (string)invocation.GetObjectAttachmentWithoutConvert(Constants.INVOCATION_NEED_MOCK);
            string value = (string)invocation.GetAttachment(Constants.INVOCATION_NEED_MOCK);
            if (value == null)
            {
                if (needToPrintMessage)
                {
                    //messageHolder.Value = "invocation.need.mock not set. Return normal Invokers.";
                }
                return invokers.Intersect(normalInvokers).ToList();
                //return invokers.and(normalInvokers);
            }
            else if (bool.TrueString.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                if (needToPrintMessage)
                {
                    //messageHolder.Value = "invocation.need.mock is true. Return mocked Invokers.";
                }
                return invokers.Intersect(mockedInvokers).ToList();
                //return invokers.and(mockedInvokers);
            }
        }
        if (needToPrintMessage)
        {
            //messageHolder.Value = "Directly Return. Reason: invocation.need.mock is set but not match true";
        }
        return invokers;
    }

    //public void Notify(BitList<IInvoker> invokers)
    //{
    //    cacheMockedInvokers(invokers);
    //    cacheNormalInvokers(invokers);
    //}

    private void cacheMockedInvokers(IList<URL> invokers)
    {
        IList<URL> clonedInvokers = invokers;//.clone();
        //clonedInvokers.removeIf((invoker) => !invoker.getUrl().getProtocol().equals(Constants.MOCK_PROTOCOL));
        mockedInvokers = clonedInvokers;
    }

    //@SuppressWarnings("rawtypes")
    private void cacheNormalInvokers(IList<URL> invokers)
    {
        IList<URL> clonedInvokers = invokers;//.clone();
        //clonedInvokers.removeIf((invoker)->invoker.getUrl().getProtocol().Equals(Constants.MOCK_PROTOCOL));
        normalInvokers = clonedInvokers;
    }

    protected string doBuildSnapshot()
    {
        Dictionary<string, IList<URL>> grouping = new();
        grouping.Add("Mocked", mockedInvokers);
        grouping.Add("Normal", normalInvokers);
        return new RouterGroupingState(this.GetType().Name, mockedInvokers.Count + normalInvokers.Count, grouping).ToString();
    }
}
