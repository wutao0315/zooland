﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Thrift.Transport.Client;

namespace Zooyard.Rpc.ThriftImpl.Header
{
    public class TBinaryHeaderServerProtocol : TBinaryProtocol
    {

        private IDictionary<string, string> HEAD_INFO;

        public TBinaryHeaderServerProtocol(TTransport transport) : base(transport)
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

        public void MarkTFramedTransport(TProtocol protocol)
        {
            try
            {
                if (protocol.Transport is TStreamTransport stream) 
                {
                    var tioInputStream = TStreamTransportFieldsCache.getInstance().GetInputStream();
                    if (tioInputStream == null)
                    {
                        return;
                    }
                    var inputStream = (Stream)tioInputStream.GetValue(stream);
                    inputStream.Position = 0;
                }
            }
            catch (Exception e)
            {

                throw e;
            }

            //try
            //{
            //    TField tioInputStream = TIOStreamTransportFieldsCache.getInstance().getTIOInputStream();
            //    if (tioInputStream == null)
            //    {
            //        return;
            //    }
            //    BufferedInputStream inputStream = (BufferedInputStream)tioInputStream.get(protocol.Transport);
            //    inputStream.mark(0);
            //}
            //catch (Exception e)
            //{
            //    //e.printStackTrace();
            //}
        }


        /// <summary>
        /// 重置TFramedTransport流，不影响Thrift原有流程
        /// </summary>
        /// <param name="protocol"></param>
        public void ResetTFramedTransport(TProtocol protocol)
        {
            try
            {
                if (protocol.Transport is TStreamTransport stream)
                {
                    var tioInputStream = TStreamTransportFieldsCache.getInstance().GetInputStream();
                    if (tioInputStream == null)
                    {
                        return;
                    }
                    var inputStream = (Stream)tioInputStream.GetValue(stream);

                    inputStream.Seek(inputStream.Position, SeekOrigin.Begin);
                }

                //TField tioInputStream = TIOStreamTransportFieldsCache.getInstance().getTIOInputStream();
                //if (tioInputStream == null)
                //{
                //    return;
                //}
                //BufferedInputStream inputStream = (BufferedInputStream)tioInputStream.get(protocol.getTransport());
                //inputStream.reset();
            }
            catch (Exception e)
            {
                throw e;
                //e.printStackTrace();
            }
        }

        private class TStreamTransportFieldsCache
        {
            private static TStreamTransportFieldsCache instance;
            private FieldInfo inputStream_;
            private string TStreamTransport_inputStream_ = "_inputStream";

            private TStreamTransportFieldsCache()
            {

                inputStream_ = typeof(TStreamTransport).GetField(TStreamTransport_inputStream_);
                //inputStream_ = TIOStreamTransport.class.getDeclaredField(TIOStreamTransport_inputStream_);
                //inputStream_.SetAccessible(true);
            }

            public static TStreamTransportFieldsCache getInstance()
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        instance = new TStreamTransportFieldsCache();
                    }
                }
                return instance;
            }

            public FieldInfo GetInputStream()
            {
                return inputStream_;
            }
        }

        public new class Factory : TProtocolFactory
        {
            public override TProtocol GetProtocol(TTransport trans)
            {
                return new TBinaryHeaderServerProtocol(trans);
            }
        }
    }
}
