using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Zooyard.Utils;

namespace Zooyard;

public sealed record URL
{
    public static readonly Regex COMMA_SPLIT_PATTERN = new("\\s*[,]+\\s*", RegexOptions.Compiled);
    public const string BACKUP_KEY = "backup";
    public const string DEFAULT_KEY_PREFIX = "default.";
    public const string LOCALHOST_KEY = "localhost";
    public const string ANYHOST_KEY = "anyhost";
    public const string ANYHOST_VALUE = "0.0.0.0";

    public const string GROUP_KEY = "group";
    public const string VERSION_KEY = "version";
    public const string INTERFACE_KEY = "interface";
    public const string APP_KEY = "app";
    public const string PROTOCOL_KEY = "protocol";
    public const string USERNAME_KEY = "username";
    public const string PASSWORD_KEY = "password";
    public const string HOST_KEY = "host";
    public const string PORT_KEY = "port";
    public const string PATH_KEY = "path";

    //private readonly IDictionary<string, string> parameters;

    // ==== cache ====
    private volatile IDictionary<string, IConvertible>? numbers;//Number to long
    private volatile IDictionary<string, URL>? urls;
    private volatile string? ip;
    private volatile string? full;
    private volatile string? identity;
    private volatile string? parameter;
    //private volatile string? @string;

    internal URL()
    {
    }

    public URL(string protocol, string host, int port, string path = "", string username = "", string password = "", IDictionary<string, string>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Invalid url, password without username!");
        }
        Protocol = protocol;
        Username = username;
        Password = password;
        Host = host;
        Port = (port < 0 ? 0 : port);
        // trim the beginning "/"
        while (path.StartsWith('/'))
        {
            path = path[1..];
        }
        Path = path;
        Parameters = parameters == null ? [] : new Dictionary<string, string>(parameters);
    }

    /// <summary>
    /// Parse url string
    /// </summary>
    /// <param name="url"> URL string </param>
    /// <returns> URL instance </returns>
    /// <seealso cref="URL"> </seealso>
    public static URL ValueOf(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("url == null");
        }
        string protocol = string.Empty;
        string username = string.Empty;
        string password = string.Empty;
        string host = string.Empty;
        int port = 0;
        string path = string.Empty;
        var parameters = new Dictionary<string, string>();
        int i = url.IndexOf('?'); // seperator between body and parameters
        if (i >= 0)
        {
            var parts = url[(i + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries);
            parameters = [];
            foreach (var part in parts)
            {
                var partinner = part.Trim();
                if (partinner.Length > 0)
                {
                    int j = partinner.IndexOf('=');
                    if (j >= 0)
                    {
                        parameters[partinner[..j]] = partinner[(j + 1)..];
                    }
                    else
                    {
                        parameters[part] = partinner;
                    }
                }
            }
            url = url[..i];
        }
        i = url.IndexOf("://");
        if (i >= 0)
        {
            if (i == 0)
            {
                throw new Exception("url missing protocol: \"" + url + "\"");
            }
            protocol = url[..i];
            url = url[(i + 3)..];
        }
        else
        {
            i = url.IndexOf(":/");
            if (i >= 0)
            {
                if (i == 0)
                {
                    throw new Exception("url missing protocol: \"" + url + "\"");
                }
                protocol = url[..i];
                url = url[(i + 1)..];
            }
        }

        i = url.IndexOf('/');
        if (i >= 0)
        {
            path = url[(i + 1)..];
            url = url[..i];
        }
        i = url.IndexOf('@');
        if (i >= 0)
        {
            username = url[..i];
            int j = username.IndexOf(':');
            if (j >= 0)
            {
                password = username[(j + 1)..];
                username = username[..j];
            }
            url = url[(i + 1)..];
        }
        i = url.IndexOf(':');
        if (i >= 0 && i < url.Length - 1)
        {
            port = Convert.ToInt32(url[(i + 1)..]);
            url = url[..i];
        }
        if (url.Length > 0)
        {
            host = url;
        }
        return new URL(protocol,  host, port, path, username, password, parameters);
    }

    public string Protocol { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Authority
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Username) 
                && string.IsNullOrWhiteSpace(Password))
            {
                return "";
            }
            return $"{Username ?? ""}:{Password ?? ""}";
        }
    }

    public string Host { get; private set; } = string.Empty;

    /// <summary>
    /// 获取IP地址.
    /// 
    /// 请注意：
    /// 如果和Socket的地址对比，
    /// 或用地址作为Map的Key查找，
    /// 请使用IP而不是Host，
    /// 否则配置域名会有问题
    /// </summary>
    /// <returns> ip </returns>
    public string? Ip
    {
        get
        {
            if (ip == null && !string.IsNullOrWhiteSpace(Host))
            {
                ip = NetUtil.GetIpByHost(Host);
            }
            return ip;
        }
    }

    public int Port { get; init; }

    public int GetPort(int defaultPort)
    {
        return Port <= 0 ? defaultPort : Port;
    }

    public string? Address
    {
        get=> Port <= 0 ? Host : $"{Host}:{Port}";
    }

    public string BackupAddress
    {
        get
        {
            return GetBackupAddress(0);
        }
    }

    public string GetBackupAddress(int defaultPort)
    {
        var address = new StringBuilder(AppendDefaultPort(Address, defaultPort));
        var backups = GetParameter(BACKUP_KEY, Array.Empty<string>());
        if (backups != null && backups.Length > 0)
        {
            foreach (var backup in backups)
            {
                address.Append(',');
                address.Append(AppendDefaultPort(backup, defaultPort));
            }
        }
        return address.ToString();
    }

    public IList<URL> BackupUrls
    {
        get
        {
            var urls = new List<URL>
            {
                this
            };
            string[]? backups = GetParameter(BACKUP_KEY, Array.Empty<string>());
            if (backups?.Length > 0)
            {
                foreach (var backup in backups)
                {
                    urls.Add(SetAddress(backup));
                }
            }
            return urls;
        }
    }

    private string? AppendDefaultPort(string? address, int defaultPort)
    {
        if (!string.IsNullOrEmpty(address) && defaultPort > 0)
        {
            int i = address.IndexOf(':');
            if (i < 0)
            {
                return address + ":" + defaultPort;
            }
            else if (Convert.ToInt32(address[(i + 1)..]) == 0)
            {
                return address[..(i + 1)] + defaultPort;
            }
        }
        return address;
    }

    public string Path { get; init; } = string.Empty;

    public string? AbsolutePath
    {
        get
        {
            if (Path != null && !Path.StartsWith('/'))
            {
                return "/" + Path;
            }
            return Path;
        }
    }

    public URL SetProtocol(string protocol)
    {
        return this with { Protocol = protocol };
    }

    public URL SetUsername(string username)
    {
        return this with { Username = username };
    }

    public URL SetPassword(string password)
    {
        return this with { Password = password };
    }

    public URL SetAddress(string address)
    {
        int i = address.LastIndexOf(':');
        string host;
        int port = Port;
        if (i >= 0)
        {
            host = address[..i];
            port = Convert.ToInt32(address[(i + 1)..]);
        }
        else
        {
            host = address;
        }
        return this with { Host = host, Port = port };
    }

    public URL SetHost(string host)
    {
        return this with { Host = host };
    }

    public URL SetPort(int port)
    {
        return this with { Port = port };
    }

    public URL SetPath(string path)
    {
        return this with { Path = path };
    }

    public Dictionary<string, string> Parameters { get; init; } = [];

    public string GetParameterAndDecoded(string key)
    {
        return GetParameterAndDecoded(key, "");
    }

    public string GetParameterAndDecoded(string key, string defaultValue)
    {
        return Decode(GetParameter(key, defaultValue));
    }

    public string? GetParameter(string key)
    {
        if (Parameters == null) 
        {
            return null;
        }
        if (Parameters.ContainsKey(key))
        {
            return Parameters[key];
        }

        if (Parameters.ContainsKey(DEFAULT_KEY_PREFIX + key))
        {
            return Parameters[DEFAULT_KEY_PREFIX + key];
        }
        return null;
    }

    public string GetParameter(string key, string defaultValue)
    {
        string? value = GetParameter(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return value;
    }

    public string[] GetParameter(string key, string[] defaultValue)
    {
        string? value = GetParameter(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return COMMA_SPLIT_PATTERN.Split(value);
    }

    private IDictionary<string, IConvertible> Numbers
    {
        get
        {
            // 允许并发重复创建
            numbers ??= new ConcurrentDictionary<string, IConvertible>();
            return numbers;
        }
    }

    private IDictionary<string, URL> Urls
    {
        get
        {
            // 允许并发重复创建
            urls ??= new ConcurrentDictionary<string, URL>();
            return urls;
        }
    }

    public URL? GetUrlParameter(string key)
    {
        if (Urls.ContainsKey(key))
        {
            return Urls[key];
        }
        string value = GetParameterAndDecoded(key);
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        var u = ValueOf(value);
        Urls.Add(key, u);
        return u;
    }

    public T GetParameter<T>(string key, T defaultValue = default!)
        where T : IConvertible
    {
        if (Numbers.ContainsKey(key))
        {
            return (T)Numbers[key];
        }
        var value = GetParameter(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        T b = (T)value.ChangeType(typeof(T))!;
        Numbers.Add(key, b);
        return b;
    }


    public T GetPositiveParameter<T>(string key, T defaultValue) 
        where T:IComparable<int>,IConvertible
    {
        if (defaultValue.CompareTo(0)<=0)
        {
            throw new ArgumentException("defaultValue <= 0");
        }
        var value = GetParameter(key, defaultValue);
        if (value.CompareTo(0) <= 0)
        {
            return defaultValue;
        }
        return value;
    }


    public char GetParameter(string key, char defaultValue)
    {
        string? value = GetParameter(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return value[0];
    }

    public bool GetParameter(string key, bool defaultValue)
    {
        string? value = GetParameter(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return Convert.ToBoolean(value);
    }

    public bool HasParameter(string key)
    {
        var value = GetParameter(key);
        return !string.IsNullOrEmpty(value);
    }

    public string GetMethodParameterAndDecoded(string method, string key)
    {
        return Decode(GetMethodParameter(method, key));
    }

    public string GetMethodParameterAndDecoded(string method, string key, string defaultValue)
    {
        return Decode(GetMethodParameter(method, key, defaultValue));
    }

    public string? GetInterfaceParameter(string interfaceName, string key)
    {
        string? value = null;

        var interfaceKey = $"interface.{interfaceName}.{key}";
        
        if (Parameters!= null && Parameters.ContainsKey(interfaceKey))
        {
            value = Parameters[interfaceKey];
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            value = GetParameter(key);
        }
        return value;
    }

    public string GetInterfaceParameter(string interfaceName, string key, string defaultValue)
    {
        string? value = GetInterfaceParameter(interfaceName, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return value;
    }


    public string? GetMethodParameter(string method, string key)
    {
        var methodKey = method + "." + key;
        string? value = null;
        if (Parameters!=null && Parameters.ContainsKey(methodKey))
        {
            value = Parameters[methodKey];
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            value = GetParameter(key);
        }
        return value;
    }

    public string GetMethodParameter(string method, string key, string defaultValue)
    {
        string? value = GetMethodParameter(method, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return value;
    }

    public T GetMethodParameter<T>(string method, string key, T defaultValue=default!) 
        where T:IConvertible
    {
        var methodKey = method + "." + key;
        if (Numbers.ContainsKey(methodKey))
        {
            return (T)Numbers[methodKey];
        }
        var value = GetMethodParameter(method, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        T d = (T)value.ChangeType(typeof(T))!;
        Numbers.Add(methodKey, d);
        return d;
    }

    public T GetMethodPositiveParameter<T>(string method, string key, T defaultValue) 
        where T : IComparable, IConvertible
    {
        if (defaultValue.CompareTo(0) <= 0)
        {
            throw new ArgumentException("defaultValue <= 0");
        }
        var value = GetMethodParameter<T>(method, key, defaultValue);
        if (value.CompareTo(0) <= 0)
        {
            return defaultValue;
        }
        return value;
    }

    public char GetMethodParameter(string method, string key, char defaultValue)
    {
        var value = GetMethodParameter(method, key);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        return value[0];
    }

    public bool GetMethodParameter(string method, string key, bool defaultValue)
    {
        var value = GetMethodParameter(method, key);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        return Convert.ToBoolean(value);
    }

    public bool HasMethodParameter(string? method, string? key)
    {
        if (method == null)
        {
            if (Parameters == null) 
            {
                return false;
            }
            var suffix = "." + key;
            foreach (var fullKey in Parameters.Keys)
            {
                if (fullKey.EndsWith(suffix))
                {
                    return true;
                }
            }
            return false;
        }
        if (key == null)
        {
            if (Parameters == null)
            {
                return false;
            }
            var prefix = method + ".";
            foreach (var fullKey in Parameters.Keys)
            {
                if (fullKey.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }
        var value = GetMethodParameter(method, key);
        return value != null && value.Length > 0;
    }

    public bool LocalHost
    {
        get
        {
            return NetUtil.IsLocalHost(Host) || GetParameter(LOCALHOST_KEY, false);
        }
    }

    public bool AnyHost
    {
        get
        {
            return ANYHOST_VALUE.Equals(Host) || GetParameter(ANYHOST_KEY, false);
        }
    }

    public URL AddParameterAndEncoded(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }
        return AddParameter(key, Encode(value));
    }
    
    public URL AddParameter<T>(string key, T value)
    {
        return AddParameter(key, Convert.ToString(value));
    }
    public URL AddParameter(string key, Enum value)
    {
        if (value == null)
        {
            return this;
        }
        return AddParameter(key, value.ToString());
    }

    public URL AddParameter(string key, double? value)
    {
        if (value == null)
        {
            return this;
        }
        return AddParameter(key, value.ToString());
    }

    public URL AddParameter(string key, char[] value)
    {
        if (value == null || value.Length == 0)
        {
            return this;
        }
        return AddParameter(key, Convert.ToString(value));
    }

    public URL AddParameter(string? key, string? value)
    {
        if (string.IsNullOrEmpty(key) 
            || string.IsNullOrEmpty(value))
        {
            return this;
        }
        //如果没有修改，直接返回。
        if (Parameters!=null 
            && Parameters.ContainsKey(key) 
            && Parameters[key] == value) // value != null
        {
            return this;
        }

        var map = new Dictionary<string, string>(Parameters ?? [])
        {
            [key] = value
        };
        return this with { Parameters = map };
    }

    public URL AddParameterIfAbsent(string? key, string? value)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return this;
        }
        if (HasParameter(key))
        {
            return this;
        }
        var map = new Dictionary<string, string>(Parameters ?? [])
        {
            [key] = value
        };

        return this with { Parameters = map };
    }

    /// <summary>
    /// Add parameters to a new url.
    /// </summary>
    /// <param name="parameters"> </param>
    /// <returns> A new URL  </returns>
    public URL AddParameters(IDictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return this;
        }

        bool hasAndEqual = true;
        foreach (var entry in parameters)
        {
            if (Parameters == null 
                || (!Parameters.ContainsKey(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value)) 
                || !Parameters[entry.Key].Equals(entry.Value))
            {
                hasAndEqual = false;
                break;
            }
        }
        // 如果没有修改，直接返回。
        if (hasAndEqual)
        {
            return this;
        }

        var map = new Dictionary<string, string>(Parameters ?? []);
        map.PutAll(parameters);

        return this with { Parameters = map };
    }

    public URL AddParametersIfAbsent(IDictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return this;
        }
        var map = new Dictionary<string, string>(parameters);

        map.PutAll(Parameters);


        return this with { Parameters = map };
    }

    public URL AddParameters(params string[] pairs)
    {
        if (pairs == null || pairs.Length == 0)
        {
            return this;
        }
        if (pairs.Length % 2 != 0)
        {
            throw new ArgumentException("Map pairs can not be odd number.");
        }
        var map = new Dictionary<string, string>();
        int len = pairs.Length / 2;
        for (int i = 0; i < len; i++)
        {
            map[pairs[2 * i]] = pairs[2 * i + 1];
        }
        return AddParameters(map);
    }

    public URL AddParameterString(string query)
    {
        if (query == null || query.Length == 0)
        {
            return this;
        }
        return AddParameters(StringUtils.ParseQueryString(query));
    }

    public URL RemoveParameter(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return this;
        }
        return RemoveParameters(key);
    }

    public URL RemoveParameters(ICollection<string> keys)
    {
        if (keys == null || keys.Count == 0)
        {
            return this;
        }
        return RemoveParameters([.. keys]);
    }

    public URL RemoveParameters(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return this;
        }
        var map = new Dictionary<string, string>(Parameters ?? []);
        foreach (string key in keys)
        {
            map.Remove(key);
        }
        if (map.Count == Parameters?.Count)
        {
            return this;
        }


        return this with { Parameters = map };
    }

    public URL ClearParameters()
    {
        return this with { Parameters = [] };
    }

    

    public string? GetRawParameter(string key)
    {
        if (PROTOCOL_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Protocol;
        }
        if (USERNAME_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Username;
        }
        if (PASSWORD_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Password;
        }
        if (HOST_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Host;
        }
        if (PORT_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToString(Port);
        }
        if (PATH_KEY.Equals(key, StringComparison.OrdinalIgnoreCase))
        {
            return Path;
        }
        return GetParameter(key);
    }

    public IDictionary<string, string> ToMap()
    {
        var map = new Dictionary<string, string>(Parameters ?? []);
        if (Protocol != null)
        {
            map[PROTOCOL_KEY] = Protocol;
        }
        if (Username != null)
        {
            map[USERNAME_KEY] = Username;
        }
        if (Password != null)
        {
            map[PASSWORD_KEY] = Password;
        }
        if (Host != null)
        {
            map[HOST_KEY] = Host;
        }
        if (Port > 0)
        {
            map[PORT_KEY] = Convert.ToString(Port);
        }
        if (Path != null)
        {
            map[PATH_KEY] = Path;
        }
        return map;
    }

    public override string ToString()
    {
        // no show username and password
        return BuildString(false, true); 
    }

    public string ToString(params string[] parameters)
    {
        // no show username and password
        return BuildString(false, true, parameters);
    }

    public string ToIdentityString()
    {
        if (identity != null)
        {
            return identity;
        }
        // only return identity message, see the method "equals" and "hashCode"
        return identity = BuildString(true, false);
    }

    public string ToIdentityString(params string[] parameters)
    {
        // only return identity message, see the method "equals" and "hashCode"
        return BuildString(true, false, parameters);
    }

    public string ToFullString()
    {
        if (full != null)
        {
            return full;
        }
        return full = BuildString(true, true);
    }

    public string ToFullString(params string[] parameters)
    {
        return BuildString(true, true, parameters);
    }

    public string ToParameterString()
    {
        if (parameter != null)
        {
            return parameter;
        }
        return parameter = ToParameterString(Array.Empty<string>());
    }

    public string ToParameterString(params string[] parameters)
    {
        var buf = new StringBuilder();
        BuildParameters(buf, false, parameters);
        return buf.ToString();
    }

    private void BuildParameters(StringBuilder buf, bool concat, params string[] parameters)
    {
        if (Parameters.Count <= 0)
        {
            return;
        }

        bool first = true;
        foreach (var entry in Parameters)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || !parameters.Contains(entry.Key))
            {
                continue; 
            }

            if (first)
            {
                if (concat)
                {
                    buf.Append('?');
                }
                first = false;
            }
            else
            {
                buf.Append('&');
            }
            buf.Append(entry.Key);
            buf.Append('=');
            buf.Append(entry.Value == null ? "" : entry.Value.Trim());
        }
    }

    private string BuildString(bool appendUser, bool appendParameter, params string[] parameters)
    {
        return BuildString(appendUser, appendParameter, false, false, parameters);
    }

    private string BuildString(bool appendUser, bool appendParameter, bool useIP, bool useService, params string[] parameters)
    {
        var buf = new StringBuilder();
        if (Protocol?.Length > 0)
        {
            buf.Append(Protocol);
            buf.Append("://");
        }
        if (appendUser && Username?.Length > 0)
        {
            buf.Append(Username);
            if (Password?.Length > 0)
            {
                buf.Append(':');
                buf.Append(Password);
            }
            buf.Append('@');
        }
        string? host;
        if (useIP)
        {
            host = Ip;
        }
        else
        {
            host = Host;
        }
        if (host?.Length > 0)
        {
            buf.Append(host);
            if (Port > 0)
            {
                buf.Append(':');
                buf.Append(Port);
            }
        }
        string? path;
        if (useService)
        {
            path = ServiceKey;
        }
        else
        {
            path = Path;
        }
        if (path?.Length > 0)
        {
            buf.Append('/');
            buf.Append(path);
        }
        if (appendParameter)
        {
            BuildParameters(buf, true, parameters);
        }
        return buf.ToString();
    }

    public DnsEndPoint ToDnsEndPoint()
    {
        if (string.IsNullOrWhiteSpace(Host)) 
        {
            throw new ArgumentException($"{nameof(Host)} is null or empty");
        }
        return new DnsEndPoint(Host, Port);
    }
    public string? Application
    {
        get
        {
            var inf = GetParameter(APP_KEY);
            if (inf == null)
            {
                return null;
            }
            var buf = new StringBuilder();
            buf.Append(inf);
            var version = GetParameter(VERSION_KEY);
            if (version?.Length > 0)
            {
                buf.Append(':').Append(version);
            }
            return buf.ToString();
        }
    }
    public string? ServiceKey
    {
        get
        {
            var inf = ServiceInterface;
            if (inf == null)
            {
                return null;
            }
            var buf = new StringBuilder();
            var group = GetParameter(GROUP_KEY);
            if (group?.Length > 0)
            {
                buf.Append(group).Append('/');
            }
            buf.Append(inf);
            var version = GetParameter(VERSION_KEY);
            if (version?.Length > 0)
            {
                buf.Append(':').Append(version);
            }
            return buf.ToString();
        }
    }

    public string ToServiceString()
    {
        return BuildString(true, false, true, true);
    }

    public string? ServiceInterface
    {
        get
        {
            return GetParameter(INTERFACE_KEY, Path);
        }
    }

    public URL SetServiceInterface(string service)
    {
        return AddParameter(INTERFACE_KEY, service);
    }

    public static string Encode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }
        try
        {
            return WebUtility.UrlEncode(value);
        }
        catch
        {
            throw;
        }
    }

    public static string Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }
        try
        {
            return WebUtility.UrlDecode(value);
        }
        catch
        {
            throw;
        }
    }


    public override int GetHashCode()
    {
        var result = HashCode.Combine(
            Protocol?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Host?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Port.GetHashCode(),
            Username?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Password?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Path?.GetHashCode(StringComparison.OrdinalIgnoreCase),
             CaseSensitiveEqualHelper.GetHashCode(Parameters)
            );
            return result;
    }
}

public sealed record BadUrl 
{
    public BadUrl(URL url, Exception ex, DateTime? dt = default)
    {
        Url = url;
        CurrentException = ex;
        BadTime = dt ?? DateTime.Now;
    }
    public URL Url { get; init; }
    public Exception CurrentException { get; set; }
    public DateTime BadTime { get; set; }
}
