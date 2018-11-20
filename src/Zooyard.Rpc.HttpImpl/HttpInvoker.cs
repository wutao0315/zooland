﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Zooyard.Core;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpInvoker : IInvoker
    {
        public const string PARAMETERTYPE_KEY = "parametertype";
        public const string DEFAULT_PARAMETERTYPE = "json";
        public const string METHODTYPE_KEY = "methodtype";
        public const string DEFAULT_METHODTYPE = "post";
        private readonly URL _url;
        private readonly HttpClient _instance;
        private readonly ILogger _logger;
        /// <summary>
        /// 开启标志
        /// </summary>
        protected bool[] isOpen = new bool[] { false };

        public HttpInvoker(HttpClient instance,URL url, bool[] isOpen,ILoggerFactory loggerFactory)
        {
            _instance = instance;
            _url = url;
            this.isOpen = isOpen;
            _logger = loggerFactory.CreateLogger<HttpInvoker>();


        }

        public IResult Invoke(IInvocation invocation)
        {
            var parameterType = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            var method = _url.GetMethodParameterAndDecoded(invocation.MethodInfo.Name, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();
            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(_instance, isOpen);
            var value = stub.Request($"/{_url.Path}/{invocation.MethodInfo.Name.ToLower()}", parameterType, method, parameters, invocation.Arguments).GetAwaiter().GetResult();
            
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
            _logger.LogInformation($"Invoke:{invocation.MethodInfo.Name}");
            var result = new RpcResult(JsonConvert.DeserializeObject(value, invocation.MethodInfo.ReturnType));
            return result;
        }
    }
}
