namespace Zooyard.Attributes;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardGrpcAttribute : ZooyardAttribute
{
    public const string TYPENAME = "Zooyard.GrpcImpl.GrpcClientPool, Zooyard.GrpcImpl";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="proxyType"></param>
    public ZooyardGrpcAttribute(string serviceName, Type proxyType) : base(TYPENAME, serviceName)
    {
        base.ProxyType = proxyType;
    }
}