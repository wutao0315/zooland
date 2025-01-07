using Jint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zooyard;

namespace ZooyardTest
{
    [TestClass]
    public class JintTest
    {
        [TestMethod]
        public void TestScript()
        {
            
            var script = @"
            function route(invokers, address, invocation, context)
            {
                var Zooyard = importNamespace('Zooyard')
                var ListOfURL = System.Collections.Generic.List(Zooyard.URL);
                var list = new ListOfURL();
                for (i = 0; i < invokers.Count; i++)
                {
                    if (""10.20.153.10"" != invokers[i].Host)
                    {
                        list.Add(invokers[i]);
                    }
                }
                return list;
            }";

            CancellationTokenSource _cts = new();

            URL address = URL.ValueOf("http://localhost");
            var invokers = new List<URL>
            {
                URL.ValueOf("http://10.20.153.10"),
                URL.ValueOf("http://10.20.153.11"),
                URL.ValueOf("http://10.20.153.12")
            };
            IInvocation? invocation = null;

            var context = new Dictionary<string, string>();

            using var scriptEngine = new Engine(options =>
            {
                options.AllowClr();

                options.AllowClr(typeof(URL).Assembly);

                var limitMemory = address.GetParameter("limit_memory", 4_000_000);
                // Limit memory allocations to 4 MB
                options.LimitMemory(limitMemory);

                var timeout_interval = address.GetParameter("timeout_interval", 4);
                // Set a timeout to 4 seconds.
                options.TimeoutInterval(TimeSpan.FromSeconds(timeout_interval));

                var max_statements = address.GetParameter("max_statements", 1000);
                // Set limit of 1000 executed statements.
                options.MaxStatements(1000);

                // Use a cancellation token.
                options.CancellationToken(_cts.Token);

            });

            scriptEngine.Execute(script);

            try
            {
                var list = scriptEngine.Invoke("route", invokers, address, invocation, context);

                var listObj = list.ToObject();

                var listResult = listObj as List<URL>;

                Assert.IsNotNull(listResult);

                Assert.AreEqual(2, listResult.Count);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [TestMethod]
        public void TestScript2()
        {
            var script = @"
            function route(invokers, address, invocation, context)
            {
                return invokers;
            }";

            CancellationTokenSource _cts = new();

            URL address = URL.ValueOf("http://localhost");
            var invokers = new List<URL>
            {
                URL.ValueOf("http://10.20.153.10")
            };
            IInvocation? invocation = null;

            var context = new Dictionary<string, string>();

            using var scriptEngine = new Engine(options =>
            {
                options.AllowClr();

                options.AllowClr(typeof(URL).Assembly);

                var limitMemory = address.GetParameter("limit_memory", 4_000_000);
                // Limit memory allocations to 4 MB
                options.LimitMemory(limitMemory);

                var timeout_interval = address.GetParameter("timeout_interval", 4);
                // Set a timeout to 4 seconds.
                options.TimeoutInterval(TimeSpan.FromSeconds(timeout_interval));

                var max_statements = address.GetParameter("max_statements", 1000);
                // Set limit of 1000 executed statements.
                options.MaxStatements(1000);

                // Use a cancellation token.
                options.CancellationToken(_cts.Token);

            });

            scriptEngine.Execute(script);

            try
            {
                var list = scriptEngine.Invoke("route", invokers, address, invocation, context);

                var listObj = list.ToObject();

                var listResult = listObj as List<URL>;

                Assert.IsNotNull(listResult);

                Assert.AreEqual(1, listResult.Count);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
