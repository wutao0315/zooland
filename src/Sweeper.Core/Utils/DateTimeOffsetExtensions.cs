using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Utils
{
    public static class DateTimeOffsetExtensions
    {
        public static long ToUnixTimeMilliseconds(this DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.UtcDateTime.Ticks / 10000L - 62135596800000L;
        }

        public static long CurrentTimeMillis(this DateTime dateTime)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var lTime = dateTime - dt;
            return (long)lTime.TotalMilliseconds - 28800000;
        }
    }
}
