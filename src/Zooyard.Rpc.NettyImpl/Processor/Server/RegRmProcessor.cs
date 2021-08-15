using DotNetty.Transport.Channels;
using System;
using System.Threading.Tasks;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Loader;
using Zooyard.Logging;
using Zooyard.Utils;
using Version = Zooyard.Rpc.NettyImpl.Protocol.Version;

namespace Zooyard.Rpc.NettyImpl.Processor.Server
{
	/// <summary>
	/// process RM client registry message.
	/// <para>
	/// process message type:
	/// <seealso cref="RegisterRMRequest"/>
	/// </para>
	/// </summary>
	public class RegRmProcessor : IRemotingProcessor
	{
		private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(RegRmProcessor));

		private IRemotingServer remotingServer;

		private IRegisterCheckAuthHandler checkAuthHandler;

		public RegRmProcessor(IRemotingServer remotingServer)
		{
			this.remotingServer = remotingServer;
			this.checkAuthHandler = EnhancedServiceLoader.Load<IRegisterCheckAuthHandler>();
		}

		public virtual async Task Process(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			await OnRegRmMessage(ctx, rpcMessage);
		}

		private async Task OnRegRmMessage(IChannelHandlerContext ctx, RpcMessage rpcMessage)
		{
			var message = (RegisterRMRequest) rpcMessage.Body;
			string ipAndPort = NetUtil.ToStringAddress(ctx.Channel.RemoteAddress);
			bool isSuccess = false;
			string errorInfo = string.Empty;
			try
			{
				if (checkAuthHandler == null || await checkAuthHandler.RegResourceManagerCheckAuth(message))
				{
					ChannelManager.RegisterRMChannel(message, ctx.Channel);
					Version.PutChannelVersion(ctx.Channel, message.Version);
					isSuccess = true;
					if (Logger().IsEnabled(LogLevel.Debug))
					{
						Logger().LogDebug($"checkAuth for client:{ipAndPort},vgroup:{message.TransactionServiceGroup},applicationId:{ message.ApplicationId} is OK");
					}
				}
			}
			catch (Exception exx)
			{
				isSuccess = false;
				errorInfo = exx.Message;
				Logger().LogError(exx, $"RM register fail, error message:{errorInfo}");
			}
			var response = new RegisterRMResponse(isSuccess);
			if (!string.IsNullOrWhiteSpace(errorInfo))
			{
				response.Msg = errorInfo;
			}
			await remotingServer.SendAsyncResponse(rpcMessage, ctx.Channel, response);
			if (Logger().IsEnabled(LogLevel.Debug))
			{
				Logger().LogInformation($"RM register success,message:{message},channel:{ctx.Channel},client version:{message.Version}");
			}
		}
	}
}