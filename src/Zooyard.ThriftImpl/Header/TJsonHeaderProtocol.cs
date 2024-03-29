﻿using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;

namespace Zooyard.ThriftImpl.Header;

public class TJsonHeaderProtocol : TJsonProtocol
{

    private IDictionary<string, string> HEAD_INFO;

    public TJsonHeaderProtocol(TTransport transport) : base(transport)
    {
        HEAD_INFO = new Dictionary<string, string>();
    }

    public override async Task WriteMessageBeginAsync(TMessage message, CancellationToken cancellationToken)
    {
        //trace start
        //TraceUtils.startLocalTracer("rpc.thrift start");

        string methodName = message.Name;
        //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_METHOD, methodName);
        TTransport transport = this.Transport;


        //string hostAddress = ((TSocket)transport).getSocket().getRemoteSocketAddress().toString();
        //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_SERVER, hostAddress);

        await base.WriteMessageBeginAsync(message, cancellationToken);
        //write trace header to field0
        await WriteFieldZero(cancellationToken);
    }





    public async Task WriteFieldZero(CancellationToken cancellationToken)
    {
        var TRACE_HEAD = new TField("traceHeader", TType.Map, (short)0);
        await WriteFieldBeginAsync(TRACE_HEAD, cancellationToken);
        {
            IDictionary<string, string> traceInfo = GenTraceInfo();
            await WriteMapBeginAsync(new TMap(TType.String, TType.String, traceInfo.Count), cancellationToken);
            foreach (var entry in traceInfo) {
                await WriteStringAsync(entry.Key, cancellationToken);
                await WriteStringAsync(entry.Value, cancellationToken);
            }
            await WriteMapEndAsync(cancellationToken);
        }
        await WriteFieldEndAsync(cancellationToken);
    }

    private IDictionary<string, string> GenTraceInfo()
    {
        //gen trace info
        return HEAD_INFO;
    }

    public override async ValueTask<TMessage> ReadMessageBeginAsync(CancellationToken cancellationToken)
    {
        TMessage tMessage = await base.ReadMessageBeginAsync(cancellationToken);
        if (tMessage.Type == TMessageType.Exception)
        {
            TApplicationException x = await TApplicationException.ReadAsync(this, cancellationToken);
            //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_EXCEPTION, StringUtil.trimNewlineSymbolAndRemoveExtraSpace(x.getMessage()));
            //TraceUtils.endAndSendLocalTracer();
        }
        else if (tMessage.Type == TMessageType.Reply)
        {
            //TraceUtils.endAndSendLocalTracer();
        }
        return tMessage;
    }
}
