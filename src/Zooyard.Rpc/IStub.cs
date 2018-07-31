using System;

namespace Zooyard.Rpc
{
    public interface IStub
    {
        /// <summary>
        /// 数据获取通用接口
        /// </summary>
        /// <typeparam name="T">响应类型</typeparam>
        /// <param name="relativeUrl">相对url</param>
        /// <param name="paras">参数</param>
        /// <returns>响应</returns>
        T Get<T>(string relativeUrl, params object[] paras);

        /// <summary>
        /// 数据获取通用接口
        /// </summary>
        /// <param name="relativeUrl">相对URL</param>
        /// <param name="paras">参数</param>
        /// <returns>响应</returns>
        object Get(string relativeUrl, params object[] paras);

        /// <summary>
        /// 数据提交通用接口
        /// </summary>
        /// <typeparam name="T">响应类型</typeparam>
        /// <param name="relativeUrl">相对URL</param>
        /// <param name="paras">参数</param>
        /// <returns>响应</returns>
        T Post<T>(string relativeUrl, params object[] paras);

        /// <summary>
        /// 数据提交通用接口
        /// </summary>
        /// <param name="relativeUrl">相对URL</param>
        /// <param name="paras">参数</param>
        /// <returns>响应</returns>
        object Post(string relativeUrl, params object[] paras);

        /// <summary>
        /// 数据提交通用接口，无需响应结果
        /// </summary>
        /// <param name="relativeUrl">相对URL</param>
        /// <param name="paras">参数</param>
        void PostOnly(string relativeUrl, params object[] paras);
    }
}
