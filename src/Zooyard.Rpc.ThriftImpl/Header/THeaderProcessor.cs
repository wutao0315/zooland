using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;

namespace Zooyard.Rpc.ThriftImpl.Header;

public class THeaderProcessor : ITAsyncProcessor
{
    private ITAsyncProcessor realProcessor;

    public THeaderProcessor(ITAsyncProcessor realProcessor)
    {
        this.realProcessor = realProcessor;
    }

    public async Task<bool> ProcessAsync(TProtocol iprot, TProtocol oprot, CancellationToken cancellationToken = default)
    {
        if (iprot is TBinaryHeaderServerProtocol serverProtocolBinary)
        {
            serverProtocolBinary.MarkTFramedTransport(iprot);
            TMessage tMessage = await serverProtocolBinary.ReadMessageBeginAsync(cancellationToken);
            await serverProtocolBinary.ReadFieldZero(cancellationToken);
            IDictionary<string, string> headInfo = serverProtocolBinary.Head;

            string methodName = tMessage.Name;

            if (iprot.Transport is TSocketTransport socket)
            {
                string hostAddress = socket.Host.MapToIPv4().ToString();
            }
            else if (iprot.Transport is TTlsSocketTransport socketTls) 
            {
                string hostAddress = socketTls.Host.MapToIPv4().ToString();
            }
            else if (iprot.Transport is TStreamTransport stream)
            {
            }
            else if (iprot.Transport is THttpTransport http)
            {
                string hostAddress = http.RequestHeaders.Host;
            }
            else if (iprot.Transport is TNamedPipeTransport pipe)
            {
            }
            else if (iprot.Transport is TMemoryBufferTransport memory)
            {
            }

            //string traceId = headInfo.get(TRACE_ID.getValue());
            //string parentSpanId = headInfo.get(PARENT_SPAN_ID.getValue());
            //string isSampled = headInfo.get(IS_SAMPLED.getValue());
            //bool sampled = isSampled == null || bool.Parse(isSampled);

            //if (traceId != null && parentSpanId != null)
            //{
            //    //TraceUtils.startLocalTracer("rpc.thrift receive", traceId, parentSpanId, sampled);
            //    string methodName = tMessage.Name;
            //    //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_METHOD, methodName);
            //    TTransport transport = iprot.Transport;

            //    //string hostAddress = ((TSocket)transport).getSocket().getRemoteSocketAddress().toString();
            //    //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_SERVER, hostAddress);
            //}
            serverProtocolBinary.ResetTFramedTransport(iprot);
        } 
        else if (iprot is TCompactHeaderServerProtocol serverProtocolCompact) { }
        else if (iprot is TJsonHeaderServerProtocol serverProtocolJson) { }

        bool result = await realProcessor.ProcessAsync(iprot, oprot, cancellationToken);
        if (iprot is TBinaryHeaderServerProtocol)
        {
            //TraceUtils.endAndSendLocalTracer();
        }
        return result;
    }
}
