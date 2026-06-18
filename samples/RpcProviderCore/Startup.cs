using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
//using RpcContractWcf.HelloService;
using System.Text;
using System.Text.RegularExpressions;

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



/// <summary>
/// 环境变量.
/// </summary>
public static class Env
{
    /// <summary>
    /// 系统环境变量-应用名称.
    /// </summary>
    public const string ServiceName = "APP_SERVICENAME";
    public const string ServiceInstanceId = "APP_SERVICEINSTANCEID";
    public const string ServiceNamespaceId = "APP_NAMESPACEID";

    public const string EnvTenantId = "APP_TENANTID";
    public const string EnvTenantKey = "APP_TENANTKEY";
    /// <summary>
    /// 系统环境变量-分组
    /// </summary>
    public const string ServiceGroupName = "APP_GROUPNAME";
    public const string EnvLayerName = "APP_LAYERNAME";
}

public static class EnvUtil
{
    static EnvUtil()
    {
        try
        {
            var hostIp = NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .Select(network => network.GetIPProperties())
                .OrderByDescending(properties => properties.GatewayAddresses.Count)
                .SelectMany(properties => properties.UnicastAddresses)
                .FirstOrDefault(address => !IPAddress.IsLoopback(address.Address) &&
                                           address.Address.AddressFamily == AddressFamily.InterNetwork);

            if (hostIp != null)
                HostIp = hostIp.Address.ToString();
        }
        catch
        {
            // ignored  
        }
    }
    public static string GetCurrentIp(string preferredNetworks)
    {
        var instanceIp = "";

        try
        {
            // 获取可用网卡
            var nics = NetworkInterface.GetAllNetworkInterfaces()?.Where(network => network.OperationalStatus == OperationalStatus.Up);

            // 获取所有可用网卡IP信息
            var ipCollection = nics?.Select(x => x.GetIPProperties())?.SelectMany(x => x.UnicastAddresses);

            var preferredNetworksArr = string.IsNullOrEmpty(preferredNetworks)
                ? Array.Empty<string>() : preferredNetworks.Split(",");
            if (ipCollection != null)
            {
                foreach (var ipadd in ipCollection)
                {
                    if (!IPAddress.IsLoopback(ipadd.Address) &&
                        ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (string.IsNullOrEmpty(preferredNetworks))
                        {
                            instanceIp = ipadd.Address.ToString();
                            break;
                        }

                        if (!preferredNetworksArr.Any(preferredNetwork =>
                                ipadd.Address.ToString().StartsWith(preferredNetwork)
                                || Regex.IsMatch(ipadd.Address.ToString(), preferredNetwork))) continue;
                        instanceIp = ipadd.Address.ToString();
                        break;
                    }
                }
            }
        }
        catch
        {
            // ignored
        }

        return instanceIp;
    }

    public static string HostIp { get; } = "127.0.0.1";
    public static string GetNamespaceId(string? defaultVal = null)
    {
        var result = Environment.GetEnvironmentVariable(Env.ServiceNamespaceId);
        if (string.IsNullOrWhiteSpace(result))
        {
            if (!string.IsNullOrWhiteSpace(defaultVal))
            {
                result = defaultVal;
            }
            else
            {
                result = "public";
            }

            Environment.SetEnvironmentVariable(Env.ServiceNamespaceId, result);
        }

        return result!;
    }
    public static string GetGroupName(string? defaultVal = null)
    {
        var result = Environment.GetEnvironmentVariable(Env.ServiceGroupName);
        if (string.IsNullOrWhiteSpace(result))
        {
            if (!string.IsNullOrWhiteSpace(defaultVal))
            {
                result = defaultVal;
            }
            else
            {
                result = "default_group";
            }

            Environment.SetEnvironmentVariable(Env.ServiceGroupName, result);
        }

        return result!;
    }
    public static string GetServiceName(string? defaultVal = null)
    {
        var result = Environment.GetEnvironmentVariable(Env.ServiceName);

        if (string.IsNullOrWhiteSpace(result))
        {
            if (!string.IsNullOrWhiteSpace(defaultVal))
            {
                result = defaultVal;
            }
            else
            {
                result = AppDomain.CurrentDomain.FriendlyName;
            }

            Environment.SetEnvironmentVariable(Env.ServiceName, result);
        }

        return result!;
    }


    /// <summary>
    /// 生成唯一实例名称：机器名 + 短哈希码
    /// </summary>
    /// <returns>格式如 "DESKTOP-ABC123-7F3A92"</returns>
    public static string Generate(int hashLength = 10)
    {
        long timestamp = DateTime.Now.Ticks;
        byte[] randomBytes = RandomNumberGenerator.GetBytes(16);
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);
        byte[] combinedBytes = new byte[timestampBytes.Length + randomBytes.Length];
        Buffer.BlockCopy(timestampBytes, 0, combinedBytes, 0, timestampBytes.Length);
        Buffer.BlockCopy(randomBytes, 0, combinedBytes, timestampBytes.Length, randomBytes.Length);

        byte[] hashBytes = SHA256.HashData(combinedBytes);

        var shortHash = new StringBuilder();
        for (int i = 0; i < hashLength; i++)
        {
            shortHash.Append(hashBytes[i].ToString("X2"));
            if (shortHash.Length >= hashLength)
                break;
        }

        string hashStr = shortHash.ToString().Substring(0, hashLength);
        string machineName = Environment.MachineName;

        return $"{machineName}-{hashStr}@{HostIp}";
    }

    /// <summary>
    /// 基于类型获取TraceSource
    /// </summary>
    /// <returns></returns>
    public static (string, string, string) GetTraceSource()
    {
        //var version = type.Assembly.GetName().Version?.ToString()??"1.0.0";
        var namespaceId = Environment.GetEnvironmentVariable(Env.ServiceNamespaceId) ?? "public";
        var serviceName = Environment.GetEnvironmentVariable(Env.ServiceName) ?? AppDomain.CurrentDomain.FriendlyName;
        //public$$serviceName
        var trace_source = $"{namespaceId}$${serviceName}";
        return (trace_source, namespaceId, serviceName);
    }

    public static ActivitySource CreateActivitySource(this Type type)
    {
        var (trace_source, namespaceId, serviceName) = GetTraceSource();
        var version = type.Assembly.GetName().Version?.ToString();
        var result = new ActivitySource(trace_source, version);
        return result;
    }
}