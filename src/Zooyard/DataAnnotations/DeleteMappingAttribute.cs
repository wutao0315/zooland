namespace Zooyard.DataAnnotations;

[AttributeUsage(AttributeTargets.Method)]
public class DeleteMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DeleteMappingAttribute(string value) : base(value, RequestMethod.DELETE)
    {
    }
}