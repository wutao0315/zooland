using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Thrift.Protocols;
using Thrift.Server;
using Thrift.Transports.Client;
using Zooyard.Core;

namespace Zooyard.Rpc.ThriftImpl.Extensions
{
    public class ThriftOption
    {
        public IDictionary<string,string> Clients { get; set; }
    }
    
    public static class ServiceBuilderExtensions
    {
        public static void AddThriftClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptionsMonitor<ThriftOption>>().CurrentValue;
                var loggerFactory = serviceProvder.GetService<ILoggerFactory>();
                var thriftClientTypes = new Dictionary<string, Type>();
                foreach (var item in option.Clients)
                {
                    thriftClientTypes.Add(item.Key, Type.GetType(item.Value));
                }

                var pool = new ThriftClientPool(
                    transportTypes : new Dictionary<string, Type>
                        {
                            {"TSocket", typeof(TSocketClientTransport)},
                            {"TBuffered", typeof(TBufferedClientTransport)},
                            {"TFramed", typeof(TFramedClientTransport)},
                            {"THttp", typeof(THttpClientTransport)},
                            {"TMemoryBuffer", typeof(TMemoryBufferClientTransport)},
                            {"TNamedPipe", typeof(TNamedPipeClientTransport)},
                            {"TTLSSocket", typeof(TTlsSocketClientTransport)},
                            {"TStream", typeof(TStreamClientTransport)},
                        },
                    protocolTypes: new Dictionary<string, Type>
                        {
                            { "TBinaryProtocol",typeof(TBinaryProtocol)},
                            { "TCompactProtocol",typeof(TCompactProtocol)},
                            { "TJSONProtocol",typeof(TJsonProtocol)},
                            { "TMultiplexedProtocol",typeof(TMultiplexedProtocol)},
                        },
                    clientTypes: thriftClientTypes
                );

                return pool;
            });

        }

        public static void AddThriftServer(this IServiceCollection services)
        {
            services.AddSingleton<TBaseServer, AsyncBaseServer>();
            services.AddSingleton<IServer, ThriftServer>();
        }
    }
}
