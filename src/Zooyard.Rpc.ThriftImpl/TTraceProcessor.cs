using System;
using System.Collections.Generic;
using System.Text;
using Thrift.Protocols;

namespace Zooyard.Rpc.ThriftImpl
{
    public class TTraceProcessor: TProcessor
    {
        private TProcessor realProcessor;

        public TTraceProcessor(TProcessor realProcessor)
        {
            this.realProcessor = realProcessor;
        }

        public bool process(TProtocol @in, TProtocol @out)
        {
            if (@in instanceof TTraceServerBinaryProtocol) {
                TTraceServerProtocol serverProtocol = (TTraceServerProtocol) @in;
                serverProtocol.markTFramedTransport(@in);
                    TMessage tMessage = serverProtocol.readMessageBegin();
                serverProtocol.readFieldZero();
                    IDictionary<string, string> headInfo = serverProtocol.getHead();

                string traceId = headInfo.get(TRACE_ID.getValue());
                string parentSpanId = headInfo.get(PARENT_SPAN_ID.getValue());
                string isSampled = headInfo.get(IS_SAMPLED.getValue());
                bool sampled = isSampled == null || bool.Parse(isSampled);

                if (traceId != null && parentSpanId != null) {
                    TraceUtils.startLocalTracer("rpc.thrift receive", traceId, parentSpanId, sampled);
                    string methodName = tMessage.Name;
                    TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_METHOD, methodName);
                    TTransport transport = @in.getTransport();
                    string hostAddress = ((TSocket)transport).getSocket().getRemoteSocketAddress().toString();
                    TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_SERVER, hostAddress);
                }
                serverProtocol.resetTFramedTransport(@in);

            }
            bool result = realProcessor.process(@in, @out);
        if (@in instanceof TTraceServerProtocol) {
            TraceUtils.endAndSendLocalTracer();
        }
        return result;
        }
    }
}
