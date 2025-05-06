using System.Reflection;
using Zooyard.Attributes;

namespace Zooyard;

public interface IInvocation
{
    public string Id { get; }
    string ServiceName { get; }
    string Version { get; }
    URL Url { get; }
    Type TargetType { get; }
    MethodInfo MethodInfo { get; }
    object[] Arguments { get; }
    Type[] ArgumentTypes { get; }
    ProtocolServiceKey ProtocolServiceKey { get; }
    ZooyardAttribute Attribute { get; }
    object Put(object key, object value);

    object? Get(object key);

    IDictionary<object, object> GetAttributes();
    string ServiceNamePoint();
    string PointVersion();
    void SetAttachment(string key, string value);
    string? GetAttachment(string key, string? defaultValue = default);
    void SetObjectAttachment(string key, object value);
    object? GetObjectAttachment(string key, object? defaultValue = default);

    IDictionary<string, string> Metadatas { get; }
    IDictionary<string, string> Headers { get; }
    IDictionary<string, string> Params { get; }

    //void SetMetadata(string key, string value);
    //T? GetMetadata<T>(string key, T? defaultValue = default);


}

public class RpcInvocation : IInvocation
{
    private readonly SemaphoreSlim attachmentLock = new(1, 1);
    public RpcInvocation(string id, ZooyardAttribute zooyardAttribute, string url, string serviceName, Type targetType, MethodInfo methodInfo, object[]? arguments)
    {
        //兼容只传后面路径的问题
        if (url.IndexOf("://")<=0 || url.IndexOf(":/") <= 0) 
        {
            if (url.StartsWith('/'))
            {
                url = "http://127.0.0.1" + url;
            }
            else 
            {
                url = "http://127.0.0.1/" + url;
            }
        }

        Url = URL.ValueOf(url);
        Id = id;
        ServiceName = serviceName;
        Version = zooyardAttribute.Version;
        TargetType = targetType;
        MethodInfo = methodInfo;
        Arguments = arguments ?? Array.Empty<object>();
        ArgumentTypes = arguments == null ? Array.Empty<Type>() : (from item in methodInfo.GetParameters() select item.ParameterType).ToArray();
        var group = Url.GetParameter(CommonConstants.GROUP_KEY, CommonConstants.DEFAULT_GROUP);
        ProtocolServiceKey = new ProtocolServiceKey(targetType.FullName!, zooyardAttribute.Version, group, Url.Protocol);
        Attribute = zooyardAttribute;

        SetAttachment(CommonConstants.PATH_KEY, Url.Path);
        SetAttachment(CommonConstants.VERSION_KEY, zooyardAttribute.Version);
        SetAttachment(CommonConstants.INTERFACE_KEY, targetType.FullName!);
        SetAttachment(CommonConstants.GROUP_KEY, group);

        if (Url.HasParameter(CommonConstants.TIMEOUT_KEY))
        {
            SetAttachment(CommonConstants.TIMEOUT_KEY, Url.GetParameter(CommonConstants.TIMEOUT_KEY)!);
        }
        if (Url.HasParameter(CommonConstants.TOKEN_KEY))
        {
            SetAttachment(CommonConstants.TOKEN_KEY, Url.GetParameter(CommonConstants.TOKEN_KEY)!);
        }
        if (Url.HasParameter(CommonConstants.APPLICATION_KEY))
        {
            SetAttachment(CommonConstants.APPLICATION_KEY, Url.GetParameter(CommonConstants.APPLICATION_KEY)!);
        }
    }

    private Dictionary<string, object> attachments = new ();
    private Dictionary<object, object> attributes = new ();

    private Dictionary<string, string> metadatas = new();
    private Dictionary<string, string> headers = new();
    private Dictionary<string, string> parameters = new();
    public string Id { get; }
    public string ServiceName { get; }
    public string Version { get;}
    public URL Url { get; }
    public Type TargetType { get; }
    public MethodInfo MethodInfo { get; }
    public object[] Arguments { get; }
    public Type[] ArgumentTypes { get; }
    public ProtocolServiceKey ProtocolServiceKey { get; }
    public ZooyardAttribute Attribute { get; }
    public object Put(object key, object value)
    {
        return attributes[key] = value;
    }

    public object? Get(object key)
    {
        attributes.TryGetValue(key, out var value);
        return value;
    }

    public IDictionary<object, object> GetAttributes()
    {
        return attributes;
    }

    public string ServiceNamePoint()
    {
        var result = string.IsNullOrWhiteSpace(ServiceName) ? "" : $"{ServiceName}.";
        return result;
    }
    public string PointVersion()
    {
        var result = string.IsNullOrWhiteSpace(Version) ? "" : $".{Version}";
        return result;
    }
    public void SetAttachment(string key, string value) 
    {
        SetObjectAttachment(key, value);
    }
    public string? GetAttachment(string key, string? defaultValue = default)
    {
        try
        {
            attachmentLock.Wait() ;
            if (attachments == null)
            {
                return defaultValue;
            }
            attachments.TryGetValue(key, out var value);
            if (value is string strValue) {
                if (string.IsNullOrWhiteSpace(strValue))
                {
                    return defaultValue;
                }
                else
                {
                    return strValue;
                }
            }
            return defaultValue;
        }
        finally
        {
            attachmentLock.Release();
        }
    }
    public void SetObjectAttachment(string key, object value)
    {
        try
        {
            attachmentLock.Wait();
            if (attachments == null)
            {
                attachments = new Dictionary<string, object>();
            }
            attachments[key] = value;
        }
        finally
        {
            attachmentLock.Release();
        }
    }
    public object? GetObjectAttachment(string key, object? defaultValue = default)
    {
        try
        {
            attachmentLock.Wait();
            if (attachments == null)
            {
                return defaultValue;
            }
            attachments.TryGetValue(key, out var value);
            if (value == null)
            {
                return defaultValue;
            }
            return value;
        }
        finally
        {
            attachmentLock.Release();
        }
    }

    public IDictionary<string, string> Metadatas => metadatas;
    public IDictionary<string, string> Headers => headers;
    public IDictionary<string, string> Params => parameters;

    //public void SetMetadata(string key, string value)
    //{
    //    if (metadata == null)
    //    {
    //        metadata = new Dictionary<string, string>();
    //    }
    //    metadata[key] = value;
    //}
    //public T? GetMetadata<T>(string key, T? defaultValue = default)
    //{
    //    if (metadata == null)
    //    {
    //        return defaultValue;
    //    }
    //    metadata.TryGetValue(key, out var value);
    //    if (value is string strValue)
    //    {
    //        if (string.IsNullOrWhiteSpace(strValue))
    //        {
    //            return defaultValue;
    //        }
    //    }

    //    return (T?)value.ChangeType(typeof(T));
    //}
}
