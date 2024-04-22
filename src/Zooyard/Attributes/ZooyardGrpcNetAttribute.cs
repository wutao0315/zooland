namespace Zooyard.Attributes;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardGrpcNetAttribute : ZooyardAttribute
{
    public const string TYPENAME = "Zooyard.GrpcNetImpl.GrpcClientPool, Zooyard.GrpcNetImpl";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="proxyType"></param>
    public ZooyardGrpcNetAttribute(string serviceName, Type proxyType) : base(TYPENAME, serviceName)
    {
        base.ProxyType = proxyType;
    }
}