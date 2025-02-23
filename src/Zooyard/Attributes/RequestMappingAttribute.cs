namespace Zooyard.Attributes;


[AttributeUsage(AttributeTargets.Method)]
public class RequestMappingAttribute : AbstractAttribute
{
    /// <summary>
    /// 指定请求的实际地址
    /// </summary>
    public string Value { get; init; } = string.Empty;
    /// <summary>
    /// 指定请求的method类型（GET,POST,PUT,DELETE）等。
    /// </summary>
    public RequestMethod Method { get; init; } = RequestMethod.NONE;
    /// <summary>
    /// 指定处理请求的提交内容类型（Context-Type）。
    /// </summary>
    public string Consumes { get; init; } = string.Empty;
    /// <summary>
    /// 指定返回的内容类型，还可以设置返回值的字符编码
    /// </summary>
    public string Produces { get; init; } = string.Empty;

   
    /// <summary>
    /// 该参数在Rest接口中，代表通用返回类型封装 ResultInfo 代表当前接口的返回类型
    /// 该属性空代表不起作用，该属性设置后影响当前接口，并覆盖接口上的设置
    /// </summary>
    public Type? BaseReturnType { get; set; }
    /// <summary>
    /// 构造函数
    /// </summary>
    public RequestMappingAttribute()
    {
    }
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
    public RequestMappingAttribute(string value, RequestMethod method) : this(value)
    {
        Method = method;
    }
}