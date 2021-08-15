using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Zooyard
{
    /// <summary>
    /// The type Constants.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// default charset name
        /// </summary>
        public const string DEFAULT_CHARSET_NAME = "UTF-8";
        /// <summary>
        /// default charset
        /// </summary>
        public static readonly Encoding DEFAULT_CHARSET = Encoding.GetEncoding(DEFAULT_CHARSET_NAME);

        public static string FILE_SPLIT = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\" : "/";

        public static string DefaultDumpDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\opt\dump\" : "/opt/dump/";
    }
}
