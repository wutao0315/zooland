using System.Reflection;

namespace Zooyard;

public interface IInvocation
{
    string ServiceName { get; }
    string Version { get; }
    string Url { get; }
    Type TargetType { get; }
    MethodInfo MethodInfo { get; }
    object[] Arguments { get; }
    Type[] ArgumentTypes { get; }
    string ServiceNamePoint();
    string PointVersion();
    string GetAttachment(string key, string defaultValue=default!) {
        return "";
    }
}

public class RpcInvocation : IInvocation
{
    public RpcInvocation(string serviceName, string version, string url, Type targetType, MethodInfo methodInfo, object[]? arguments)
    {
        ServiceName = serviceName;
        Version = version;
        Url = url;
        TargetType = targetType;
        MethodInfo = methodInfo;
        Arguments = arguments ?? Array.Empty<object>();
        ArgumentTypes = arguments == null ? Array.Empty<Type>() : (from item in arguments select item.GetType()).ToArray();
    }
    public string ServiceName { get; }
    public string Version { get;}
    public string Url { get; }
    public Type TargetType { get; }
    public MethodInfo MethodInfo { get; }
    public object[] Arguments { get; }
    public Type[] ArgumentTypes { get; }
    public string ServiceNamePoint()
    {
        var result = string.IsNullOrWhiteSpace(ServiceName) ? "" : $"{ServiceName}.";
        return result;
    }
    public string PointVersion()
    {
        var result = string.IsNullOrWhiteSpace(Version) ? "" : $".{Version}";
        return result;
    }
}
