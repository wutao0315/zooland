using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;

namespace Zooyard.SignalRImpl;

public class SignalRClientPool(ILoggerFactory _loggerFactory) : AbstractClientPool(_loggerFactory.CreateLogger<SignalRClientPool>())
{
    public const string AUTH_NAME = "auth_name";
    public const string AUTH_VALUE = "auth_value";
    public const string PROTOCOL_KEY = "protocol";


    protected override async Task<IClient> CreateClient(URL url)
    {
       
        var authNames = url.GetParameter(AUTH_NAME, Array.Empty<string>());
        var authValues = url.GetParameter(AUTH_VALUE, Array.Empty<string>());

        var protocol = url.GetParameter(PROTOCOL_KEY, "").ToLower();

        var builder = new HubConnectionBuilder()
            .WithUrl(new Uri($"{url.Protocol}://{url.Host}:{url.Port}/{url.Path}"), options => 
            {
                for (int i = 0; i < authNames.Length; i++)
                {
                    var authValue = authValues.ElementAtOrDefault(i)??"";
                    options.Headers[authNames[i]] = authValue;
                }
            })
            .WithAutomaticReconnect();

        if (!string.IsNullOrWhiteSpace(protocol) || protocol != "json")
        {
            switch (protocol)
            {
                case "newtonsoftjson":
                    builder.AddNewtonsoftJsonProtocol();
                    break;
                case "messagepack":
                    builder.AddMessagePackProtocol();
                    break;
                default:throw new NotSupportedException($"protocol {protocol} not support");
            }
        }

        var connection = builder.Build();

        await connection.StartAsync();

        return new SignalRClientImpl(_loggerFactory.CreateLogger<SignalRClientImpl>(), connection, url);
    }
}
