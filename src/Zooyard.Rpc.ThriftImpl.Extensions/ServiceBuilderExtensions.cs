using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Thrift;
using Thrift.Protocols;
using Thrift.Server;
using Thrift.Transports;
using Thrift.Transports.Client;
using Thrift.Transports.Server;
using Zooyard.Core;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;

namespace Zooyard.Rpc.ThriftImpl.Extensions
{
    public class ThriftOption
    {
        public IDictionary<string,string> Clients { get; set; }
    }

    public class ThriftServerOption
    {
        public string ProtocolFactoryType { get; set; }
        public ThriftTransportOption Transport { get; set; }
    }
    public class ThriftTransportOption
    {
        public string TransportType { get; set; }
        public int Port { get; set; }
        public int ClientTimeOut { get; set; }
        public bool UserBufferedSockets { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddThriftClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptions<ThriftOption>>().Value;
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
                    thriftClientTypes : thriftClientTypes,
                    loggerFactory: loggerFactory
                );

                return pool;
            });

        }

        public static void AddThriftServer<ifacade, facade, processor>(this IServiceCollection services)
            where processor : class, ITAsyncProcessor
            where ifacade : class
            where facade : class,ifacade
        {
            services.AddTransient<ifacade, facade>();
            services.AddTransient<ITAsyncProcessor, processor>();

            services.AddSingleton((serviceProvider)=> 
            {
                var option = serviceProvider.GetService<IOptions<ThriftServerOption>>().Value;
                var transportType =Type.GetType(option.Transport.TransportType);
                var transport = transportType.GetConstructor(new[] {typeof(int), typeof(int), typeof(bool) })
                .Invoke(new object[] {option.Transport.Port,option.Transport.ClientTimeOut,option.Transport.UserBufferedSockets })
                 as TServerTransport;

                return transport;
            });

            services.AddSingleton((serviceProvider) => {
                var option = serviceProvider.GetService<IOptions<ThriftServerOption>>().Value;
                var factoryType = Type.GetType(option.ProtocolFactoryType);
                var factory = factoryType.GetConstructor(null).Invoke(null)
                 as ITProtocolFactory;
                return factory;
            });
            
            services.AddSingleton<AsyncBaseServer>();

        }
    }
}
