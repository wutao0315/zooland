// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Zooyard.Realtime.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zooyard.Realtime.Server.Internal;

internal sealed partial class DefaultHubProtocolResolver : IRpcProtocolResolver
{
    private readonly ILogger<DefaultHubProtocolResolver> _logger;
    private readonly List<IRpcProtocol> _hubProtocols;
    private readonly Dictionary<string, IRpcProtocol> _availableProtocols;

    public IReadOnlyList<IRpcProtocol> AllProtocols => _hubProtocols;

    public DefaultHubProtocolResolver(IEnumerable<IRpcProtocol> availableProtocols, ILogger<DefaultHubProtocolResolver> logger)
    {
        _logger = logger ?? NullLogger<DefaultHubProtocolResolver>.Instance;
        _availableProtocols = new Dictionary<string, IRpcProtocol>(StringComparer.OrdinalIgnoreCase);

        foreach (var protocol in availableProtocols)
        {
            Log.RegisteredSignalRProtocol(_logger, protocol.Name, protocol.GetType());
            _availableProtocols[protocol.Name] = protocol;
        }
        _hubProtocols = _availableProtocols.Values.ToList();
    }

    public IRpcProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols)
    {
        protocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));

        if (_availableProtocols.TryGetValue(protocolName, out var protocol) && (supportedProtocols == null || supportedProtocols.Contains(protocolName, StringComparer.OrdinalIgnoreCase)))
        {
            Log.FoundImplementationForProtocol(_logger, protocolName);
            return protocol;
        }

        // null result indicates protocol is not supported
        // result will be validated by the caller
        return null;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Registered SignalR Protocol: {ProtocolName}, implemented by {ImplementationType}.", EventName = "RegisteredSignalRProtocol")]
        public static partial void RegisteredSignalRProtocol(ILogger logger, string protocolName, Type implementationType);

        [LoggerMessage(2, LogLevel.Debug, "Found protocol implementation for requested protocol: {ProtocolName}.", EventName = "FoundImplementationForProtocol")]
        public static partial void FoundImplementationForProtocol(ILogger logger, string protocolName);
    }
}
