using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;

namespace Zooyard.Rpc.ThriftImpl.Trace
{
    public class TTraceProcessor : ITAsyncProcessor
    {
        private ITAsyncProcessor realProcessor;

        public TTraceProcessor(ITAsyncProcessor realProcessor)
        {
            this.realProcessor = realProcessor;
        }

        public async Task<bool> ProcessAsync(TProtocol iprot, TProtocol oprot, CancellationToken cancellationToken = default)
        {
            if (iprot is TTraceServerBinaryProtocol serverProtocol)
            {
                //serverProtocol.markTFramedTransport(iprot);
                TMessage tMessage = await serverProtocol.ReadMessageBeginAsync(cancellationToken);
                await serverProtocol.ReadFieldZero(cancellationToken);
                IDictionary<string, string> headInfo = serverProtocol.Head;

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
                //serverProtocol.resetTFramedTransport(iprot);
            }

            bool result = await realProcessor.ProcessAsync(iprot, oprot, cancellationToken);
            if (iprot is TTraceServerBinaryProtocol)
            {
                //TraceUtils.endAndSendLocalTracer();
            }
            return result;
        }
    }
}
