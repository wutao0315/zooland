using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;
using Zooyard.Diagnositcs;

namespace Zooyard.ThriftImpl.Header;

public class THeaderProcessor : ITAsyncProcessor
{
    internal static string ActivitySourceName = typeof(THeaderProcessor).FullName!;
    //internal static string ActivityName = typeof(THeaderProcessor).FullName!;

    private readonly ITAsyncProcessor _realProcessor;
 
    public THeaderProcessor(ITAsyncProcessor realProcessor)
    {
        _realProcessor = realProcessor;
    }

    public async Task<bool> ProcessAsync(TProtocol iprot, TProtocol oprot, CancellationToken cancellationToken = default)
    {
        //long startTimestamp = 0;
        IDictionary<string, string?>? headInfo = null;
        var methodName = "";
        var hostAddress = "";
        if (iprot.Transport is TSocketTransport socket)
        {
            //socket.TcpClient.Client
            hostAddress = socket.Host?.MapToIPv4()?.ToString()??"";
        }
        else if (iprot.Transport is TTlsSocketTransport socketTls)
        {
            hostAddress = socketTls.Host?.MapToIPv4()?.ToString()??"";
        }
        else if (iprot.Transport is THttpTransport http)
        {
            hostAddress = http.RequestHeaders.Host??"";
        }
        //else if (iprot.Transport is TStreamTransport stream)
        //{
        //}
        //else if (iprot.Transport is TNamedPipeTransport pipe)
        //{
        //}
        //else if (iprot.Transport is TMemoryBufferTransport memory)
        //{
        //}

        if (iprot is TBinaryHeaderServerProtocol serverProtocolBinary)
        {
            TMessage tMessage = await serverProtocolBinary.BaseReadMessageBeginAsync(cancellationToken);
            await serverProtocolBinary.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolBinary.Head;
            methodName = tMessage.Name;
        } 
        else if (iprot is TCompactHeaderServerProtocol serverProtocolCompact) 
        {
            TMessage tMessage = await serverProtocolCompact.BaseReadMessageBeginAsync(cancellationToken);
            await serverProtocolCompact.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolCompact.Head;
            methodName = tMessage.Name;
        }
        else if (iprot is TJsonHeaderServerProtocol serverProtocolJson) 
        {
            TMessage tMessage = await serverProtocolJson.BaseReadMessageBeginAsync(cancellationToken);
            await serverProtocolJson.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolJson.Head;
            methodName = tMessage.Name;
        }

        using var activity = ActivityHelper.Start(ActivitySourceName,headInfo, ActivityKind.Server);
        Activity.Current = activity;

        activity?.AddTag("rpc.thrift.method",methodName);
        activity?.AddTag("rpc.thrift.server", hostAddress);

        bool result = await _realProcessor.ProcessAsync(iprot, oprot, cancellationToken);
        return result;
    }

}
