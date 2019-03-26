using System;

namespace Zooyard.Core
{
    public interface IClientPool
    {
        URL Address { get; set; }

        IClient GetClient(URL url);

        /// <summary>
        /// 归还一个连接至连接池
        /// </summary>
        /// <param name="client">连接</param>
        void Recovery(IClient client);
        /// <summary>
        /// 销毁连接
        /// </summary>
        /// <param name="client">连接</param>
        void DestoryClient(IClient client);
        /// <summary>
        /// 超时清除
        /// </summary>
        /// <param name="overTime"></param>
        void TimeOver(DateTime overTime);
    }
}
