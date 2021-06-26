using System.Reflection;
using System.Threading.Tasks;

namespace Zooyard.Core.DynamicProxy
{
    public interface IInterceptor
    {
        /// <summary>
        /// 代理模块
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        object Intercept(object obj, string methodName, params object[] args);
    }
}
