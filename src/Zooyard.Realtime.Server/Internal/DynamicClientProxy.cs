using System.Dynamic;

namespace Zooyard.Realtime.Server.Internal;

internal sealed class DynamicClientProxy : DynamicObject
{
    private readonly IClientProxy _clientProxy;

    public DynamicClientProxy(IClientProxy clientProxy)
    {
        _clientProxy = clientProxy;
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        result = _clientProxy.SendCoreAsync(binder.Name, args!);
        return true;
    }
}
