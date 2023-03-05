using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Zooyard.Logging;
using Zooyard.Utils;

namespace Zooyard.Utils;

public class NetUtil
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NetUtil));

    public const string LOCALHOST = "127.0.0.1";

    public const string ANYHOST = "0.0.0.0";

    private const string SPLIT_IPV4_CHARACTER = "\\.";
    private const string SPLIT_IPV6_CHARACTER = ":";

    private static readonly Regex ADDRESS_PATTERN = new("^\\d{1,3}(\\.\\d{1,3}){3}\\:\\d{1,5}$", RegexOptions.Compiled);
    private static readonly Regex LOCAL_IP_PATTERN = new ("127(\\.\\d{1,3}){3}$", RegexOptions.Compiled);
    private static readonly Regex IP_PATTERN = new("\\d{1,3}(\\.\\d{1,3}){3,5}$", RegexOptions.Compiled);

    public static bool IsLocalHost(string? host)
    {
        return host != null && (LOCAL_IP_PATTERN.IsMatch(host) || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase));
    }

    public static bool IsAnyHost(string host)
    {
        return "0.0.0.0".Equals(host);
    }

    public static bool IsInvalidLocalHost(string? host)
    {
        return string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase) || host.Equals("0.0.0.0") || (LOCAL_IP_PATTERN.IsMatch(host));
    }

    private static bool IsValidAddress(IPAddress address)
    {

        if (address == null || IPAddress.IsLoopback(address))
        {
            return false;
        }

        var name = address.ToString();
        return (name != null && !ANYHOST.Equals(name) && !LOCALHOST.Equals(name) && IP_PATTERN.IsMatch(name));
    }

    public static string LocalHost
    {
        get
        {
            IPAddress? address = LocalAddress;
            return address == null ? LOCALHOST : address.MapToIPv4().ToString();
        }
    }

    public static string FilterLocalHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return host;
        }
        if (host.Contains("://"))
        {
            var u = URL.ValueOf(host);
            if (IsInvalidLocalHost(u.Host))
            {
                return u.SetHost(LocalHost).ToFullString();
            }
        }
        else if (host.Contains(':'))
        {
            int i = host.LastIndexOf(':');
            if (IsInvalidLocalHost(host.Substring(0, i)))
            {
                return LocalHost + host.Substring(i);
            }
        }
        else
        {
            if (IsInvalidLocalHost(host))
            {
                return LocalHost;
            }
        }
        return host;
    }

    private static volatile IPAddress? LOCAL_ADDRESS = null;

    /// <summary>
    /// 遍历本地网卡，返回第一个合理的IP。
    /// </summary>
    /// <returns> 本地网卡IP </returns>
    public static IPAddress? LocalAddress
    {
        get
        {
            if (LOCAL_ADDRESS != null)
            {
                return LOCAL_ADDRESS;
            }
            IPAddress? localAddress = LocalAddress0;
            LOCAL_ADDRESS = localAddress;
            return localAddress;
        }
    }


    private static IPAddress? LocalAddress0
    {
        get
        {
            IPAddress? localAddress = null;
            try
            {
                var localHost = Dns.GetHostAddresses(Dns.GetHostName());
                localAddress = localHost[0].MapToIPv4();
                if (IsValidAddress(localAddress))
                {
                    return localAddress;
                }
            }
            catch (Exception e)
            {
                Logger().LogWarning(e, "Failed to retriving ip address, " + e.Message);
            }
            try
            {
                string hostName = Dns.GetHostName();
                var addresses = Dns.GetHostAddresses(hostName);
                if (addresses != null)
                {
                    foreach (var item in addresses)
                    {
                        try
                        {
                            if (IsValidAddress(item))
                            {
                                return item;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger().LogWarning(e, "Failed to retriving ip address, " + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger().LogWarning(e, "Failed to retriving ip address, " + e.Message);
            }
            Logger().LogError("Could not get local host ip address, will use 127.0.0.1 instead.");
            return localAddress;
        }
    }

    /// <param name="hostName"> </param>
    /// <returns> ip address or hostName if UnknownHostException  </returns>
    public static string GetIpByHost(string hostName)
    {
        try
        {
            var ips = Dns.GetHostAddresses(hostName);
            foreach (var ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
                else
                {
                    if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return ip.ToString();
                    }
                }
            }
            return hostName;
        }
        catch (Exception)
        {
            return hostName;
        }
    }

    /// <summary>
    /// To string address string.
    /// </summary>
    /// <param name="address"> the address </param>
    /// <returns> the string </returns>
    public static string ToStringAddress(EndPoint address)
    {
        return ToStringAddress((IPEndPoint)address);
    }

    /// <summary>
    /// To ip address string.
    /// </summary>
    /// <param name="address"> the address </param>
    /// <returns> the string </returns>
    public static string ToIpAddress(EndPoint address)
    {
        IPEndPoint inetSocketAddress = (IPEndPoint)address;
        return inetSocketAddress.Address.MapToIPv4().ToString();
    }

    /// <summary>
    /// To string address string.
    /// </summary>
    /// <param name="address"> the address </param>
    /// <returns> the string </returns>
    public static string ToStringAddress(IPEndPoint address)
    {
        return address.Address.MapToIPv4().ToString() + ":" + address.Port;
    }
    /// <summary>
    /// To inet socket address inet socket address.
    /// </summary>
    /// <param name="address"> the address </param>
    /// <returns> the inet socket address </returns>
    public static IPEndPoint ToIPEndPoint(string address)
    {
        int i = address.IndexOf(':');
        string host;
        int port;
        if (i > -1)
        {
            host = address.Substring(0, i);
            port = int.Parse(address.Substring(i + 1));
        }
        else
        {
            host = address;
            port = 0;
        }
        return new IPEndPoint(IPAddress.Parse(host), port);
    }

    public static bool matchIpExpression(string pattern, string host, int port)
    {

        // if the pattern is subnet format, it will not be allowed to config port param in pattern.
        if (pattern.Contains('/'))
        {
            var utils = CidrIpAddress.Parse(pattern);
            return utils.IsIpFromSameSubnet(host);
            //CIDRUtils utils = new CIDRUtils();
            //return utils.isInRange(host);
        }


        return matchIpRange(pattern, host, port);
    }
    public static bool matchIpRange(string pattern, string host, int port)
    {
        if (pattern == null || host == null)
        {
            throw new ArgumentException("Illegal Argument pattern or hostName. Pattern:" + pattern + ", Host:" + host);
        }
        pattern = pattern.Trim();
        if ("*.*.*.*".Equals(pattern) || "*".Equals(pattern))
        {
            return true;
        }

        
        IPAddress inetAddress = IPAddress.Parse(host);
        bool isIpv4 = isValidV4Address(inetAddress);
        string[] hostAndPort = getPatternHostAndPort(pattern, isIpv4);
        if (hostAndPort[1] != null && !hostAndPort[1].Equals(port.ToString()))
        {
            return false;
        }
        pattern = hostAndPort[0];

        string splitCharacter = SPLIT_IPV4_CHARACTER;
        if (!isIpv4)
        {
            splitCharacter = SPLIT_IPV6_CHARACTER;
        }
        string[] mask = pattern.Split(splitCharacter);
        // check format of pattern
        checkHostPattern(pattern, mask, isIpv4);

        host = inetAddress.ToString();//.getHostAddress();
        if (pattern.Equals(host))
        {
            return true;
        }

        // short name condition
        if (!ipPatternContainExpression(pattern))
        {
            IPAddress patternAddress = IPAddress.Parse(pattern);// InetAddress.getByName(pattern);
            return patternAddress.ToString().Equals(host);
        }

        string[] ipAddress = host.Split(splitCharacter);

        for (int i = 0; i < mask.Length; i++)
        {
            if ("*".Equals(mask[i]) || mask[i].Equals(ipAddress[i]))
            {
                continue;
            }
            else if (mask[i].Contains('-'))
            {
                string[] rangeNumStrs = mask[i].Split('-');
                if (rangeNumStrs.Length != 2)
                {
                    throw new ArgumentException("There is wrong format of ip Address: " + mask[i]);
                }
                int min = getNumOfIpSegment(rangeNumStrs[0], isIpv4);
                int max = getNumOfIpSegment(rangeNumStrs[1], isIpv4);
                int ip = getNumOfIpSegment(ipAddress[i], isIpv4);
                if (ip < min || ip > max)
                {
                    return false;
                }
            }
            else if ("0".Equals(ipAddress[i]) && ("0".Equals(mask[i]) || "00".Equals(mask[i]) || "000".Equals(mask[i]) || "0000".Equals(mask[i])))
            {
                continue;
            }
            else if (!mask[i].Equals(ipAddress[i]))
            {
                return false;
            }
        }
        return true;
    }

    static bool isValidV4Address(IPAddress? address)
    {
        
        if (address == null || IPAddress.IsLoopback(address))
        {
            return false;
        }

        string name = address.ToString();//.getHostAddress();
        return (name != null
            && IP_PATTERN.IsMatch(name)
            && !ANYHOST.Equals(name)
            && !LOCALHOST.Equals(name));
    }
    private static int getNumOfIpSegment(string ipSegment, bool isIpv4)
    {
        if (isIpv4)
        {
            return int.Parse(ipSegment);
        }
        return int.Parse(ipSegment);// Integer.parseInt(ipSegment, 16)
    }
    private static bool ipPatternContainExpression(string pattern)
    {
        return pattern.Contains('*') || pattern.Contains('-');
    }
    private static void checkHostPattern(string pattern, string[] mask, bool isIpv4)
    {
        if (!isIpv4)
        {
            if (mask.Length != 8 && ipPatternContainExpression(pattern))
            {
                throw new ArgumentException("If you config ip expression that contains '*' or '-', please fill qualified ip pattern like 234e:0:4567:0:0:0:3d:*. ");
            }
            if (mask.Length != 8 && !pattern.Contains("::"))
            {
                throw new ArgumentException("The host is ipv6, but the pattern is not ipv6 pattern : " + pattern);
            }
        }
        else
        {
            if (mask.Length != 4)
            {
                throw new ArgumentException("The host is ipv4, but the pattern is not ipv4 pattern : " + pattern);
            }
        }
    }
    static string[] getPatternHostAndPort(string pattern, bool isIpv4)
    {
        var result = new string[2];
        if (pattern.StartsWith("[") && pattern.Contains("]:"))
        {
            int end = pattern.IndexOf("]:");
            result[0] = pattern.Substring(1, end);
            result[1] = pattern.Substring(end + 2);
            return result;
        }
        else if (pattern.StartsWith("[") && pattern.EndsWith("]"))
        {
            result[0] = pattern.Substring(1, pattern.Length - 1);
            result[1] = "";
            return result;
        }
        else if (isIpv4 && pattern.Contains(':'))
        {
            int end = pattern.IndexOf(':');
            result[0] = pattern.Substring(0, end);
            result[1] = pattern.Substring(end + 1);
            return result;
        }
        else
        {
            result[0] = pattern;
            return result;
        }
    }
}
public class NetworkInterfaceManager
{
    static NetworkInterfaceManager()
    {
        try
        {
            var hostIp = NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up)
                .Select(network => network.GetIPProperties())
                .OrderByDescending(properties => properties.GatewayAddresses.Count)
                .SelectMany(properties => properties.UnicastAddresses)
                .FirstOrDefault(address => !IPAddress.IsLoopback(address.Address) &&
                                           address.Address.AddressFamily == AddressFamily.InterNetwork);

            if (hostIp != null)
                LocalIp = hostIp.Address.ToString();
        }
        catch
        {
            // ignored
        }
    }

    public static string LocalIp { get; } = "127.0.0.1";
}
public class CidrIpAddress
{
    const char CIDR_SEPARATOR = '/';
    const byte IPV6_BIT_LENGTH = 128;
    const byte IPV4_BIT_LENGTH = 32;
    const byte BYTE_MASK = 0b11111111;

    const string FormatExceptionMessage = "cidrAddressString is not a valid CIDR address";

    public IPAddress IpAddress { get; protected set; } = IPAddress.None;
    public IPAddress SubnetIp { get; protected set; } = IPAddress.None;
    private byte[] SubnetIpMask { get; set; } = Array.Empty<byte>();
    private int SubnetBitLengthMask { get; set; }

    #region [ constructors ]
    /// <summary>
    /// Initialize a new instance by Ip and subnet mask
    /// </summary>
    public CidrIpAddress(IPAddress ip, int subnetBitLengthMask)
    {
        IpAddress = ip;
        SubnetBitLengthMask = subnetBitLengthMask;

        CalculateSubnetIpAddress();
    }

    /// <summary>
    /// Initialize a new instance by CidrIpAddress and subnet mask.
    /// Useful for changing subnet bit length mask from an existing CidrIpAddress
    /// </summary>
    public CidrIpAddress(CidrIpAddress cidr, int subnetBitLengthMask)
    {
        IpAddress = cidr.IpAddress;
        SubnetBitLengthMask = subnetBitLengthMask;

        CalculateSubnetIpAddress();
    }

    private CidrIpAddress() { }

    /// <summary>
    /// Parse a cidr ip string and returns an instance of the CidrIpAddress
    /// </summary>
    /// <param name="cidrAddressString">Cidr formatted string using / to split ip and subnet. If no subnet is specified, the subnet mask will be full</param>
    public static CidrIpAddress Parse(string cidrAddressString)
    {
        if (cidrAddressString == null) throw new ArgumentNullException("cidrAddress");

        var pieces = cidrAddressString.Trim().Split(CIDR_SEPARATOR);
        if (pieces.Length > 2) throw new FormatException(FormatExceptionMessage);

        var ip = pieces[0];
        var subnetMask = pieces[1];

        var cidr = new CidrIpAddress
        {
            //throws FormatException if string is not an IP
            IpAddress = IPAddress.Parse(ip)
        };

        if (pieces.Length == 2)
        {
            cidr.SubnetBitLengthMask = Convert.ToInt32(subnetMask);

            //checking subnet ranges
            if (cidr.SubnetBitLengthMask < 0) throw new FormatException(FormatExceptionMessage);

            if (cidr.IpAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                if (cidr.SubnetBitLengthMask > IPV4_BIT_LENGTH) throw new FormatException(FormatExceptionMessage);
            }
            else if (cidr.SubnetBitLengthMask > IPV6_BIT_LENGTH) throw new FormatException(FormatExceptionMessage);
        }
        else
        {
            cidr.SubnetBitLengthMask = (cidr.IpAddress.AddressFamily == AddressFamily.InterNetwork)
                ? IPV4_BIT_LENGTH : IPV6_BIT_LENGTH;
        }

        cidr.SubnetIpMask = new byte[((cidr.IpAddress.AddressFamily == AddressFamily.InterNetwork)
                ? IPV4_BIT_LENGTH : IPV6_BIT_LENGTH) / 8];

        cidr.CalculateSubnetIpAddress();

        return cidr;
    }
    #endregion


    #region [ interface ]
    /// <summary>
    /// Check if the ip belongs to the subnet of this instance
    /// </summary>
    public bool IsIpFromSameSubnet(IPAddress ip)
    {
        if (ip == null) return false;

        if (SubnetIp.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                ip = ip.MapToIPv6();
            }
        }
        else
        {
            //will not map IPv6 to IPv4 in this case
            if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        }

        var maskedIp = ApplyMask(ip.GetAddressBytes());

        var subnetIp = SubnetIp.GetAddressBytes();

        for (var i = 0; i < subnetIp.Length; i += 1)
        {
            if (subnetIp[i] != maskedIp[i]) return false;
        }

        //if IPV6, the scopeId should be the same
        //to be considered of the same subnet
        if ((SubnetIp.AddressFamily == AddressFamily.InterNetworkV6)
            && (SubnetIp.ScopeId != ip.ScopeId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if the cidrIp belongs to the subnet of this instance
    /// </summary>
    public bool IsIpFromSameSubnet(CidrIpAddress cidrIp)
    {
        if (cidrIp == null) return false;

        return IsIpFromSameSubnet(cidrIp.IpAddress);
    }

    /// <summary>
    /// Checks if the ip belongs to the subnet of this instance
    /// </summary>
    public bool IsIpFromSameSubnet(string ip)
    {
        if (!IPAddress.TryParse(ip, out var parsedIp)) return false;

        return IsIpFromSameSubnet(parsedIp);
    }

    /// <summary>
    /// Return a string formatted in CIDR notation
    /// </summary>
    public override string ToString()
    {
        return IpAddress.ToString() + CIDR_SEPARATOR + SubnetBitLengthMask;
    }

    /// <summary>
    /// Return a 3 line string containing the IP in bits, the mask in bits, and the subnet IP in bits.
    /// Useful for debugging
    /// </summary>
    public string ToBinaryString()
    {
        string binaryIp = "";
        var binaryIpBytes = IpAddress.GetAddressBytes();
        for (var i = 0; i < SubnetIpMask.Length; i += 1)
        {
            binaryIp += Convert.ToString(binaryIpBytes[i], 2).PadLeft(8, '0')
                + (((i + 1) == binaryIpBytes.Length) ? "" : ".");
        }

        string binaryMask = "";
        for (var i = 0; i < SubnetIpMask.Length; i += 1)
        {
            binaryMask += Convert.ToString(SubnetIpMask[i], 2).PadLeft(8, '0')
                + (((i + 1) == SubnetIpMask.Length) ? "" : ".");
        }

        string binarySubnetIp = "";
        var subnetIpBytes = SubnetIp.GetAddressBytes();
        for (var i = 0; i < SubnetIpMask.Length; i += 1)
        {
            binarySubnetIp += Convert.ToString(subnetIpBytes[i], 2).PadLeft(8, '0')
                + (((i + 1) == subnetIpBytes.Length) ? "" : ".");
        }

        return string.Format("ip:{0}\nmk:{1}\nsi:{2}", binaryIp, binaryMask, binarySubnetIp);
    }
    #endregion


    private void CalculateSubnetIpAddress()
    {
        var ipBytes = IpAddress.GetAddressBytes();
        var subnetIp = new byte[ipBytes.Length];
        SubnetIpMask = new byte[ipBytes.Length];

        var bytesMask = SubnetBitLengthMask / 8;
        var remainingBits = SubnetBitLengthMask % 8;

        for (var i = 0; i < bytesMask; i += 1)
        {
            SubnetIpMask[i] = BYTE_MASK;
            subnetIp[i] = (byte)(BYTE_MASK & ipBytes[i]);
        }

        if (remainingBits > 0)
        {
            var bitMask = (byte)(BYTE_MASK ^ (BYTE_MASK >> remainingBits));
            SubnetIpMask[bytesMask] = bitMask;
            subnetIp[bytesMask] = (byte)(bitMask & ipBytes[bytesMask]);
        }

        for (var i = bytesMask + 1; i < ipBytes.Length; i += 1)
        {
            SubnetIpMask[i] = 0;
            subnetIp[i] = 0;
        }

        SubnetIp = new IPAddress(subnetIp);
    }

    private byte[] ApplyMask(byte[] ip)
    {
        var maskedIp = new byte[ip.Length];

        for (var i = 0; i < ip.Length; i += 1)
        {
            maskedIp[i] = (byte)(ip[i] & SubnetIpMask[i]);
        }

        return maskedIp;
    }
}