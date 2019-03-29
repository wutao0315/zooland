using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace Zooyard.Core.Utils
{
    public class NetUtils
    {

        public const string LOCALHOST = "127.0.0.1";

        public const string ANYHOST = "0.0.0.0";

        private static readonly Regex LOCAL_IP_PATTERN = new Regex("127(\\.\\d{1,3}){3}$", RegexOptions.Compiled);

        public static bool isLocalHost(string host)
        {
            return host != null && (LOCAL_IP_PATTERN.IsMatch(host) || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool isAnyHost(string host)
        {
            return "0.0.0.0".Equals(host);
        }

        public static bool isInvalidLocalHost(string host)
        {
            return host == null || host.Length == 0 || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase) || host.Equals("0.0.0.0") || (LOCAL_IP_PATTERN.IsMatch(host));
        }




        private static readonly Regex IP_PATTERN = new Regex("\\d{1,3}(\\.\\d{1,3}){3,5}$", RegexOptions.Compiled);

        private static bool isValidAddress(IPAddress address)
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
                IPAddress address = LocalAddress;
                return address == null ? LOCALHOST : address.MapToIPv4().ToString();
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
                if (isInvalidLocalHost(u.Host))
                {
                    return u.SetHost(LocalHost).ToFullString();
                }
            }
            else if (host.Contains(":"))
            {
                int i = host.LastIndexOf(':');
                if (isInvalidLocalHost(host.Substring(0, i)))
                {
                    return LocalHost + host.Substring(i);
                }
            }
            else
            {
                if (isInvalidLocalHost(host))
                {
                    return LocalHost;
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


        private static IPAddress LocalAddress0
        {
            get
            {
                IPAddress localAddress = null;
                try
                {
                    var localHost = Dns.GetHostAddresses(Dns.GetHostName());
                    localAddress = localHost[0].MapToIPv4();
                    if (isValidAddress(localAddress))
                    {
                        return localAddress;
                    }
                }
                catch (Exception e)
                {
                    //_logger.Warn("Failed to retriving ip address, " + e.Message, e);
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
                                if (isValidAddress(item))
                                {
                                    return item;
                                }
                            }
                            catch (Exception e)
                            {
                                //_logger.Warn("Failed to retriving ip address, " + e.Message, e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //_logger.Warn("Failed to retriving ip address, " + e.Message, e);
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
                var ips = Dns.GetHostAddresses(hostName);
                foreach (var ip in ips)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                    else
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
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
            

            //try
            //{
            //    var me = Dns.GetHostEntry(hostName);

            //    return me.AddressList[0].MapToIPv4().ToString();
            //}
            //catch (Exception)
            //{
            //    return hostName;
            //}
        }




    }
}
