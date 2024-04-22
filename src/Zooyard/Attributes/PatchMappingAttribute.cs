namespace Zooyard.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PatchMappingAttribute(string value) : RequestMappingAttribute(value, RequestMethod.Patch)
{
}