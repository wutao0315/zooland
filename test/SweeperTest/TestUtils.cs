using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sweeper.Core.Tag;
using Sweeper.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SweeperTest
{
    public class TestUtils
    {
        public static int finishedSpansSize(MockTracer tracer)
        {
            return tracer.FinishedSpans.Count;
        }


        public static List<MockSpan> getByTag<T>(List<MockSpan> spans, AbstractTag<T> key, Object value)
        {
            List<MockSpan> found = new List<MockSpan>(spans.Count);
            foreach (MockSpan span in spans)
            {
                if (span.Tags[key.getKey()].Equals(value))
                {
                    found.Add(span);
                }
            }
            return found;
        }

        public static MockSpan getOneByTag<T>(List<MockSpan> spans, AbstractTag<T> key, Object value)
        {
            List<MockSpan> found = getByTag(spans, key, value);
            if (found.Count > 1)
            {
                throw new Exception("there is more than one span with tag '"
                        + key.getKey() + "' and value '" + value + "'");
            }
            if ((found?.Count ?? 0) <= 0)
            {
                return null;
            }
            else
            {
                return found[0];
            }
        }

        //public static void sleep()
        //{
        //    try
        //    {
        //        TimeUnit.MILLISECONDS.sleep(new Random().nextInt(2000));
        //    }
        //    catch (Exception e)
        //    {
        //        e.printStackTrace();
        //        Thread.currentThread().interrupt();
        //    }
        //}

        //public static void sleep(long milliseconds)
        //{
        //    try
        //    {
        //        TimeUnit.MILLISECONDS.sleep(milliseconds);
        //    }
        //    catch (Exception e)
        //    {
        //        e.printStackTrace();
        //        Thread.currentThread().interrupt();
        //    }
        //}

        public static void sortByStartMicros(List<MockSpan> spans)
        {
            spans.Sort(new CompareSpan());
        //    Collections.sort(spans, new Comparator<MockSpan>() {
        //        @Override
        //        public int compare(MockSpan o1, MockSpan o2)
        //    {
        //        return Long.compare(o1.startMicros(), o2.startMicros());
        //    }
        //});
        }

        public class CompareSpan : IComparer<MockSpan>
        {
            public int Compare(MockSpan x, MockSpan y)
            {
                return x.StartMicros > y.StartMicros ? 1 : 0;
            }
        }

        public static void assertSameTrace(List<MockSpan> spans)
        {
            for (int i = 0; i < spans.Count - 1; i++)
            {
                Assert.Equals(true, spans[spans.Count - 1].FinishMicros >= spans[i].FinishMicros);
                Assert.Equals(spans[spans.Count - 1].Context.TraceId, spans[i].Context.TraceId);
                Assert.Equals(spans[spans.Count - 1].Context.SpanId, spans[i].ParentId);
            }
        }
    }
}
