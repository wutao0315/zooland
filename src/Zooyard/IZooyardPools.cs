using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard
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
        /// <summary>
        /// 执行远程调用
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>

        Task<IResult<T>> Invoke<T>(IInvocation invocation);
    }
}
