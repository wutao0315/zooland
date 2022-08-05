using System.Text;

namespace Zooyard.Utils;

public class UrlUtils
{
    public static bool isMatchGlobPattern(String pattern, String value, URL param)
    {
        if (param != null && pattern.StartsWith("$"))
        {
            pattern = param.GetRawParameter(pattern.Substring(1));
        }
        return isMatchGlobPattern(pattern, value);
    }

    public static bool isMatchGlobPattern(String pattern, String value)
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


    //in the url string,mark the param begin
    private const string URL_PARAM_STARTING_SYMBOL = "?";

    //public static URL parseURL(String address, Map<String, String> defaults)
    //{
    //    if (string.IsNullOrWhiteSpace(address))
    //    {
    //        throw new ArgumentException("Address is not allowed to be empty, please re-enter.");
    //    }
    //    String url;
    //    if (address.Contains("://") || address.Contains(URL_PARAM_STARTING_SYMBOL))
    //    {
    //        url = address;
    //    }
    //    else
    //    {
    //        string[] addresses = COMMA_SPLIT_PATTERN.split(address);
    //        url = addresses[0];
    //        if (addresses.Length > 1)
    //        {
    //            var backup = new StringBuilder();
    //            for (int i = 1; i < addresses.Length; i++)
    //            {
    //                if (i > 1)
    //                {
    //                    backup.Append(',');
    //                }
    //                backup.Append(addresses[i]);
    //            }
    //            url += URL_PARAM_STARTING_SYMBOL + RemotingConstants.BACKUP_KEY + "=" + backup.ToString();
    //        }
    //    }
    //    String defaultProtocol = defaults == null ? null : defaults.get(PROTOCOL_KEY);
    //    if (string.IsNullOrWhiteSpace(defaultProtocol))
    //    {
    //        defaultProtocol = DUBBO_PROTOCOL;
    //    }
    //    String defaultUsername = defaults == null ? null : defaults.get(USERNAME_KEY);
    //    String defaultPassword = defaults == null ? null : defaults.get(PASSWORD_KEY);
    //    int defaultPort = StringUtils.parseInteger(defaults == null ? null : defaults.get(PORT_KEY));
    //    String defaultPath = defaults == null ? null : defaults.get(PATH_KEY);
    //    Dictionary<String, String> defaultParameters = defaults == null ? null : new(defaults);
    //    if (defaultParameters != null)
    //    {
    //        defaultParameters.remove(PROTOCOL_KEY);
    //        defaultParameters.remove(USERNAME_KEY);
    //        defaultParameters.remove(PASSWORD_KEY);
    //        defaultParameters.remove(HOST_KEY);
    //        defaultParameters.remove(PORT_KEY);
    //        defaultParameters.remove(PATH_KEY);
    //    }
    //    URL u = URL.CacheableValueOf(url);
    //    bool changed = false;
    //    String protocol = u.getProtocol();
    //    String username = u.getUsername();
    //    String password = u.getPassword();
    //    String host = u.getHost();
    //    int port = u.getPort();
    //    String path = u.getPath();
    //    Dictionary<String, String> parameters = new Dictionary<>(u.getParameters());
    //    if (StringUtils.isEmpty(protocol))
    //    {
    //        changed = true;
    //        protocol = defaultProtocol;
    //    }
    //    if (StringUtils.isEmpty(username) && StringUtils.isNotEmpty(defaultUsername))
    //    {
    //        changed = true;
    //        username = defaultUsername;
    //    }
    //    if (StringUtils.isEmpty(password) && StringUtils.isNotEmpty(defaultPassword))
    //    {
    //        changed = true;
    //        password = defaultPassword;
    //    }
    //    /*if (u.isAnyHost() || u.isLocalHost()) {
    //        changed = true;
    //        host = NetUtils.getLocalHost();
    //    }*/
    //    if (port <= 0)
    //    {
    //        if (defaultPort > 0)
    //        {
    //            changed = true;
    //            port = defaultPort;
    //        }
    //        else
    //        {
    //            changed = true;
    //            port = 9090;
    //        }
    //    }
    //    if (StringUtils.isEmpty(path))
    //    {
    //        if (StringUtils.isNotEmpty(defaultPath))
    //        {
    //            changed = true;
    //            path = defaultPath;
    //        }
    //    }
    //    if (defaultParameters != null && defaultParameters.Count() > 0)
    //    {
    //        foreach (var entry in defaultParameters)
    //        {
    //            String key = entry.Key;
    //            String defaultValue = entry.Value;
    //            if (StringUtils.isNotEmpty(defaultValue))
    //            {
    //                String value = parameters.get(key);
    //                if (StringUtils.isEmpty(value))
    //                {
    //                    changed = true;
    //                    parameters.put(key, defaultValue);
    //                }
    //            }
    //        }
    //    }
    //    if (changed)
    //    {
    //        u = new ServiceConfigURL(protocol, username, password, host, port, path, parameters);
    //    }
    //    return u;
    //}

    //public static List<URL> parseURLs(string address, Dictionary<string, string> defaults)
    //{
    //    if (string.IsNullOrWhiteSpace(address))
    //    {
    //        throw new ArgumentException("Address is not allowed to be empty, please re-enter.");
    //    }
    //    String[] addresses = REGISTRY_SPLIT_PATTERN.split(address);
    //    if (addresses == null || addresses.length == 0)
    //    {
    //        throw new ArgumentException("Addresses is not allowed to be empty, please re-enter."); //here won't be empty
    //    }
    //    List<URL> registries = new List<URL>();
    //    foreach (String addr in addresses)
    //    {
    //        registries.Add(parseURL(addr, defaults));
    //    }
    //    return registries;
    //}


    ///**
    // * Check if the given value matches the given pattern. The pattern supports wildcard "*".
    // *
    // * @param pattern pattern
    // * @param value   value
    // * @return true if match otherwise false
    // */
    //static boolean isItemMatch(String pattern, String value)
    //{
    //    if (StringUtils.isEmpty(pattern))
    //    {
    //        return value == null;
    //    }
    //    else
    //    {
    //        return "*".equals(pattern) || pattern.equals(value);
    //    }
    //}

    ///**
    // * @param serviceKey, {group}/{interfaceName}:{version}
    // * @return [group, interfaceName, version]
    // */
    //public static String[] parseServiceKey(String serviceKey)
    //{
    //    String[] arr = new String[3];
    //    int i = serviceKey.indexOf('/');
    //    if (i > 0)
    //    {
    //        arr[0] = serviceKey.substring(0, i);
    //        serviceKey = serviceKey.substring(i + 1);
    //    }

    //    int j = serviceKey.indexOf(':');
    //    if (j > 0)
    //    {
    //        arr[2] = serviceKey.substring(j + 1);
    //        serviceKey = serviceKey.substring(0, j);
    //    }
    //    arr[1] = serviceKey;
    //    return arr;
    //}

    ///**
    // * NOTICE: This method allocate too much objects, we can use {@link URLStrParser#parseDecodedStr(String)} instead.
    // * <p>
    // * Parse url string
    // *
    // * @param url URL string
    // * @return URL instance
    // * @see URL
    // */
    //public static URL valueOf(String url)
    //{
    //    if (url == null || (url = url.trim()).length() == 0)
    //    {
    //        throw new IllegalArgumentException("url == null");
    //    }
    //    String protocol = null;
    //    String username = null;
    //    String password = null;
    //    String host = null;
    //    int port = 0;
    //    String path = null;
    //    Map<String, String> parameters = null;
    //    int i = url.indexOf('?'); // separator between body and parameters
    //    if (i >= 0)
    //    {
    //        String[] parts = url.substring(i + 1).split("&");
    //        parameters = new HashMap<>();
    //        for (String part : parts)
    //        {
    //            part = part.trim();
    //            if (part.length() > 0)
    //            {
    //                int j = part.indexOf('=');
    //                if (j >= 0)
    //                {
    //                    String key = part.substring(0, j);
    //                    String value = part.substring(j + 1);
    //                    parameters.put(key, value);
    //                    // compatible with lower versions registering "default." keys
    //                    if (key.startsWith(DEFAULT_KEY_PREFIX))
    //                    {
    //                        parameters.putIfAbsent(key.substring(DEFAULT_KEY_PREFIX.length()), value);
    //                    }
    //                }
    //                else
    //                {
    //                    parameters.put(part, part);
    //                }
    //            }
    //        }
    //        url = url.substring(0, i);
    //    }
    //    i = url.indexOf("://");
    //    if (i >= 0)
    //    {
    //        if (i == 0)
    //        {
    //            throw new IllegalStateException("url missing protocol: \"" + url + "\"");
    //        }
    //        protocol = url.substring(0, i);
    //        url = url.substring(i + 3);
    //    }
    //    else
    //    {
    //        // case: file:/path/to/file.txt
    //        i = url.indexOf(":/");
    //        if (i >= 0)
    //        {
    //            if (i == 0)
    //            {
    //                throw new IllegalStateException("url missing protocol: \"" + url + "\"");
    //            }
    //            protocol = url.substring(0, i);
    //            url = url.substring(i + 1);
    //        }
    //    }

    //    i = url.indexOf('/');
    //    if (i >= 0)
    //    {
    //        path = url.substring(i + 1);
    //        url = url.substring(0, i);
    //    }
    //    i = url.lastIndexOf('@');
    //    if (i >= 0)
    //    {
    //        username = url.substring(0, i);
    //        int j = username.indexOf(':');
    //        if (j >= 0)
    //        {
    //            password = username.substring(j + 1);
    //            username = username.substring(0, j);
    //        }
    //        url = url.substring(i + 1);
    //    }
    //    i = url.lastIndexOf(':');
    //    if (i >= 0 && i < url.length() - 1)
    //    {
    //        if (url.lastIndexOf('%') > i)
    //        {
    //            // ipv6 address with scope id
    //            // e.g. fe80:0:0:0:894:aeec:f37d:23e1%en0
    //            // see https://howdoesinternetwork.com/2013/ipv6-zone-id
    //            // ignore
    //        }
    //        else
    //        {
    //            port = Integer.parseInt(url.substring(i + 1));
    //            url = url.substring(0, i);
    //        }
    //    }
    //    if (url.length() > 0)
    //    {
    //        host = url;
    //    }

    //    return new ServiceConfigURL(protocol, username, password, host, port, path, parameters);
    //}

}
