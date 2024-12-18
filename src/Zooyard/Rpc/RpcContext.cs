namespace Zooyard.Rpc;

/// <summary>
/// Thread local context. (API, ThreadLocal, ThreadSafe)
/// 
/// 注意：RpcContext是一个临时状态记录器，当接收到RPC请求，或发起RPC请求时，RpcContext的状态都会变化。
/// 比如：A调B，B再调C，则B机器上，在B调C之前，RpcContext记录的是A调B的信息，在B调C之后，RpcContext记录的是B调C的信息。
/// </summary>
public sealed record RpcContext
{
    private static readonly AsyncLocal<RpcContext> LOCAL = new();
    private volatile Dictionary<string, string> attachments = new ();

    private IList<URL>? urls;
    
    public static RpcContext GetContext()
    {
        LOCAL.Value ??= new RpcContext();
        return LOCAL.Value;
    }

    public IList<URL>? Urls
    {
        get
        {
            return urls == null && Url != null ? new List<URL>() { Url } : urls;
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
            attachments.Remove(key);
        }
        else
        {
            attachments[key] = value;
        }
        return this;
    }

    //public RpcContext SetAttachment(string key, object value)
    //{
    //    return SetObjectAttachment(key, value);
    //}

    //public RpcContext SetObjectAttachment(string key, object value)
    //{
    //    if (value == null)
    //    {
    //        attachments.Remove(key);
    //    }
    //    else
    //    {
    //        attachments[key] = value;
    //    }
    //    return this;
    //}

    public RpcContext RemoveAttachment(string key)
    {
        attachments.Remove(key);
        return this;
    }

    //public IDictionary<string, object> GetObjectAttachments()
    //{
    //    return attachments;
    //}

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

    //public RpcContext SetObjectAttachments(IDictionary<string, object> attachment)
    //{
    //    this.attachments.Clear();
    //    if (attachment.Count > 0)
    //    {
    //        this.attachments = attachment;
    //    }
    //    return this;
    //}

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
}

//public record RpcContextAttachment
//{
//    protected volatile IDictionary<string, object> attachments = new Dictionary<string, object>();
//    public string? GetAttachment(string key)
//    {
//        if (attachments.TryGetValue(key, out var value) && value is string val) {
//            return val;
//        }
//        return null;
//    }

//    public object? GetObjectAttachment(string key)
//    {
//        attachments.TryGetValue(key, out var obj);
//        return obj;
//    }

//    public RpcContextAttachment SetAttachment(string key, string value)
//    {
//        return SetObjectAttachment(key, value);
//    }

//    public RpcContextAttachment SetAttachment(string key, object value)
//    {
//        return SetObjectAttachment(key, value);
//    }

//    public RpcContextAttachment SetObjectAttachment(string key, object value)
//    {
//        if (value == null)
//        {
//            attachments.Remove(key);
//        }
//        else
//        {
//            attachments[key] =  value;
//        }
//        return this;
//    }

//    public RpcContextAttachment RemoveAttachment(string key)
//    {
//        attachments.Remove(key);
//        return this;
//    }

//    public IDictionary<string, object> GetObjectAttachments()
//    {
//        return attachments;
//    }

//    public RpcContextAttachment SetAttachments(IDictionary<string, string> attachment)
//    {
//        this.attachments.Clear();
//        if (attachment != null && attachment.Count > 0)
//        {
//            foreach (var item in attachment)
//            {
//                attachments[item.Key] = item.Value;
//            }
//        }
//        return this;
//    }

//    public RpcContextAttachment SetObjectAttachments(IDictionary<string, object> attachment)
//    {
//        this.attachments.Clear();
//        if (attachment.Count>0)
//        {
//            this.attachments = attachment;
//        }
//        return this;
//    }

//    public void ClearAttachments()
//    {
//        this.attachments.Clear();
//    }

//    public RpcContextAttachment? CopyOf(bool needCopy)
//    {
//        if (!IsValid())
//        {
//            return null;
//        }

//        if (needCopy)
//        {
//            var copy = new RpcContextAttachment();
//            if (attachments.Count>0)
//            {
//                copy.attachments.PutAll(this.attachments);
//            }
//            return copy;
//        }
//        else
//        {
//            return this;
//        }
//    }

//    protected bool IsValid()
//    {
//        return attachments.Count>0;
//    }
//}



