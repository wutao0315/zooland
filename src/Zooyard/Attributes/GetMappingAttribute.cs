namespace Zooyard.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class GetMappingAttribute(string value) : RequestMappingAttribute(value, RequestMethod.GET)
{
}