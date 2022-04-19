using Zooyard.Rpc.NettyImpl.Protocol;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// To handle the received RPC message on upper level.
/// 
/// </summary>
public interface ITransactionMessageHandler
{

	/// <summary>
	/// On a request received.
	/// </summary>
	/// <param name="request"> received request message </param>
	/// <param name="context"> context of the RPC </param>
	/// <returns> response to the request </returns>
	Task<AbstractResultMessage> OnRequest(AbstractMessage request, RpcContext context);

	/// <summary>
	/// On a response received.
	/// </summary>
	/// <param name="response"> received response message </param>
	/// <param name="context">  context of the RPC </param>
	Task OnResponse(AbstractResultMessage response, RpcContext context);
}
