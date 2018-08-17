using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core
{
    public interface ITraceIdGenerator
    {
        /// <summary>
        /// 生成唯一标识
        /// </summary>
        /// <returns></returns>
        long GenerateTraceId();
    }

    public class UUIDGenerator : ITraceIdGenerator
    {
        //区域标识位数
        private readonly static int regionIdBits = 3;
        //机器标识位数
        private readonly static int workerIdBits = 10;
        //序列号标识位数
        private readonly static int sequenceBits = 10;

        //区域标志ID最大值
        private readonly static int maxRegionId = -1 ^ (-1 << regionIdBits);
        // 机器ID最大值
        private readonly static int maxWorkerId = -1 ^ (-1 << workerIdBits);
        // 序列号ID最大值
        private readonly static int sequenceMask = -1 ^ (-1 << sequenceBits);

        // 机器ID偏左移10位
        private readonly static int workerIdShift = sequenceBits;
        // 业务ID偏左移20位
        private readonly static int regionIdShift = sequenceBits + workerIdBits;
        // 时间毫秒左移23位
        private readonly static int timestampLeftShift = sequenceBits + workerIdBits + regionIdBits;


        private static long lastTimestamp = -1L;

        private int sequence = 0;
        private readonly int workerId;
        private readonly int regionId;
        //基准时间
        private readonly long twepoch;

        public UUIDGenerator(int workerId)
        {
            // 如果超出范围就抛出异常
            if (workerId > maxWorkerId || workerId < 0)
            {
                throw new ArgumentException("worker Id can't be greater than %d or less than 0");
            }
            this.workerId = workerId;
            this.regionId = 0;
            this.twepoch = 1288834974657L;//Thu, 04 Nov 2010 01:42:54 GMT
        }
        public UUIDGenerator(int workerId, int regionId)
        {

            // 如果超出范围就抛出异常
            if (workerId > maxWorkerId || workerId < 0)
            {
                throw new ArgumentException("worker Id can't be greater than %d or less than 0");
            }
            if (regionId > maxRegionId || regionId < 0)
            {
                throw new ArgumentException("datacenter Id can't be greater than %d or less than 0");
            }

            this.workerId = workerId;
            this.regionId = regionId;
            this.twepoch = 1288834974657L;//Thu, 04 Nov 2010 01:42:54 GMT
        }
        public UUIDGenerator(int workerId, int regionId, long twepoch)
        {

            // 如果超出范围就抛出异常
            if (workerId > maxWorkerId || workerId < 0)
            {
                throw new ArgumentException("worker Id can't be greater than %d or less than 0");
            }
            if (regionId > maxRegionId || regionId < 0)
            {
                throw new ArgumentException("datacenter Id can't be greater than %d or less than 0");
            }

            this.workerId = workerId;
            this.regionId = regionId;
            this.twepoch = twepoch;
        }
        public long GenerateTraceId()
        {
            return this.nextId(false, 0);
        }
        /// <summary>
        /// 实际产生代码的
        /// </summary>
        /// <param name="isPadding"></param>
        /// <param name="busId"></param>
        /// <returns></returns>
        private long nextId(bool isPadding, long busId)
        {

            long timestamp = timeGen();
            long paddingnum = regionId;

            if (isPadding)
            {
                paddingnum = busId;
            }

            if (timestamp < lastTimestamp)
            {
                try
                {
                    throw new Exception("Clock moved backwards.  Refusing to generate id for " + (lastTimestamp - timestamp) + " milliseconds");
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            //如果上次生成时间和当前时间相同,在同一毫秒内
            if (lastTimestamp == timestamp)
            {
                //sequence自增，因为sequence只有10bit，所以和sequenceMask相与一下，去掉高位
                sequence = (sequence + 1) & sequenceMask;
                //判断是否溢出,也就是每毫秒内超过1024，当为1024时，与sequenceMask相与，sequence就等于0
                if (sequence == 0)
                {
                    //自旋等待到下一毫秒
                    timestamp = tailNextMillis(lastTimestamp);
                }
            }
            else
            {
                // 如果和上次生成时间不同,重置sequence，就是下一毫秒开始，sequence计数重新从0开始累加,
                // 为了保证尾数随机性更大一些,最后一位设置一个随机数

                sequence = new Random().Next(10);
            }

            lastTimestamp = timestamp;
            #pragma warning disable CS0675
            return ((timestamp - twepoch) << timestampLeftShift) | (paddingnum << regionIdShift) | (workerId << workerIdShift) | sequence;

        }

        /// <summary>
        ///  防止产生的时间比之前的时间还要小（由于NTP回拨等问题）,保持增量的趋势.
        /// </summary>
        /// <param name="lastTimestamp"></param>
        /// <returns></returns>
        private long tailNextMillis(long lastTimestamp)
        {
            long timestamp = this.timeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = this.timeGen();
            }
            return timestamp;
        }

        /// <summary>
        /// 获取当前的时间戳
        /// </summary>
        /// <returns></returns>
        protected long timeGen()
        {
            var currentTimeMillis = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return currentTimeMillis;
        }
    }
}
