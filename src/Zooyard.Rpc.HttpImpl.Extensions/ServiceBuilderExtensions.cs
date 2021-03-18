using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zooyard.Core;

namespace Zooyard.Rpc.HttpImpl.Extensions
{
    public class HttpServerOption
    {
        public IEnumerable<string>  Urls { get; set; }
    }

    public static class ServiceBuilderExtensions
    {
        public static void AddHttpClient(this IServiceCollection services)
        {
            services.AddSingleton<HttpClientPool>();
        }


        public static void AddHttpServer<Startup>(this IServiceCollection services, string[] args)
            where Startup : class
        {
            services.AddTransient((serviceProvider) =>
            {
                var option = serviceProvider.GetService<IOptionsMonitor<HttpServerOption>>().CurrentValue;
                var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls(option.Urls.ToArray())
                .Build();

                return host;
            });

            services.AddTransient<IServer, HttpServer>();
        }
    }
}
