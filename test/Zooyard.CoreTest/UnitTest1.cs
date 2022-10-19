using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Zooyard;

namespace ZooyardTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestClient()
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
            //services.Configure<AkkaOption>(config.GetSection("akka"));
            services.Configure<GrpcOption>(config.GetSection("grpc"));
            //services.Configure<NettyOption>(config.GetSection("netty"));
            //services.Configure<ThriftOption>(config.GetSection("thrift"));
            //services.Configure<WcfOption>(config.GetSection("wcf"));
            services.Configure<ZooyardOption>(config.GetSection("zooyard"));
            services.AddLogging();
            //services.AddAkkaClient();
            services.AddZooyardGrpc();
            services.AddZooyardHttp();


            //services.AddNettyImpl();

            //services.AddThriftClient();
            //services.AddWcfClient();
            services.AddZoolandClient(config);

            using var bsp = services.BuildServiceProvider();
                //var akkaHelloService = bsp.GetService<RpcContractAkka.IHelloService>();
                //Assert.IsNotNull(akkaHelloService);
                var tgrpcHelloService = bsp.GetService<RpcContractGrpc.IHelloService>();
                Assert.IsNotNull(tgrpcHelloService);
                var httpHelloService = bsp.GetService<RpcContractHttp.IHelloService>();
                Assert.IsNotNull(httpHelloService);
                var nettyHelloService = bsp.GetService<RpcContractNetty.IHelloService>();
                Assert.IsNotNull(nettyHelloService);
                //var thriftHelloService = bsp.GetService<RpcContractThrift.IHelloService>();
                //Assert.IsNotNull(thriftHelloService);
                //var wcfHelloService = bsp.GetService<RpcContractWcf.IHelloService>();
                //Assert.IsNotNull(wcfHelloService);

        }
    }
}
