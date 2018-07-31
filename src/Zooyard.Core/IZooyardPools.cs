using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Core
{
    public interface IZooyardPools
    {
        /// <summary>
        /// 地址
        /// 如果是registry开头，就代表是注册中心的地址
        /// 否则就是直连地址
        /// </summary>
        URL Address { get; }
        /// <summary>
        /// clear all cache
        /// </summary>
        void CacheClear();
        ///// <summary>
        ///// 获取客户端缓存
        ///// </summary>
        ///// <param name="invocation">服务路径</param>
        ///// <returns>客户端服务连接</returns>
        //ICache GetCache(IInvocation invocation);
        ///// <summary>
        ///// 选择负载均衡算法
        ///// </summary>
        ///// <param name="invocation"></param>
        ///// <returns></returns>
        //ILoadBalance GetLoadBalance(IInvocation invocation);

        ///// <summary>
        ///// 获取集群执行器
        ///// </summary>
        ///// <param name="invocation"></param>
        ///// <returns></returns>
        //ICluster GetCluster(IInvocation invocation);
        ///// <summary>
        ///// 根据配置获取路径集合
        ///// </summary>
        ///// <param name="invocation"></param>
        ///// <returns></returns>
        //IList<URL> GetUrls(IInvocation invocation);
        /// <summary>
        /// 执行远程调用
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>

        IResult Invoke(IInvocation invocation);
    }
}
