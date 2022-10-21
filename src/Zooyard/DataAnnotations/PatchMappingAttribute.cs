namespace Zooyard.DataAnnotations;

[AttributeUsage(AttributeTargets.Method)]
public class PatchMappingAttribute : RequestMappingAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PatchMappingAttribute(string value) : base(value, RequestMethod.Patch)
    {
    }
}