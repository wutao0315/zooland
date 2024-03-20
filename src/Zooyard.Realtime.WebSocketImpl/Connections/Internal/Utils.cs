namespace Zooyard.WebSocketsImpl.Connections.Internal;

internal static class Utils
{
    public static Uri AppendPath(Uri url, string path)
    {
        var builder = new UriBuilder(url);
        if (!builder.Path.EndsWith("/"))
        {
            builder.Path += "/";
        }
        builder.Path += path;
        return builder.Uri;
    }

    internal static Uri AppendQueryString(Uri url, string qs)
    {
        if (string.IsNullOrEmpty(qs))
        {
            return url;
        }

        var builder = new UriBuilder(url);
        var newQueryString = builder.Query;
        if (!string.IsNullOrEmpty(builder.Query))
        {
            newQueryString += "&";
        }
        newQueryString += qs;

        if (newQueryString.Length > 0 && newQueryString[0] == '?')
        {
            newQueryString = newQueryString.Substring(1);
        }

        builder.Query = newQueryString;
        return builder.Uri;
    }
}
