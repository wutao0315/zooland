using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpInvoker));

        public const string PARAMETERTYPE_KEY = "parametertype";
        public const string DEFAULT_PARAMETERTYPE = "json";
        public const string METHODTYPE_KEY = "methodtype";
        public const string DEFAULT_METHODTYPE = "post";
        private readonly URL _url;
        private readonly HttpClient _instance;
        private readonly int _clientTimeout;
        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };

        public HttpInvoker(HttpClient instance, int clientTimeout, URL url, bool[] isOpen)
        {
            _instance = instance;
            _clientTimeout = clientTimeout;
            _url = url;
            this.isOpen = isOpen;
        }
        public override object Instance =>_instance;
        public override int ClientTimeout => _clientTimeout;
        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
        {
            var methodName = invocation.MethodInfo.Name;
            var endStr = "Async";
            if (invocation.MethodInfo.Name.EndsWith(endStr, StringComparison.OrdinalIgnoreCase))
            {
                methodName = methodName.Substring(0, methodName.Length- endStr.Length);
            }

            var parameterType = _url.GetMethodParameterAndDecoded(methodName, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            var method = _url.GetMethodParameterAndDecoded(methodName, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();
            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(_instance, isOpen);
            var value = await stub.Request($"/{_url.Path}/{methodName.ToLower()}", parameterType, method, parameters, invocation.Arguments);
            Logger().LogInformation($"Invoke:{invocation.MethodInfo.Name}");

            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task)) 
            {
                return new RpcResult();
            }

            if (invocation.MethodInfo.ReturnType.IsValueType || invocation.MethodInfo.ReturnType == typeof(string))
            {
                return new RpcResult(value);
            }

            if (invocation.MethodInfo.ReturnType.IsGenericType &&
               invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) 
            {
                var tastGenericType = invocation.MethodInfo.ReturnType.GenericTypeArguments[0];

                if (tastGenericType.IsValueType || tastGenericType == typeof(string))
                {
                    return new RpcResult(value);
                }

                var genericData = value.DeserializeJson(tastGenericType);
                return new RpcResult(genericData);
            }

            var result = new RpcResult(value.DeserializeJson(invocation.MethodInfo.ReturnType));
            return result;

        }
    }
}
