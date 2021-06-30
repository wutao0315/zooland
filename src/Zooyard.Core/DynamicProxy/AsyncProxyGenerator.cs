using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Zooyard.Core.DynamicProxy
{
    public class AsyncProxyGenerator : IDisposable
    {
        private readonly ConcurrentDictionary<Type, Dictionary<Type, Type>> _proxyTypeCaches;

        private readonly ProxyAssembly _proxyAssembly;

        //private readonly MethodInfo _dispatchProxyInvokeMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("Invoke");
        //private readonly MethodInfo _dispatchProxyInvokeAsyncMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsync");
        //private readonly MethodInfo _dispatchProxyInvokeAsyncTMethod = typeof(ProxyExecutor).GetTypeInfo().GetDeclaredMethod("InvokeAsyncT");


        private readonly IZooyardPools _zooyardPools;
        private readonly string _app;
        private readonly string _version;

        public AsyncProxyGenerator(IZooyardPools zooyardPools, string app, string version)
        {
            _proxyTypeCaches = new ConcurrentDictionary<Type, Dictionary<Type, Type>>();
            _proxyAssembly = new ProxyAssembly();
            _zooyardPools = zooyardPools;
            _app = app;
            _version = version;
        }
        /// <summary> 创建代理 </summary>
        /// <param name="interfaceType"></param>
        /// <param name="proxyProvider"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public object CreateProxy(Type interfaceType)
        {
            var proxiedType = GetProxyType(typeof(ProxyExecutor), interfaceType);
            return Activator.CreateInstance(proxiedType, new ProxyHandler(this));
        }

        /// <summary> 创建代理 </summary>
        /// <param name="interfaceType"></param>
        /// <param name="baseType"></param>
        /// <param name="proxyProvider"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public object CreateProxy(Type interfaceType, Type baseType, IZooyardPools zooyardPools, string app, string version)
        {
            var proxiedType = GetProxyType(baseType, interfaceType);
            return Activator.CreateInstance(proxiedType, new ProxyHandler(this));
        }

        /// <summary> 获取代理类型 </summary>
        /// <param name="baseType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private Type GetProxyType(Type baseType, Type interfaceType)
        {
            if (!_proxyTypeCaches.TryGetValue(baseType, out var interfaceToProxy))
            {
                interfaceToProxy = new Dictionary<Type, Type>();
                _proxyTypeCaches[baseType] = interfaceToProxy;
            }

            if (!interfaceToProxy.TryGetValue(interfaceType, out var generatedProxy))
            {
                generatedProxy = GenerateProxyType(baseType, interfaceType);
                interfaceToProxy[interfaceType] = generatedProxy;
            }

            return generatedProxy;
        }

        /// <summary> 生成代理类型 </summary>
        /// <param name="baseType"></param>
        /// <param name="interfaceType"></param>
        /// <returns></returns>
        private Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            if (!interfaceType.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException($"InterfaceType_Must_Be_Interface, {interfaceType.FullName}", nameof(interfaceType));
            }

            if (baseTypeInfo.IsSealed)
            {
                throw new ArgumentException($"BaseType_Cannot_Be_Sealed, {baseTypeInfo.FullName}", nameof(baseType));
            }

            if (baseTypeInfo.IsAbstract)
            {
                throw new ArgumentException($"BaseType_Cannot_Be_Abstract {baseType.FullName}", nameof(baseType));
            }

            var pb = _proxyAssembly.CreateProxy($"Zooyard.Proxy.{interfaceType.Namespace}_{interfaceType.Name}", baseType);

            foreach (var t in interfaceType.GetTypeInfo().ImplementedInterfaces)
                pb.AddInterfaceImpl(t);

            pb.AddInterfaceImpl(interfaceType);

            var generatedProxyType = pb.CreateType();
            return generatedProxyType;
        }

        private ProxyMethodResolverContext Resolve(object[] args)
        {
            var packed = new PackedArgs(args);
            var method = _proxyAssembly.ResolveMethodToken(packed.DeclaringType, packed.MethodToken);
            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(packed.GenericTypes);

            return new ProxyMethodResolverContext(packed, method);
        }

        public object Invoke(object[] args)
        {
            var context = Resolve(args);

            // Call (protected method) DispatchProxyAsync.Invoke()
            object returnValue = null;
            try
            {
                //Debug.Assert(_dispatchProxyInvokeMethod != null);
                //returnValue = _dispatchProxyInvokeMethod.Invoke(context.Packed.DispatchProxy,
                //    new object[] { context.Method, context.Packed.Args });
                var icn = new RpcInvocation(_app, _version, context.Packed.DeclaringType, context.Method, context.Packed.Args);
                var result = _zooyardPools.Invoke<object>(icn).GetAwaiter().GetResult();
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }

            return returnValue;
        }
        public T Invoke<T>(object[] args)
        {
            var context = Resolve(args);

            // Call (protected method) DispatchProxyAsync.Invoke()
            T returnValue = default;
            try
            {
                //Debug.Assert(_dispatchProxyInvokeMethod != null);
                //returnValue = _dispatchProxyInvokeMethod.Invoke(context.Packed.DispatchProxy,
                //    new object[] { context.Method, context.Packed.Args });
                var icn = new RpcInvocation(_app, _version, context.Packed.DeclaringType, context.Method, context.Packed.Args);
                var result = _zooyardPools.Invoke<T>(icn).GetAwaiter().GetResult();
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }

            return returnValue;
        }

        public async Task InvokeAsync(object[] args)
        {
            var context = Resolve(args);

            // Call (protected Task method) NetCoreStackDispatchProxy.InvokeAsync()
            try
            {
                //Debug.Assert(_dispatchProxyInvokeAsyncMethod != null);
                //await (Task)_dispatchProxyInvokeAsyncMethod.Invoke(context.Packed.DispatchProxy,
                //                                                       new object[] { context.Method, context.Packed.Args });
                var icn = new RpcInvocation(_app, _version, context.Packed.DeclaringType, context.Method, context.Packed.Args);
                await _zooyardPools.Invoke<object>(icn);
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }

        public async Task<T> InvokeAsync<T>(object[] args)
        {
            var context = Resolve(args);

            var returnValue = default(T);
            try
            {
                //Debug.Assert(_dispatchProxyInvokeAsyncTMethod != null);
                //var genericmethod = _dispatchProxyInvokeAsyncTMethod.MakeGenericMethod(typeof(T));
                //returnValue = await (Task<T>)genericmethod.Invoke(context.Packed.DispatchProxy,
                //                                                       new object[] { context.Method, context.Packed.Args });
                var icn = new RpcInvocation(_app, _version, context.Packed.DeclaringType, context.Method, context.Packed.Args);
                var result = await _zooyardPools.Invoke<T>(icn);
                returnValue = result.Value;
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
            return returnValue;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
