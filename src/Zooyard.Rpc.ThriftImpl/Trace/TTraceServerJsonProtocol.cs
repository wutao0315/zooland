using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;

namespace Zooyard.Rpc.ThriftImpl.Trace
{
    public class TTraceServerJsonProtocol : TBinaryProtocol
    {
        private IDictionary<string, string> HEAD_INFO;
        public TTraceServerJsonProtocol(TTransport transport) : base(transport)
        {
            HEAD_INFO = new Dictionary<string, string>();
        }

        public async Task<bool> ReadFieldZero(CancellationToken cancellationToken)
        {
            TField schemeField = await this.ReadFieldBeginAsync(cancellationToken);

            if (schemeField.ID == 0 && schemeField.Type == TType.Map)
            {
                TMap _map = await this.ReadMapBeginAsync(cancellationToken);
                HEAD_INFO = new Dictionary<string, string>(2 * _map.Count);
                for (int i = 0; i < _map.Count; ++i)
                {
                    string key = await this.ReadStringAsync(cancellationToken);
                    string value = await this.ReadStringAsync(cancellationToken);
                    HEAD_INFO.Add(key, value);
                }
                await this.ReadMapEndAsync(cancellationToken);
            }
            await this.ReadFieldEndAsync(cancellationToken);
            return HEAD_INFO.Count > 0;
        }

        public IDictionary<string, string> Head => HEAD_INFO;

        //public void markTFramedTransport(TProtocol @in)
        //{
        //    try
        //    {
        //        TField tioInputStream = TIOStreamTransportFieldsCache.getInstance().getTIOInputStream();
        //        if (tioInputStream == null)
        //        {
        //            return;
        //        }
        //        BufferedInputStream inputStream = (BufferedInputStream)tioInputStream.get(@in.Transport);
        //        inputStream.mark(0);
        //    }
        //    catch (Exception e)
        //    {
        //        //e.printStackTrace();
        //    }
        //}


        ///*
        // * 重置TFramedTransport流，不影响Thrift原有流程
        // */
        //public void resetTFramedTransport(TProtocol @in)
        //{
        //    try
        //    {
        //        TField tioInputStream = TIOStreamTransportFieldsCache.getInstance().getTIOInputStream();
        //        if (tioInputStream == null)
        //        {
        //            return;
        //        }
        //        BufferedInputStream inputStream = (BufferedInputStream)tioInputStream.get(in.getTransport());
        //        inputStream.reset();
        //    }
        //    catch (Exception e)
        //    {
        //        //e.printStackTrace();
        //    }
        //}

        private class TIOStreamTransportFieldsCache
        {
            private static TIOStreamTransportFieldsCache instance;
            private TField inputStream_;
            private string TIOStreamTransport_inputStream_ = "inputStream_";

            private TIOStreamTransportFieldsCache()
            {
                //inputStream_ = TIOStreamTransport.class.getDeclaredField(TIOStreamTransport_inputStream_);
                //inputStream_.SetAccessible(true);
            }

            public static TIOStreamTransportFieldsCache getInstance()
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        instance = new TIOStreamTransportFieldsCache();
                    }
                }
                return instance;
            }

            public TField getTIOInputStream()
            {
                return inputStream_;
            }
        }


        public class Factory : TProtocolFactory
        {
            public override TProtocol GetProtocol(TTransport trans)
            {
                return new TTraceServerBinaryProtocol(trans);
            }
        }
    }
}
