namespace Zooyard.DataAnnotations;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class GrpcProxyAttribute : Attribute
{
    public string ServiceName { get; init; }
    public Type ProxyType { get; init; }
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="proxyType"></param>
    public GrpcProxyAttribute(string serviceName, Type proxyType)
    {
        ServiceName = serviceName;
        ProxyType = proxyType;
    }
}