using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zooyard;
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
        services.Configure<ThriftOption>(config.GetSection("thrift"));
        services.Configure<ZooyardOption>(config.GetSection("zooyard"));
        services.AddLogging();
        services.AddZooyardGrpc();
        services.AddZooyardHttp();
        //services.AddNettyImpl();

        services.AddZooyardThrift();
        services.AddZoolandClient(config);

        using var bsp = services.BuildServiceProvider();
        var helloServiceThrift = bsp.GetRequiredService<RpcContractThrift.IHelloService>();
        var helloServiceGrpc = bsp.GetRequiredService<RpcContractGrpc.IHelloService>();
        var helloServiceHttp = bsp.GetRequiredService<RpcContractHttp.IHelloService>();
        var helloServiceNetty = bsp.GetRequiredService<RpcContractNetty.IHelloService>();

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
        var callNameVoid = await helloServiceThrift.CallNameVoidAsync();
        Console.WriteLine(callNameVoid);
        await helloServiceThrift.CallNameAsync(helloword);
        Console.WriteLine("CallName called");
        await helloServiceThrift.CallVoidAsync();
        Console.WriteLine("CallVoid called");
        var hello = await helloServiceThrift.HelloAsync(helloword);
        Console.WriteLine(hello);
        var helloResult = await helloServiceThrift.SayHelloAsync(helloword + "perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResult = await helloServiceThrift.ShowHelloAsync(helloResult);
        Console.WriteLine(showResult);

        ThriftHelloCall(helloServiceThrift, helloword);
    }

    private static void ThriftHelloCall(RpcContractThrift.IHelloService helloServiceThrift, string helloword = "world")
    {
        Console.WriteLine("ThriftHello---------------------------------------------------------------------------");
        var callNameVoid = helloServiceThrift.CallNameVoid();
        Console.WriteLine(callNameVoid);
        helloServiceThrift.CallName(helloword);
        Console.WriteLine("CallName called");
        helloServiceThrift.CallVoid();
        Console.WriteLine("CallVoid called");
        var hello = helloServiceThrift.Hello(helloword);
        Console.WriteLine(hello);
        var helloResult = helloServiceThrift.SayHello(helloword + "perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResult = helloServiceThrift.ShowHello(helloResult);
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

        GrpcHelloCall(helloServiceGrpc, helloword);
    }
    private static void GrpcHelloCall(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
    {
        Console.WriteLine("GrpcHelloCall---------------------------------------------------------------------------");
        var callNameVoid = helloServiceGrpc.CallNameVoid(new RpcContractGrpc.Void());
        Console.WriteLine(callNameVoid);
        helloServiceGrpc.CallName(new RpcContractGrpc.NameResult { Name = helloword });
        Console.WriteLine("CallName called");
        helloServiceGrpc.CallVoid(new RpcContractGrpc.Void());
        Console.WriteLine("CallVoid called");
        var hello = helloServiceGrpc.Hello(new RpcContractGrpc.NameResult { Name = helloword });
        Console.WriteLine(hello.Name);
        var helloResult = helloServiceGrpc.SayHello(new RpcContractGrpc.NameResult { Name = $"{helloword} perfect world" });
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + " show perfect world";
        var showResult = helloServiceGrpc.ShowHello(helloResult);
        Console.WriteLine(showResult.Name);
    }
    //private static void WcfHello(RpcContractWcf.IHelloService helloServiceWcf, string helloword = "world")
    //{
    //    var callNameVoid = helloServiceWcf.CallNameVoid();
    //    Console.WriteLine(callNameVoid);
    //    helloServiceWcf.CallName(helloword);
    //    Console.WriteLine("CallName called");
    //    helloServiceWcf.CallVoid();
    //    Console.WriteLine("CallVoid called");
    //    var helloWcf = helloServiceWcf.Hello(helloword);
    //    Console.WriteLine(helloWcf);
    //    var helloResultWcf = helloServiceWcf.SayHello($"{helloword} perfect world");
    //    Console.WriteLine($"{helloResultWcf.Name},{helloResultWcf.Gender},{helloResultWcf.Head}");
    //    helloResultWcf.Name = helloword + "show perfect world";
    //    var showResultWcf = helloServiceWcf.ShowHello(helloResultWcf);
    //    Console.WriteLine(showResultWcf);
    //}
    private static async Task HttpHello(RpcContractHttp.IHelloService helloServiceHttp, string helloword = "world")
    {
        Console.WriteLine("HttpHello---------------------------------------------------------------------------");
        var callNameVoid = await helloServiceHttp.CallNameVoidAsync();
        Console.WriteLine(callNameVoid);
        await helloServiceHttp.CallNameAsync(helloword);
        Console.WriteLine("CallName called");
        await helloServiceHttp.CallVoidAsync();
        Console.WriteLine("CallVoid called");
        var helloWcf = await helloServiceHttp.HelloAsync(helloword);
        Console.WriteLine(helloWcf);
        var helloResultWcf = await helloServiceHttp.SayHelloAsync($"{helloword} perfect world");
        Console.WriteLine($"{helloResultWcf.Name},{helloResultWcf.Gender},{helloResultWcf.Head}");
        helloResultWcf.Name = helloword + "show perfect world";
        var showResultWcf = await helloServiceHttp.ShowHelloAsync(helloResultWcf);
        Console.WriteLine(showResultWcf);

        HttpHelloCall(helloServiceHttp, helloword);
    }
    private static void HttpHelloCall(RpcContractHttp.IHelloService helloServiceHttp, string helloword = "world")
    {
        Console.WriteLine("HttpHelloCall---------------------------------------------------------------------------");
        var callNameVoid = helloServiceHttp.CallNameVoid();
        Console.WriteLine(callNameVoid);
        helloServiceHttp.CallName(helloword);
        Console.WriteLine("CallName called");
        helloServiceHttp.CallVoid();
        Console.WriteLine("CallVoid called");
        var helloWcf = helloServiceHttp.Hello(helloword);
        Console.WriteLine(helloWcf);
        var helloResultWcf = helloServiceHttp.SayHello($"{helloword} perfect world");
        Console.WriteLine($"{helloResultWcf.Name},{helloResultWcf.Gender},{helloResultWcf.Head}");
        helloResultWcf.Name = helloword + "show perfect world";
        var showResultWcf = helloServiceHttp.ShowHello(helloResultWcf);
        Console.WriteLine(showResultWcf);
    }
    //private static void AkkaHello(RpcContractAkka.IHelloService akkaServiceHttp, string helloword = "world")
    //{
    //    var callNameVoid = akkaServiceHttp.CallNameVoid();
    //    Console.WriteLine(callNameVoid);
    //    akkaServiceHttp.CallName(new RpcContractAkka.NameResult { Name = helloword });
    //    Console.WriteLine("CallName called");
    //    akkaServiceHttp.CallVoid();
    //    Console.WriteLine("CallVoid called");
    //    var hello = akkaServiceHttp.Hello(new RpcContractAkka.NameResult { Name = helloword });
    //    Console.WriteLine(hello.Name);
    //    var helloResult = akkaServiceHttp.SayHello(new RpcContractAkka.NameResult { Name = $"{helloword} perfect world" });
    //    Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
    //    helloResult.Name = helloword + "show perfect world";
    //    var showResultWcf = akkaServiceHttp.ShowHello(helloResult);
    //    Console.WriteLine(showResultWcf.Name);
    //}
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

        NettyHelloCall(nettyService, helloword);
    }

    private static void NettyHelloCall(RpcContractNetty.IHelloService nettyService, string helloword = "world")
    {
        Console.WriteLine("NettyHello---------------------------------------------------------------------------");
        var callNameVoid = nettyService.CallNameVoid();
        Console.WriteLine(callNameVoid);
        nettyService.CallName(helloword);
        Console.WriteLine("CallName called");
        nettyService.CallVoid();
        Console.WriteLine("CallVoid called");
        var hello = nettyService.Hello(helloword);
        Console.WriteLine(hello);
        var helloResult = nettyService.SayHello($"{helloword} perfect world");
        Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
        helloResult.Name = helloword + "show perfect world";
        var showResultNetty = nettyService.ShowHello(helloResult);
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
                var mode = Console.ReadLine().ToLower();
                helloword = mode;
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
