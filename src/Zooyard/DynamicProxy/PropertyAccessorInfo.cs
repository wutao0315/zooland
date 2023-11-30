using System.Reflection;
using System.Reflection.Emit;

namespace Zooyard.DynamicProxy;

internal sealed class PropertyAccessorInfo(MethodInfo interfaceGetMethod, MethodInfo interfaceSetMethod)
{
    public MethodInfo InterfaceGetMethod { get; } = interfaceGetMethod;
    public MethodInfo InterfaceSetMethod { get; } = interfaceSetMethod;
    public MethodBuilder? GetMethodBuilder { get; set; }
    public MethodBuilder? SetMethodBuilder { get; set; }
}
