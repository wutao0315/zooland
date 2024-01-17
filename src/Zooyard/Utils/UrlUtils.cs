using System.Text;

namespace Zooyard.Utils;

public class UrlUtils
{
    public static bool IsMatchGlobPattern(string pattern, string? value, URL? param)
    {
        if (param != null && pattern.StartsWith("$"))
        {
            pattern = param.GetRawParameter(pattern.Substring(1))!;
        }
        return IsMatchGlobPattern(pattern, value);
    }

    public static bool IsMatchGlobPattern(string? pattern, string? value)
    {
        if ("*".Equals(pattern))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(pattern) && string.IsNullOrWhiteSpace(value))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        int i = pattern.LastIndexOf('*');
        // doesn't find "*"
        if (i == -1)
        {
            return value.Equals(pattern);
        }
        // "*" is at the end
        else if (i == pattern.Length - 1)
        {
            return value.StartsWith(pattern.Substring(0, i));
        }
        // "*" is at the beginning
        else if (i == 0)
        {
            return value.EndsWith(pattern.Substring(i + 1));
        }
        // "*" is in the middle
        else
        {
            string prefix = pattern.Substring(0, i);
            string suffix = pattern.Substring(i + 1);
            return value.StartsWith(prefix) && value.EndsWith(suffix);
        }
    }
}
