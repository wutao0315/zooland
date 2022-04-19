namespace Zooyard.DynamicProxy;

public class ProxyExecutor
{
    //private readonly IZooyardPools _clientPools;

    //public ProxyExecutor(IZooyardPools clientPools)
    //{
    //    _clientPools = clientPools;
    //}

    //private static IDictionary<string, object> GetParameters(MethodBase method, IReadOnlyList<object> args)
    //{
    //    var dict = new Dictionary<string, object>();
    //    var parameters = method.GetParameters();
    //    if (!parameters.Any())
    //        return dict;
    //    for (var i = 0; i < parameters.Length; i++)
    //    {
    //        var parameter = parameters[i];
    //        dict.Add(parameter.Name, args[i]);
    //    }
    //    return dict;
    //}

    //public object Invoke(IInvocation icn)
    //{
    //    //调用上下文
    //    var result = _clientPools.Invoke(icn).GetAwaiter().GetResult();
    //    return result.Value;
    //}

    //public async Task InvokeAsync(IInvocation icn)
    //{
    //    //调用上下文
    //    await _clientPools.Invoke(icn);
    //}

    //public async Task<T> InvokeAsyncT<T>(IInvocation icn)
    //{
    //    //调用上下文
    //    var result = await _clientPools.Invoke(icn);
    //    return (T)result.Value;
    //}
}
