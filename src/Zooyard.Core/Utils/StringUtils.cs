using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Zooyard.Core.Utils
{
    public sealed class StringUtils
    {
        private static readonly ILog _logger = LogManager.GetLogger("StringUtils");
        public static readonly string[] EMPTY_STRING_ARRAY = new string[0];

        private static readonly Regex KVP_PATTERN = new Regex("([_.a-zA-Z0-9][-_.a-zA-Z0-9]*)[=](.*)", RegexOptions.Compiled);  //key value pair pattern.

        private static readonly Regex INT_PATTERN = new Regex("^\\d+$", RegexOptions.Compiled);

        public static bool isBlank(string str)
        {
            if (str == null || str.Length == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// is empty string.
        /// </summary>
        /// <param name="str"> source string. </param>
        /// <returns> is empty. </returns>
        public static bool isEmpty(string str)
        {
            if (str == null || str.Length == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// is not empty string.
        /// </summary>
        /// <param name="str"> source string. </param>
        /// <returns> is not empty. </returns>
        public static bool isNotEmpty(string str)
        {
            return str != null && str.Length > 0;
        }

        /// 
        /// <param name="s1"> </param>
        /// <param name="s2"> </param>
        /// <returns> equals </returns>
        public static bool isEquals(string s1, string s2)
        {
            if (s1 == null && s2 == null)
            {
                return true;
            }
            if (s1 == null || s2 == null)
            {
                return false;
            }
            return s1.Equals(s2);
        }

        /// <summary>
        /// is integer string.
        /// </summary>
        /// <param name="str"> </param>
        /// <returns> is integer </returns>
        public static bool isInteger(string str)
        {
            if (str == null || str.Length == 0)
            {
                return false;
            }
            return INT_PATTERN.IsMatch(str);
        }

        public static int parseInteger(string str)
        {
            if (!isInteger(str))
            {
                return 0;
            }
            return Convert.ToInt32(str);
        }

        public static bool isContains(string values, string value)
        {
            if (values == null || values.Length == 0)
            {
                return false;
            }
            return isContains(URL.COMMA_SPLIT_PATTERN.Split(values), value);
        }

        /// 
        /// <param name="values"> </param>
        /// <param name="value"> </param>
        /// <returns> contains </returns>
        public static bool isContains(string[] values, string value)
        {
            if (value != null && value.Length > 0 && values != null && values.Length > 0)
            {
                foreach (string v in values)
                {
                    if (value.Equals(v))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool isNumeric(string str)
        {
            if (str == null)
            {
                return false;
            }
            int sz = str.Length;
            for (int i = 0; i < sz; i++)
            {
                if (char.IsDigit(str[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// 
        /// <param name="e"> </param>
        /// <returns> string </returns>
        public static string ToString(Exception e)
        {
            
            //UnsafeStringWriter w = new UnsafeStringWriter();
            //PrintWriter p = new PrintWriter(w);
            //JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
            //p.print(e.GetType().FullName);
            Console.Write(e.GetType().FullName);
            if (e.Message != null)
            {
                //p.print(": " + e.Message);
                Console.Write(": " + e.Message);
            }
            Console.WriteLine();
            //p.println();
            try
            {
                Console.Write(e);
                //e.printStackTrace(p);
                return e.Message.ToString();
            }
            finally
            {
                //p.close();
            }
        }

        /// 
        /// <param name="msg"> </param>
        /// <param name="e"> </param>
        /// <returns> string </returns>
        public static string ToString(string msg, Exception e)
        {
            Console.WriteLine(msg);
            Console.Write(e);
            return e.Message;
            //UnsafeStringWriter w = new UnsafeStringWriter();
            //w.write(msg + "\n");
            //PrintWriter p = new PrintWriter(w);
            //try
            //{
            //    e.printStackTrace(p);
            //    return w.ToString();
            //}
            //finally
            //{
            //    p.close();
            //}
        }

        /// <summary>
        /// translat.
        /// </summary>
        /// <param name="src"> source string. </param>
        /// <param name="from"> src char table. </param>
        /// <param name="to"> target char table. </param>
        /// <returns> String. </returns>
        public static string translat(string src, string from, string to)
        {
            if (isEmpty(src))
            {
                return src;
            }
            StringBuilder sb = null;
            int ix;
            char c;
            for (int i = 0, len = src.Length; i < len; i++)
            {
                c = src[i];
                ix = from.IndexOf(c);
                if (ix == -1)
                {
                    if (sb != null)
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder(len);
                        sb.Append(src, 0, i);
                    }
                    if (ix < to.Length)
                    {
                        sb.Append(to[ix]);
                    }
                }
            }
            return sb == null ? src : sb.ToString();
        }

        /// <summary>
        /// split.
        /// </summary>
        /// <param name="ch"> char. </param>
        /// <returns> string array. </returns>
        public static string[] Split(string str, char ch)
        {
            IList<string> list = null;
            char c;
            int ix = 0, len = str.Length;
            for (int i = 0; i < len; i++)
            {
                c = str[i];
                if (c == ch)
                {
                    if (list == null)
                    {
                        list = new List<string>();
                    }
                    list.Add(str.Substring(ix, i - ix));
                    ix = i + 1;
                }
            }
            if (ix > 0)
            {
                list.Add(str.Substring(ix));
            }
            return list == null ? EMPTY_STRING_ARRAY : list.ToArray();
            //return list == null ? EMPTY_STRING_ARRAY : (string[])list.toArray(EMPTY_STRING_ARRAY);
        }

        /// <summary>
        /// join string.
        /// </summary>
        /// <param name="array"> String array. </param>
        /// <returns> String. </returns>
        public static string join(string[] array)
        {
            if (array.Length == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            foreach (string s in array)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        /// <summary>
        /// join string like javascript.
        /// </summary>
        /// <param name="array"> String array. </param>
        /// <param name="split"> split </param>
        /// <returns> String. </returns>
        public static string join(string[] array, char split)
        {
            if (array.Length == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(split);
                }
                sb.Append(array[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// join string like javascript.
        /// </summary>
        /// <param name="array"> String array. </param>
        /// <param name="split"> split </param>
        /// <returns> String. </returns>
        public static string join(string[] array, string split)
        {
            if (array.Length == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(split);
                }
                sb.Append(array[i]);
            }
            return sb.ToString();
        }

        public static string join(ICollection<string> coll, string split)
        {
            if (coll.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            bool isFirst = true;
            foreach (string s in coll)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(split);
                }
                sb.Append(s);
            }
            return sb.ToString();
        }

        /// <summary>
        /// parse key-value pair.
        /// </summary>
        /// <param name="str"> string. </param>
        /// <param name="itemSeparator"> item separator. </param>
        /// <returns> key-value map; </returns>
        private static IDictionary<string, string> parseKeyValuePair(string str, string itemSeparator)
        {
            string[] tmp = str.Split(new[] { itemSeparator }, StringSplitOptions.RemoveEmptyEntries);
            IDictionary<string, string> map = new Dictionary<string, string>(tmp.Length);
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

        public static string getQueryStringValue(string qs, string key)
        {
            IDictionary<string, string> map = StringUtils.parseQueryString(qs);
            return map[key];
        }

        /// <summary>
        /// parse query string to Parameters.
        /// </summary>
        /// <param name="qs"> query string. </param>
        /// <returns> Parameters instance. </returns>
        public static IDictionary<string, string> parseQueryString(string qs)
        {
            if (qs == null || qs.Length == 0)
            {
                return new Dictionary<string, string>();
            }
            return parseKeyValuePair(qs, "&");
        }

        public static string getServiceKey(IDictionary<string, string> ps)
        {
            var buf = new StringBuilder();

            if (ps.ContainsKey(URL.GROUP_KEY)) 
            {
                var group = ps[URL.GROUP_KEY];
                if (!string.IsNullOrEmpty(group))
                {
                    buf.Append(group).Append("/");
                }
            }
            
            buf.Append(ps[URL.INTERFACE_KEY]);
            if (ps.ContainsKey(URL.VERSION_KEY)) 
            {
                var version = ps[URL.VERSION_KEY];
                if (!string.IsNullOrEmpty(version))
                {
                    buf.Append(":").Append(version);
                }
            }
           
            return buf.ToString();
        }

        public static string toQueryString(IDictionary<string, string> ps)
        {
            var buf = new StringBuilder();
            if (ps != null && ps.Count > 0)
            {
                foreach (KeyValuePair<string, string> entry in (new SortedDictionary<string, string>(ps)))
                {
                    var key = entry.Key;
                    var value = entry.Value;
                    if (!string.IsNullOrEmpty(key)&&!string.IsNullOrEmpty(value))
                    {
                        if (buf.Length > 0)
                        {
                            buf.Append("&");
                        }
                        buf.Append(key);
                        buf.Append("=");
                        buf.Append(value);
                    }
                }
            }
            return buf.ToString();
        }

        public static string camelToSplitName(string camelName, string split)
        {
            if (camelName == null || camelName.Length == 0)
            {
                return camelName;
            }
            StringBuilder buf = null;
            for (int i = 0; i < camelName.Length; i++)
            {
                char ch = camelName[i];
                if (ch >= 'A' && ch <= 'Z')
                {
                    if (buf == null)
                    {
                        buf = new StringBuilder();
                        if (i > 0)
                        {
                            buf.Append(camelName.Substring(0, i));
                        }
                    }
                    if (i > 0)
                    {
                        buf.Append(split);
                    }
                    buf.Append(char.ToLower(ch));
                }
                else if (buf != null)
                {
                    buf.Append(ch);
                }
            }
            return buf == null ? camelName : buf.ToString();
        }
        public static string ToArgumentString(ParameterInfo[] parameterInfos,object[] args)
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
                        buf.Append(Newtonsoft.Json.JsonConvert.SerializeObject(arg));
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e.Message, e);
                        buf.Append(arg);
                    }
                }
                buf.Append(",");
            }

            if (buf.Length > 0)
            {
                buf.Length = buf.Length - 1;
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

            buf.Length = buf.Length - 1;

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
