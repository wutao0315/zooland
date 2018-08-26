using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweeper.Core;
using Sweeper.Core.Support;
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
    public class activate_deactivate
    {

        private readonly MockTracer tracer = new MockTracer(new AutoFinishScopeManager(),
            new MockTracer.TextMapPropagator());
        //private final ScheduledExecutorService service = Executors.newScheduledThreadPool(10);

        public class RunnableAction
        {
            private readonly AutoFinishScope.Continuation continuation;
           public RunnableAction(AutoFinishScope scope)
            {
                continuation = scope.capture();
                Console.WriteLine("Action created");
            }
            public void run()
            {
                Console.WriteLine("Action started");
                AutoFinishScope scope = continuation.activate();

                try
                {
                    Thread.Sleep(1000);
                    //TimeUnit.SECONDS.sleep(1); // without sleep first action can finish before second is started
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }

                // set random tag starting with 'test_tag_' to test that finished span has all of them
                scope.span().setTag("test_tag_" + new Random().Next(), "random");

                scope.Dispose();
                Console.WriteLine("Action finished");
            }

        }

        [TestMethod]
        public void test_one_scheduled_action()
        {
            var entrythread = entryThread();
            entrythread.Start();

            while (TestUtils.finishedSpansSize(tracer) < 1)
            {

            }

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(1, finished.Count);

            Assert.AreEqual(1, getTestTagsCount(finished[0]));
        }

        [TestMethod]
        public void test_two_scheduled_actions()
        {
            var entryThread = entryThreadWithTwoActions();
            entryThread.Start();
            entryThread.Join(10_000);
            // Entry thread is completed but Actions are still running (or even not started)

            while (TestUtils.finishedSpansSize(tracer) < 1)
            {

            }

            List<MockSpan> finished = tracer.FinishedSpans;
            Assert.AreEqual(1, finished.Count);

            // Check that two actions finished and each added to span own tag ('test_tag_{random}')
            Assert.AreEqual(2, getTestTagsCount(finished[0]));
        }

        private int getTestTagsCount(MockSpan mockSpan)
        {
            int tagCounter = 0;
            foreach (var tagKey in mockSpan.Tags.Keys)
            {
                if (tagKey.StartsWith("test_tag_"))
                {
                    tagCounter++;
                }
            }
            return tagCounter;
        }

        /**
         * Thread will be completed before action completed.
         */
        private Thread entryThread()
        {
            return new Thread(new ThreadStart(() => {
                Console.WriteLine("Entry thread started");
                using (IScope scope = tracer.buildSpan("parent").startActive(false))
                {
                    var action = new RunnableAction((AutoFinishScope)scope);

                    // Action is executed at some time and we are not able to check status
                    //service.schedule(action, 500, TimeUnit.MILLISECONDS);

                    new Timer(new TimerCallback((state)=> { action.run(); }),null,500,0);
                    Console.WriteLine("Entry thread finished");
                };
            }));
        }

        /**
         * Thread will be completed before action completed.
         */
        private Thread entryThreadWithTwoActions()
        {

            return new Thread(new ThreadStart(()=> {
                Console.Write("Entry thread 2x started");
                using (IScope scope = tracer.buildSpan("parent").startActive(false))
                {
                    var action = new RunnableAction((AutoFinishScope)scope);
                    var action2 = new RunnableAction((AutoFinishScope)scope);

                    Random random = new Random();

                    new Timer(new TimerCallback((state) => { action.run(); }), null, random.Next(1000) + 100, 0);
                    new Timer(new TimerCallback((state) => { action2.run(); }), null, random.Next(1000) + 100, 0);
                    // Actions are executed at some time and most likely are running in parallel
                    //service.schedule(action, random.Next(1000) + 100, TimeUnit.MILLISECONDS);
                    //service.schedule(action2, random.Next(1000) + 100, TimeUnit.MILLISECONDS);
                    Console.WriteLine("Entry thread 2x finished");
                }

            }));
        }
    }
}
