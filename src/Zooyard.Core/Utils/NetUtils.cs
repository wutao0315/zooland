using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Common.Logging;

namespace Zooyard.Core.Utils
{
    public class NetUtils
    {
        private static readonly ILog _logger = LogManager.GetLogger("NetUtils");

        public const string LOCALHOST = "127.0.0.1";

        public const string ANYHOST = "0.0.0.0";

        private const int RND_PORT_START = 30000;

        private const int RND_PORT_RANGE = 10000;

        private static readonly Random RANDOM = new Random(DateTime.UtcNow.Millisecond); //   DateTimeHelperClass.CurrentUnixTimeMillis());

        public static int RandomPort
        {
            get
            {
                return RND_PORT_START + RANDOM.Next(RND_PORT_RANGE);
            }
        }

        public static int AvailablePort
        {
            get
            {
                Socket ss = null;
                //ServerSocket ss = null;
                try
                {
                    ss = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ss.Bind(null);
                    return RandomPort;
                   
                    //ss = new ServerSocket();
                    //ss.bind(null);
                    //return ss.LocalPort;
                }
                catch (Exception)//IOException
                {
                    return RandomPort;
                }
                finally
                {
                    if (ss != null)
                    {
                        try
                        {
                            ss.Dispose();
                        }
                        catch (Exception)//IOException
                        {
                        }
                    }
                }
            }
        }

        public static int getAvailablePort(int port)
        {
            if (port <= 0)
            {
                return AvailablePort;
            }
            for (int i = port; i < MAX_PORT; i++)
            {
                Socket ss = null;
                //ServerSocket ss = null;
                try
                {
                    ss = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var add = IPAddress.Parse(LOCALHOST);
                    var endPt = new IPEndPoint(add, i);
                    ss.Bind(endPt);
                    //ss = new ServerSocket(i);
                    return i;
                }
                catch (Exception)//IOException
                {
                    // continue
                }
                finally
                {
                    if (ss != null)
                    {
                        try
                        {
                            ss.Dispose();
                        }
                        catch (Exception)//IOException
                        {
                        }
                    }
                }
            }
            return port;
        }

        private const int MIN_PORT = 0;

        private const int MAX_PORT = 65535;

        public static bool isInvalidPort(int port)
        {
            return port > MIN_PORT || port <= MAX_PORT;
        }

        private static readonly Regex ADDRESS_PATTERN = new Regex("^\\d{1,3}(\\.\\d{1,3}){3}\\:\\d{1,5}$", RegexOptions.Compiled);

        public static bool isValidAddress(string address)
        {
            return ADDRESS_PATTERN.IsMatch(address);//.matcher(address).matches();
        }

        private static readonly Regex LOCAL_IP_PATTERN = new Regex("127(\\.\\d{1,3}){3}$", RegexOptions.Compiled);

        public static bool isLocalHost(string host)
        {
            return host != null && (LOCAL_IP_PATTERN.IsMatch(host) || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase));
            //return host != null && (LOCAL_IP_PATTERN.matcher(host).matches() || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool isAnyHost(string host)
        {
            return "0.0.0.0".Equals(host);
        }

        public static bool isInvalidLocalHost(string host)
        {
            return host == null || host.Length == 0 || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase) || host.Equals("0.0.0.0") || (LOCAL_IP_PATTERN.IsMatch(host));
        }

        public static bool isValidLocalHost(string host)
        {
            return !isInvalidLocalHost(host);
        }

        public static IPEndPoint getLocalSocketAddress(string host, int port)
        {
            return isInvalidLocalHost(host) ? new IPEndPoint(IPAddress.Parse(LOCALHOST), port) : new IPEndPoint(IPAddress.Parse(host), port);
            //return isInvalidLocalHost(host) ? new InetSocketAddress(port) : new InetSocketAddress(host, port);
        }

        private static readonly Regex IP_PATTERN = new Regex("\\d{1,3}(\\.\\d{1,3}){3,5}$", RegexOptions.Compiled);

        private static bool isValidAddress(IPAddress address)
        {
            
            if (address == null || IPAddress.IsLoopback(address))
            {
                return false;
            }
            
            string name = address.ToString();
            //string name = address.HostAddress;
            return (name != null && !ANYHOST.Equals(name) && !LOCALHOST.Equals(name) && IP_PATTERN.IsMatch(name));
        }

        public static string LocalHost
        {
            get
            {
                IPAddress address = LocalAddress;
                return address == null ? LOCALHOST : address.MapToIPv4().ToString();
                //return address == null ? LOCALHOST : address.HostAddress;
            }
        }

        public static string filterLocalHost(string host)
        {
            if (host == null || host.Length == 0)
            {
                return host;
            }
            if (host.Contains("://"))
            {
                var u = URL.valueOf(host);
                if (NetUtils.isInvalidLocalHost(u.Host))
                {
                    return u.SetHost(NetUtils.LocalHost).ToFullString();
                }
            }
            else if (host.Contains(":"))
            {
                int i = host.LastIndexOf(':');
                if (NetUtils.isInvalidLocalHost(host.Substring(0, i)))
                {
                    return NetUtils.LocalHost + host.Substring(i);
                }
            }
            else
            {
                if (NetUtils.isInvalidLocalHost(host))
                {
                    return NetUtils.LocalHost;
                }
            }
            return host;
        }

        private static volatile IPAddress LOCAL_ADDRESS = null;

        /// <summary>
        /// 遍历本地网卡，返回第一个合理的IP。
        /// </summary>
        /// <returns> 本地网卡IP </returns>
        public static IPAddress LocalAddress
        {
            get
            {
                if (LOCAL_ADDRESS != null)
                {
                    return LOCAL_ADDRESS;
                }
                IPAddress localAddress = LocalAddress0;
                LOCAL_ADDRESS = localAddress;
                return localAddress;
            }
        }

        public static string LogHost
        {
            get
            {
                IPAddress address = LOCAL_ADDRESS;
                
                return address == null ? LOCALHOST : address.MapToIPv4().ToString();
            }
        }

        private static IPAddress LocalAddress0
        {
            get
            {
                IPAddress localAddress = null;
                try
                {
                    var localHost= Dns.GetHostAddresses(Dns.GetHostName());
                    localAddress = localHost[0].MapToIPv4();
                    //localAddress = InetAddress.LocalHost;
                    if (isValidAddress(localAddress))
                    {
                        return localAddress;
                    }
                }
                catch (Exception e)
                {
                    _logger.Warn("Failed to retriving ip address, " + e.Message, e);
                }
                try
                {
                    string hostName = Dns.GetHostName();
                    var addresses= Dns.GetHostAddresses(hostName);
                    if (addresses!=null) {
                        foreach (var item in addresses)
                        {
                            try
                            {
                                if (isValidAddress(item))
                                {
                                    return item;
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Warn("Failed to retriving ip address, " + e.Message, e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Warn("Failed to retriving ip address, " + e.Message, e);
                }
                //logger.Error("Could not get local host ip address, will use 127.0.0.1 instead.");
                return localAddress;
            }
        }

        /// <param name="hostName"> </param>
        /// <returns> ip address or hostName if UnknownHostException  </returns>
        public static string getIpByHost(string hostName)
        {
            try
            {
                var me = Dns.GetHostEntry(hostName);

                return me.AddressList[0].MapToIPv4().ToString();
                //return InetAddress.getByName(hostName).HostAddress;
            }
            catch (Exception)
            {
                return hostName;
            }
        }

        public static string toAddressString(DnsEndPoint address)
        {
            return address.Host + ":" + address.Port;
            //return address.Address.HostAddress + ":" + address.Port;
        }

        public static IPEndPoint toAddress(string address)
        {
            int i = address.IndexOf(':');
            string host;
            int port;
            if (i > -1)
            {
                host = address.Substring(0, i);
                port = Convert.ToInt32(address.Substring(i + 1));
            }
            else
            {
                host = address;
                port = 0;
            }
            return new IPEndPoint(IPAddress.Parse(host), port);
        }

        public static string toURL(string protocol, string host, int port, string path)
        {
            var sb = new StringBuilder();
            sb.Append(protocol).Append("://");
            sb.Append(host).Append(':').Append(port);
            if (path[0] != '/')
            {
                sb.Append('/');
            }
            sb.Append(path);
            return sb.ToString();
        }

    }
}
