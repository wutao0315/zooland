using System.Reflection.Metadata;

namespace Zooyard.Attributes;


/// <summary>
/// 反向代理GRPC客户端
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class ZooyardHttpAttribute : ZooyardAttribute
{

public const string TYPENAME = "Zooyard.HttpImpl.HttpClientPool, Zooyard.HttpImpl";
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceName"></param>
    public ZooyardHttpAttribute(string serviceName) : base(TYPENAME, serviceName)
    {
    }
}
