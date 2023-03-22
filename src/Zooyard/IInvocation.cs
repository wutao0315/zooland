using System.Reflection;
using System.Threading;
using Zooyard.Rpc;
using Zooyard.Utils;

namespace Zooyard;

public interface IInvocation
{
    string ServiceName { get; }
    string Version { get; }
    string Url { get; }
    Type TargetType { get; }
    MethodInfo MethodInfo { get; }
    object[] Arguments { get; }
    Type[] ArgumentTypes { get; }
    string ProtocolServiceKey { get; }
    object Put(object key, object value);

    object? Get(object key);

    IDictionary<object, object> GetAttributes();
    string ServiceNamePoint();
    string PointVersion();
    void SetAttachment(string key, string value);
    string? GetAttachment(string key, string? defaultValue = default);
    void SetObjectAttachment(string key, object value);
    object? GetObjectAttachment(string key, object? defaultValue = default);

   
}

public class RpcInvocation : IInvocation
{
    private readonly SemaphoreSlim attachmentLock = new(1, 1);
    public RpcInvocation(string serviceName, string version, string url, Type targetType, MethodInfo methodInfo, object[]? arguments)
    {
        ServiceName = serviceName;
        Version = version;
        Url = url;
        TargetType = targetType;
        MethodInfo = methodInfo;
        Arguments = arguments ?? Array.Empty<object>();
        ArgumentTypes = arguments == null ? Array.Empty<Type>() : (from item in arguments select item.GetType()).ToArray();
    }

    private IDictionary<string, object> attachments = new Dictionary<string, object>();
    private IDictionary<object, object> attributes = new Dictionary<object, object>();
    public string ServiceName { get; }
    public string Version { get;}
    public string Url { get; }
    public Type TargetType { get; }
    public MethodInfo MethodInfo { get; }
    public object[] Arguments { get; }
    public Type[] ArgumentTypes { get; }
    public string ProtocolServiceKey { get; } = String.Empty;

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
}
