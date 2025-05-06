namespace Zooyard.Attributes;


/// <summary>
/// 接口代理
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public abstract class ZooyardAttribute : AbstractAttribute
{

    /// <summary>
    /// 服务Id
    /// </summary>
    public string AppId { get; init; }
    /// <summary>
    /// 类型名称
    /// </summary>
    public string TypeName { get; init; }
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; init; }
    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    public string Protocol { get; set; } = "http";

    private string _url = string.Empty;
    /// <summary>
    /// 默认路径和默认配置参数
    /// </summary>
    public string Url
    {
        get
        {
            var idx = _url.IndexOf("://");
            if (idx >= 0) 
            {
                if (idx == 0)
                {
                    return $"{Protocol}{_url}";
                }

                return _url;
            }

            return $"{Protocol}://{ServiceName}{(_url.StartsWith('/') ? _url : "/" + _url)}";
        }
        init => _url = value;
    }
    /// <summary>
    /// 参数配置，key=configkey@default从配置中心拉取数据
    /// </summary>
    public string Config { get; set; } = "";
    /// <summary>
    /// 代理类型
    /// </summary>
    public Type? ProxyType { get; set; }
    /// <summary>
    /// 该参数在Rest接口中，代表通用返回类型封装 ResultInfo 代表当前接口的返回类型
    /// 该属性空代表不起作用，该属性设置后影响所有接口
    /// </summary>
    public Type? BaseReturnType { get; set; }
    /// <summary>
    ///构造函数
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="serviceName"></param>
    /// <param name="appId"></param>
    public ZooyardAttribute(string typeName, string serviceName, string appId = default!)
    {
        TypeName = typeName;
        ServiceName = serviceName;
        AppId = appId;
    }
}