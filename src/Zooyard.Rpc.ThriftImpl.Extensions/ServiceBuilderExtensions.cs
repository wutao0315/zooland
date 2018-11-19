using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Thrift.Protocols;
using Thrift.Transports.Client;
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
    

    public static class ServiceBuilderExtensions
    {
        public static void AddThrift(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptions<ThriftOption>>()?.Value;

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
                    thriftClientTypes : thriftClientTypes
                );

                return pool;
            });

        }
    }
}
