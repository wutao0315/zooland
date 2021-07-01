using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;
using System.Reflection;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Zooyard.Rpc.HttpImpl
{
    public class HttpInvoker : AbstractInvoker
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpInvoker));

        //public const string PARAMETERTYPE_KEY = "parametertype";
        public const string DEFAULT_PARAMETERTYPE = "json";
        //public const string METHODTYPE_KEY = "methodtype";
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
        protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
        {
            var methodName = invocation.MethodInfo.Name;
            var endStr = "Async";
            if (invocation.MethodInfo.Name.EndsWith(endStr, StringComparison.OrdinalIgnoreCase))
            {
                methodName = methodName.Substring(0, methodName.Length- endStr.Length);
            }
            var pathList = _url.Path?.Split('/', StringSplitOptions.RemoveEmptyEntries)??new string[0];
            var pathUrl = new List<string>(pathList);
            var method = DEFAULT_METHODTYPE;
            var parameterType = DEFAULT_PARAMETERTYPE;
            

            var targetDescription = invocation.TargetType.GetCustomAttribute<DescriptionAttribute>();
            if (targetDescription != null && !string.IsNullOrWhiteSpace(targetDescription.Description)) 
            {
                pathUrl.AddRange(targetDescription.Description.Split('/', StringSplitOptions.RemoveEmptyEntries));
            }
            var methodDescription = invocation.MethodInfo.GetCustomAttribute<DescriptionAttribute>();
            if (methodDescription != null && !string.IsNullOrWhiteSpace(methodDescription.Description))
            {
                //ShowHello|post&json
                var actions = methodDescription.Description.Split('|');
                if (actions.Length<2) 
                {
                    throw new Exception("place set action discrption like this sample ShowHello|post&json");
                }

                if (!string.IsNullOrWhiteSpace(actions[0]) || actions[0] != "_") 
                {
                    methodName = actions[0];
                }
                pathUrl.Add(methodName);

                var methodAndContentTypeList = actions[1].Split('&');
                if (methodAndContentTypeList.Length < 2)
                {
                    throw new Exception("place set action discrption like this sample ShowHello|post&json");
                }

                if (!string.IsNullOrWhiteSpace(methodAndContentTypeList[0])|| methodAndContentTypeList[0] != "_") 
                {
                    method = methodAndContentTypeList[0];
                }
                if (!string.IsNullOrWhiteSpace(methodAndContentTypeList[1]) || methodAndContentTypeList[1] != "_")
                {
                    parameterType = methodAndContentTypeList[1];
                }
            }
            else 
            {
                pathUrl.Add(methodName);
            }

            //var parameterType = _url.GetMethodParameterAndDecoded(methodName, PARAMETERTYPE_KEY, DEFAULT_PARAMETERTYPE).ToLower();
            //var method = _url.GetMethodParameterAndDecoded(methodName, METHODTYPE_KEY, DEFAULT_METHODTYPE).ToLower();

            var parameters = invocation.MethodInfo.GetParameters();
            var stub = new HttpStub(_instance, isOpen);
            var watch = Stopwatch.StartNew();
            string value = null;
            try
            {
                value = await stub.Request($"/{string.Join('/', pathUrl)}", parameterType, method, parameters, invocation.Arguments);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.StackTrace);
                throw ex;
            }
            finally
            {
                watch.Stop();
                Logger().LogInformation($"Http Invoke {watch.ElapsedMilliseconds} ms");
            }

            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task)) 
            {
                return new RpcResult<T>(watch.ElapsedMilliseconds);
            }

            if (invocation.MethodInfo.ReturnType.IsValueType || invocation.MethodInfo.ReturnType == typeof(string))
            {
                return new RpcResult<T>((T)value.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
            }

            if (invocation.MethodInfo.ReturnType.IsGenericType &&
               invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) 
            {
                var tastGenericType = invocation.MethodInfo.ReturnType.GenericTypeArguments[0];

                if (tastGenericType.IsValueType || tastGenericType == typeof(string))
                {
                    return new RpcResult<T>((T)value.ChangeType(typeof(T)), watch.ElapsedMilliseconds);
                }

                var genericData = value.DeserializeJson<T>();
                return new RpcResult<T>(genericData, watch.ElapsedMilliseconds);
            }

            var result = new RpcResult<T>(value.DeserializeJson<T>(), watch.ElapsedMilliseconds);
            return result;

        }
    }
}
