namespace Zooyard.Attributes;


/// <summary>
/// 反向代理Thrift客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardThriftAttribute : ZooyardAttribute
{
    public const string TYPENAME = "Zooyard.ThriftImpl.ThriftClientPool, Zooyard.ThriftImpl";
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="proxyType"></param>
    public ZooyardThriftAttribute(string serviceName, Type proxyType) : base(TYPENAME, serviceName)
    {
        base.ProxyType = proxyType;
    }
}