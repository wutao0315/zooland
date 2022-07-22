using System.Net;

namespace Zooyard.Utils;

public class Local
{
    static Local()
    {
        try
        {
            ReadHostName();
            ReadIp();
            ReadProcessorCount();
        }
        catch (Exception) { }
    }

    public static string HostName { get; private set; } = string.Empty;

    public static string Ipv4 { get; private set; } = string.Empty;

    public static string Ipv6 { get; private set; } = string.Empty;

    public static int ProcessorCount { get; private set; }

    private static void ReadProcessorCount()
    {
        ProcessorCount = Environment.ProcessorCount;
    }

    private static void ReadHostName()
    {
        HostName = Dns.GetHostName();
    }

    private static void ReadIp()
    {
        var ips = Dns.GetHostAddresses(HostName);
        foreach (var ip in ips)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Ipv4 = ip.ToString();
            }
            else
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    Ipv6 = ip.ToString();
                }
            }
        }
    }
}
