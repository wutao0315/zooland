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

namespace Zooyard.Core
{
    public class ZooyardFactory<T>
    {
        public ZooyardFactory(IZooyardPools pools, string app, string version) {
            _pools = pools;
            _app = app;
            _version = version;
        }
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

        public T CreateYard()
        {
            var interceptor = new ServiceInterceptor<T>(_pools, _app, _version);
            var result = InterfaceProxy.New<T>(interceptor);
            return result;
        }
    }
    internal class ServiceInterceptor<T> : IInterceptor
    {
        private readonly IZooyardPools _clientPools;
        private readonly string _app;
        private readonly string _version;
        public ServiceInterceptor(IZooyardPools clientPools, string app, string version)
        {
            _clientPools = clientPools;
            _app = app;
            _version = version;
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
            var icn = new RpcInvocation(_app, _version, target, methodInfo, args);
            
            var result = _clientPools.Invoke(icn).GetAwaiter().GetResult();
            return result.Value;
        }
    }

}
