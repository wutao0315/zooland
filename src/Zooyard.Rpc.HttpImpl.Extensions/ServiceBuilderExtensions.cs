using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Zooyard.Core;
using Zooyard.Rpc.Cache;
using Zooyard.Rpc.Cluster;
using Zooyard.Rpc.LoadBalance;

namespace Zooyard.Rpc.HttpImpl.Extensions
{
    public static class ServiceBuilderExtensions
    {
        public static void AddHttpClient(this IServiceCollection services)
        {
            services.AddSingleton<HttpClientPool>();
        }

        public static void AddHttpServer<Startup>(this IServiceCollection services, string[] args)
            where Startup : class
        {
            services.AddSingleton(WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build());
            services.AddSingleton<IServer, HttpServer>();
        }
    }
}
