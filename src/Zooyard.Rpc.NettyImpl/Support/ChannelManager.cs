using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Exceptions;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Constant;

namespace Zooyard.Rpc.NettyImpl.Support
{
    /// <summary>
    /// The type channel manager.
    /// 
    /// </summary>
    public class ChannelManager
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(ChannelManager));

		private static readonly ConcurrentDictionary<IChannel, RpcContext> IDENTIFIED_CHANNELS = new ();

		/// <summary>
		/// Is registered boolean.
		/// </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> the boolean </returns>
		public static bool IsRegistered(IChannel channel)
		{
			return IDENTIFIED_CHANNELS.ContainsKey(channel);
		}

		/// <summary>
		/// Gets get context from identified.
		/// </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> the get context from identified </returns>
		public static RpcContext GetContextFromIdentified(IChannel channel)
		{
			IDENTIFIED_CHANNELS.TryGetValue(channel, out RpcContext ctx);
			return ctx;
		}

		private static string BuildClientId(string applicationId, IChannel channel)
		{
			return applicationId + Constants.CLIENT_ID_SPLIT_CHAR + ChannelUtil.GetAddressFromChannel(channel);
		}

		private static string[] ReadClientId(string clientId)
		{
			return clientId.Split(Constants.CLIENT_ID_SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries);
		}

		private static RpcContext BuildChannelHolder(string version,
			string applicationId,
			string txServiceGroup,
			string dbkeys,
			IChannel channel)
		{
            RpcContext holder = new()
            {
                Version = version,
                ClientId = BuildClientId(applicationId, channel),
                ApplicationId = applicationId,
                TransactionServiceGroup = txServiceGroup,
				Channel = channel,
			};
            holder.AddResources(DbKeytoSet(dbkeys));
			return holder;
		}


		private static ISet<string> DbKeytoSet(string dbkey)
		{
			if (string.IsNullOrWhiteSpace(dbkey))
			{
				return null;
			}
			return new HashSet<string>(dbkey.Split(Constants.DBKEYS_SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries));
		}

		/// <summary>
		/// Release rpc context.
		/// </summary>
		/// <param name="channel"> the channel </param>
		public static void ReleaseRpcContext(IChannel channel)
		{
			RpcContext rpcContext = GetContextFromIdentified(channel);
			if (rpcContext != null)
			{
				rpcContext.Release();
			}
		}

		/// <summary>
		/// Gets get same income client channel.
		/// </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> the get same income client channel </returns>
		public static IChannel GetSameClientChannel(IChannel channel)
		{
			if (channel.Active)
			{
				return channel;
			}
			RpcContext rpcContext = GetContextFromIdentified(channel);
			if (rpcContext == null)
			{
				Logger().LogError($"rpcContext is null,channel:{channel},active:{channel.Active}");
				return null;
			}
			if (rpcContext.Channel.Active)
			{
				// recheck
				return rpcContext.Channel;
			}
			int? clientPort = ChannelUtil.GetClientPortFromChannel(channel);
			
			return null;

		}

		private static IChannel GetChannelFromSameClientMap(IDictionary<int?, RpcContext> clientChannelMap, int exclusivePort)
		{
			if (clientChannelMap != null && clientChannelMap.Count > 0)
			{
				foreach (var entry in clientChannelMap)
				{
					if (entry.Key == exclusivePort)
					{
						clientChannelMap.Remove(entry.Key);
						continue;
					}
					IChannel channel = entry.Value.Channel;
					if (channel.Active)
					{
						return channel;
					}
					clientChannelMap.Remove(entry.Key);
				}
			}
			return null;
		}

		/// <summary>
		/// Gets get channel.
		/// </summary>
		/// <param name="resourceId"> Resource ID </param>
		/// <param name="clientId">   Client ID - ApplicationId:IP:Port </param>
		/// <returns> Corresponding channel, NULL if not found. </returns>
		public static IChannel GetChannel(string resourceId, string clientId)
		{
			IChannel resultChannel = null;

			string[] clientIdInfo = ReadClientId(clientId);

			if (clientIdInfo == null || clientIdInfo.Length != 3)
			{
				throw new FrameworkException("Invalid Client ID: " + clientId);
			}

			string targetApplicationId = clientIdInfo[0];
			string targetIP = clientIdInfo[1];
			int targetPort = int.Parse(clientIdInfo[2]);

			
			if (string.ReferenceEquals(targetApplicationId, null))
			{
				if (Logger().IsEnabled(LogLevel.Information))
				{
					Logger().LogInformation($"No channel is available for resource[{resourceId}]");
				}
				return null;
			}

			return resultChannel;
		}
	}
}
