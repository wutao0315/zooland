using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ZooTa.Config
{
    /// <summary>
    /// The interface Config change listener.
    /// 
    /// @author jimin.jm @alibaba-inc.com
    /// @date 2018 /12/20
    /// </summary>
    public interface IConfigChangeListener
    {

        /// <summary>
        /// Gets executor.
        /// </summary>
        /// <returns> the executor </returns>
        TaskFactory Executor { get; }

        /// <summary>
        /// Receive config info.
        /// </summary>
        /// <param name="configInfo"> the config info </param>
        void ReceiveConfigInfo(string configInfo);
    }
}
