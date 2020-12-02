using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Zooyard.Core.Logging;

namespace Zooyard.Core.Utils
{
    public sealed class StringUtils
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(StringUtils));
        //public static readonly string[] EMPTY_STRING_ARRAY = new string[0];

        private static readonly Regex KVP_PATTERN = new Regex("([_.a-zA-Z0-9][-_.a-zA-Z0-9]*)[=](.*)", RegexOptions.Compiled);  //key value pair pattern.

        //private static readonly Regex INT_PATTERN = new Regex("^\\d+$", RegexOptions.Compiled);
        
        /// <summary>
        /// parse key-value pair.
        /// </summary>
        /// <param name="str"> string. </param>
        /// <param name="itemSeparator"> item separator. </param>
        /// <returns> key-value map; </returns>
        private static IDictionary<string, string> parseKeyValuePair(string str, string itemSeparator)
        {
            var tmp = str.Split(new[] { itemSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var map = new Dictionary<string, string>(tmp.Length);
            for (int i = 0; i < tmp.Length; i++)
            {
                Match matcher = KVP_PATTERN.Match(tmp[i]);
                if (matcher.Success == false)
                {
                    continue;
                }
                map[matcher.Groups[1].Value] = matcher.Groups[2].Value;
            }
            return map;
        }

        /// <summary>
        /// parse query string to Parameters.
        /// </summary>
        /// <param name="qs"> query string. </param>
        /// <returns> Parameters instance. </returns>
        public static IDictionary<string, string> ParseQueryString(string qs)
        {
            if (qs == null || qs.Length == 0)
            {
                return new Dictionary<string, string>();
            }
            return parseKeyValuePair(qs, "&");
        }

        //public static string GetServiceKey(IDictionary<string, string> ps)
        //{
        //    var buf = new StringBuilder();

        //    if (ps.ContainsKey(URL.GROUP_KEY)) 
        //    {
        //        var group = ps[URL.GROUP_KEY];
        //        if (!string.IsNullOrEmpty(group))
        //        {
        //            buf.Append(group).Append("/");
        //        }
        //    }
            
        //    buf.Append(ps[URL.INTERFACE_KEY]);
        //    if (ps.ContainsKey(URL.VERSION_KEY)) 
        //    {
        //        var version = ps[URL.VERSION_KEY];
        //        if (!string.IsNullOrEmpty(version))
        //        {
        //            buf.Append(":").Append(version);
        //        }
        //    }
           
        //    return buf.ToString();
        //}

        //public static string ToQueryString(IDictionary<string, string> ps)
        //{
        //    var buf = new StringBuilder();
        //    if (ps != null && ps.Count > 0)
        //    {
        //        foreach (KeyValuePair<string, string> entry in (new SortedDictionary<string, string>(ps)))
        //        {
        //            var key = entry.Key;
        //            var value = entry.Value;
        //            if (!string.IsNullOrEmpty(key)&&!string.IsNullOrEmpty(value))
        //            {
        //                if (buf.Length > 0)
        //                {
        //                    buf.Append("&");
        //                }
        //                buf.Append(key);
        //                buf.Append("=");
        //                buf.Append(value);
        //            }
        //        }
        //    }
        //    return buf.ToString();
        //}

        //public static string CamelToSplitName(string camelName, string split)
        //{
        //    if (camelName == null || camelName.Length == 0)
        //    {
        //        return camelName;
        //    }
        //    StringBuilder buf = null;
        //    for (int i = 0; i < camelName.Length; i++)
        //    {
        //        char ch = camelName[i];
        //        if (ch >= 'A' && ch <= 'Z')
        //        {
        //            if (buf == null)
        //            {
        //                buf = new StringBuilder();
        //                if (i > 0)
        //                {
        //                    buf.Append(camelName.Substring(0, i));
        //                }
        //            }
        //            if (i > 0)
        //            {
        //                buf.Append(split);
        //            }
        //            buf.Append(char.ToLower(ch));
        //        }
        //        else if (buf != null)
        //        {
        //            buf.Append(ch);
        //        }
        //    }
        //    return buf == null ? camelName : buf.ToString();
        //}

        public static string ToArgumentString(ParameterInfo[] parameterInfos, object[] args)
        {
            var buf = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.GetType().IsValueType || arg.GetType().IsPrimitive || arg is string)
                {
                    buf.Append(parameterInfos[i].Name);
                    buf.Append("=");
                    buf.Append(arg);
                }
                else
                {
                    try
                    {
                        //buf.Append(Newtonsoft.Json.JsonConvert.SerializeObject(arg));
                        buf.Append(arg);
                    }
                    catch (Exception e)
                    {
                        Logger().LogWarning(e, e.Message);
                        buf.Append(arg);
                    }
                }
                buf.Append(",");
            }

            if (buf.Length > 0)
            {
                buf.Length -= 1;
            }
            return buf.ToString();
        }
        public static string ToParameterString(ParameterInfo[] parameterInfos)
        {
            var buf = new StringBuilder("_");

            foreach (var item in parameterInfos)
            {
                buf.Append(item.Name);
                buf.Append("_");
            }

            buf.Length -= 1;

            return buf.ToString();
        }

        public static string Md5(string input)
        {
            var md5Hasher = new MD5CryptoServiceProvider();
            var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            var sbuilder = new StringBuilder();
            foreach (var item in data)
            {
                sbuilder.Append(item.ToString("x2"));
            }
            return sbuilder.ToString();
        }
    }
}
