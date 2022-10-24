using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zooyard;
using Zooyard.DotNettyImpl;
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
        services.Configure<GrpcOption>(config.GetSection("grpc"));
        //services.Configure<NettyOption>(config.GetSection("netty"));
        //services.Configure<ThriftOption>(config.GetSection("thrift"));
        services.Configure<ZooyardOption>(config.GetSection("zooyard"));
        services.AddLogging();
        services.AddZooyardGrpc();
        services.AddZooyardHttp();
        //services.AddZooyardNetty();
        services.AddZooyardThrift();
        services.AddMemoryCache();
        services.AddZoolandClient(typeof(RpcContractThrift.IHelloService)
            , typeof(RpcContractGrpc.IHelloService)
            , typeof(RpcContractHttp.IHelloService)
            //, typeof(RpcContractNetty.IHelloService)
            );

        using var bsp = services.BuildServiceProvider();
        var helloServiceThrift = bsp.GetRequiredService<RpcContractThrift.IHelloService>();
        var helloServiceGrpc = bsp.GetRequiredService<RpcContractGrpc.IHelloService>();
        var helloServiceHttp = bsp.GetRequiredService<RpcContractHttp.IHelloService>();
        //var helloServiceNetty = bsp.GetRequiredService<RpcContractNetty.IHelloService>();

        while (true)
        {
            //Console.WriteLine("请选择:wcf | grpc | thrift | http | akka | netty | all");
            Console.WriteLine("请选择:grpc | thrift | http | netty | all");
            var mode = Console.ReadLine()?.ToLower()??"all";
            switch (mode)
            {
                case "grpc":
                    CallWhile(async (helloword) => { await GrpcHello(helloServiceGrpc, helloword); });
                    break;
                case "thrift":
                    CallWhile(async(helloword) => { await ThriftHello(helloServiceThrift, helloword); });
                    break;
                case "http":
                    CallWhile(async (helloword) => { await HttpHello(helloServiceHttp, helloword); });
                    break;
                //case "netty":
                //    CallWhile(async (helloword) => { await NettyHello(helloServiceNetty, helloword); });
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
                        Task.Run(async () =>
                        {
                            try
                            {
                                await GrpcHello(helloServiceGrpc);
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

                        //Task.Run(async () =>
                        //{

                        //    try
                        //    {
                        //        await NettyHello(helloServiceNetty);
                        //    }
                        //    catch
                        //    {

                        //        throw;
                        //    }
                        //});
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

    private static async Task GrpcHello(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
    {
        Console.WriteLine("GrpcHello---------------------------------------------------------------------------");
        var callNameVoid = await helloServiceGrpc.CallNameVoidAsync(new RpcContractGrpc.Void());
        Console.WriteLine(callNameVoid);
        await helloServiceGrpc.CallNameAsync(new RpcContractGrpc.NameResult { Name = helloword });
        Console.WriteLine("CallName called");
        await helloServiceGrpc.CallVoidAsync(new RpcContractGrpc.Void());
        Console.WriteLine("CallVoid called");
        var hello = await helloServiceGrpc.HelloAsync(new RpcContractGrpc.NameResult { Name = helloword });
        Console.WriteLine(hello.Name);
        var helloResult = await helloServiceGrpc.SayHelloAsync(new RpcContractGrpc.NameResult { Name = $"{helloword} perfect world" });
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + " show perfect world";
        var showResult = await helloServiceGrpc.ShowHelloAsync(helloResult);
        Console.WriteLine(showResult.Name);

        //GrpcHelloCall(helloServiceGrpc, helloword);
    }
    //private static void GrpcHelloCall(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
    //{
    //    Console.WriteLine("GrpcHelloCall---------------------------------------------------------------------------");
    //    var callNameVoid = helloServiceGrpc.CallNameVoid(new RpcContractGrpc.Void());
    //    Console.WriteLine(callNameVoid);
    //    helloServiceGrpc.CallName(new RpcContractGrpc.NameResult { Name = helloword });
    //    Console.WriteLine("CallName called");
    //    helloServiceGrpc.CallVoid(new RpcContractGrpc.Void());
    //    Console.WriteLine("CallVoid called");
    //    var hello = helloServiceGrpc.Hello(new RpcContractGrpc.NameResult { Name = helloword });
    //    Console.WriteLine(hello.Name);
    //    var helloResult = helloServiceGrpc.SayHello(new RpcContractGrpc.NameResult { Name = $"{helloword} perfect world" });
    //    Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
    //    helloResult.Name = helloword + " show perfect world";
    //    var showResult = helloServiceGrpc.ShowHello(helloResult);
    //    Console.WriteLine(showResult.Name);
    //}

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
