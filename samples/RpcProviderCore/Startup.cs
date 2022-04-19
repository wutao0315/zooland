using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
//using RpcContractWcf.HelloService;
using System.Text;

namespace RpcProviderCore;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHealthChecks();
        // iis
        services.Configure<IISOptions>(iis =>
        {
            iis.AuthenticationDisplayName = "Windows";
            iis.AutomaticAuthentication = false;
        });
        //Api version
        //services.AddApiVersioning(option => {
        //    option.ReportApiVersions = true;
        //    //option.ApiVersionReader = new HeaderApiVersionReader("api-version");
        //    option.ApiVersionReader = new QueryStringApiVersionReader(parameterName: "version");
        //    option.AssumeDefaultVersionWhenUnspecified = true;
        //    option.DefaultApiVersion = new ApiVersion(1, 0);
        //});

        services.AddLogging();

        services.AddScoped<IHelloRepository, HelloRepository>();

        
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("Everything");

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

       
        //app.UseSoapEndpoint<IHelloServiceWcf>("/Hello/HelloServiceWcfImpl", new BasicHttpBinding(), SoapSerializer.DataContractSerializer);

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }
}
