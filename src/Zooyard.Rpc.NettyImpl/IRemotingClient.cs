using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Rpc.NettyImpl.Processor;

namespace Zooyard.Rpc.NettyImpl
{
    /// <summary>
    /// The interface remoting client.
    /// </summary>
    public interface IRemotingClient
    {
		/// <summary>
		/// client send sync request.
		/// In this request, if <seealso cref="NettyClientConfig"/> is enabled,
		/// the message will be sent in batches.
		/// </summary>
		/// <param name="msg"> transaction message <seealso cref="IProtocol"/> </param>
		/// <returns> server result message </returns>
		/// <exception cref="TimeoutException"> TimeoutException </exception>
		Task<object> SendSyncRequest(object msg);

		/// <summary>
		/// client send sync request.
		/// </summary>
		/// <param name="channel"> client channel </param>
		/// <param name="msg">     transaction message <seealso cref="io.seata.core.protocol"/> </param>
		/// <returns> server result message </returns>
		/// <exception cref="TimeoutException"> TimeoutException </exception>
		Task<object> SendSyncRequest(IChannel channel, object msg);

		/// <summary>
		/// client send async request.
		/// </summary>
		/// <param name="channel"> client channel </param>
		/// <param name="msg">     transaction message <seealso cref="io.seata.core.protocol"/> </param>
		Task SendAsyncRequest(IChannel channel, object msg);

		/// <summary>
		/// client send async response.
		/// </summary>
		/// <param name="serverAddress"> server address </param>
		/// <param name="rpcMessage">    rpc message from server request </param>
		/// <param name="msg">           transaction message <seealso cref="io.seata.core.protocol"/> </param>
		Task SendAsyncResponse(string serverAddress, RpcMessage rpcMessage, object msg);

		/// <summary>
		/// On register msg success.
		/// </summary>
		/// <param name="serverAddress">  the server address </param>
		/// <param name="channel">        the channel </param>
		/// <param name="response">       the response </param>
		/// <param name="requestMessage"> the request message </param>
		Task OnRegisterMsgSuccess(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage);

		/// <summary>
		/// On register msg fail.
		/// </summary>
		/// <param name="serverAddress">  the server address </param>
		/// <param name="channel">        the channel </param>
		/// <param name="response">       the response </param>
		/// <param name="requestMessage"> the request message </param>
		Task OnRegisterMsgFail(string serverAddress, IChannel channel, object response, AbstractMessage requestMessage);

		/// <summary>
		/// register processor
		/// </summary>
		/// <param name="messageType"> <seealso cref="io.seata.core.protocol.MessageType"/> </param>
		/// <param name="processor">   <seealso cref="RemotingProcessor"/> </param>
		/// <param name="executor">    thread pool </param>
		Task RegisterProcessor(int messageType, IRemotingProcessor processor, IExecutorService executor);
	}
}
