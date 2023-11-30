using System.Reflection;

namespace Zooyard.DynamicProxy;

internal class ProxyMethodResolverContext(PackedArgs packed, MethodInfo method)
{
    public PackedArgs Packed { get; } = packed;
    public MethodInfo Method { get; } = method;
}
