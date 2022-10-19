using System.Reflection;

namespace Zooyard;

public interface IInvocation
{
    string App { get; }
    string Version { get; }
    Type TargetType { get; }
    MethodInfo MethodInfo { get; }
    object[]? Arguments { get; }
    Type[]? ArgumentTypes { get; }
    string AppPoint();
    string PointVersion();
    string getAttachment(string key, string defaultValue=default!) {
        return "";
    }
}

public class RpcInvocation : IInvocation
{
    public RpcInvocation(string app, string version, Type targetType, MethodInfo methodInfo, object[]? arguments)
    {
        App = app;
        Version = version;
        TargetType = targetType;
        MethodInfo = methodInfo;
        Arguments = arguments;
        ArgumentTypes = arguments == null ? null : (from item in arguments select item.GetType()).ToArray();
    }
    public string App { get; }
    public string Version { get;}
    public Type TargetType { get; }
    public MethodInfo MethodInfo { get; }
    public object[]? Arguments { get; }
    public Type[]? ArgumentTypes { get; }
    public string AppPoint()
    {
        var result = string.IsNullOrWhiteSpace(App) ? "" : $"{App}.";
        return result;
    }
    public string PointVersion()
    {
        var result = string.IsNullOrWhiteSpace(Version) ? "" : $".{Version}";
        return result;
    }
}
