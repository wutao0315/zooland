namespace Zooyard.DataAnnotations;

/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardNettyAttribute : ZooyardAttribute
{
    public const string TYPENAME = "Zooyard.Rpc.NettyImpl.NettyClientPool, Zooyard.Rpc.NettyImpl";
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    public ZooyardNettyAttribute(string serviceName) : base(TYPENAME, serviceName)
    {
        ServiceName = serviceName;
    }
}
