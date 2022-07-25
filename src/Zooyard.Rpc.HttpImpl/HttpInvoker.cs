using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.HttpImpl;

public class HttpInvoker : AbstractInvoker
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(HttpInvoker));

    //public const string PARAMETERTYPE_KEY = "parametertype";
    public const string DEFAULT_PARAMETERTYPE = "json";
    //public const string METHODTYPE_KEY = "methodtype";
    public const string DEFAULT_METHODTYPE = "post";
    private readonly URL _url;
    private readonly IHttpClientFactory _instance;
    private readonly int _clientTimeout;
    /// <summary>
    /// 开启标志
    /// </summary>
    protected bool[] isOpen = new bool[] { false };

    public HttpInvoker(IHttpClientFactory instance, int clientTimeout, URL url, bool[] isOpen)
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

        var parameters = invocation.MethodInfo.GetParameters();
        var stub = new HttpStub(_instance, isOpen, _clientTimeout);
        var watch = Stopwatch.StartNew();
        string? value = null;
        try
        {
            using var stream = await stub.Request($"/{string.Join('/', pathUrl)}", parameterType, method, parameters, invocation.Arguments);
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
                return new RpcResult<T>((T)bytes.ChangeType(genType), watch.ElapsedMilliseconds);
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
