using System;
using System.Collections.Generic;
using System.Text;
using Zooyard;

namespace Zooyard.Rpc.NettyImpl
{
    /// <summary>
    /// Used inside Akka.Remote for constructing the low-level Helios threadpool, but inside
    /// vanilla Akka it's also used for constructing custom fixed-size-threadpool dispatchers.
    /// </summary>
    public class ThreadPoolConfig
    {
        private readonly URL _config;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="config">TBD</param>
        public ThreadPoolConfig(URL config)
        {
            _config = config;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public int PoolSizeMin
        {
            get { return _config.GetParameter<int>("pool-size-min"); }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public double PoolSizeFactor
        {
            get { return _config.GetParameter<int>("pool-size-factor"); }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public int PoolSizeMax
        {
            get { return _config.GetParameter<int>("pool-size-max"); }
        }

        #region Static methods

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="floor">TBD</param>
        /// <param name="scalar">TBD</param>
        /// <param name="ceiling">TBD</param>
        /// <returns>TBD</returns>
        public static int ScaledPoolSize(int floor, double scalar, int ceiling)
        {
            return Math.Min(Math.Max((int)(Environment.ProcessorCount * scalar), floor), ceiling);
        }

        #endregion
    }
}
