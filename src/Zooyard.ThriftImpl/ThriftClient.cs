using Microsoft.Extensions.Logging;
using Thrift;
using Zooyard.Rpc.Support;

namespace Zooyard.ThriftImpl;

public class ThriftClient : AbstractClient
{
    //private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(ThriftClient));
    public override string System => "zy_thrift";
    public override URL Url { get; }
    public override int ClientTimeout { get; }
    private readonly ILogger _logger;
    private readonly TBaseClient _thriftclient;

    public ThriftClient(ILogger logger, TBaseClient thriftclient, int clientTimeout, URL url)
    {
        _logger = logger;
        _thriftclient = thriftclient;
        this.ClientTimeout = clientTimeout;
        this.Url = url;
    }


    public override async Task<IInvoker> Refer(CancellationToken cancellationToken = default)
    {
        await this.Open(cancellationToken);
        //thrift client service
        return new ThriftInvoker(_logger, _thriftclient, this.ClientTimeout);
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
        _logger.LogInformation("open");
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
        _logger.LogInformation("close");
    }

    public override async ValueTask DisposeAsync()
    {
        await Close();
        _thriftclient.Dispose();
        _logger.LogInformation("Dispose");
    }
}
