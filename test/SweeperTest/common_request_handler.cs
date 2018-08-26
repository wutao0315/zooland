using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweeper.Core;
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
    public class common_request_handler
    {
        public class Client
        {
            private readonly RequestHandler requestHandler;
            public Client(RequestHandler requestHandler)
            {
                this.requestHandler = requestHandler;
            }

            public string send(object message)
            {

                var context = new Context();
                var result = Task.Run<string>(() => {
                    Console.WriteLine($"send {message}");
                    //TestUtils.sleep();
                    Thread.Sleep(new Random().Next(2000));
                    Task.Run(() => {
                        Thread.Sleep(new Random().Next(2000));
                        requestHandler.beforeRequest(message, context);
                    }).GetAwaiter().GetResult();

                    Task.Run(() => {
                        Thread.Sleep(new Random().Next(2000));
                        requestHandler.afterResponse(message, context);
                    }).GetAwaiter().GetResult();
                    return message + ":response";
                });
                Task.WaitAll(result);
                return result.Result;
            }
        }
        public class Context : Dictionary<string, object> { }
        public class RequestHandler
        {

           public static readonly string OPERATION_NAME = "send";

            private readonly ITracer tracer;

            private readonly ISpanContext parentContext;

            public RequestHandler(ITracer tracer) : this(tracer, null)
            {
            }

            public RequestHandler(ITracer tracer, ISpanContext parentContext)
            {
                this.tracer = tracer;
                this.parentContext = parentContext;
            }

            public void beforeRequest(object request, Context context)
            {
                Console.WriteLine($"before send {request}");

                // we cannot use active span because we don't know in which thread it is executed
                // and we cannot therefore activate span. thread can come from common thread pool.
                ISpanBuilder spanBuilder = tracer.buildSpan(OPERATION_NAME)
                        .ignoreActiveSpan()
                        .withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_CLIENT);

                if (parentContext != null)
                {
                    spanBuilder.asChildOf(parentContext);
                }

                context.Add("span", spanBuilder.start());
            }

            public void afterResponse(object response, Context context)
            {
                Console.WriteLine($"after response {response}");

                var spanObject = context["span"];
                if (spanObject is ISpan)
                {
                    ISpan span = (ISpan)spanObject;
                    span.finish();
                }
            }

        }
        private MockTracer tracer = new MockTracer(new ThreadLocalScopeManager(), new MockTracer.TextMapPropagator());
        private Client client;
        [TestInitialize]
        public void Before()
        {
            tracer.reset();
            client = new Client(new RequestHandler(tracer));
        }


        [TestMethod]
        public void two_requests()
        {
            var responseFuture = client.send("message");
            var responseFuture2 = client.send("message2");

            Assert.AreEqual("message:response", responseFuture);
            Assert.AreEqual("message2:response", responseFuture2);

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(2, finished.Count);

            foreach (MockSpan span in finished)
            {
                Assert.AreEqual(Tags.SPAN_KIND_CLIENT, span.Tags[Tags.SPAN_KIND.getKey()]);
            }

            Assert.AreNotEqual(finished[0].Context.TraceId, finished[1].Context.TraceId);
            Assert.AreEqual(0, finished[0].ParentId);
            Assert.AreEqual(0, finished[1].ParentId);

            Assert.IsNull(tracer.ScopeManager.active());
        }

        /**
         * active parent is not picked up by child
         */
        [TestMethod]
        public void parent_not_picked_up()
        {
            using (IScope parentScope = tracer.buildSpan("parent").startActive(true))
            {
                string response = client.send("no_parent");
                Assert.AreEqual("no_parent:response", response);
            }


            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(2, finished.Count);

            MockSpan child = getOneByOperationName(finished, RequestHandler.OPERATION_NAME);
            Assert.IsNotNull(child);

            MockSpan parent = getOneByOperationName(finished, "parent");
            Assert.IsNotNull(parent);

            // Here check that there is no parent-child relation although it should be because child is
            // created when parent is active
            Assert.AreNotEqual(parent.Context.SpanId, child.ParentId);

        }

        /**
         * Solution is bad because parent is per client (we don't have better choice).
         * Therefore all client requests will have the same parent.
         * But if client is long living and injected/reused in different places then initial parent will not be correct.
         */
        [TestMethod]
        public void bad_solution_to_set_parent()
        {
            Client client;
            using (IScope parentScope = tracer.buildSpan("parent").startActive(true))
            {
                client = new Client(new RequestHandler(tracer, parentScope.span().Context));
                var responseScope = client.send("correct_parent");
                Assert.AreEqual("correct_parent:response", responseScope);
            }

            // Send second request, now there is no active parent, but it will be set, ups
            var response = client.send("wrong_parent");
            Assert.AreEqual("wrong_parent:response", response);

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(3, finished.Count);

            TestUtils.sortByStartMicros(finished);


            MockSpan parent = getOneByOperationName(finished, "parent");
            Assert.IsNotNull(parent);

            // now there is parent/child relation between first and second span:
            Assert.AreEqual(parent.Context.SpanId, finished[1].ParentId);

            // third span should not have parent, but it has, damn it
            Assert.AreEqual(parent.Context.SpanId, finished[2].ParentId);
        }

        private static MockSpan getOneByOperationName(List<MockSpan> spans, String name)
        {
            MockSpan found = null;
            foreach (MockSpan span in spans)
            {
                if (name == span.OperationName)
                {
                    if (found != null)
                    {
                        throw new Exception("there is more than one span with operation name '"
                                + name + "'");
                    }
                    found = span;
                }
            }
            return found;
        }

        [TestMethod]
        public void common_request_handler_test()
        {



        }
    }
}
