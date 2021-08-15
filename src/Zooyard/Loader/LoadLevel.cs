using System;
using System.Collections.Generic;
using System.Text;

namespace Zooyard.Loader
{
    /// <summary>
    /// The interface Load level.
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class LoadLevel : System.Attribute
    {
        /// <summary>
        /// Name string.
        /// </summary>
        /// <returns> the string </returns>
        internal string name;

        /// <summary>
        /// Order int.
        /// </summary>
        /// <returns> the int </returns>
        internal int order;
        /// <summary>
		/// Scope enum.
		/// @return
		/// </summary>
		internal Scope scope;

        public LoadLevel(string name, int order = 0, Scope scope = Scope.SINGLETON)
        {
            this.name = name;
            this.order = order;
            this.scope = scope;
        }
    }
    /// <summary>
	/// the scope of the extension
	/// 
	/// @author haozhibei
	/// </summary>
	public enum Scope
    {
        /// <summary>
        /// The extension will be loaded in singleton mode
        /// </summary>
        SINGLETON,

        /// <summary>
        /// The extension will be loaded in multi instance mode
        /// </summary>
        PROTOTYPE

    }
}
