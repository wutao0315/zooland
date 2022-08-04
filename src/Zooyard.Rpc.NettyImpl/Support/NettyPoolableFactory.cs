using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using System.Net;
using Zooyard.Exceptions;
using Zooyard.Logging;
using Zooyard.Rpc.NettyImpl.Protocol;
using Zooyard.Utils;

namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// The type Netty key poolable factory.
/// 
/// </summary>
public class NettyPoolableFactory: IAsyncDisposable
{
    private static readonly Func<Action<LogLevel, string, Exception?>> Logger = () => LogManager.CreateLogger(typeof(NettyPoolableFactory));

    private readonly ConcurrentDictionary<NettyPoolKey, IChannel> poolData = new ();
    private readonly AbstractNettyRemotingClient rpcRemotingClient;
    private readonly NettyClientBootstrap clientBootstrap;

    /// <summary>
    /// Instantiates a new Netty key poolable factory.
    /// </summary>
    /// <param name="rpcRemotingClient"> the rpc remoting client </param>
    public NettyPoolableFactory(AbstractNettyRemotingClient rpcRemotingClient,
        NettyClientBootstrap clientBootstrap)
    {
        this.rpcRemotingClient = rpcRemotingClient;
        this.clientBootstrap = clientBootstrap;
    }

    public async Task<IChannel> MakeObject(NettyPoolKey key)
    {
        IPEndPoint address = NetUtil.ToIPEndPoint(key.Address);
        if (Logger().IsEnabled(LogLevel.Information)) 
        {
            Logger().LogInformation($"NettyPool create channel to {key}");
        }
        IChannel tmpChannel = await clientBootstrap.GetNewChannel(address);
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        object response;
        IChannel channelToServer = null;
        if (key.Message == null)
        {
            throw new FrameworkException($"register msg is null.");
        }
        try
        {
            response = await rpcRemotingClient.SendSyncRequest(tmpChannel, key.Message);
            if (!IsResponseSuccess(response))
            {
                await rpcRemotingClient.OnRegisterMsgFail(key.Address, tmpChannel, response, key.Message);
            }
            else
            {
                channelToServer = tmpChannel;
                await rpcRemotingClient.OnRegisterMsgSuccess(key.Address, tmpChannel, response, key.Message);
            }
        }
        catch (Exception exx)
        {
            if (tmpChannel != null)
            {
                await tmpChannel.CloseAsync();
            }
            throw new FrameworkException($"register error,err:{exx.Message}");
        }
        if (Logger().IsEnabled(LogLevel.Information))
        {
            Logger().LogInformation($"register success, cost {(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start)}ms, version:{GetVersion(response)},channel:{channelToServer}");
        }
        return channelToServer;
    }

    private bool IsResponseSuccess(object response)
    {
        if (response == null)
        {
            return false;
        }
        return false;
    }

    private string GetVersion(object response)
    {
        return ((AbstractIdentifyResponse)response).Version;
    }

    public async Task DestroyObject(NettyPoolKey key, IChannel channel)
    {
        if (channel != null)
        {
            if (Logger().IsEnabled(LogLevel.Information)) 
            {
                Logger().LogInformation($"will destroy channel:{channel}");
            }

            await channel.DisconnectAsync();
            await channel.CloseAsync();
        }
        if (poolData.TryRemove(key, out IChannel cn)) 
        {
            await cn.DisconnectAsync();
            await cn.CloseAsync();
        }
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var item in poolData)
        {
            await DestroyObject(item.Key, item.Value);
        }
    }
    public bool ValidateObject(NettyPoolKey key, IChannel obj)
    {
        if (obj != null && obj.Active)
        {
            return true;
        }
        if (Logger().IsEnabled(LogLevel.Information)) 
        {
            Logger().LogInformation($"channel valid false,channel:{obj}");
        }
        return false;
    }
    public void ReturnObject(NettyPoolKey key, IChannel obj) 
    {
        poolData.TryAdd(key, obj);
    }
    public async Task InvalidateObject(NettyPoolKey key, IChannel obj)
    {
        await DestroyObject(key, obj);
    }
    public async Task<IChannel> BorrowObject(NettyPoolKey key)
    {
        if (poolData.TryRemove(key, out IChannel result))
        {
            return result;
        }
        result = await this.MakeObject(key);
        return result;
    }
}
