using System;
using Zooyard.Core;

namespace Zooyard.Rpc
{
    public interface IPool : IDisposable
    {
        /// <summary>
        /// 配置路径
        /// </summary>
        URL Url { set; get; }
        /// <summary>
        /// 连接属性，从连接池取出一个连接或归还一个连接至连接池
        /// </summary>
        IClient Client { get; set; }
        /// <summary>
        /// 重置连接池
        /// </summary>
        void Reset();
        /// <summary>
        /// 通信超时时间，单位毫秒
        /// </summary>
        int ClientTimeout { set; get; }
        /// <summary>
        /// 启动版本，连接池重启时版本递增
        /// </summary>
        int Version { set; get; }
    }
}
