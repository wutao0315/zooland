using System.Xml.Linq;

namespace Zooyard.DataAnnotations;


/// <summary>
/// 接口代理
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardAttribute : Attribute
{
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
    /// <summary>
    /// 默认路径和默认配置参数
    /// </summary>
    public string Url { get; init; } = "";
    /// <summary>
    /// 代理类型
    /// </summary>
    public Type? ProxyType { get; set; }
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="serviceName"></param>
    public ZooyardAttribute(string typeName, string serviceName)
    {
        TypeName = typeName;
        ServiceName = serviceName;
    }
}