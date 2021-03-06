﻿using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.GrpcImpl.Extensions
{
    public class GrpcOption
    {
        public IDictionary<string, string> Credentials { get; set; }
        public IDictionary<string, string> Clients { get; set; }
    }

    public class GrpcServerOption
    {
        public IDictionary<string,string> Services { get; set; }
        public IEnumerable<ServerPortOption> ServerPorts { get; set; }
    }
    public class ServerPortOption
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Credentials { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddGrpcClient(this IServiceCollection services)
        {
            services.AddSingleton((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptionsMonitor<GrpcOption>>().CurrentValue;

                var credentials = new Dictionary<string, ChannelCredentials>
                {
                    { "Insecure", ChannelCredentials.Insecure}
                };

                if (option.Credentials?.Count>0)
                {
                    foreach (var item in option.Credentials)
                    {
                        var credential = serviceProvder.GetService(Type.GetType(item.Value)) as ChannelCredentials;
                        credentials.Add(item.Key, credential);
                    }
                }

                var grpcClientTypes = new Dictionary<string, Type>();
                foreach (var item in option.Clients)
                {
                    grpcClientTypes.Add(item.Key, Type.GetType(item.Value));
                }

                var interceptors = serviceProvder.GetServices<ClientInterceptor>();

                var pool = new GrpcClientPool(
                    credentials:credentials,
                    grpcClientTypes:grpcClientTypes, 
                    interceptors: interceptors
                );

                return pool;
            });

        }

        public static void AddGrpcServer(this IServiceCollection services)
        {
            services.AddTransient<IEnumerable<ServerServiceDefinition>>((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptionsMonitor<GrpcServerOption>>().CurrentValue;
                var result = new List<ServerServiceDefinition>();

                foreach (var item in option.Services)
                {
                    var contractType = Type.GetType(item.Key);
                    var implType = Type.GetType(item.Value);
                    var implValue = serviceProvder.GetService(implType);
                    var definition = contractType.GetMethod("BindService",new[] { implType })
                    .Invoke(null, new[] { implValue }) as ServerServiceDefinition;
                    result.Add(definition);
                }

                return result;
            });

            services.AddTransient<IEnumerable<ServerPort>>((serviceProvder) => 
            {
                var option = serviceProvder.GetService<IOptionsMonitor<GrpcServerOption>>().CurrentValue;
                var result = new List<ServerPort>();
                foreach (var item in option.ServerPorts)
                {
                    var defaultCredential = ServerCredentials.Insecure;
                    if (!string.IsNullOrWhiteSpace(item.Credentials) 
                    && item.Credentials!="default"
                    && item.Credentials!= "Insecure"
                    ) {
                        var credentialType = Type.GetType(item.Credentials);
                        defaultCredential = serviceProvder.GetService(credentialType) as ServerCredentials;
                    }
                    var port = new ServerPort(item.Host,item.Port, defaultCredential);
                    result.Add(port);
                }
                return result;
            });
            services.AddTransient<IServer, GrpcServer>();
        }
    }
}
