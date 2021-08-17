using DotNetty.Transport.Channels;
using System;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Constant;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Support
{
    public class ChannelUtil
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ChannelUtil));

		/// <summary>
		/// get address from channel </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> address </returns>
		public static string GetAddressFromChannel(IChannel channel)
		{
			string address = NetUtil.ToStringAddress(channel.RemoteAddress);
            if (channel.RemoteAddress.ToString().IndexOf(Constants.ENDPOINT_BEGIN_CHAR, StringComparison.Ordinal) == 0)
            {
                address = channel.RemoteAddress.ToString().Substring(Constants.ENDPOINT_BEGIN_CHAR.Length);
            }
            return address;
		}

		/// <summary>
		/// get client ip from channel </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> client ip </returns>
		public static string GetClientIpFromChannel(IChannel channel)
		{
			string address = GetAddressFromChannel(channel);
			string clientIp = address;
			if (clientIp.Contains(Constants.IP_PORT_SPLIT_CHAR))
			{
				clientIp = clientIp.Substring(0, clientIp.LastIndexOf(Constants.IP_PORT_SPLIT_CHAR, StringComparison.Ordinal));
			}
			return clientIp;
		}

		/// <summary>
		/// get client port from channel </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> client port </returns>
		public static int? GetClientPortFromChannel(IChannel channel)
		{
			string address = GetAddressFromChannel(channel);
			int? port = 0;
			try
			{
				if (address.Contains(Constants.IP_PORT_SPLIT_CHAR))
				{
					port = int.Parse(address.Substring(address.LastIndexOf(Constants.IP_PORT_SPLIT_CHAR, StringComparison.Ordinal) + 1));
				}
			}
			catch (FormatException exx)
			{
				Logger().LogError(exx, exx.Message);
			}
			return port;
		}
	}
}
