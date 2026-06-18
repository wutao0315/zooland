using System.Diagnostics;
using System.Reflection;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Thrift.Transport.Client;
using Zooyard.Diagnositcs;

namespace Zooyard.ThriftImpl.Header;

public class TCompactHeaderServerProtocol : TCompactProtocol
{

    private IDictionary<string, string?> HEAD_INFO;

    public TCompactHeaderServerProtocol(TTransport transport) : base(transport)
    {
        HEAD_INFO = new Dictionary<string, string?>();
    }

    public async Task<bool> ReadFieldZero(CancellationToken cancellationToken)
    {
        TField schemeField = await this.ReadFieldBeginAsync(cancellationToken);

        if (schemeField.ID == 0 && schemeField.Type == TType.Map)
        {
            TMap _map = await this.ReadMapBeginAsync(cancellationToken);
            HEAD_INFO = new Dictionary<string, string?>(2 * _map.Count);
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
            return new TCompactHeaderServerProtocol(trans);
        }
    }
}
