using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpInvoker : IInvoker
    {
        public const string PARAMETERTYPE_KEY = "parametertype";
        public const string DEFAULT_PARAMETERTYPE = "json";
        public const string METHODTYPE_KEY = "methodtype";
        public const string DEFAULT_METHODTYPE = "post";
        private URL Url { get; set; }
        private HttpClient Instance { get; set; }
        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };

        public HttpInvoker(HttpClient instance,URL url, bool[] isOpen)
        {
            Instance = instance;
            this.Url = url;
            this.isOpen = isOpen;
        }

        public IResult Invoke(IInvocation invocation)
        {
            //var paraTypes = new Type[invocation.Arguments.Length + 1];
            //var parasPlus = new object[invocation.Arguments.Length + 1];
            //for (var i = 0; i < invocation.Arguments.Length; i++)
            //{
            //    paraTypes[i] = invocation.Arguments[i].GetType();
            //    parasPlus[i] = invocation.Arguments[i];
            //}
            //paraTypes[invocation.Arguments.Length] = typeof(Grpc.Core.CallOptions);
            //parasPlus[invocation.Arguments.Length] = new Grpc.Core.CallOptions()
            //    .WithDeadline(DateTime.UtcNow.AddMilliseconds(ClientTimeout));
            //var method = Instance.GetType().GetMethod(invocation.MethodName, paraTypes);
            //var value = method.Invoke(Instance, parasPlus);

            
            var parameterType = Url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            var method = Url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();
            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(Instance, isOpen);
            var value = stub.Request($"/{Url.Path}/{invocation.MethodInfo.Name.ToLower()}", parameterType, method, parameters, invocation.Arguments).GetAwaiter().GetResult();
            
            if (invocation.MethodInfo.ReturnType.IsValueType)
            {
                if (invocation.MethodInfo.ReturnType == typeof(void))
                {
                    return new RpcResult();
                }
                return new RpcResult(value.ChangeType(invocation.MethodInfo.ReturnType));
            }

            var result = new RpcResult(JsonConvert.DeserializeObject(value, invocation.MethodInfo.ReturnType));
            return result;
        }
    }
}
