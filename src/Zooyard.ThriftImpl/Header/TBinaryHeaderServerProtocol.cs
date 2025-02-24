﻿using System.Reflection;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Thrift.Transport.Client;

namespace Zooyard.ThriftImpl.Header;

public class TBinaryHeaderServerProtocol : TBinaryProtocol
{

    private IDictionary<string, string> HEAD_INFO;

    public TBinaryHeaderServerProtocol(TTransport transport) : base(transport)
    {
        HEAD_INFO = new Dictionary<string, string>();
    }

    public async Task<bool> ReadFieldZero(CancellationToken cancellationToken)
    {
        TField schemeField = await ReadFieldBeginAsync(cancellationToken);

        if (schemeField.ID == 0 && schemeField.Type == TType.Map)
        {
            TMap _map = await ReadMapBeginAsync(cancellationToken);
            HEAD_INFO = new Dictionary<string, string>(2 * _map.Count);
            for (int i = 0; i < _map.Count; ++i)
            {
                string key = await ReadStringAsync(cancellationToken);
                string value = await ReadStringAsync(cancellationToken);
                HEAD_INFO.Add(key, value);
            }
            await ReadMapEndAsync(cancellationToken);
        }
        await ReadFieldEndAsync(cancellationToken);
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

                if (tioInputStream.GetValue(stream) is Stream inputStream) 
                {
                    inputStream.Position = 0;
                };
            }
        }
        catch
        {

            throw;
        }
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
                if (tioInputStream.GetValue(stream) is Stream inputStream) 
                {
                    inputStream.Seek(inputStream.Position, SeekOrigin.Begin);
                }
            }
        }
        catch
        {
            throw;
        }
    }

    private class TStreamTransportFieldsCache
    {
        private static TStreamTransportFieldsCache? instance;
        private FieldInfo? inputStream_;
        private string TStreamTransport_inputStream_ = "_inputStream";

        private TStreamTransportFieldsCache()
        {

            inputStream_ = typeof(TStreamTransport).GetField(TStreamTransport_inputStream_)!;
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

        public FieldInfo? GetInputStream()
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
