using System.Reflection;

namespace Zooyard.Core.DynamicProxy
{
    public interface IInterceptor
    {
        //void Intercept(IProxyInvocation invocation);
        object Intercept(object obj,string methodName, params object[] args);
        //object Intercept(object obj, int rid, string name, params object[] args);
    }
}
