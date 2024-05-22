using Microsoft.Extensions.Logging;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Text;
using Zooyard.Utils;

namespace Zooyard.HttpImpl;

/// <summary>
/// HTTP代理类
/// </summary>
public class HttpStub
{
    private readonly ILogger _logger;
    /// <summary>
    /// http客户端
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="httpClient">http长连接客户端</param>
    /// <param name="timeout">超时时长</param>
    public HttpStub(ILogger logger, HttpClient httpClient, int timeout)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
    }

    public async Task<Stream> Request(IList<string> path, string contentType, string method, ParameterInfo[] parameters, object[] paras, IDictionary<string, string> headers)
    {
        try
        {
            var (paraItems, fileItems) = GetDic(parameters, paras);

            var paraDic = new Dictionary<string, string>(paraItems);

            var pathList = new List<string>();
            foreach (var item in path)
            {
                var pathItem = item;
                if (!item.Contains('{') || !item.Contains('}'))
                {
                    pathList.Add(pathItem);
                    continue;
                }

                foreach (var dic in paraItems)
                {
                    if ($"{{{dic.Key}}}" == item)
                    {
                        paraDic.Remove(dic.Key);
                        pathItem = dic.Value;
                        break;
                    }
                }
                pathList.Add(pathItem);
            }
            string requestUri = $"/{string.Join('/', pathList)}";

            var httpMethod = new HttpMethod(method);
            var relatedUrl = requestUri;
            if (httpMethod == HttpMethod.Get)
            {
                relatedUrl = $"{requestUri}?{string.Join("&", paraDic.Select(para => para.Key + "=" + WebUtility.UrlEncode(para.Value)))}";
            }

            using HttpContent? content = GetContent(contentType, parameters, paras, paraItems, fileItems);
            var request = new HttpRequestMessage(httpMethod, relatedUrl) { Content = content };

            foreach (var item in headers)
            {
                request.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }

            var response = await _httpClient.SendAsync(request);
            var data = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await new StreamReader(data).ReadToEndAsync();
                string errorMsg = $"statuscode:{response.StatusCode},{content},{responseBody}";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }
    }

    private HttpContent? GetContent(string contentType,
        ParameterInfo[] parameters,
        object[] paras,
        IDictionary<string, string> paraItems,
        IDictionary<string, byte[]> fileItems)
    {
        HttpContent? content = null;
        switch (contentType)
        {
            case "application/x-www-form-urlencoded":
                content = new FormUrlEncodedContent(paraItems);
                break;
            case "multipart/form-data":
                var multipartFormDataContent = new MultipartFormDataContent();
                foreach (var item in paraItems)
                {
                    multipartFormDataContent.Add(new StringContent(item.Value), string.Format("\"{0}\"", item.Key));
                }
                foreach (var item in fileItems)
                {
                    var fileNameKey = $"{item.Key}_fn";
                    var fileName = paraItems.ContainsKey(fileNameKey) ? paraItems[fileNameKey] : item.Key;
                    multipartFormDataContent.Add(new ByteArrayContent(item.Value), item.Key, fileName);
                }
                content = multipartFormDataContent;
                break;
            case "application/json":
                var json = GetJson(parameters, paras);
                content = new StringContent(json, Encoding.UTF8, "application/json");
                break;
        }

        return content;
    }

    private string GetJson(ParameterInfo[] parameterInfos, object[] paras)
    {
        var paraDic = new Dictionary<string, object>();
        for (int i = 0; i < paras.Length; i++)
        {
            var para = paras[i];
            var paraType = para.GetType();
            var paraInfo = parameterInfos[i];

            if (string.IsNullOrWhiteSpace(paraInfo.Name))
            {
                continue;
            }

            if (paraType.IsValueTypeOrString())
            {
                paraDic.Add(paraInfo.Name, para.ToStringValueType());
            }
            else
            {
                var paraPairs = ObjectUtil.ExecuteObj(para);
                foreach (var paraPair in paraPairs)
                {
                    paraDic[paraPair.Key] = paraPair.Value;
                }
            }
        }
        return paraDic.ToJsonString("{}");
    }

    private (IDictionary<string, string>, IDictionary<string, byte[]>) GetDic(ParameterInfo[] parameterInfos, object[] paras)
    {
        var paraDic = new Dictionary<string, string>();
        var paraFileDic = new Dictionary<string, byte[]>();
        for (int i = 0; i < paras.Length; i++)
        {
            var para = paras[i];
            var paraType = para.GetType();
            var paraInfo = parameterInfos[i];

            if (string.IsNullOrWhiteSpace(paraInfo.Name))
            {
                continue;
            }

            if (para is byte[] paraBytes)
            {
                paraFileDic.Add(paraInfo.Name, paraBytes);
            }
            else if (paraType.IsValueTypeOrString())
            {
                paraDic.Add(paraInfo.Name, para.ToStringValueType());
            }
            else
            {
                var (paraPairs, paraFiles) = ObjectUtil.Execute(para, paraInfo.Name);
                foreach (var paraPair in paraPairs)
                {
                    paraDic[paraPair.Key] = paraPair.Value.ToString();
                }
                foreach (var paraFile in paraFiles)
                {
                    paraFileDic[paraFile.Key] = paraFile.Value;
                }
            }
        }
        return (paraDic, paraFileDic);
    }
}


public class ObjectUtil
{
    public static (Dictionary<string, string>, Dictionary<string, byte[]>) Execute(object obj, string prefix = "")
    {
        return ExecuteInternal(obj, prefix: prefix);
    }
    public static Dictionary<string, object> ExecuteObj(object obj, string prefix = "")
    {
        var dictionary = new Dictionary<string, object>();
        var type = obj.GetType();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";

            var fastProp = FastProperty.GetOrCreate(property);
            var value = fastProp.Get(obj);

            if (value == null)
            {
                dictionary.Add(key, "");
                continue;
            }

            dictionary.Add(key, value);
        }

        return dictionary;
    }
    private static (Dictionary<string, string>, Dictionary<string, byte[]> dictionaryBytes) ExecuteInternal(
        object obj,
        string prefix = "",
        Dictionary<string, string> dictionary = default!,
        Dictionary<string, byte[]> dictionaryBytes = default!)
    {
        dictionary ??= new Dictionary<string, string>();
        dictionaryBytes ??= new Dictionary<string, byte[]>();
        var type = obj.GetType();
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";

            var fastProp = FastProperty.GetOrCreate(property);
            var value = fastProp.Get(obj);

            if (value == null)
            {
                dictionary.Add(key, "");
                continue;
            }

            if (value is byte[] valBytes)
            {
                dictionaryBytes.Add(key, valBytes);
            }
            else if (property.PropertyType.IsValueTypeOrString())
            {
                dictionary.Add(key, value.ToStringValueType());
            }
            else
            {
                if (value is IEnumerable enumerable)
                {
                    var counter = 0;
                    foreach (var item in enumerable)
                    {
                        var itemKey = $"{key}[{counter++}]";
                        var itemType = item.GetType();

                        if (item is byte[] itemBytes)
                        {
                            dictionaryBytes.Add(itemKey, itemBytes);
                        }
                        if (itemType.IsValueTypeOrString())
                        {
                            dictionary.Add(itemKey, item.ToStringValueType());
                        }
                        else
                        {
                            ExecuteInternal(item, itemKey, dictionary, dictionaryBytes);
                        }
                    }
                }
                else
                {
                    ExecuteInternal(value, key, dictionary, dictionaryBytes);
                }
            }
        }

        return (dictionary, dictionaryBytes);
    }
}
internal static class Extensions
{
    internal static bool IsValueTypeOrString(this Type type)
    {
        return type.IsValueType || type == typeof(string);
    }

    internal static string ToStringValueType(this object value)
    {
        return value switch
        {
            DateTime dateTime => dateTime.ToString("o"),
            bool boolean => boolean.ToStringLowerCase(),
            _ => value.ToString()!
        };
    }

    internal static bool IsIEnumerable(this Type type)
    {
        return type.IsAssignableFrom(typeof(IEnumerable));
    }

    internal static string ToStringLowerCase(this bool boolean)
    {
        return boolean ? "true" : "false";
    }
}
