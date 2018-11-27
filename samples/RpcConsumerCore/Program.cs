using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zooyard.Core.Extensions;
using Zooyard.Rpc.AkkaImpl.Extensions;
using Zooyard.Rpc.GrpcImpl.Extensions;
using Zooyard.Rpc.HttpImpl.Extensions;
using Zooyard.Rpc.NettyImpl.Extensions;
using Zooyard.Rpc.ThriftImpl.Extensions;
using Zooyard.Rpc.WcfImpl.Extensions;

namespace RpcConsumerCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "/Config")
                .AddJsonFile("zooyard.akka.json", false, true)
                .AddJsonFile("zooyard.grpc.json", false, true)
                .AddJsonFile("zooyard.netty.json", false, true)
                .AddJsonFile("zooyard.thrift.json", false, true)
                .AddJsonFile("zooyard.wcf.json", false, true)
                .AddJsonFile("zooyard.json", false, true);

            var config = builder.Build();

            IServiceCollection services = new ServiceCollection();
            services.Configure<AkkaOption>(config.GetSection("akka"));
            services.Configure<GrpcOption>(config.GetSection("grpc"));
            services.Configure<NettyOption>(config.GetSection("netty"));
            services.Configure<ThriftOption>(config.GetSection("thrift"));
            services.Configure<WcfOption>(config.GetSection("wcf"));
            services.Configure<ZoolandOption>(config.GetSection("zooyard"));
            services.AddLogging();
            services.AddAkkaClient();
            services.AddGrpcClient();
            services.AddHttpClient();
            services.AddNettyClient();

            services.AddThriftClient();
            services.AddWcfClient();
            services.AddZoolandClient(config);

            using (var bsp = services.BuildServiceProvider())
            {
                var helloServiceThrift = bsp.GetService<RpcContractThrift.IHelloService>();
                var helloServiceGrpc = bsp.GetService<RpcContractGrpc.IHelloService>();
                var helloServiceWcf = bsp.GetService<RpcContractWcf.IHelloService>();
                var helloServiceHttp = bsp.GetService<RpcContractHttp.IHelloService>();
                var helloServiceAkka = bsp.GetService<RpcContractAkka.IHelloService>();
                var helloServiceNetty = bsp.GetService<RpcContractNetty.IHelloService>();
                //RpcContractNetty.IHelloService helloServiceNetty = null;

                while (true)
                {
                    Console.WriteLine("请选择:wcf | grpc | thrift | http | akka | netty | all");
                    var mode = Console.ReadLine().ToLower();
                    switch (mode)
                    {
                        case "wcf":
                            CallWhile((helloword) => { WcfHello(helloServiceWcf, helloword); });
                            break;
                        case "grpc":
                            CallWhile((helloword) => { GrpcHello(helloServiceGrpc, helloword); });
                            break;
                        case "thrift":
                            CallWhile((helloword) => { ThriftHello(helloServiceThrift, helloword); });
                            break;
                        case "http":
                            CallWhile((helloword) => { HttpHello(helloServiceHttp, helloword); });
                            break;
                        case "akka":
                            CallWhile((helloword) => { AkkaHello(helloServiceAkka, helloword); });
                            break;
                        case "netty":
                            CallWhile((helloword) => { NettyHello(helloServiceNetty, helloword); });
                            break;
                        case "all":
                            for (int i = 0; i < 3; i++)
                            {
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        WcfHello(helloServiceWcf);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    }
                                });
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        GrpcHello(helloServiceGrpc);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    }

                                });
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        ThriftHello(helloServiceThrift);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw ex;
                                    }

                                });
                                Task.Run(() =>
                                {
                                    try
                                    {
                                        HttpHello(helloServiceHttp);
                                    }
                                    catch (Exception ex)
                                    {

                                        throw ex;
                                    }

                                });
                                Task.Run(() =>
                                {

                                    try
                                    {
                                        AkkaHello(helloServiceAkka);
                                    }
                                    catch (Exception ex)
                                    {

                                        throw ex;
                                    }
                                });
                                Task.Run(() =>
                                {

                                    try
                                    {
                                        NettyHello(helloServiceNetty);
                                    }
                                    catch (Exception ex)
                                    {

                                        throw ex;
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

                

        }
        private static void ThriftHello(RpcContractThrift.IHelloService helloServiceThrift, string helloword = "world")
        {
            var token = CancellationToken.None;
            var callNameVoid = helloServiceThrift.CallNameVoidAsync(token).GetAwaiter().GetResult();
            Console.WriteLine(callNameVoid);
            helloServiceThrift.CallNameAsync(helloword, token).GetAwaiter().GetResult();
            Console.WriteLine("CallName called");
            helloServiceThrift.CallVoidAsync(token).GetAwaiter().GetResult();
            Console.WriteLine("CallVoid called");
            var hello = helloServiceThrift.HelloAsync(helloword, token).GetAwaiter().GetResult();
            Console.WriteLine(hello);
            var helloResult = helloServiceThrift.SayHelloAsync(helloword + "perfect world", token).GetAwaiter().GetResult();
            Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}", token);
            helloResult.Name = helloword + "show perfect world";
            var showResult = helloServiceThrift.ShowHelloAsync(helloResult, token).GetAwaiter().GetResult();
            Console.WriteLine(showResult);
        }
        private static void GrpcHello(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
        {
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
            helloResult.Name = helloword + "show perfect world";
            var showResult = helloServiceGrpc.ShowHello(helloResult);
            Console.WriteLine(showResult.Name);
        }
        private static void WcfHello(RpcContractWcf.IHelloService helloServiceWcf, string helloword = "world")
        {
            var callNameVoid = helloServiceWcf.CallNameVoid();
            Console.WriteLine(callNameVoid);
            helloServiceWcf.CallName(helloword);
            Console.WriteLine("CallName called");
            helloServiceWcf.CallVoid();
            Console.WriteLine("CallVoid called");
            var helloWcf = helloServiceWcf.Hello(helloword);
            Console.WriteLine(helloWcf);
            var helloResultWcf = helloServiceWcf.SayHello($"{helloword} perfect world");
            Console.WriteLine($"{helloResultWcf.Name},{helloResultWcf.Gender},{helloResultWcf.Head}");
            helloResultWcf.Name = helloword + "show perfect world";
            var showResultWcf = helloServiceWcf.ShowHello(helloResultWcf);
            Console.WriteLine(showResultWcf);
        }
        private static void HttpHello(RpcContractHttp.IHelloService helloServiceHttp, string helloword = "world")
        {
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
        private static void AkkaHello(RpcContractAkka.IHelloService akkaServiceHttp, string helloword = "world")
        {
            var callNameVoid = akkaServiceHttp.CallNameVoid();
            Console.WriteLine(callNameVoid);
            akkaServiceHttp.CallName(new RpcContractAkka.NameResult { Name = helloword });
            Console.WriteLine("CallName called");
            akkaServiceHttp.CallVoid();
            Console.WriteLine("CallVoid called");
            var hello = akkaServiceHttp.Hello(new RpcContractAkka.NameResult { Name = helloword });
            Console.WriteLine(hello.Name);
            var helloResult = akkaServiceHttp.SayHello(new RpcContractAkka.NameResult { Name = $"{helloword} perfect world" });
            Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
            helloResult.Name = helloword + "show perfect world";
            var showResultWcf = akkaServiceHttp.ShowHello(helloResult);
            Console.WriteLine(showResultWcf.Name);

        }
        private static void NettyHello(RpcContractNetty.IHelloService nettyServiceHttp, string helloword = "world")
        {
            var callNameVoid = nettyServiceHttp.CallNameVoid();
            Console.WriteLine(callNameVoid);
            nettyServiceHttp.CallName(helloword);
            Console.WriteLine("CallName called");
            nettyServiceHttp.CallVoid();
            Console.WriteLine("CallVoid called");
            var hello = nettyServiceHttp.Hello(helloword);
            Console.WriteLine(hello);
            var helloResult = nettyServiceHttp.SayHello($"{helloword} perfect world");
            Console.WriteLine($"{helloResult.Name},{helloResult.Gender},{helloResult.Head}");
            helloResult.Name = helloword + "show perfect world";
            var showResultNetty = nettyServiceHttp.ShowHello(helloResult);
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
}
