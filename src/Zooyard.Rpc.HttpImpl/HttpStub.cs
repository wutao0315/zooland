using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zooyard.Core.Logging;

namespace Zooyard.Rpc.HttpImpl
{
    /// <summary>
    /// HTTP代理类
    /// </summary>
    public class HttpStub
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpStub));
        /// <summary>
        /// http客户端
        /// </summary>
        protected HttpClient client;

        /// <summary>
        /// http客户端状态标识
        /// </summary>
        protected bool[] openFlag;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="client">http长连接客户端</param>
        /// <param name="isOpen">http客户端状态标识</param>
        public HttpStub(HttpClient client, bool[] isOpen)
        {
            this.client = client;
            openFlag = isOpen;
        }

        ///// <summary>
        ///// Http头
        ///// </summary>
        //public HttpRequestHeaders Headers { get=> client.DefaultRequestHeaders; }

        /// <summary>
        /// 对象公共字段或属性以键值对有序字典输出，不包含Null值
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>键值对有序字典</returns>
        protected IEnumerable<KeyValuePair<string, string>> ObjToStrParas(object obj)
        {
            var retParas = new Dictionary<string, string>();
            if (obj == null)
            {
                return retParas;
            }

            //获取对象类型
            var objType = obj.GetType();

            //写入公共字段
            foreach (var field in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var filedValue = field.GetValue(obj);
                if (filedValue != null)
                {
                    retParas.Add(field.Name, filedValue.ToString());
                }
            }

            //写入公共属性
            foreach (var property in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyValue = property.GetValue(obj);
                if (propertyValue != null)
                {
                    retParas.Add(property.Name, propertyValue.ToString());
                }
            }

            //返回映射字典
            return retParas;
        }

        public async Task<string> Request(string methodName,string parameterType,string method,ParameterInfo[] parameterInfos, object[] paras)
        {
            var paraDic = new Dictionary<string, string>();
            for (int i = 0; i < paras.Length; i++)
            {
                var para = paras[i];
                if (para.GetType().IsValueType || para.GetType().IsPrimitive || para is string)
                {
                    paraDic.Add(parameterInfos[i].Name, para.ToString());
                }
                else {
                    var paraPairs = ObjToStrParas(para);
                    foreach (var paraPair in paraPairs)
                    {
                        if (!paraDic.ContainsKey(paraPair.Key))
                        {
                            paraDic.Add(paraPair.Key, paraPair.Value);
                        }
                    }
                }
            }
            var relatedUrl = methodName;
            HttpContent content;
            switch (parameterType)
            {
                case "url":
                    relatedUrl = $"{methodName}?{string.Join("&", paraDic.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)))}";
                    content = null;
                    break;
                case "form":
                    content = new FormUrlEncodedContent(paraDic);
                    break;
                case "json":
                default:
                    content = new StringContent(paraDic.ToJsonString("{}"),Encoding.UTF8, "application/json");
                    break;
            }

            try
            {
                var request = new HttpRequestMessage(new HttpMethod(method), relatedUrl) { Content = content };
                //request.Headers.Add("","");

                var response = await client.SendAsync(request).ConfigureAwait(false);
                var data = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger().LogError($"statuscode:{response.StatusCode},{data}");
                    return null;
                }
                return data;
            }
            catch(Exception ex)
            {
                openFlag[0] = false;
                Logger().LogError(ex);
                throw ex;
            }
        }
    }
}
