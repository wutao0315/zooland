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
using Zooyard.Core;
using Zooyard.Core.Extensions;
using Zooyard.Extensions;
//using Zooyard.Rpc.AkkaImpl.Extensions;
using Zooyard.Rpc.GrpcImpl.Extensions;
using Zooyard.Rpc.HttpImpl.Extensions;
using Zooyard.Rpc.NettyImpl.Extensions;
using Zooyard.Rpc.ThriftImpl.Extensions;
//using Zooyard.Rpc.WcfImpl.Extensions;

namespace RpcConsumerCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                //.AddJsonFile("zooyard.akka.json", false, true)
                .AddJsonFile("zooyard.grpc.json", false, true)
                .AddJsonFile("zooyard.netty.json", false, true)
                .AddJsonFile("zooyard.thrift.json", false, true)
                //.AddJsonFile("zooyard.wcf.json", false, true)
                .AddJsonFile("zooyard.json", false, true)
                .AddJsonFile("nlog.json", false, true);

            var config = builder.Build();
            ZooyardLogManager.UseConsoleLogging(Zooyard.Core.Logging.LogLevel.Debug);

            IServiceCollection services = new ServiceCollection();
            //services.Configure<AkkaOption>(config.GetSection("akka"));
            services.Configure<GrpcOption>(config.GetSection("grpc"));
            services.Configure<NettyOption>(config.GetSection("netty"));
            services.Configure<ThriftOption>(config.GetSection("thrift"));
            //services.Configure<WcfOption>(config.GetSection("wcf"));
            services.Configure<ZooyardOption>(config.GetSection("zooyard"));
            services.AddLogging();
            //services.AddAkkaClient();
            services.AddGrpcClient();
            services.AddHttpClient();
            services.AddNettyClient();

            services.AddThriftClient();
            //services.AddWcfClient();
            services.AddZoolandClient(config);

            using var bsp = services.BuildServiceProvider();
            var helloServiceThrift = bsp.GetService<RpcContractThrift.IHelloService>();
            var helloServiceGrpc = bsp.GetService<RpcContractGrpc.IHelloService>();
            //var helloServiceWcf = bsp.GetService<RpcContractWcf.IHelloService>();
            var helloServiceHttp = bsp.GetService<RpcContractHttp.IHelloService>();
            //var helloServiceAkka = bsp.GetService<RpcContractAkka.IHelloService>();
            var helloServiceNetty = bsp.GetService<RpcContractNetty.IHelloService>();
            //RpcContractNetty.IHelloService helloServiceNetty = null;

            while (true)
            {
                //Console.WriteLine("请选择:wcf | grpc | thrift | http | akka | netty | all");
                Console.WriteLine("请选择:grpc | thrift | http | netty | all");
                var mode = Console.ReadLine().ToLower();
                switch (mode)
                {
                    //case "wcf":
                    //    CallWhile((helloword) => { WcfHello(helloServiceWcf, helloword); });
                    //    break;
                    case "grpc":
                        CallWhile(async (helloword) => { await GrpcHello(helloServiceGrpc, helloword); });
                        break;
                    case "thrift":
                        CallWhile(async(helloword) => { await ThriftHello(helloServiceThrift, helloword); });
                        break;
                    case "http":
                        CallWhile(async (helloword) => { await HttpHello(helloServiceHttp, helloword); });
                        break;
                    //case "akka":
                    //    CallWhile((helloword) => { AkkaHello(helloServiceAkka, helloword); });
                    //    break;
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
                                catch (Exception ex)
                                {
                                    throw ex;
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
        }
        private static async Task GrpcHello(RpcContractGrpc.IHelloService helloServiceGrpc, string helloword = "world")
        {
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
            var callNameVoid = await helloServiceHttp.CallNameVoid();
            Console.WriteLine(callNameVoid);
            await helloServiceHttp.CallName(helloword);
            Console.WriteLine("CallName called");
            await helloServiceHttp.CallVoid();
            Console.WriteLine("CallVoid called");
            var helloWcf = await helloServiceHttp.Hello(helloword);
            Console.WriteLine(helloWcf);
            var helloResultWcf = await helloServiceHttp.SayHello($"{helloword} perfect world");
            Console.WriteLine($"{helloResultWcf.Name},{helloResultWcf.Gender},{helloResultWcf.Head}");
            helloResultWcf.Name = helloword + "show perfect world";
            var showResultWcf = await helloServiceHttp.ShowHello(helloResultWcf);
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
