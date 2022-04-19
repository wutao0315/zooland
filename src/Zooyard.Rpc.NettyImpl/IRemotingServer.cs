using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using Zooyard.Rpc.NettyImpl.Processor;
using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// The interface Remoting server.
/// 
/// </summary>
public interface IRemotingServer
{
	/// <summary>
	/// server send sync request.
	/// </summary>
	/// <param name="resourceId"> rm client resourceId </param>
	/// <param name="clientId">   rm client id </param>
	/// <param name="msg">        transaction message <seealso cref="IProtocol"/> </param>
	/// <returns> client result message </returns>
	/// <exception cref="TimeoutException"> TimeoutException </exception>
	Task<object> SendSyncRequest(string resourceId, string clientId, object msg);

	/// <summary>
	/// server send sync request.
	/// </summary>
	/// <param name="channel"> client channel </param>
	/// <param name="msg">     transaction message <seealso cref="IProtocol"/> </param>
	/// <returns> client result message </returns>
	/// <exception cref="TimeoutException"> TimeoutException </exception>
	Task<object> SendSyncRequest(IChannel channel, object msg);

	/// <summary>
	/// server send async request.
	/// </summary>
	/// <param name="channel"> client channel </param>
	/// <param name="msg">     transaction message <seealso cref="IProtocol"/> </param>
	Task SendAsyncRequest(IChannel channel, object msg);

	/// <summary>
	/// server send async response.
	/// </summary>
	/// <param name="rpcMessage"> rpc message from client request </param>
	/// <param name="channel">    client channel </param>
	/// <param name="msg">        transaction message <seealso cref="io.seata.core.protocol"/> </param>
	Task SendAsyncResponse(RpcMessage rpcMessage, IChannel channel, object msg);

	/// <summary>
	/// register processor
	/// </summary>
	/// <param name="messageType"> <seealso cref="IProtocol.MessageType"/> </param>
	/// <param name="processor">   <seealso cref="RemotingProcessor"/> </param>
	/// <param name="executor">    thread pool </param>
	Task RegisterProcessor(int messageType, IRemotingProcessor processor, IExecutorService executor);
}
