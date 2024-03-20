// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Zooyard.Realtime.Protocol;

namespace Zooyard.Realtime.Server.Internal;

internal abstract class HubDispatcher<THub> where THub : Hub
{
    public abstract Task OnConnectedAsync(HubConnectionContext connection);
    public abstract Task OnDisconnectedAsync(HubConnectionContext connection, Exception? exception);
    public abstract Task DispatchMessageAsync(HubConnectionContext connection, RpcMessage hubMessage);
    public abstract IReadOnlyList<Type> GetParameterTypes(string name);
    public abstract string? GetTargetName(ReadOnlySpan<byte> targetUtf8Bytes);
}
