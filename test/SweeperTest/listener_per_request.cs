using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweeper.Core;
using Sweeper.Core.Support;
using Sweeper.Core.Tag;
using Sweeper.Mock;

namespace SweeperTest
{
    [TestClass]
    public class listener_per_request
    {
        public class Client
        {
            //private final ExecutorService executor = Executors.newCachedThreadPool();
            private readonly ITracer tracer;

            public Client(ITracer tracer)
            {
                this.tracer = tracer;
            }



            /**
             * Async execution
             */
            private object execute(object message, ResponseListener responseListener)
            {
                var result = Task.Run<object>(() =>
                {
                    var response = message + ":response";
                    responseListener.onResponse(response);
                    return response;
                });
                Task.WaitAll(result);
                return result.Result;
            }

            public object send(object message)
            {
                ISpan span = tracer.buildSpan("send").
                        withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_CLIENT)
                        .start();
                return execute(message, new ResponseListener(span));
            }

        }
        public class ResponseListener
        {
            private readonly ISpan span;

            public ResponseListener(ISpan span)
            {
                this.span = span;
            }

            /**
             * executed when response is received from server. Any thread.
             */
            public void onResponse(object response)
            {
                span.finish();
            }

        }


        [TestMethod]
        public void test_listener_per_request()
        {

            var tracer = new MockTracer(new ThreadLocalScopeManager(), new MockTracer.TextMapPropagator());
            var client = new Client(tracer);
            var response = client.send("message");
            
            Assert.AreEqual("message:response", response);

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(1, finished.Count);
            Assert.IsNotNull(TestUtils.getOneByTag<string>(finished, Tags.SPAN_KIND, Tags.SPAN_KIND_CLIENT));
            Assert.IsNull(tracer.ScopeManager.active());

        }
    }
}
