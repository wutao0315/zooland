using System;
using System.Linq;
using System.Reflection;

namespace Zooyard.Core
{
    public interface IInvocation
    {
        string App { get; }
        string Version { get; }
        Type TargetType { get; }
        MethodInfo MethodInfo { get; }
        object[] Arguments { get; }
        Type[] ArgumentTypes { get; }
        string AppPoint();
        string PointVersion();
    }

    public class RpcInvocation : IInvocation
    {
        public RpcInvocation(string app, string version, Type targetType, MethodInfo methodInfo, object[] arguments)
        {
            App = app;
            Version = version;
            TargetType = targetType;
            MethodInfo = methodInfo;
            Arguments = arguments;
            ArgumentTypes = arguments == null ? null : (from item in arguments select item.GetType()).ToArray();
        }
        public string App { get; private set; }
        public string Version { get; private set; }
        public Type TargetType { get; private set; }
        public MethodInfo MethodInfo { get; private set; }
        public object[] Arguments { get; private set; }
        public Type[] ArgumentTypes { get; private set; }
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
}
