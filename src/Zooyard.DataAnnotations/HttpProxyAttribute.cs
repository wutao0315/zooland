using System.Reflection.Metadata;

namespace Zooyard.DataAnnotations;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class HttpProxyAttribute : Attribute
{
    public string ServiceName { get; init; }
    public string Version { get; init; } = "1.0";
    public string BaseUrl { get; init; } = String.Empty;
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="proxyType"></param>
    public HttpProxyAttribute(string serviceName)
    {
        ServiceName = serviceName;
    }
}
public enum RequestMethod 
{
    NONE, GET, POST, PUT, DELETE, Patch
}

[AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method)]
public class RequestMappingAttribute : Attribute
{
    /// <summary>
    /// 指定请求的实际地址
    /// </summary>
    public string Value { get; init; } = String.Empty;
    /// <summary>
    /// 指定请求的method类型（GET,POST,PUT,DELETE）等。
    /// </summary>
    public RequestMethod Method { get; init; } = RequestMethod.NONE;
    /// <summary>
    /// 指定处理请求的提交内容类型（Context-Type）。
    /// </summary>
    public string Consumes { get; init; } = String.Empty;
    /// <summary>
    /// 指定返回的内容类型，还可以设置返回值的字符编码
    /// </summary>
    public string Produces { get; init; } = String.Empty;
    /// <summary>
    /// 指定request中必须包含某些参数值，才让该方法处理。
    /// </summary>
    public List<Dictionary<string, string>> Params { get; init; } = new();
    /// <summary>
    /// 指定request中必须包含某些指定的header值，才让该方法处理请求
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="value"></param>
    public RequestMappingAttribute(string value)
    {
        Value = value;
    }
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="value"></param>
    /// <param name="method"></param>
    public RequestMappingAttribute(string value, RequestMethod method):this(value)
    {
        Method = method;
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class GetMappingAttribute : RequestMappingAttribute
{

    /// <summary>
    /// 构造函数
    /// </summary>
    public GetMappingAttribute(string value):base(value, RequestMethod.GET)
    {
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class PostMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PostMappingAttribute(string value) : base(value, RequestMethod.POST)
    {
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class PutMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PutMappingAttribute(string value) : base(value, RequestMethod.PUT)
    {
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class DeleteMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DeleteMappingAttribute(string value) : base(value, RequestMethod.DELETE)
    {
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class PatchMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PatchMappingAttribute(string value) : base(value, RequestMethod.Patch)
    {
    }
}
