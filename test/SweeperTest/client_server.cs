using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweeper.Core;
using Sweeper.Core.Propagation;
using Sweeper.Core.Support;
using Sweeper.Core.Tag;
using Sweeper.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SweeperTest
{
    [TestClass]
    public class client_server
    {
        public class Client
        {
            private readonly Queue<Message> queue;
            private readonly ITracer tracer;

            public Client(Queue<Message> queue, ITracer tracer)
            {
                this.queue = queue;
                this.tracer = tracer;
            }

            public void send()
            {
                Message message = new Message();
                using (IScope scope = tracer.buildSpan("send")
                 .withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_CLIENT)
                 .withTag(Tags.COMPONENT.getKey(), "example-client")
                 .startActive(true))
                {
                    tracer.inject(scope.span().Context, Builtin<ITextMap>.TEXT_MAP, new TextMapInjectAdapter(message));
                    queue.Enqueue(message);

                }
            }
        }
        

        public class Message : Dictionary<string, string> { }

        private MockTracer tracer = new MockTracer(new ThreadLocalScopeManager(), new MockTracer.TextMapPropagator());
        private Queue<Message> queue = new Queue<Message>(10);
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();     
        [TestInitialize]
        public void Before() {
            Task.Run(()=> {
                while (!cancellationTokenSource.IsCancellationRequested)
                {

                    try
                    {
                        if (queue.Count<=0)
                        {
                            continue;
                        }
                        var message = queue.Dequeue();
                        ISpanContext context = tracer.extract(Builtin<ITextMap>.TEXT_MAP, new TextMapExtractAdapter(message));
                        using (IScope scope = tracer.buildSpan("receive")
                                              .withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_SERVER)
                                              .withTag(Tags.COMPONENT.getKey(), "example-server")
                                              .asChildOf(context)
                                              .startActive(true))
                        {

                        }
                    }
                    catch (Exception e)
                    {
                        Thread.CurrentThread.Interrupt();
                        return;
                    }
                }

            }, cancellationTokenSource.Token);
            //Server server = new Server(queue, tracer);
            //server.Start();
        }
        [TestCleanup]
        public void after()
        {
            try
            {

                cancellationTokenSource.Cancel();
            }
            catch (Exception ex)
            {

                throw;
            }
           
            
            //Thread.CurrentThread.Interrupt();
            //Thread.CurrentThread.Join(5_000);
            //server.interrupt();
            //server.join(5_000L);
        }
        [TestMethod]
        public void test_listener_per_request()
        {
            Client client = new Client(queue, tracer);
            client.send();

            while (TestUtils.finishedSpansSize(tracer)<2)
            {

            }
            //TestUtils.finishedSpansSize(tracer)
            //await().atMost(15, TimeUnit.SECONDS).until(finishedSpansSize(tracer), equalTo(2));

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(2, finished.Count);
            Assert.AreEqual(finished[0].Context.TraceId, finished[1].Context.TraceId);
            Assert.IsNotNull(TestUtils.getOneByTag(finished, Tags.SPAN_KIND, Tags.SPAN_KIND_CLIENT));
            Assert.IsNotNull(TestUtils.getOneByTag(finished, Tags.SPAN_KIND, Tags.SPAN_KIND_SERVER));
            Assert.IsNull(tracer.ScopeManager.active());


        }
    }
}
