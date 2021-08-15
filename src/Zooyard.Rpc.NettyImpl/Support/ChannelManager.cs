using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Exceptions;
using Zooyard.Logging;
using Version = Zooyard.Rpc.NettyImpl.Protocol.Version;

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
		/// resourceId -> applicationId -> ip -> port -> RpcContext
		/// </summary>
		private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>>> RM_CHANNELS = new ();

		/// <summary>
		/// ip+appname,port
		/// </summary>
		private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>> TM_CHANNELS = new ();

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
		/// Gets get role from channel.
		/// </summary>
		/// <param name="channel"> the channel </param>
		/// <returns> the get role from channel </returns>
		public static NettyPoolKey.TransactionRole? GetRoleFromChannel(IChannel channel)
		{
			if (IDENTIFIED_CHANNELS.TryGetValue(channel, out RpcContext context) && context != null)
			{
				return context.ClientRole;
			}
			return null;
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

		private static RpcContext BuildChannelHolder(NettyPoolKey.TransactionRole clientRole,
			string version,
			string applicationId,
			string txServiceGroup,
			string dbkeys,
			IChannel channel)
		{
            RpcContext holder = new()
            {
                ClientRole = clientRole,
                Version = version,
                ClientId = BuildClientId(applicationId, channel),
                ApplicationId = applicationId,
                TransactionServiceGroup = txServiceGroup,
				Channel = channel,
			};
            holder.AddResources(DbKeytoSet(dbkeys));
			return holder;
		}

		/// <summary>
		/// Register tm channel.
		/// </summary>
		/// <param name="request"> the request </param>
		/// <param name="channel"> the channel </param>
		/// <exception cref="IncompatibleVersionException"> the incompatible version exception </exception>
		public static void RegisterTMChannel(RegisterTMRequest request, IChannel channel)
		{
			Version.CheckVersion(request.Version);
			RpcContext rpcContext = BuildChannelHolder(NettyPoolKey.TransactionRole.TMROLE, 
				request.Version,
				request.ApplicationId,
				request.TransactionServiceGroup,
				null,
				channel);
			rpcContext.HoldInIdentifiedChannels(IDENTIFIED_CHANNELS);
			string clientIdentified = rpcContext.ApplicationId + Constants.CLIENT_ID_SPLIT_CHAR + ChannelUtil.GetClientIpFromChannel(channel);
			ConcurrentDictionary<int?, RpcContext> clientIdentifiedMap = TM_CHANNELS.GetOrAdd(clientIdentified, key => new ConcurrentDictionary<int?, RpcContext>());
			rpcContext.HoldInClientChannels(clientIdentifiedMap);
		}

		/// <summary>
		/// Register rm channel.
		/// </summary>
		/// <param name="resourceManagerRequest"> the resource manager request </param>
		/// <param name="channel">                the channel </param>
		/// <exception cref="IncompatibleVersionException"> the incompatible  version exception </exception>
		public static void RegisterRMChannel(RegisterRMRequest resourceManagerRequest, IChannel channel)
		{
			Version.CheckVersion(resourceManagerRequest.Version);
			ISet<string> dbkeySet = DbKeytoSet(resourceManagerRequest.ResourceIds);
			RpcContext rpcContext;
			if (!IDENTIFIED_CHANNELS.ContainsKey(channel))
			{
				rpcContext = BuildChannelHolder(NettyPoolKey.TransactionRole.RMROLE, 
					resourceManagerRequest.Version, 
					resourceManagerRequest.ApplicationId, 
					resourceManagerRequest.TransactionServiceGroup,
					resourceManagerRequest.ResourceIds,
					channel);
				rpcContext.HoldInIdentifiedChannels(IDENTIFIED_CHANNELS);
			}
			else
			{
				IDENTIFIED_CHANNELS.TryGetValue(channel, out rpcContext);
				rpcContext.AddResources(dbkeySet);
			}
			if (dbkeySet == null || dbkeySet.Count == 0)
			{
				return;
			}
			foreach (string resourceId in dbkeySet)
			{
				string clientIp;
				ConcurrentDictionary<int?, RpcContext> portMap = RM_CHANNELS
					.GetOrAdd(resourceId, key => new ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>>())
					.GetOrAdd(resourceManagerRequest.ApplicationId, key => new ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>())
					.GetOrAdd(clientIp = ChannelUtil.GetClientIpFromChannel(channel), key => new ConcurrentDictionary<int?, RpcContext>());

				rpcContext.HoldInResourceManagerChannels(resourceId, portMap);
				UpdateChannelsResource(resourceId, clientIp, resourceManagerRequest.ApplicationId);
			}
		}

		private static void UpdateChannelsResource(string resourceId, string clientIp, string applicationId)
		{
			var sourcePortMap = RM_CHANNELS[resourceId][applicationId][clientIp];
			foreach (var rmChannelEntry in RM_CHANNELS)
			{
				if (rmChannelEntry.Key.Equals(resourceId))
				{
					continue;
				}
				var applicationIdMap = rmChannelEntry.Value;
				if (!applicationIdMap.ContainsKey(applicationId))
				{
					continue;
				}
				var clientIpMap = applicationIdMap[applicationId];
				if (!clientIpMap.ContainsKey(clientIp))
				{
					continue;
				}
				var portMap = clientIpMap[clientIp];
				foreach (var portMapEntry in portMap)
				{
					int? port = portMapEntry.Key;
					if (!sourcePortMap.ContainsKey(port))
					{
						RpcContext rpcContext = portMapEntry.Value;
						sourcePortMap.TryAdd(port, rpcContext);
						rpcContext.HoldInResourceManagerChannels(resourceId, port);
					}
				}
			}
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
			NettyPoolKey.TransactionRole? clientRole = rpcContext.ClientRole;
			if (clientRole!= null && clientRole.Value == NettyPoolKey.TransactionRole.TMROLE)
			{
				string clientIdentified = rpcContext.ApplicationId + Constants.CLIENT_ID_SPLIT_CHAR + ChannelUtil.GetClientIpFromChannel(channel);
				if (!TM_CHANNELS.ContainsKey(clientIdentified))
				{
					return null;
				}
				TM_CHANNELS.TryGetValue(clientIdentified,out ConcurrentDictionary<int?, RpcContext> clientRpcMap);
				return GetChannelFromSameClientMap(clientRpcMap, clientPort.Value);
			}
			else if (clientRole == NettyPoolKey.TransactionRole.RMROLE)
			{
				foreach (IDictionary<int?, RpcContext> clientRmMap in rpcContext.ClientRMHolderMap.Values)
				{
					IChannel sameClientChannel = GetChannelFromSameClientMap(clientRmMap, clientPort.Value);
					if (sameClientChannel != null)
					{
						return sameClientChannel;
					}
				}
			}
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

			
			if (string.ReferenceEquals(targetApplicationId, null) 
				|| !RM_CHANNELS.TryGetValue(resourceId, out ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>> applicationIdMap) 
				|| applicationIdMap == null 
				|| applicationIdMap.IsEmpty)
			{
				if (Logger().IsEnabled(LogLevel.Information))
				{
					Logger().LogInformation($"No channel is available for resource[{resourceId}]");
				}
				return null;
			}

			
			if (applicationIdMap.TryGetValue(targetApplicationId, out ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>> ipMap) 
				&& ipMap != null 
				&& !ipMap.IsEmpty)
			{
				// Firstly, try to find the original channel through which the branch was registered.
				if (ipMap.TryGetValue(targetIP, out ConcurrentDictionary<int?, RpcContext> portMapOnTargetIP) 
					&& portMapOnTargetIP != null 
					&& !portMapOnTargetIP.IsEmpty)
				{
					
					if (portMapOnTargetIP.TryGetValue(targetPort, out RpcContext exactRpcContext)
						&& exactRpcContext != null)
					{
						IChannel channel = exactRpcContext.Channel;
						if (channel.Active)
						{
							resultChannel = channel;
							if (Logger().IsEnabled(LogLevel.Debug))
							{
								Logger().LogDebug($"Just got exactly the one {channel} for {clientId}");
							}
						}
						else
						{
							if (portMapOnTargetIP.Remove(targetPort,out exactRpcContext))
							{
								if (Logger().IsEnabled(LogLevel.Information))
								{
									Logger().LogInformation($"Removed inactive {channel}");
								}
							}
						}
					}

					// The original channel was broken, try another one.
					if (resultChannel == null)
					{
						foreach (var portMapOnTargetIPEntry in portMapOnTargetIP)
						{
							IChannel channel = portMapOnTargetIPEntry.Value.Channel;

							if (channel.Active)
							{
								resultChannel = channel;
								if (Logger().IsEnabled(LogLevel.Information))
								{
									Logger().LogInformation($"Choose {channel} on the same IP[{targetIP}] as alternative of {clientId}");
								}
								break;
							}
							else
							{
								if (portMapOnTargetIP.Remove(portMapOnTargetIPEntry.Key,out _))
								{
									if (Logger().IsEnabled(LogLevel.Information))
									{
										Logger().LogInformation($"Removed inactive {channel}");
									}
								}
							}
						}
					}
				}

				// No channel on the this app node, try another one.
				if (resultChannel == null)
				{
					foreach (var ipMapEntry in ipMap)
					{
						if (ipMapEntry.Key.Equals(targetIP))
						{
							continue;
						}

						ConcurrentDictionary<int?, RpcContext> portMapOnOtherIP = ipMapEntry.Value;
						if (portMapOnOtherIP == null || portMapOnOtherIP.IsEmpty)
						{
							continue;
						}

						foreach (var portMapOnOtherIPEntry in portMapOnOtherIP)
						{
							IChannel channel = portMapOnOtherIPEntry.Value.Channel;

							if (channel.Active)
							{
								resultChannel = channel;
								if (Logger().IsEnabled(LogLevel.Information))
								{
									Logger().LogInformation($"Choose {channel} on the same application[{targetApplicationId}] as alternative of {clientId}");
								}
								break;
							}
							else
							{
								if (portMapOnOtherIP.Remove(portMapOnOtherIPEntry.Key, out _))
								{
									if (Logger().IsEnabled(LogLevel.Information))
									{
										Logger().LogInformation($"Removed inactive {channel}");
									}
								}
							}
						}
						if (resultChannel != null)
						{
							break;
						}
					}
				}
			}

			if (resultChannel == null)
			{
				resultChannel = TryOtherApp(applicationIdMap, targetApplicationId);

				if (resultChannel == null)
				{
					if (Logger().IsEnabled(LogLevel.Information))
					{
						Logger().LogInformation($"No channel is available for resource[{resourceId}] as alternative of {clientId}");
					}
				}
				else
				{
					if (Logger().IsEnabled(LogLevel.Information))
					{
						Logger().LogInformation($"Choose {resultChannel} on the same resource[{resourceId}] as alternative of {clientId}");
					}
				}
			}

			return resultChannel;

		}

		private static IChannel TryOtherApp(ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>> applicationIdMap, string myApplicationId)
		{
			IChannel chosenChannel = null;
			foreach (var applicationIdMapEntry in applicationIdMap)
			{
				if (!string.IsNullOrWhiteSpace(myApplicationId) 
					&& applicationIdMapEntry.Key.Equals(myApplicationId))
				{
					continue;
				}

				var targetIPMap = applicationIdMapEntry.Value;
				if (targetIPMap == null || targetIPMap.IsEmpty)
				{
					continue;
				}

				foreach (var targetIPMapEntry in targetIPMap)
				{
					var portMap = targetIPMapEntry.Value;
					if (portMap == null || portMap.IsEmpty)
					{
						continue;
					}

					foreach (var portMapEntry in portMap)
					{
						IChannel channel = portMapEntry.Value.Channel;
						if (channel.Active)
						{
							chosenChannel = channel;
							break;
						}
						else
						{
							if (portMap.Remove(portMapEntry.Key, out _))
							{
								if (Logger().IsEnabled(LogLevel.Information))
								{
									Logger().LogInformation($"Removed inactive {channel}");
								}
							}
						}
					}
					if (chosenChannel != null)
					{
						break;
					}
				}
				if (chosenChannel != null)
				{
					break;
				}
			}
			return chosenChannel;

		}

		/// <summary>
		/// get rm channels
		/// 
		/// @return
		/// </summary>
		public static IDictionary<string, IChannel> RmChannels
		{
			get
			{
				if (RM_CHANNELS.IsEmpty)
				{
					return null;
				}
				var channels = new Dictionary<string, IChannel>(RM_CHANNELS.Count);
                foreach (var item in RM_CHANNELS)
                {
					IChannel channel = TryOtherApp(item.Value, null);
					if (channel == null)
					{
						continue;
					}
					channels[item.Key] = channel;
				}
				return channels;
			}
		}
	}
}
