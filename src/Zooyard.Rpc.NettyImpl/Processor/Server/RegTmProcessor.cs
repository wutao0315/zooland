using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Loader;
using Zooyard.Logging;
using Zooyard.Utils;


namespace Zooyard.Rpc.NettyImpl.Processor.Server
{

	/// <summary>
	/// process TM client registry message.
	/// <para>
	/// process message type: <seealso cref="RegisterTMRequest"/>
	/// </para>
	/// </summary>
	public class RegTmProcessor : IRemotingProcessor
	{

		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RegTmProcessor));

		private IRemotingServer remotingServer;

		private IRegisterCheckAuthHandler checkAuthHandler;

		public RegTmProcessor(IRemotingServer remotingServer)
		{
			this.remotingServer = remotingServer;
			this.checkAuthHandler = EnhancedServiceLoader.Load<IRegisterCheckAuthHandler>();
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			await OnRegTmMessage(ctx, rpcMessage);
		}

		private async Task OnRegTmMessage(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			var message = (RegisterTMRequest) rpcMessage.Body;
			string ipAndPort = NetUtil.ToStringAddress(ctx.Channel.RemoteAddress);
            Protocol.Version.PutChannelVersion(ctx.Channel, message.Version);
			bool isSuccess = false;
			string errorInfo = string.Empty;
			try
			{
				if (checkAuthHandler == null || await checkAuthHandler.RegTransactionManagerCheckAuth(message))
				{
					ChannelManager.RegisterTMChannel(message, ctx.Channel);
                    Protocol.Version.PutChannelVersion(ctx.Channel, message.Version);
					isSuccess = true;
					if (Logger().IsEnabled(LogLevel.Debug))
					{
						Logger().LogDebug($"checkAuth for client:{ipAndPort},vgroup:{message.TransactionServiceGroup},applicationId:{message.ApplicationId}");
					}
				}
			}
			catch (Exception exx)
			{
				isSuccess = false;
				errorInfo = exx.Message;
				Logger().LogError(exx, $"TM register fail, error message:{errorInfo}");
			}
			var response = new RegisterTMResponse(isSuccess);
			if (!string.IsNullOrWhiteSpace(errorInfo))
			{
				response.Msg = errorInfo;
			}
			await remotingServer.SendAsyncResponse(rpcMessage, ctx.Channel, response);
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogInformation($"TM register success,message:{message},channel:{ctx.Channel},client version:{message.Version}");
			}
		}

	}

}