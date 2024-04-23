using MemberGrpc;
using RpcContractHttp;

namespace RpcConsumerCore;

class Program
{
    static async Task Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
   Host.CreateDefaultBuilder(args)
       .ConfigureAppConfiguration((hostingContext, config) => {
           hostingContext.HostingEnvironment.ApplicationName = "RpcConsumerCore";
           hostingContext.HostingEnvironment.ContentRootPath = Directory.GetCurrentDirectory();
           var env = hostingContext.HostingEnvironment;
       })
       .ConfigureServices((hostingContext, services) =>
       {
           var env = hostingContext.HostingEnvironment;

           var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Config");
           var builder = new ConfigurationBuilder()
               .SetBasePath(basePath)
               //.AddJsonFile("service.akka.json", false, true)
               .AddJsonFile("service.grpc.json", true, true)
               .AddJsonFile("service.netty.json", true, true)
               .AddJsonFile("service.thrift.json", true, true)
               .AddJsonFile("service.http.json", true, true)
               .AddJsonFile("service.json", true, true)
               .AddJsonFile("nlog.json", true, true);

           var config = builder.Build();

           services.AddLogging();


           services.Configure<GrpcNetOption>(config.GetSection("grpc"));
           services.AddLogging();

           services.AddMemoryCache();
           services.AddRpc()
               .LoadFromConfig(config.GetSection("zooyard"))
               .AddHttp()
               //.AddDotNetty()
               //.AddThrift()
               //.AddGrpcNet()
               .AddContract<RpcContractHttp.IHelloService>()
               //.AddContracts(
               ////typeof(RpcContractThrift.IHelloService)
               ////, typeof(RpcContractGrpcNet.IHelloNetService)
               ////, 
               //typeof(RpcContractHttp.IHelloService)
               ////, typeof(RpcContractNetty.IHelloService)
               ////, typeof(MemberGrpc.ISessionService)
               //)
               ;

           services.AddSingleton<IHelloService, RpcContractHttp.HelloServiceClientTest>();
           //services.AddSingleton<IHelloService, RpcContractHttp.HelloServiceClient>();

           //using var bsp = services.BuildServiceProvider();
           //var helloServiceThrift = bsp.GetRequiredService<RpcContractThrift.IHelloService>();
           //var helloServiceGrpcNet = bsp.GetRequiredService<RpcContractGrpcNet.IHelloNetService>();
           //var helloServiceHttp = bsp.GetRequiredService<RpcContractHttp.IHelloService>();
           //var helloServiceNetty = bsp.GetRequiredService<RpcContractNetty.IHelloService>();
           //var sessionService = bsp.GetRequiredService<MemberGrpc.ISessionService>();

           services.AddHostedService<TestHostedService>();
           

       })
       .ConfigureWebHostDefaults(webBuilder =>
       {
           // webBuilder.UseStartup<Startup>();
       });

   
}

public class TestHostedService(RpcContractHttp.IHelloService helloServiceHttp) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                //Console.WriteLine("请选择:wcf | grpc | thrift | http | akka | netty | all");
                Console.WriteLine("请选择:grpcnet | grpcmember | thrift | http | netty | all");
                var mode = Console.ReadLine()?.ToLower() ?? "all";
                switch (mode)
                {
                    //case "grpcnet":
                    //    await CallWhile(async (helloword) => { await GrpcNetHello(helloServiceGrpcNet, helloword); });
                    //    break;
                    //case "grpcmember":
                    //    await CallWhile(async (helloword) => { await GrpcNetMember(sessionService, helloword); });
                    //    break;
                    //case "thrift":
                    //    await CallWhile(async (helloword) => { await ThriftHello(helloServiceThrift, helloword); });
                    //    break;
                    case "http":
                        helloServiceHttp.CallName("test");

                        await CallWhile(async (helloword) => { await HttpHello(helloServiceHttp, helloword); });

                        var callNameVoid = await helloServiceHttp.CallNameVoid();
                        Console.WriteLine(callNameVoid);
                        break;
                    //case "netty":
                    //    await CallWhile(async (helloword) => { await NettyHello(helloServiceNetty, helloword); });
                    //    break;
                    case "all":
                        //for (int i = 0; i < 3; i++)
                        //{
                        //    _=Task.Run(async () =>
                        //    {
                        //        try
                        //        {
                        //            await GrpcNetHello(helloServiceGrpcNet);
                        //        }
                        //        catch
                        //        {
                        //            throw;
                        //        }

                        //    });
                        //    _ = Task.Run(async () =>
                        //    {
                        //        try
                        //        {
                        //            await ThriftHello(helloServiceThrift);
                        //        }
                        //        catch (Exception)
                        //        {
                        //            throw;
                        //        }

                        //    });
                        //    _ = Task.Run(async () =>
                        //    {
                        //        try
                        //        {
                        //            await HttpHello(helloServiceHttp);
                        //        }
                        //        catch
                        //        {

                        //            throw;
                        //        }

                        //    });
                        //    //Task.Run(() =>
                        //    //{

                        //    //    try
                        //    //    {
                        //    //        AkkaHello(helloServiceAkka);
                        //    //    }
                        //    //    catch (Exception ex)
                        //    //    {

                        //    //        throw ex;
                        //    //    }
                        //    //});

                        //    _ = Task.Run(async () =>
                        //    {
                        //        try
                        //        {
                        //            await NettyHello(helloServiceNetty);
                        //        }
                        //        catch
                        //        {

                        //            throw;
                        //        }
                        //    });
                        //}
                        break;
                }

                if (mode == "end")
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ex" + e.Message);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
     
    }

    private static async Task ThriftHello(RpcContractThrift.IHelloService helloServiceThrift, string helloword = "world")
    {
        Console.WriteLine("ThriftHello---------------------------------------------------------------------------");
        var callNameVoid = await helloServiceThrift.CallNameVoid();
        Console.WriteLine(callNameVoid);
        await helloServiceThrift.CallName(helloword);
        Console.WriteLine("CallName called");
        await helloServiceThrift.CallVoid();
        Console.WriteLine("CallVoid called");
        var hello = await helloServiceThrift.Hello(helloword);
        Console.WriteLine(hello);
        var helloResult = await helloServiceThrift.SayHello(helloword + "perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResult = await helloServiceThrift.ShowHello(helloResult);
        Console.WriteLine(showResult);
    }

    private static async Task GrpcNetMember(ISessionService helloServiceGrpc, string helloword = "world")
    {
        Console.WriteLine("GrpcNetMember---------------------------------------------------------------------------");

        var entity = new PrmSessionEntity
        {
            App = "test",
            Remark = "[\r\n  \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0\"\r\n]",
            UserHostAddress = "0.0.0.1:50667",
            AppId = 3000000000,
            OrgId = 2000000000,
            ExtData = "test",
            NowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var req = new MemberLoginRequest
        {
            UserName = "admin",
            Pwd = "123456",
            RememberMe = true,
            UserType = 0,
            SessionEntity = entity
        };
        //var r = await client.MemberLoginAsync(req);

        var response = await helloServiceGrpc.MemberLogin(req);
        Console.WriteLine($"{response.Code}:{response.Msg}");
        if (response.Code != 0)
        {
            throw new Exception(response.Msg);
        }

        var all = response.Data.Unpack<PrmAllSession>();

        Console.WriteLine(all);
    }

    private static async Task GrpcNetHello(RpcContractGrpcNet.IHelloNetService helloServiceGrpc, string helloword = "world")
    {
        Console.WriteLine("GrpcNetHello---------------------------------------------------------------------------");
        var callNameVoid = await helloServiceGrpc.CallNameVoid(new RpcContractGrpcNet.Void());
        Console.WriteLine(callNameVoid);
        await helloServiceGrpc.CallName(new RpcContractGrpcNet.NameResult { Name = helloword });
        Console.WriteLine("Net CallName called");
        await helloServiceGrpc.CallVoid(new RpcContractGrpcNet.Void());
        Console.WriteLine("Net CallVoid called");
        var hello = await helloServiceGrpc.Hello(new RpcContractGrpcNet.NameResult { Name = helloword });
        Console.WriteLine(hello.Name);
        var helloResult = await helloServiceGrpc.SayHello(new RpcContractGrpcNet.NameResult { Name = $"{helloword} perfect world" });
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + " show perfect world";
        var showResult = await helloServiceGrpc.ShowHello(helloResult);
        Console.WriteLine(showResult.Name);
    }

    private static async Task HttpHello(RpcContractHttp.IHelloService helloServiceHttp, string helloword = "world")
    {
        Console.WriteLine("HttpHello---------------------------------------------------------------------------");
        var callNameVoid = await helloServiceHttp.CallNameVoid();
        Console.WriteLine(callNameVoid);
        helloServiceHttp.CallName(helloword);
        Console.WriteLine("CallName called");
        await helloServiceHttp.CallVoid();
        Console.WriteLine("CallVoid called");
        var hello = await helloServiceHttp.Hello(helloword);
        Console.WriteLine(hello);
        var helloResult = await helloServiceHttp.SayHello($"{helloword} perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResult = await helloServiceHttp.ShowHello(helloResult);
        Console.WriteLine(showResult);
    }
    private static async Task NettyHello(RpcContractNetty.IHelloService nettyService, string helloword = "world")
    {
        Console.WriteLine("NettyHello---------------------------------------------------------------------------");
        var callNameVoid = await nettyService.CallNameVoidAsync();
        Console.WriteLine(callNameVoid);
        await nettyService.CallNameAsync(helloword);
        Console.WriteLine("CallName called");
        await nettyService.CallVoidAsync();
        Console.WriteLine("CallVoid called");
        var hello = await nettyService.HelloAsync(helloword);
        Console.WriteLine(hello);
        var helloResult = await nettyService.SayHelloAsync($"{helloword} perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResultNetty = await nettyService.ShowHelloAsync(helloResult);
        Console.WriteLine(showResultNetty);
    }
    private static async Task CallWhile(Func<string, Task> map)
    {
        var helloword = "world";
        while (true)
        {
            try
            {
                await map(helloword);
                var mode = Console.ReadLine();
                helloword = mode ?? "hello";
                if (helloword == "end")
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }
    }
}