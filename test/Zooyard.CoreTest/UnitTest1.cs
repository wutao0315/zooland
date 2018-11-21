using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zooyard.Rpc.ThriftImpl.Extensions;
using Zooyard.Core.Extensions;
using Microsoft.Extensions.Configuration;
using System.IO;
using Zooyard.Rpc.WcfImpl.Extensions;
using Zooyard.Rpc.AkkaImpl.Extensions;
using Zooyard.Rpc.GrpcImpl.Extensions;
using Zooyard.Rpc.NettyImpl.Extensions;
using Zooyard.Rpc.HttpImpl.Extensions;
using DotNetty.Transport.Channels;
using System.Collections.Generic;
using DotNetty.Handlers.Logging;
using DotNetty.Codecs;

namespace Zooyard.CoreTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "/config")
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

            var handlers = new List<IChannelHandler>
                        {
                            new LoggingHandler(),
                            new LengthFieldPrepender(lengthFieldLength:4),
                            new LengthFieldBasedFrameDecoder(
                                maxFrameLength: int.MaxValue,
                                lengthFieldOffset:0,
                                lengthFieldLength:4,
                                lengthAdjustment:0,
                                initialBytesToStrip:4)
                        };
            services.AddNettyClient(new Dictionary<string, IEnumerable<IChannelHandler>>
            {
                { "socket", handlers},
                { "libuv", handlers }
            });

            services.AddThriftClient();
            services.AddWcfClient();
            services.AddZoolandClient(config);

            var bsp = services.BuildServiceProvider();
            var akkaHelloService = bsp.GetService<RpcContractAkka.IHelloService>();
            Assert.IsNotNull(akkaHelloService);
            var tgrpcHelloService = bsp.GetService<RpcContractGrpc.IHelloService>();
            Assert.IsNotNull(tgrpcHelloService);
            var httpHelloService = bsp.GetService<RpcContractHttp.IHelloService>();
            Assert.IsNotNull(httpHelloService);
            var nettyHelloService = bsp.GetService<RpcContractNetty.IHelloService>();
            Assert.IsNotNull(nettyHelloService);
            var thriftHelloService = bsp.GetService<RpcContractThrift.IHelloService>();
            Assert.IsNotNull(thriftHelloService);
            var wcfHelloService = bsp.GetService<RpcContractWcf.IHelloService>();
            Assert.IsNotNull(wcfHelloService);
            


        }
    }
}
