using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using Zooyard.Rpc.NettyImpl.Support;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// The type rpc context.
/// 
/// </summary>
public class RpcContext
{
	/// <summary>
	/// id
	/// </summary>
	private ConcurrentDictionary<IChannel, RpcContext> clientIDHolderMap;

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

		if (ResourceSets != null)
		{
                ResourceSets.Clear();
		}
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
