using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Zooyard.DataAnnotations;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(HttpInvoker));

    public const string DEFAULT_CONTENTTYPE = "application/json";
    public const string DEFAULT_METHODTYPE = "post";
    private readonly URL _url;
    private readonly IHttpClientFactory _instance;
    private readonly int _clientTimeout;

    public HttpInvoker(IHttpClientFactory instance, int clientTimeout, URL url)
    {
        _instance = instance;
        _clientTimeout = clientTimeout;
        _url = url;
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
        var contentType = DEFAULT_CONTENTTYPE;
        var parameters = invocation.MethodInfo.GetParameters();
        var header = new Dictionary<string, string>();
        

        var targetDescription = invocation.TargetType.GetCustomAttribute<RequestMappingAttribute>();
        if (targetDescription != null) 
        {
            header = targetDescription.Headers;
            if (!string.IsNullOrWhiteSpace(targetDescription.Value))
                pathUrl.AddRange(targetDescription.Value.Split('/', StringSplitOptions.RemoveEmptyEntries));
        }
        var methodDescription = invocation.MethodInfo.GetCustomAttribute<RequestMappingAttribute>();
        if (methodDescription != null)
        {
            if (!string.IsNullOrWhiteSpace(methodDescription.Value))
            {
                var methodNames = methodDescription.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
                pathUrl.AddRange(methodNames);
            }
            method = methodDescription.Method.ToString();
            contentType = methodDescription.Consumes;
            foreach (var item in methodDescription.Headers)
            {
                header[item.Key] = item.Value;
            }
        }
        else 
        {
            pathUrl.Add(methodName);
        }

        var client = _instance.CreateClient();
        client.BaseAddress = new Uri($"{_url.Protocol}://{_url.Host}:{_url.Port}");

        var stub = new HttpStub(client, _clientTimeout);
        var watch = Stopwatch.StartNew();
        string? value = null;
        try
        {
            using var stream = await stub.Request(pathUrl, contentType, method, parameters, invocation.Arguments, header);
            if (stream == null)
            {
                throw new Exception($"{nameof(stream)} is null");
            }
            var genType = typeof(T);
            //文件流处理
            if (genType == typeof(byte[]))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                // 设置当前流的位置为流的开始
                stream.Seek(0, SeekOrigin.Begin);
                watch.Stop();
                return new RpcResult<T>((T)bytes.ChangeType(genType)!, watch.ElapsedMilliseconds);
            }
            else 
            {
                using var sr = new StreamReader(stream);
                value = sr.ReadToEnd();
            }
            
        }
        catch (Exception ex)
        {
            Debug.Print(ex.StackTrace);
            throw;
        }
        finally
        {
            if(watch.IsRunning)
               watch.Stop();

            Logger().LogInformation($"Http Invoke {watch.ElapsedMilliseconds} ms");
        }

        if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task)) 
        {
            return new RpcResult<T>(watch.ElapsedMilliseconds);
        }

        if (invocation.MethodInfo.ReturnType.IsValueType || invocation.MethodInfo.ReturnType == typeof(string))
        {
            return new RpcResult<T>((T)value.ChangeType(typeof(T))!, watch.ElapsedMilliseconds);
        }

        if (invocation.MethodInfo.ReturnType.IsGenericType &&
           invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)) 
        {
            var tastGenericType = invocation.MethodInfo.ReturnType.GenericTypeArguments[0];

            if (tastGenericType.IsValueType || tastGenericType == typeof(string))
            {
                return new RpcResult<T>((T)value.ChangeType(typeof(T))!, watch.ElapsedMilliseconds);
            }

            var genericData = value.DeserializeJson<T>();
            return new RpcResult<T>(genericData, watch.ElapsedMilliseconds);
        }

        var result = new RpcResult<T>(value.DeserializeJson<T>(), watch.ElapsedMilliseconds);
        return result;

    }
}
