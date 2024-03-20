using System.Reflection;

namespace Zooyard.DynamicProxy;

public record ProxyMethodResolverContext(PackedArgs packed, MethodInfo method)
{
    public PackedArgs Packed { get; } = packed;
    public MethodInfo Method { get; } = method;
}
