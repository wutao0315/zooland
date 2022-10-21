using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Zooyard.Logging;

namespace Zooyard.Utils;

public sealed class StringUtils
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(StringUtils));

    private static readonly Regex KVP_PATTERN = new ("([_.a-zA-Z0-9][-_.a-zA-Z0-9]*)[=](.*)", RegexOptions.Compiled);  //key value pair pattern.

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

    public static string ToArgumentString(ParameterInfo[] parameterInfos, object[] args)
    {
        var buf = new StringBuilder();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.GetType().IsValueType || arg.GetType().IsPrimitive || arg is string)
            {
                buf.Append(parameterInfos[i].Name);
                buf.Append('=');
                buf.Append(arg);
            }
            else
            {
                try
                {
                    buf.Append(arg);
                }
                catch (Exception e)
                {
                    Logger().LogWarning(e, e.Message);
                    buf.Append(arg);
                }
            }
            buf.Append(',');
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
            buf.Append('_');
        }

        buf.Length -= 1;

        return buf.ToString();
    }

    public static string Md5(string input)
    {
        var md5Hasher = MD5.Create();
        var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
        var sbuilder = new StringBuilder();
        foreach (var item in data)
        {
            sbuilder.Append(item.ToString("x2"));
        }
        return sbuilder.ToString();
    }
}
