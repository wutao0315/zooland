using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocols;
using Thrift.Protocols.Entities;
using Thrift.Transports;

namespace Zooyard.Rpc.ThriftImpl.Trace
{
    public class TTraceClientBinaryProtocol : TBinaryProtocol
    {

        private IDictionary<string, string> HEAD_INFO;

        public TTraceClientBinaryProtocol(TClientTransport transport) : base(transport)
        {
            HEAD_INFO = new Dictionary<string, string>();
        }

        public override async Task WriteMessageBeginAsync(TMessage message)
        {

            //trace start
            //TraceUtils.startLocalTracer("rpc.thrift start");
            
            string methodName = message.Name;
            //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_METHOD, methodName);
            TClientTransport transport = this.Transport;


            //string hostAddress = ((TSocket)transport).getSocket().getRemoteSocketAddress().toString();
            //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_SERVER, hostAddress);

            await base.WriteMessageBeginAsync(message);
            //write trace header to field0
            await WriteFieldZero();
        }



        public async Task WriteFieldZero()
        {
            TField TRACE_HEAD = new TField("traceHeader", TType.Map, (short)0);
            await this.WriteFieldBeginAsync(TRACE_HEAD);
            {
                IDictionary<string, string> traceInfo = GenTraceInfo();
                await this.WriteMapBeginAsync(new TMap(TType.String, TType.String, traceInfo.Count));
                foreach (var entry in traceInfo) {
                    await this.WriteStringAsync(entry.Key);
                    await this.WriteStringAsync(entry.Value);
                }
                await this.WriteMapEndAsync();
            }
            await this.WriteFieldEndAsync();
        }

        private IDictionary<string, string> GenTraceInfo()
        {
            //gen trace info
            return HEAD_INFO;
        }

        public override async Task<TMessage> ReadMessageBeginAsync(CancellationToken cancellationToken)
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
        //clientReceive
        public override async Task<TMessage> ReadMessageBeginAsync()
        {
            var message = await this.ReadMessageBeginAsync(CancellationToken.None);
            return message;
        }
    }
}
