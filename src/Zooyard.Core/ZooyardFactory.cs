using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reflection;
using Zooyard.Core;
using Zooyard.Core.Utils;
using Zooyard.Core.DynamicProxy;
using System.Diagnostics;
using Zooyard.Core.Diagnositcs;
using System.Reflection.Emit;

namespace Zooyard.Core
{
    public class ZooyardFactory<T>
    {
        /// <summary>
        /// remoting service pools
        /// </summary>
        private readonly IZooyardPools _pools;
        /// <summary>
        /// application name
        /// </summary>
        private readonly string _app;
        /// <summary>
        /// api version
        /// </summary>
        private readonly string _version;

        public ZooyardFactory(IZooyardPools pools, string app, string version) 
        {
            _pools = pools;
            _app = app;
            _version = version;
        }

        public T CreateYard()
        {
            var proxyGenerator = new AsyncProxyGenerator(_pools, _app, _version);
            return (T)proxyGenerator.CreateProxy(typeof(T));
        }

        //public T CreateYard()
        //{
        //    var interceptor = new ServiceInterceptor<T>(_pools, _app, _version);
        //    var result = InterfaceProxy.New<T>(interceptor);
        //    return result;
        //}
    }
    //internal class ServiceInterceptor<T> : IInterceptor
    //{
    //    private readonly IZooyardPools _clientPools;
    //    private readonly string _app;
    //    private readonly string _version;
    //    public ServiceInterceptor(IZooyardPools clientPools, string app, string version)
    //    {
    //        _clientPools = clientPools;
    //        _app = app;
    //        _version = version;
    //    }
    //    /// <summary>
    //    // 根据负载均衡算法获取相应的URL
    //    // 重试机制
    //    // 1、直接重试当前链接，如果重试n次失败，切换链接再重试n次链接;如此循环将所有链接尝试一遍，如果失败抛出异常
    //    // 2、当前链接失败，直接重试其他连接，重试所有链接n便之后，如果失败抛出异常
    //    // 3、当前链接失败，直接抛出异常
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    /// <param name="methodName"></param>
    //    /// <param name="args"></param>
    //    public object Intercept(object obj, string methodName, params object[] args)
    //    {
    //        var target = typeof(T);
    //        var argTypes = (from arg in args select arg.GetType())?.ToArray()??new Type[] { };
    //        var methodInfo = target.GetMethod(methodName, argTypes);

    //        //if (methodInfo.ReturnType == typeof(Task) ||
    //        //    (methodInfo.ReturnType.IsGenericType &&
    //        //     methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))) 
    //        //{
    //        //    Console.WriteLine("Asynchronous method found...");
    //        //}

    //        //调用上下文
    //        var icn = new RpcInvocation(_app, _version, target, methodInfo, args);

    //        //return InterceptAsync(icn);
    //        var result = _clientPools.Invoke(icn).GetAwaiter().GetResult();

    //        //if (methodInfo.ReturnType == typeof(Task) ||
    //        //    (methodInfo.ReturnType.IsGenericType &&
    //        //     methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
    //        //{
    //        //    Console.WriteLine("Asynchronous method found...");
    //        //    return Task.FromResult(result.Value);
    //        //}

    //        return result.Value;
    //        //var result = await _clientPools.Invoke(icn);
    //        //return Task.FromResult(result.Value);
    //    }

    //    //private async void InterceptAsync(IInvocation invocation)
    //    //{
    //    //    //invocation.MethodInfo.ReturnType
    //    //    var result = await _clientPools.Invoke(invocation);
    //    //    return Task.FromResult(result.Value.ChangeType(invocation.MethodInfo.ReturnType));
    //    //}
    //}

}
