namespace Zooyard.Rpc.Route;

public abstract class AbstractRouter : IRoute
{
    //public AbstractRouter()
    //{
    //}
    //private GovernanceRuleRepository ruleRepository;
    public AbstractRouter(URL url)
    {
        //this.ruleRepository = url.getOrDefaultModuleModel().getExtensionLoader(GovernanceRuleRepository.class).getDefaultExtension();
        this.Url = url;
    }
    //public GovernanceRuleRepository getRuleRepository()
    //{
    //    return this.ruleRepository;
    //}
    public virtual URL Url { get; set; } 
    public virtual bool Runtime => true;
    public virtual bool Force{ get; set; } = false;
    public virtual int Priority { get; set; } = int.MaxValue;

  
    public int CompareTo(IRoute? other)
    {
        if (other == null)
        {
            throw new ArgumentException();
        }
        return this.Priority.CompareTo(other.Priority);
    }
}
