using Grpc.Net.Client;
using MemberGrpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zooyard;
using Zooyard.DotNettyImpl;
using Zooyard.HttpImpl;
using static Grpc.Core.Metadata;
//using Zooyard.Extensions;
//using Zooyard.Rpc.AkkaImpl.Extensions;
//using Zooyard.GrpcImpl.Extensions;
//using Zooyard.HttpImpl.Extensions;
//using Zooyard.Rpc.NettyImpl.Extensions;
//using Zooyard.ThriftImpl.Extensions;
//using Zooyard.Rpc.WcfImpl.Extensions;

namespace RpcConsumerCore;

class Program
{
    static void Main(string[] args)
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("zooyard.grpc.json", false, true)
            .AddJsonFile("zooyard.netty.json", false, true)
            .AddJsonFile("zooyard.thrift.json", false, true)
            .AddJsonFile("zooyard.json", false, true)
            .AddJsonFile("nlog.json", false, true);

        var config = builder.Build();
        //ZooyardLogManager.UseConsoleLogging(Zooyard.Logging.LogLevel.Debug);

        IServiceCollection services = new ServiceCollection();
        //services.Configure<GrpcOption>(config.GetSection("grpc"));
        services.Configure<GrpcNetOption>(config.GetSection("grpc"));
        //services.Configure<NettyOption>(config.GetSection("netty"));
        //services.Configure<ThriftOption>(config.GetSection("thrift"));

        services.Configure<ZooyardOption>(config.GetSection("zooyard"));
        services.AddLogging();
        //services.AddZooyardGrpc();
        services.AddZooyardHttp();
        services.AddZooyardNetty();
        services.AddZooyardThrift();

        services.AddZooyardGrpcNet();
        services.AddMemoryCache();
        services.AddZoolandClient(
            typeof(RpcContractThrift.IHelloService)
            //, typeof(RpcContractGrpc.IHelloService)
            , typeof(RpcContractGrpcNet.IHelloNetService)
            , typeof(RpcContractHttp.IHelloService)
            , typeof(RpcContractNetty.IHelloService)
            , typeof(MemberGrpc.ISessionService)
            );

        using var bsp = services.BuildServiceProvider();
        var helloServiceThrift = bsp.GetRequiredService<RpcContractThrift.IHelloService>();
        //var helloServiceGrpc = bsp.GetRequiredService<RpcContractGrpc.IHelloService>();
        var helloServiceGrpcNet = bsp.GetRequiredService<RpcContractGrpcNet.IHelloNetService>();
        var helloServiceHttp = bsp.GetRequiredService<RpcContractHttp.IHelloService>();
        var helloServiceNetty = bsp.GetRequiredService<RpcContractNetty.IHelloService>();
        var sessionService = bsp.GetRequiredService<MemberGrpc.ISessionService>();

        while (true)
        {
            //Console.WriteLine("请选择:wcf | grpc | thrift | http | akka | netty | all");
            Console.WriteLine("请选择:grpcnet | grpc | grpcmember | thrift | http | netty | all");
            var mode = Console.ReadLine()?.ToLower()??"all";
            switch (mode)
            {
                //case "grpc":
                //    CallWhile(async (helloword) => { await GrpcHello(helloServiceGrpc, helloword); });
                //    break;
                case "grpcnet":
                    CallWhile(async (helloword) => { await GrpcNetHello(helloServiceGrpcNet, helloword); });
                    break;
                case "grpcmember":
                    CallWhile(async (helloword) => { await GrpcNetMember(sessionService, helloword); });
                    break;
                case "thrift":
                    CallWhile(async(helloword) => { await ThriftHello(helloServiceThrift, helloword); });
                    break;
                case "http":
                    CallWhile(async (helloword) => { await HttpHello(helloServiceHttp, helloword); });
                    break;
                case "netty":
                    CallWhile(async (helloword) => { await NettyHello(helloServiceNetty, helloword); });
                    break;
                case "all":
                    for (int i = 0; i < 3; i++)
                    {
                        //Task.Run(() =>
                        //{
                        //    try
                        //    {
                        //        WcfHello(helloServiceWcf);
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        throw ex;
                        //    }
                        //});
                        //Task.Run(async () =>
                        //{
                        //    try
                        //    {
                        //        await GrpcHello(helloServiceGrpc);
                        //    }
                        //    catch
                        //    {
                        //        throw;
                        //    }

                        //});
                        Task.Run(async () =>
                        {
                            try
                            {
                                await GrpcNetHello(helloServiceGrpcNet);
                            }
                            catch
                            {
                                throw;
                            }

                        });
                        Task.Run(async () =>
                        {
                            try
                            {
                                await ThriftHello(helloServiceThrift);
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                        });
                        Task.Run(async () =>
                        {
                            try
                            {
                                await HttpHello(helloServiceHttp);
                            }
                            catch
                            {

                                throw;
                            }

                        });
                        //Task.Run(() =>
                        //{

                        //    try
                        //    {
                        //        AkkaHello(helloServiceAkka);
                        //    }
                        //    catch (Exception ex)
                        //    {

                        //        throw ex;
                        //    }
                        //});

                        Task.Run(async () =>
                        {
                            try
                            {
                                await NettyHello(helloServiceNetty);
                            }
                            catch
                            {

                                throw;
                            }
                        });
                    }
                    break;
            }

            if (mode == "end")
            {
                break;
            }
        }



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

    //private static async Task GrpcHello(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
    //{
    //    Console.WriteLine("GrpcHello---------------------------------------------------------------------------");
    //    var callNameVoid = await helloServiceGrpc.CallNameVoid(new RpcContractGrpc.Void());
    //    Console.WriteLine(callNameVoid);
    //    await helloServiceGrpc.CallName(new RpcContractGrpc.NameResult { Name = helloword });
    //    Console.WriteLine("CallName called");
    //    await helloServiceGrpc.CallVoid(new RpcContractGrpc.Void());
    //    Console.WriteLine("CallVoid called");
    //    var hello = await helloServiceGrpc.Hello(new RpcContractGrpc.NameResult { Name = helloword });
    //    Console.WriteLine(hello.Name);
    //    var helloResult = await helloServiceGrpc.SayHello(new RpcContractGrpc.NameResult { Name = $"{helloword} perfect world" });
    //    Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
    //    helloResult.Name = helloword + " show perfect world";
    //    var showResult = await helloServiceGrpc.ShowHello(helloResult);
    //    Console.WriteLine(showResult.Name);
    //}

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

        Console.WriteLine(all.ToJsonString("{}"));
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
        await helloServiceHttp.CallName(helloword);
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
    private static void CallWhile(Action<string> map)
    {
        var helloword = "world";
        while (true)
        {
            try
            {
                map(helloword);
                var mode = Console.ReadLine();
                helloword = mode??"hello";
                if (helloword == "end")
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
