using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State;

public abstract class AbstractStateRouter<T>: IStateRouter<T>
{
    private volatile bool force = false;
    private volatile URL url;
    private volatile IStateRouter<T>? nextRouter = null;

    //    private final GovernanceRuleRepository ruleRepository;

    //Should continue route if current router's result is empty
    private readonly bool shouldFailFast;

    public AbstractStateRouter(URL url)
    {
        //ModuleModel moduleModel = url.GetOrDefaultModuleModel();
        //this.ruleRepository = moduleModel.getExtensionLoader(GovernanceRuleRepository.class).getDefaultExtension();
        this.url = url;
        //this.shouldFailFast = Boolean.parseBoolean(ConfigurationUtils.getProperty(moduleModel, Constants.SHOULD_FAIL_FAST_KEY, "true"));
    }

    public virtual URL Url { get; set; }
    public virtual bool Runtime => true;
    public virtual bool Force { get; set; } = false;


    //public GovernanceRuleRepository getRuleRepository()
    //{
    //    return this.ruleRepository;
    //}

    public IStateRouter<T> getNextRouter()
    {
        return nextRouter;
    }
    /// <summary>
    /// Next Router node state is maintained by AbstractStateRouter and this method is not allow to override.
    /// If a specified router wants to control the behaviour of continue route or not,
    /// please override {@link AbstractStateRouter#supportContinueRoute()}
    /// </summary>
    /// <param name="nextRouter"></param>
    public void SetNextRouter(IStateRouter<T> nextRouter)
    {
        this.nextRouter = nextRouter;
    }

    public void Notify(BitList<IInvoker> invokers)
    {
        // default empty implement
    }
    
    /// <summary>
    /// Whether current router's implementation support call
    /// {@link AbstractStateRouter#continueRoute(BitList, URL, Invocation, boolean, Holder)}
    /// by router itself.
    /// </summary>
    /// <returns></returns>
    protected bool SupportContinueRoute()
    {
        return false;
    }
    public BitList<IInvoker> Route(BitList<IInvoker> invokers, URL url, IInvocation invocation, bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder) 
    {
        if (needToPrintMessage && (nodeHolder == null || nodeHolder.Value == null))
        {
            needToPrintMessage = false;
        }

        RouterSnapshotNode<T>? currentNode = null;
        RouterSnapshotNode<T>? parentNode = null;
        Holder<string>? messageHolder = null;

        // pre-build current node
        if (needToPrintMessage)
        {
            parentNode = nodeHolder.Value;
            currentNode = new RouterSnapshotNode<T>(this.GetType().Name, invokers);//.Clone());
            parentNode.appendNode(currentNode);

            // set parent node's output size in the first child invoke
            // initial node output size is zero, first child will override it
            if (parentNode.getNodeOutputSize() < invokers.Count)
            {
                parentNode.setNodeOutputInvokers(invokers);//.clone());
            }

            messageHolder = new Holder<string>();
            nodeHolder.Value = currentNode;
        }
        BitList<IInvoker> routeResult;

        // check if router support call continue route by itself
        if (!SupportContinueRoute())
        {
            routeResult = DoRoute(invokers, url, invocation, needToPrintMessage, nodeHolder, messageHolder);
            // use current node's result as next node's parameter
            if (!shouldFailFast || routeResult.Count>0)
            {
                routeResult = continueRoute(routeResult, url, invocation, needToPrintMessage, nodeHolder);
            }
        }
        else
        {
            routeResult = DoRoute(invokers, url, invocation, needToPrintMessage, nodeHolder, messageHolder);
        }

        // post-build current node
        if (needToPrintMessage)
        {
            currentNode.setRouterMessage(messageHolder.Value);
            if (currentNode.getNodeOutputSize() == 0)
            {
                // no child call
                currentNode.setNodeOutputInvokers(routeResult);//.clone());
            }
            currentNode.setChainOutputInvokers(routeResult);//.clone());
            //set
            nodeHolder.Value = parentNode;
        }
        return routeResult;
    }

    /// <summary>
    /// Filter invokers with current routing rule and only return the invokers that comply with the rule.
    /// </summary>
    /// <param name="invokers">all invokers to be routed</param>
    /// <param name="url">consumerUrl</param>
    /// <param name="invocation">invocation</param>
    /// <param name="needToPrintMessage">should current router print message</param>
    /// <param name="nodeHolder">RouterSnapshotNode In general, router itself no need to care this param, just pass to continueRoute</param>
    /// <param name="messageHolder">message holder when router should current router print message</param>
    /// <returns></returns>
    protected abstract BitList<IInvoker> DoRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation,
                                                bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder,
                                                Holder<string> messageHolder);


    /// <summary>
    /// Call next router to get result
    /// </summary>
    /// <typeparam name="Invoker">current router filtered invokers</typeparam>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <returns></returns>
    protected BitList<IInvoker> continueRoute(BitList<IInvoker> invokers, URL url, IInvocation invocation,
                                                      bool needToPrintMessage, Holder<RouterSnapshotNode<T>> nodeHolder) {
        if (nextRouter != null)
        {
            return nextRouter.Route(invokers, url, invocation, needToPrintMessage, nodeHolder);
        }
        else
        {
            return invokers;
        }
    }

    public string BuildSnapshot()
    {
        return DoBuildSnapshot() +
            "            ↓ \n" +
            nextRouter.BuildSnapshot();
    }

    protected String DoBuildSnapshot()
    {
        return this.GetType().Name+ " not support\n";
    }
}
