using System.Diagnostics;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Zooyard.Diagnositcs;

namespace Zooyard.ThriftImpl.Header;

public class TBinaryHeaderServerProtocol : TBinaryProtocol
{
    private IDictionary<string, string?> HEAD_INFO;
    public TBinaryHeaderServerProtocol(TTransport transport) : base(transport)
    {
        HEAD_INFO = new Dictionary<string, string?>();
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

    public IDictionary<string, string?> Head => HEAD_INFO;

    public TMessage TMessage { get; private set; }

    public async ValueTask<TMessage> BaseReadMessageBeginAsync(CancellationToken cancellationToken) 
    {
        TMessage = await base.ReadMessageBeginAsync(cancellationToken);
        return TMessage;
    }
    public override async ValueTask<TMessage> ReadMessageBeginAsync(CancellationToken cancellationToken)
    {
        return TMessage;
    }

    public new class Factory : TProtocolFactory
    {
        public override TProtocol GetProtocol(TTransport trans)
        {
            return new TBinaryHeaderServerProtocol(trans);
        }
    }
}
