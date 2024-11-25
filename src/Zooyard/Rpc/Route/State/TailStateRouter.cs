using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State;

public class TailStateRouter : IStateRouter
{
    private static readonly TailStateRouter INSTANCE = new ();
    private TailStateRouter() { }

    public static TailStateRouter GetInstance()
    {
        return INSTANCE;
    }

    public void SetNextRouter(IStateRouter nextRouter)
    {

    }

    //public URL Url => null;

    public IList<URL> Route(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode>? nodeHolder)
    {
        return invokers;
    }

    public void Dispose()
    {
    }

    public bool Runtime => false;

    public bool Force => false;

    //public void Notify(IList<URL> invokers)
    //{

    //}

    //public string BuildSnapshot()
    //{
    //    return "TailStateRouter End";
    //}
}
