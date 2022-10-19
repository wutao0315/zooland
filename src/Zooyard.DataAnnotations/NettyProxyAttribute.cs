namespace Zooyard.DataAnnotations;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class NettyProxyAttribute : Attribute
{
    public string ServiceName { get; init; }
    public string Version { get; init; } = "1.0";
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    public NettyProxyAttribute(string serviceName)
    {
        ServiceName = serviceName;
    }
}