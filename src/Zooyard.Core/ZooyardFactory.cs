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

namespace Zooyard.Core
{
    //public class ZooyardFactory<T> where T : class
    //{

    //    public IZooyardPools ThePools { get; set; }

    //    public T CreateYard()
    //    {
    //        var generator = new Castle.DynamicProxy.ProxyGenerator();
    //        var interceptor = new ServiceInterceptor<T>(ThePools);
    //        var result = generator.CreateInterfaceProxyWithoutTarget<T>(interceptor);

    //        return result;
    //    }
    //}
    //internal class ServiceInterceptor<T> : Castle.DynamicProxy.IInterceptor where T : class
    //{
    //    private IZooyardPools clientPools { get; set; }
    //    public ServiceInterceptor(IZooyardPools pools)
    //    {
    //        this.clientPools = pools;
    //    }
    //    /// <summary>
    //    // 根据负载均衡算法获取相应的URL
    //    // 重试机制
    //    // 1、直接重试当前链接，如果重试n次失败，切换链接再重试n次链接;如此循环将所有链接尝试一遍，如果失败抛出异常
    //    // 2、当前链接失败，直接重试其他连接，重试所有链接n便之后，如果失败抛出异常
    //    // 3、当前链接失败，直接抛出异常
    //    /// </summary>
    //    /// <param name="invocation"></param>
    //    public void Intercept(Castle.DynamicProxy.IInvocation invocation)
    //    {
    //        //调用上下文
    //        var icn = new RpcInvocation(typeof(T), invocation.Method, invocation.Arguments);

    //        var result = clientPools.Invoke(icn);
    //        invocation.ReturnValue = result.Value;

    //    }
    //}

    public class ZooyardFactory<T> where T : class
    {
        /// <summary>
        /// remoting service pools
        /// </summary>
        public IZooyardPools ThePools { get; set; }
        /// <summary>
        /// application name
        /// </summary>
        public string App { get; set; }
        /// <summary>
        /// api version
        /// </summary>
        public string Version { get; set; }

        public T CreateYard()
        {
            var interceptor = new ServiceInterceptor<T>(ThePools, App, Version);
            var result = InterfaceProxy.New<T>(interceptor);
            return result;
        }
    }
    internal class ServiceInterceptor<T> : IInterceptor where T : class
    {
        private IZooyardPools clientPools { get; set; }
        private string app { get; set; }
        private string version { get; set; }
        public ServiceInterceptor(IZooyardPools clientPools, string app, string version)
        {
            this.clientPools = clientPools;
            this.app = app;
            this.version = version;
        }
        /// <summary>
        // 根据负载均衡算法获取相应的URL
        // 重试机制
        // 1、直接重试当前链接，如果重试n次失败，切换链接再重试n次链接;如此循环将所有链接尝试一遍，如果失败抛出异常
        // 2、当前链接失败，直接重试其他连接，重试所有链接n便之后，如果失败抛出异常
        // 3、当前链接失败，直接抛出异常
        /// </summary>
        /// <param name="invocation"></param>
        public object Intercept(object obj, string methodName, params object[] args)
        {
            var target = typeof(T);
            var argTypes = (from arg in args select arg.GetType())?.ToArray()??new Type[] { };
            var methodInfo = target.GetMethod(methodName, argTypes);
            //调用上下文
            var icn = new RpcInvocation(app, version, target, methodInfo, args);

            var result = clientPools.Invoke(icn);
            return result.Value;
        }

        //public object Intercept(object obj, int rid, string name, params object[] args)
        //{
        //    //调用上下文
        //    //var icn = new RpcInvocation(typeof(T), method, args);

        //    //var result = clientPools.Invoke(icn);
        //    //return result.Value;
        //    return null;
        //}
    }

}
