namespace Zooyard.Rpc.Route;

public class Constants
{
    public const string FAIL_BACK_TASKS_KEY = "failbacktasks";

    public const int DEFAULT_FAILBACK_TASKS = 100;

    public const int DEFAULT_FORKS = 2;

    public const string WEIGHT_KEY = "weight";

    public const int DEFAULT_WEIGHT = 100;

    public const string MOCK_PROTOCOL = "mock";

    public const string FORCE_KEY = "force";

    public const string RAW_RULE_KEY = "rawRule";

    public const string VALID_KEY = "valid";

    public const string ENABLED_KEY = "enabled";

    public const string DYNAMIC_KEY = "dynamic";

    public const string SCOPE_KEY = "scope";

    public const string KEY_KEY = "key";

    public const string CONDITIONS_KEY = "conditions";

    public const string TAGS_KEY = "tags";

    //To decide whether to exclude unavailable invoker from the cluster
    public const string CLUSTER_AVAILABLE_CHECK_KEY = "cluster.availablecheck";

    //The default value of cluster.availablecheck
    public const bool DEFAULT_CLUSTER_AVAILABLE_CHECK = true;

    //To decide whether to enable sticky strategy for cluster
    public const string CLUSTER_STICKY_KEY = "sticky";

    //The default value of sticky
    public const bool DEFAULT_CLUSTER_STICKY = false;

    public const string ADDRESS_KEY = "address";

    //When this attribute appears in invocation's attachment, mock invoker will be used
    public const string INVOCATION_NEED_MOCK = "invocation.need.mock";

    //when ROUTER_KEY's value is set to ROUTER_TYPE_CLEAR, RegistryDirectory will clean all current routers
    public const string ROUTER_TYPE_CLEAR = "clean";

    public const string DEFAULT_SCRIPT_TYPE_KEY = "javascript";

    public const string PRIORITY_KEY = "priority";

    public const string RULE_KEY = "rule";

    public const string TYPE_KEY = "type";

    public const string RUNTIME_KEY = "runtime";

    public const string WARMUP_KEY = "warmup";

    //int DEFAULT_WARMUP = 10 * 60 * 1000;

    public const string CONFIG_VERSION_KEY = "configVersion";

    public const string OVERRIDE_PROVIDERS_KEY = "providerAddresses";


    //key for router type, for e.g., "script"/"file",  corresponding to ScriptRouterFactory.NAME, FileRouterFactory.NAME
    public const string ROUTER_KEY = "router";

    //The key name for reference URL in register center
    public const string REFER_KEY = "refer";

    public const string ATTRIBUTE_KEY = "attribute";

    //The key name for export URL in register center
    public const string EXPORT_KEY = "export";

    public const string PEER_KEY = "peer";

    public const string CONSUMER_URL_KEY = "CONSUMER_URL";

    //prefix of arguments router key
    public const string ARGUMENTS = "arguments";

    public const string NEED_REEXPORT = "need-reexport";

    //The key of shortestResponseSlidePeriod
    public const string SHORTEST_RESPONSE_SLIDE_PERIOD = "shortestResponseSlidePeriod";

    public const string SHOULD_FAIL_FAST_KEY = "zooyard.router.should-fail-fast";

    public const string FORCE_USE_TAG = "force.use.tag";
}

public class CommonConstants 
{
    public const string METHOD_KEY = "mehod";
    public const string METHODS_KEY = "methods";
    public const string HOST_KEY = "host";
}
