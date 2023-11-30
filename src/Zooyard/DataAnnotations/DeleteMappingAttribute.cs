namespace Zooyard.DataAnnotations;

[AttributeUsage(AttributeTargets.Method)]
public class DeleteMappingAttribute(string value) 
    : RequestMappingAttribute(value, RequestMethod.DELETE)
{
}