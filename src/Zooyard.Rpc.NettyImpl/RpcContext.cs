using DotNetty.Transport.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Support;

namespace Zooyard.Rpc.NettyImpl
{
    /// <summary>
    /// The type rpc context.
    /// 
    /// </summary>
    public class RpcContext
	{
		//private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RpcContext));

		/// <summary>
		/// id
		/// </summary>
		private ConcurrentDictionary<IChannel, RpcContext> clientIDHolderMap;

		/// <summary>
		/// tm
		/// </summary>
		private ConcurrentDictionary<int?, RpcContext> clientTMHolderMap;

		/// <summary>
		/// dbkeyRm
		/// </summary>
		private ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>> clientRMHolderMap;

		/// <summary>
		/// Release.
		/// </summary>
		public virtual void Release()
		{
			int? clientPort = ChannelUtil.GetClientPortFromChannel(Channel);
			if (clientIDHolderMap != null)
			{
				clientIDHolderMap = null;
			}
			if (ClientRole == NettyPoolKey.TransactionRole.TMROLE && clientTMHolderMap != null)
			{
                _ = clientTMHolderMap.TryRemove(clientPort, out _);
				clientTMHolderMap = null;
			}
			if (ClientRole == NettyPoolKey.TransactionRole.RMROLE && clientRMHolderMap != null)
			{
				foreach (IDictionary<int?, RpcContext> portMap in clientRMHolderMap.Values)
				{
					portMap.Remove(clientPort);
				}
				clientRMHolderMap = null;
			}
			if (ResourceSets != null)
			{
                ResourceSets.Clear();
			}
		}

		/// <summary>
		/// Hold in client channels.
		/// </summary>
		/// <param name="clientTMHolderMap"> the client tm holder map </param>
		public virtual void HoldInClientChannels(ConcurrentDictionary<int?, RpcContext> clientTMHolderMap)
		{
			if (this.clientTMHolderMap != null)
			{
				throw new InvalidOperationException();
			}
			this.clientTMHolderMap = clientTMHolderMap;
			int? clientPort = ChannelUtil.GetClientPortFromChannel(Channel);
			this.clientTMHolderMap.TryAdd(clientPort, this);
		}

		/// <summary>
		/// Hold in identified channels.
		/// </summary>
		/// <param name="clientIDHolderMap"> the client id holder map </param>
		public virtual void HoldInIdentifiedChannels(ConcurrentDictionary<IChannel, RpcContext> clientIDHolderMap)
		{
			if (this.clientIDHolderMap != null)
			{
				throw new InvalidOperationException();
			}
			this.clientIDHolderMap = clientIDHolderMap;
			this.clientIDHolderMap.TryAdd(Channel, this);
		}

		/// <summary>
		/// Hold in resource manager channels.
		/// </summary>
		/// <param name="resourceId"> the resource id </param>
		/// <param name="portMap">    the client rm holder map </param>
		public virtual void HoldInResourceManagerChannels(string resourceId, ConcurrentDictionary<int?, RpcContext> portMap)
		{
			if (this.clientRMHolderMap == null)
			{
				this.clientRMHolderMap = new ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>();
			}
			int? clientPort = ChannelUtil.GetClientPortFromChannel(Channel);
			portMap.TryAdd(clientPort, this);
			this.clientRMHolderMap.TryAdd(resourceId, portMap);
		}

		/// <summary>
		/// Hold in resource manager channels.
		/// </summary>
		/// <param name="resourceId"> the resource id </param>
		/// <param name="clientPort"> the client port </param>
		public virtual void HoldInResourceManagerChannels(string resourceId, int? clientPort)
		{
			if (this.clientRMHolderMap == null)
			{
				this.clientRMHolderMap = new ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>>();
			}
			var portMap = clientRMHolderMap.GetOrAdd(resourceId, (key)=>new ConcurrentDictionary<int?, RpcContext>());
			portMap.TryAdd(clientPort, this);
		}

		/// <summary>
		/// Gets get client rm holder map.
		/// </summary>
		/// <returns> the get client rm holder map </returns>
		public virtual ConcurrentDictionary<string, ConcurrentDictionary<int?, RpcContext>> ClientRMHolderMap => clientRMHolderMap;

		/// <summary>
		/// Gets port map.
		/// </summary>
		/// <param name="resourceId"> the resource id </param>
		/// <returns> the port map </returns>
		public virtual IDictionary<int?, RpcContext> GetPortMap(string resourceId)
		{
			if (clientRMHolderMap.TryGetValue(resourceId, out ConcurrentDictionary<int?, RpcContext> result)) 
			{
				return result;
			}
			return null;
		}

		/// <summary>
		/// Gets get client id.
		/// </summary>
		/// <returns> the get client id </returns>
		public virtual string ClientId { get; set; }

		/// <summary>
		/// Gets get channel.
		/// </summary>
		/// <returns> the get channel </returns>
		public virtual IChannel Channel { get; set; }


		/// <summary>
		/// Gets get application id.
		/// </summary>
		/// <returns> the get application id </returns>
		public virtual string ApplicationId { get; set; }


		/// <summary>
		/// Gets get transaction service group.
		/// </summary>
		/// <returns> the get transaction service group </returns>
		public virtual string TransactionServiceGroup { get; set; }


		/// <summary>
		/// Gets get client role.
		/// </summary>
		/// <returns> the get client role </returns>
		public virtual NettyPoolKey.TransactionRole? ClientRole { get; set; }


		/// <summary>
		/// Gets get version.
		/// </summary>
		/// <returns> the get version </returns>
		public virtual string Version { get; set; }


		/// <summary>
		/// Gets get resource sets.
		/// </summary>
		/// <returns> the get resource sets </returns>
		public virtual ISet<string> ResourceSets { get; set; }
		/// <summary>
		/// Add resource.
		/// </summary>
		/// <param name="resource"> the resource </param>
		public virtual void AddResource(string resource)
		{
			if (string.IsNullOrWhiteSpace(resource))
			{
				return;
			}
			this.ResourceSets ??= new HashSet<string>();
			this.ResourceSets.Add(resource);
		}
		/// <summary>
		/// Add resources.
		/// </summary>
		/// <param name="resource"> the resource </param>
		public virtual void AddResources(ISet<string> resource)
		{
			if (resource == null)
			{
				return;
			}
			this.ResourceSets ??= new HashSet<string>();
			this.ResourceSets.AddAll(resource);
		}

		public override string ToString()
		{
			return $"RpcContext{{applicationId='{ApplicationId}', transactionServiceGroup='{TransactionServiceGroup}', clientId='{ClientId}', channel={Channel}, resourceSets={ResourceSets}}}";
		}
	}
	public static class SetExtensions
	{
		public static void AddAll(this ISet<string> _this, IEnumerable<string> paras)
		{
			foreach (var item in paras)
			{
				_this.Add(item);
			}
		}
	}
}