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

namespace Zooyard.Rpc.HttpImpl
{
    /// <summary>
    /// HTTP代理类
    /// </summary>
    public class HttpStub
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        public enum ParaMode : int
        {
            /// <summary>
            /// URL参数:0
            /// </summary>
            UrlPara = 0,
            /// <summary>
            /// Form表单参数:1
            /// </summary>
            FormPara = 1,
            /// <summary>
            /// Json直接参数:2
            /// </summary>
            JsonPara = 2
        };

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

        /// <summary>
        /// Http头
        /// </summary>
        public HttpRequestHeaders Headers { get=> client.DefaultRequestHeaders; }

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
                    //content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
                    content = null;
                    break;
                case "form":
                    content = new FormUrlEncodedContent(paraDic);
                    break;
                case "json":
                default:
                    content = new StringContent(JsonConvert.SerializeObject(paraDic),Encoding.UTF8, "application/json");
                    break;
            }

            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), relatedUrl) { Content = content }).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch(Exception ex)
            {
                openFlag[0] = false;
                throw ex;
            }
        }


        /// <summary>
        /// Post请求Json数据
        /// </summary>
        /// <typeparam name="U">请求POCO类型</typeparam>
        /// <typeparam name="V">响应POCO类型</typeparam>
        /// <param name="relatedUrl">相对url，可含参数</param>
        /// <param name="reqObj">请求POCO实例</param>
        /// <param name="paraMode">参数模式</param>
        /// <returns>响应POCO实例</returns>
        public async Task<V> PostJson<U, V>(string relatedUrl, U reqObj, ParaMode paraMode = ParaMode.UrlPara)
        {
            this.Headers.Accept.TryParseAdd("application/json");
            var paras = ObjToStrParas(reqObj);
            HttpContent content;
            switch (paraMode)
            {
                case ParaMode.UrlPara:
                    var paraStr = relatedUrl.Contains('?') ? "&" : "?" +
                    string.Join("&", paras.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)));
                    relatedUrl += paraStr;
                    content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
                    break;
                case ParaMode.FormPara:
                    content = new FormUrlEncodedContent(paras);
                    break;
                default:
                    content = new StringContent(JsonConvert.SerializeObject(reqObj), Encoding.UTF8, "application/json");
                    break;
            }
            try
            {
                var response = await client.PostAsync(relatedUrl, content).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return default(V);
                }
                return JsonConvert.DeserializeObject<V>(response.Content.ReadAsStringAsync().Result);
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// Post请求Json数据
        /// </summary>
        /// <param name="relatedUrl">相对url</param>
        /// <param name="paraMode">参数模式</param>
        /// <param name="paras">参数</param>
        /// <returns>响应字符串</returns>
        public async Task<string> PostJson(string relatedUrl, ParaMode paraMode, params object[] paras)
        {
            this.Headers.Accept.TryParseAdd("application/json");
            var paraDic = new Dictionary<string, string>();
            foreach (var para in paras)
            {
                var paraPairs = ObjToStrParas(para);
                foreach (var paraPair in paraPairs)
                {
                    if (!paraDic.ContainsKey(paraPair.Key))
                    {
                        paraDic.Add(paraPair.Key, paraPair.Value);
                    }
                }
            }
            HttpContent content;
            switch (paraMode)
            {
                case ParaMode.UrlPara:
                    var paraStr = relatedUrl.Contains('?') ? "&" : "?" +
                    string.Join("&", paraDic.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)));
                    relatedUrl += paraStr;
                    content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
                    break;
                case ParaMode.FormPara:
                    content = new FormUrlEncodedContent(paraDic);
                    break;
                default:
                    content = new StringContent(JsonConvert.SerializeObject(paraDic), Encoding.UTF8, "application/json");
                    break;
            }

            try
            {
                var response = await client.PostAsync(relatedUrl, content).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// Post请求字符串数据
        /// </summary>
        /// <param name="relatedUrl">相对url，可含参数</param>
        /// <param name="formParas">Form参数键值对集合</param>
        /// <returns>响应字符串</returns>
        public async Task<string> Post(string relatedUrl, IEnumerable<KeyValuePair<string, string>> formParas)
        {
            try
            {
                var response = await client.PostAsync(relatedUrl, new FormUrlEncodedContent(formParas)).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// Post请求字符串数据
        /// </summary>
        /// <param name="relatedUrl">相对url，可含参数</param>
        /// <param name="strContent">字符串内容</param>
        /// <param name="encode">编码</param>
        /// <returns>响应字符串</returns>
        public async Task<string> Post(string relatedUrl, string strContent, Encoding encode)
        {
            try
            {
                var response = await client.PostAsync(relatedUrl, new StringContent(strContent, encode)).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// Get请求Json数据
        /// </summary>
        /// <typeparam name="U">请求POCO类型</typeparam>
        /// <typeparam name="V">响应POCO类型</typeparam>
        /// <param name="relatedUrl">相对url，可含参数</param>
        /// <param name="reqObj">请求POCO实例</param>
        /// <returns>响应POCO实例</returns>
        public async Task<V> GetJson<U, V>(string relatedUrl, U reqObj)
        {
            this.Headers.Accept.TryParseAdd("application/json");
            var paras = ObjToStrParas(reqObj);
            var paraStr = string.Empty;
            if (paras.Count() > 0)
            {
                paraStr = relatedUrl.Contains('?') ? "&" : "?" +
                string.Join("&", paras.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)));
            }
            try
            {
                var response = await client.GetAsync(relatedUrl + paraStr).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return default(V);
                }
                return JsonConvert.DeserializeObject<V>(data);
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// Get请求字符串数据
        /// </summary>
        /// <param name="relatedUrl">相对url，可含参数</param>
        /// <returns>响应字符串</returns>
        public async Task<string> Get(string relatedUrl)
        {
            try
            {
                var response = await client.GetAsync(relatedUrl).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }

        /// <summary>
        /// 获取Json响应字符串
        /// </summary>
        /// <param name="relatedUrl">相对url</param>
        /// <param name="paras">参数</param>
        /// <returns>响应字符串</returns>
        public async Task<string> GetJson(string relatedUrl, params object[] paras)
        {
            this.Headers.Accept.TryParseAdd("application/json");
            var paraDic = new Dictionary<string, string>();
            foreach (var para in paras)
            {
                var paraPairs = ObjToStrParas(para);
                foreach (var paraPair in paraPairs)
                {
                    if (!paraDic.ContainsKey(paraPair.Key))
                    {
                        paraDic.Add(paraPair.Key, paraPair.Value);
                    }
                }
            }

            var paraStr = string.Empty;
            if (paraDic.Count() > 0)
            {
                paraStr = relatedUrl.Contains('?') ? "&" : "?" +
                string.Join("&", paraDic.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)));
            }
            try
            {
                var response = await client.GetAsync(relatedUrl + paraStr).ConfigureAwait(false);
                var data = response.Content.ReadAsStringAsync().Result;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(data);
                    return null;
                }
                return data;
            }
            catch
            {
                openFlag[0] = false;
                throw;
            }
        }
    }
}
