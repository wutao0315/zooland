using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };

        public HttpInvoker(HttpClient instance,URL url, bool[] isOpen)
        {
            _instance = instance;
            _url = url;
            this.isOpen = isOpen;
        }
        public override object Instance { get { return _instance; } }
        protected override async Task<IResult> HandleInvoke(IInvocation invocation)
        {
            var parameterType = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            var method = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();
            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(_instance, isOpen);
            var value = await stub.Request($"/{_url.Path}/{invocation.MethodInfo.Name.ToLower()}", parameterType, method, parameters, invocation.Arguments);
            
            if (invocation.MethodInfo.ReturnType.IsValueType)
            {
                if (invocation.MethodInfo.ReturnType == typeof(void))
                {
                    return new RpcResult();
                }
                return new RpcResult(value.ChangeType(invocation.MethodInfo.ReturnType));
            }

            if (invocation.MethodInfo.ReturnType == typeof(string))
            {
                return new RpcResult(value);
            }
            Logger().Information($"Invoke:{invocation.MethodInfo.Name}");
            var result = new RpcResult(JsonConvert.DeserializeObject(value, invocation.MethodInfo.ReturnType));
            return result;
        }
    }
}
