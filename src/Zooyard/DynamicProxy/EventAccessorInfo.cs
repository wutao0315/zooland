using System.Reflection;
using System.Reflection.Emit;

namespace Zooyard.DynamicProxy;

internal sealed class EventAccessorInfo(MethodInfo interfaceAddMethod, MethodInfo interfaceRemoveMethod, MethodInfo interfaceRaiseMethod)
{
    public MethodInfo InterfaceAddMethod { get; } = interfaceAddMethod;
    public MethodInfo InterfaceRemoveMethod { get; } = interfaceRemoveMethod;
    public MethodInfo InterfaceRaiseMethod { get; } = interfaceRaiseMethod;
    public MethodBuilder? AddMethodBuilder { get; set; }
    public MethodBuilder? RemoveMethodBuilder { get; set; }
    public MethodBuilder? RaiseMethodBuilder { get; set; }
}
