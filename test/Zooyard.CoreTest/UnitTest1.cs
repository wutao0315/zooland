using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zooyard.Rpc.ThriftImpl.Extensions;
using Zooyard.Core.Extensions;
using Microsoft.Extensions.Configuration;
using System.IO;
using RpcContractThrift;
using Zooyard.Core;

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
                .AddJsonFile("zooyard.thrift.json", false, true)
                .AddJsonFile("zooyard.json", false, true);

            var config = builder.Build();

            IServiceCollection collection = new ServiceCollection();
            collection.Configure<ThriftOption>(config.GetSection("thrift"));
            collection.Configure<ZoolandOption>(config.GetSection("zooland"));
            collection.AddLogging();
            collection.AddThriftClient();
            collection.AddZooland(config);

            var bsp = collection.BuildServiceProvider();

            var thrifthelloService = bsp.GetService<IHelloService>();

            Assert.IsNotNull(thrifthelloService);

        }
    }
}
