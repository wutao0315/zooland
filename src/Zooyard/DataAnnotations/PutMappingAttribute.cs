namespace Zooyard.DataAnnotations;

[AttributeUsage(AttributeTargets.Method)]
public class PutMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PutMappingAttribute(string value) : base(value, RequestMethod.PUT)
    {
    }
}