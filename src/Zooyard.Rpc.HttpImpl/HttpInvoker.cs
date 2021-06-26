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
            var parameterType = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            var method = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();
            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(_instance, isOpen);
            var value = await stub.Request($"/{_url.Path}/{invocation.MethodInfo.Name.ToLower()}", parameterType, method, parameters, invocation.Arguments);
            Logger().LogInformation($"Invoke:{invocation.MethodInfo.Name}");

            if (invocation.MethodInfo.ReturnType == typeof(Task) ||
              (invocation.MethodInfo.ReturnType.IsGenericType &&
               invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
            {
                if (invocation.MethodInfo.ReturnType == typeof(Task)) 
                {
                    return new RpcResult(Task.CompletedTask);
                }

                var tastGenericType = invocation.MethodInfo.ReturnType.GenericTypeArguments[0];

                if (tastGenericType.IsValueType)
                {
                    var resultData = Task.FromResult((dynamic)value.ChangeType(tastGenericType));
                    return new RpcResult(resultData);
                }

                if (tastGenericType == typeof(string))
                {
                    return new RpcResult(Task.FromResult(value));
                }

                var genericData = (dynamic)value.DeserializeJson(tastGenericType);
                var resultDataTask = Task.FromResult(genericData);
                var result = new RpcResult(resultDataTask);
                return result;
            }
            else 
            {
                if (invocation.MethodInfo.ReturnType == typeof(void))
                {
                    return new RpcResult();
                }

                if (invocation.MethodInfo.ReturnType.IsValueType)
                {
                    return new RpcResult(value.ChangeType(invocation.MethodInfo.ReturnType));
                }

                if (invocation.MethodInfo.ReturnType == typeof(string))
                {
                    return new RpcResult(value);
                }

                var result = new RpcResult(value.DeserializeJson(invocation.MethodInfo.ReturnType));
                return result;
            }
        }
    }
}
