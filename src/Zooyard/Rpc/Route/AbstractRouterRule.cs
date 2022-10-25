namespace Zooyard.Rpc.Route;

public class AbstractRouterRule
{
    protected void ParseFromMapInner(Dictionary<string, object> map)
    {
        if (map.TryGetValue(Constants.RAW_RULE_KEY, out object? rawRuleObj))
        {
            RawRule = rawRuleObj.ToString()!;
        }

        if (map.TryGetValue(Constants.RUNTIME_KEY,out object? runtimeObj) 
            && bool.TryParse(runtimeObj.ToString(),out bool runtime))
        {
            Runtime = runtime;
        }

        if (map.TryGetValue(Constants.FORCE_KEY, out object? forceObj)
            && bool.TryParse(forceObj.ToString(), out bool force))
        {
            Force = force;
        }

        if (map.TryGetValue(Constants.VALID_KEY, out object? validObj)
            && bool.TryParse(validObj.ToString(), out bool valid))
        {
            Valid = valid;
        }

        if (map.TryGetValue(Constants.ENABLED_KEY, out object? enabledObj)
           && bool.TryParse(enabledObj.ToString(), out bool enabled))
        {
            Enabled = enabled;
        }

        if (map.TryGetValue(Constants.PRIORITY_KEY, out object? priorityObj)
            && int.TryParse(priorityObj.ToString(), out int priority))
        {
            Priority = priority;
        }

        if (map.TryGetValue(Constants.ENABLED_KEY, out object? dynamicObj)
           && bool.TryParse(dynamicObj.ToString(), out bool dynamic))
        {
            Dynamic = dynamic;
        }

        if (map.TryGetValue(Constants.SCOPE_KEY, out object? scopeObj))
        {
            Scope = scopeObj.ToString()!;
        }
        if (map.TryGetValue(Constants.KEY_KEY, out object? keyObj))
        {
            Key = keyObj.ToString()!;
        }
    }

    public string RawRule { get; set; } = string.Empty;
    public bool Runtime { get; set; } = true;
    public bool Force { get; set; } = false;
    public bool Valid { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    public bool Dynamic { get; set; } = false;
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}
