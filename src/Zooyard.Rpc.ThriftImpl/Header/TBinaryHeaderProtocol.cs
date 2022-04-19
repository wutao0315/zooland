using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;

namespace Zooyard.Rpc.ThriftImpl.Header;

public class TBinaryHeaderProtocol : TBinaryProtocol
{

    private IDictionary<string, string> HEAD_INFO;

    public TBinaryHeaderProtocol(TTransport transport) : base(transport)
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
        TField TRACE_HEAD = new TField("traceHeader", TType.Map, (short)0);
        await this.WriteFieldBeginAsync(TRACE_HEAD, cancellationToken);
        {
            IDictionary<string, string> traceInfo = GenTraceInfo();
            await this.WriteMapBeginAsync(new TMap(TType.String, TType.String, traceInfo.Count), cancellationToken);
            foreach (var entry in traceInfo) {
                await this.WriteStringAsync(entry.Key, cancellationToken);
                await this.WriteStringAsync(entry.Value, cancellationToken);
            }
            await this.WriteMapEndAsync(cancellationToken);
        }
        await this.WriteFieldEndAsync(cancellationToken);
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
