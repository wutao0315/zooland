using Microsoft.Extensions.DependencyInjection;
using Zooyard.Rpc.HttpImpl;

namespace Microsoft.Extensions.Configuration;

public class HttpServerOption
{
    public IEnumerable<string>  Urls { get; set; }
}

public static class ServiceBuilderExtensions
{
    public static void AddHttpImpl(this IServiceCollection services)
    {
        services.AddSingleton<HttpClientPool>();
    }

    //public static void AddHttpServer<Startup>(this IServiceCollection services, string[] args)
    //    where Startup : class
    //{
    //    services.AddTransient((serviceProvider) =>
    //    {
    //        var option = serviceProvider.GetRequiredService<IOptionsMonitor<HttpServerOption>>().CurrentValue;
    //        var host = new WebHostBuilder()
    //        .UseKestrel()
    //        .UseContentRoot(Directory.GetCurrentDirectory())
    //        .UseIISIntegration()
    //        .UseStartup<Startup>()
    //        .UseUrls(option.Urls.ToArray())
    //        .Build();

    //        return host;
    //    });

    //    //services.AddTransient<IServer, HttpServer>();
    //}
}
