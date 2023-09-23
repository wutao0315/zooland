using Thrift;
using Zooyard.Logging;
using Zooyard.Rpc.Support;

namespace Zooyard.ThriftImpl;

public class ThriftClient : AbstractClient
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ThriftClient));
    public override string System => "zy_thrift";
    public override URL Url { get; }
    public override int ClientTimeout { get; }
    private readonly TBaseClient _thriftclient;

    public ThriftClient(TBaseClient thriftclient, int clientTimeout, URL url)
    {
        _thriftclient = thriftclient;
        this.ClientTimeout = clientTimeout;
        this.Url = url;
    }


    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);
        //thrift client service
        return new ThriftInvoker(_thriftclient, this.ClientTimeout);
    }

    public override async Task Open(CancellationToken cancellationToken = default)
    {
        if (!_thriftclient.OutputProtocol.Transport.IsOpen)
        {
            await _thriftclient.OutputProtocol.Transport.OpenAsync(cancellationToken);
        }
        if (!_thriftclient.InputProtocol.Transport.IsOpen)
        {
            await _thriftclient.InputProtocol.Transport.OpenAsync(cancellationToken);
        }
        Logger().LogInformation("open");
    }


    public override async Task Close(CancellationToken cancellationToken = default)
    {
        if (_thriftclient.OutputProtocol.Transport.IsOpen) 
        {
            _thriftclient.OutputProtocol.Transport.Close();
        }
        if (_thriftclient.InputProtocol.Transport.IsOpen)
        {
            _thriftclient.InputProtocol.Transport.Close();
        }
        await Task.CompletedTask;
        Logger().LogInformation("close");
    }

    public override async ValueTask DisposeAsync()
    {
        await Close();
        _thriftclient.Dispose();
        Logger().LogInformation("Dispose");
    }
}
