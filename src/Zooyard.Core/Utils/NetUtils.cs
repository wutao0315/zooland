﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Zooyard.Core.Logging;

namespace Zooyard.Core.Utils
{
    public class NetUtils
    {
        private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NetUtils));

        public const string LOCALHOST = "127.0.0.1";

        public const string ANYHOST = "0.0.0.0";

        private static readonly Regex LOCAL_IP_PATTERN = new Regex("127(\\.\\d{1,3}){3}$", RegexOptions.Compiled);

        public static bool IsLocalHost(string host)
        {
            return host != null && (LOCAL_IP_PATTERN.IsMatch(host) || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool IsAnyHost(string host)
        {
            return "0.0.0.0".Equals(host);
        }

        public static bool IsInvalidLocalHost(string host)
        {
            return host == null || host.Length == 0 || host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase) || host.Equals("0.0.0.0") || (LOCAL_IP_PATTERN.IsMatch(host));
        }




        private static readonly Regex IP_PATTERN = new Regex("\\d{1,3}(\\.\\d{1,3}){3,5}$", RegexOptions.Compiled);

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
                IPAddress address = LocalAddress;
                return address == null ? LOCALHOST : address.MapToIPv4().ToString();
            }
        }

        public static string FilterLocalHost(string host)
        {
            if (host == null || host.Length == 0)
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
            else if (host.Contains(":"))
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
    }
}
