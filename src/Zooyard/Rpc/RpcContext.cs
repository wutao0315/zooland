using System.Collections.Concurrent;

namespace Zooyard.Rpc;

/// <summary>
/// Thread local context. (API, ThreadLocal, ThreadSafe)
/// 
/// 注意：RpcContext是一个临时状态记录器，当接收到RPC请求，或发起RPC请求时，RpcContext的状态都会变化。
/// 比如：A调B，B再调C，则B机器上，在B调C之前，RpcContext记录的是A调B的信息，在B调C之后，RpcContext记录的是B调C的信息。
/// </summary>
public sealed record RpcContext : IDisposable
{
    private bool _disposed;
    private static readonly AsyncLocal<RpcContext?> LOCAL = new();
    private volatile ConcurrentDictionary<string, string> attachments = new ();

    private IList<URL>? urls;
    
    public static RpcContext GetContext()
    {
        var context = LOCAL.Value;

        if (context == null || context._disposed) 
        {
            context = new RpcContext();
            LOCAL.Value = context;
        }
        return LOCAL.Value!;
    }

    public IList<URL>? Urls
    {
        get
        {
            return urls == null && Url != null ? [Url] : urls;
        }
        set
        {
            this.urls = value;
        }
    }


    public URL? Url { get; set; }


    /// <summary>
    /// get method name.
    /// </summary>
    /// <returns> method name. </returns>
    public string? MethodName { get; set; }


    /// <summary>
    /// get parameter types.
    /// 
    /// @serial
    /// </summary>
    public Type?[]? ParameterTypes { get; set; }


    /// <summary>
    /// get arguments.
    /// </summary>
    /// <returns> arguments. </returns>
    public object[]? Arguments { get; set; }

    public IDictionary<string, string> Attachments => attachments;
    public string? GetAttachment(string key)
    {
        if (attachments.TryGetValue(key, out var value) && value is string val)
        {
            return val;
        }
        return null;
    }

    //public object? GetObjectAttachment(string key)
    //{
    //    attachments.TryGetValue(key, out var obj);
    //    return obj;
    //}

    public RpcContext SetAttachment(string key, string value)
    {
        if (value == null)
        {
            attachments.TryRemove(key, out _);
        }
        else
        {
            attachments[key] = value;
        }
        return this;
    }

    public RpcContext RemoveAttachment(string key)
    {
        attachments.TryRemove(key, out _);
        return this;
    }

    public RpcContext SetAttachments(IDictionary<string, string> attachment)
    {
        this.attachments.Clear();
        if (attachment != null && attachment.Count > 0)
        {
            foreach (var item in attachment)
            {
                attachments[item.Key] = item.Value;
            }
        }
        return this;
    }

    public void ClearAttachments()
    {
        this.attachments.Clear();
    }

    public RpcContext SetInvokers(IList<URL>? invokers)
    {
        //this.invokers = invokers;
        if (invokers != null && invokers.Count > 0)
        {
            Urls = new List<URL>(invokers);
        }
        return this;
    }

    public RpcContext SetInvoker(URL invoker)
    {
        //this.invoker = invoker;
        if (invoker != null)
        {
            Url = invoker;
        }
        return this;
    }

    public RpcContext SetInvocation(IInvocation invocation)
    {
        //this.invocation = invocation;
        if (invocation != null)
        {
            MethodName = invocation.MethodInfo.Name;
            ParameterTypes = (from item in invocation.Arguments select item?.GetType()).ToArray();
            Arguments = invocation.Arguments;
        }
        return this;
    }

    public void Dispose()
    {
        if (_disposed) return;
        LOCAL.Value?.ClearAttachments();
        LOCAL.Value?.urls?.Clear();
        LOCAL.Value?.MethodName = null;
        LOCAL.Value?.ParameterTypes = null;
        LOCAL.Value?.Arguments = null;
        LOCAL.Value = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

