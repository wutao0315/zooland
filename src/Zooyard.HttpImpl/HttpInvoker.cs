using Microsoft.Extensions.Logging;
using System.Reflection;
using Zooyard.Attributes;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.HttpImpl;

public class HttpInvoker(ILogger logger, IHttpClientFactory _instance, int _clientTimeout, URL _url) : AbstractInvoker(logger)
{
    public const string DEFAULT_CONTENTTYPE = "application/json";
    public const string DEFAULT_METHODTYPE = "post";
    public override object Instance =>_instance;
    public override int ClientTimeout => _clientTimeout;
    protected override async Task<IResult<T>> HandleInvoke<T>(IInvocation invocation)
    {
        var methodName = invocation.MethodInfo.Name;
        var endStr = "Async";
        if (invocation.MethodInfo.Name.EndsWith(endStr, StringComparison.OrdinalIgnoreCase))
        {
            methodName = methodName[..^endStr.Length];
        }
        var pathList = _url.Path?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var pathUrl = new List<string>(pathList);
        var method = DEFAULT_METHODTYPE;
        var contentType = DEFAULT_CONTENTTYPE;
        var parameters = invocation.MethodInfo.GetParameters();
        
        var targetDescription = invocation.TargetType.GetCustomAttribute<RequestMappingAttribute>();
        if (targetDescription != null) 
        {
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
        }
        else 
        {
            pathUrl.Add(methodName);
        }

        var client = _instance.CreateClient();
        client.BaseAddress = new Uri($"{_url.Protocol}://{_url.Host}:{_url.Port}");

        var stub = new HttpStub(_logger, client, _clientTimeout);
        string? value = null;
        try
        {
            using var stream = await stub.Request(pathUrl, contentType, method, parameters, invocation.Arguments, RpcContext.GetContext().Attachments);
            if (stream == null)
            {
                throw new Exception($"{nameof(stream)} is null");
            }
            var genType = typeof(T);
            //文件流处理
            if (genType == typeof(byte[]))
            {
                byte[] bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length);
                // 设置当前流的位置为流的开始
                stream.Seek(0, SeekOrigin.Begin);
                return new RpcResult<T>((T)bytes.ChangeType(genType)!);
            }
            else 
            {
                using var sr = new StreamReader(stream);
                value = await sr.ReadToEndAsync();
            }
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw;
        }

        if (invocation.MethodInfo.ReturnType == typeof(T))
        {
            if (invocation.MethodInfo.ReturnType == typeof(void) || invocation.MethodInfo.ReturnType == typeof(Task))
            {
                return new RpcResult<T>();
            }

            if (invocation.MethodInfo.ReturnType.IsValueType || invocation.MethodInfo.ReturnType == typeof(string))
            {
                return new RpcResult<T>((T)value.ChangeType(typeof(T))!);
            }

            if (invocation.MethodInfo.ReturnType.IsGenericType &&
               invocation.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var tastGenericType = invocation.MethodInfo.ReturnType.GenericTypeArguments[0];

                if (tastGenericType.IsValueType || tastGenericType == typeof(string))
                {
                    return new RpcResult<T>((T)value.ChangeType(typeof(T))!);
                }

                var genericData = value.DeserializeJson<T>();
                return new RpcResult<T>(genericData);
            }
        }

        var result = new RpcResult<T>(value.DeserializeJson<T>());
        return result;

    }
}
