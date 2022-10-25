using Zooyard.Utils;

namespace Zooyard.Rpc.Route.State;

public abstract class AbstractStateRouter: IStateRouter
{
    private volatile bool force = false;
    private volatile URL address;
    private volatile IStateRouter? nextRouter = null;

    //private final GovernanceRuleRepository ruleRepository;

    //Should continue route if current router's result is empty
    private readonly bool _shouldFailFast;

    public AbstractStateRouter(URL address)
    {
        this.address = address;
        _shouldFailFast = address.GetParameter(Constants.SHOULD_FAIL_FAST_KEY, true);

        //ModuleModel moduleModel = url.GetOrDefaultModuleModel();
        //this.ruleRepository = moduleModel.getExtensionLoader(GovernanceRuleRepository.class).getDefaultExtension();
        //_shouldFailFast = Boolean.parseBoolean(ConfigurationUtils.getProperty(moduleModel, Constants.SHOULD_FAIL_FAST_KEY, "true"));
    }
    public virtual URL Address { get; set; }

    public virtual bool Runtime => true;
    public virtual bool Force { get => force; set => force = value; }


    //public GovernanceRuleRepository getRuleRepository()
    //{
    //    return this.ruleRepository;
    //}

    public IStateRouter? NextRouter { get=> nextRouter; set=> nextRouter = value; }

    //public void Notify(BitList<IInvoker> invokers)
    //{
    //    // default empty implement
    //}


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
    public IList<URL> Route(IList<URL> invokers, URL address, IInvocation invocation, bool needToPrintMessage) 
    {
        //if (needToPrintMessage && (nodeHolder == null || nodeHolder.Value == null))
        //{
        //    needToPrintMessage = false;
        //}

        //RouterSnapshotNode? currentNode = null;
        //RouterSnapshotNode? parentNode = null;
        //Holder<string>? messageHolder = null;

        //// pre-build current node
        //if (needToPrintMessage)
        //{
        //    parentNode = nodeHolder.Value;
        //    currentNode = new RouterSnapshotNode(this.GetType().Name, invokers);//.Clone());
        //    parentNode.appendNode(currentNode);

        //    // set parent node's output size in the first child invoke
        //    // initial node output size is zero, first child will override it
        //    if (parentNode.getNodeOutputSize() < invokers.Count)
        //    {
        //        parentNode.setNodeOutputInvokers(invokers);//.clone());
        //    }

        //    messageHolder = new Holder<string>();
        //    nodeHolder.Value = currentNode;
        //}
        IList<URL> routeResult;

        // check if router support call continue route by itself
        if (!SupportContinueRoute())
        {
            routeResult = DoRoute(invokers, address, invocation, needToPrintMessage);//, nodeHolder, messageHolder);
            var shouldFailFast = address.GetParameter(Constants.SHOULD_FAIL_FAST_KEY, true);//  Boolean.parseBoolean(ConfigurationUtils.getProperty(moduleModel, Constants.SHOULD_FAIL_FAST_KEY, "true"));
            // use current node's result as next node's parameter
            if (!shouldFailFast || routeResult.Count>0)
            {
                routeResult = ContinueRoute(routeResult, address, invocation, needToPrintMessage); //, nodeHolder);
            }
        }
        else
        {
            routeResult = DoRoute(invokers, address, invocation, needToPrintMessage);
        }

        //// post-build current node
        //if (needToPrintMessage)
        //{
        //    currentNode.setRouterMessage(messageHolder.Value);
        //    if (currentNode.getNodeOutputSize() == 0)
        //    {
        //        // no child call
        //        currentNode.setNodeOutputInvokers(routeResult);//.clone());
        //    }
        //    currentNode.setChainOutputInvokers(routeResult);//.clone());
        //    //set
        //    nodeHolder.Value = parentNode;
        //}
        return routeResult;
    }

    /// <summary>
    /// Filter invokers with current routing rule and only return the invokers that comply with the rule.
    /// </summary>
    /// <param name="invokers">all invokers to be routed</param>
    /// <param name="address">consumerUrl</param>
    /// <param name="invocation">invocation</param>
    /// <param name="needToPrintMessage">should current router print message</param>
    /// <returns></returns>
    protected abstract IList<URL> DoRoute(IList<URL> invokers, URL address, IInvocation invocation,
                                                bool needToPrintMessage);


    /// <summary>
    /// Call next router to get result
    /// </summary>
    /// <typeparam name="Invoker">current router filtered invokers</typeparam>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <returns></returns>
    protected IList<URL> ContinueRoute(IList<URL> invokers, URL address, IInvocation invocation,
                                                      bool needToPrintMessage)
    {
        if (nextRouter != null)
        {
            return nextRouter.Route(invokers, address, invocation, needToPrintMessage);
        }
        else
        {
            return invokers;
        }
    }

    ////public string BuildSnapshot()
    ////{
    ////    return DoBuildSnapshot() +
    ////        "            ↓ \n" +
    ////        nextRouter.BuildSnapshot();
    ////}

    //protected String DoBuildSnapshot()
    //{
    //    return this.GetType().Name+ " not support\n";
    //}
}
