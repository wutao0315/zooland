using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Zooyard.Core;

namespace Zooyard.Rpc.Extensions
{
    public static class ServiceBuilderExtensions
    {
        public static void AddRpc(this IServiceCollection services)
        {
            services.AddHostedService<ZoolandHostedService>();
        }
    }
}
