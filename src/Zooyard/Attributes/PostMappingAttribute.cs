namespace Zooyard.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PostMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PostMappingAttribute(string value) : base(value, RequestMethod.POST)
    {
    }
}