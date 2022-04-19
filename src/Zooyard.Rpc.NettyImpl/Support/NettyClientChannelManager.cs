using DotNetty.Transport.Channels;
using System.Collections.Concurrent;
using Zooyard.Exceptions;
using Zooyard.Logging;

namespace Zooyard.Rpc.NettyImpl.Support;

/// <summary>
/// Netty client pool manager.
/// 
/// </summary>
public class NettyClientChannelManager
{
    private static readonly Func<Action<LogLevel, string, Exception>> Logger = () => LogManager.CreateLogger(typeof(NettyClientChannelManager));

    private readonly ConcurrentDictionary<string, object> channelLocks = new ();

    private readonly ConcurrentDictionary<string, NettyPoolKey> poolKeyMap = new ();

    private readonly ConcurrentDictionary<string, IChannel> channels = new ();

    private readonly NettyPoolableFactory nettyClientKeyPool;

    private readonly int CHANNEL_LOCKS_MILS = 1500;

    private Func<string, NettyPoolKey> poolKeyFunction;

    internal NettyClientChannelManager(NettyPoolableFactory keyPoolableFactory,
        Func<string, NettyPoolKey> poolKeyFunction, 
        NettyClientConfig clientConfig)
    {
        nettyClientKeyPool = keyPoolableFactory;
        //nettyClientKeyPool.Config = getNettyPoolConfig(clientConfig);
        this.poolKeyFunction = poolKeyFunction;
    }

    //private GenericKeyedObjectPool.Config getNettyPoolConfig(NettyClientConfig clientConfig)
    //{
    //    GenericKeyedObjectPool.Config poolConfig = new GenericKeyedObjectPool.Config();
    //    poolConfig.maxActive = clientConfig.MaxPoolActive;
    //    poolConfig.minIdle = clientConfig.MinPoolIdle;
    //    poolConfig.maxWait = clientConfig.MaxAcquireConnMills;
    //    poolConfig.testOnBorrow = clientConfig.PoolTestBorrow;
    //    poolConfig.testOnReturn = clientConfig.PoolTestReturn;
    //    poolConfig.lifo = clientConfig.PoolLifo;
    //    return poolConfig;
    //}

    /// <summary>
    /// Get all channels registered on current Rpc Client.
    /// </summary>
    /// <returns> channels </returns>
    internal virtual ConcurrentDictionary<string, IChannel> Channels => channels;

    /// <summary>
    /// Acquire netty client channel connected to remote server.
    /// </summary>
    /// <param name="serverAddress"> server address </param>
    /// <returns> netty channel </returns>
    internal virtual async Task<IChannel> AcquireChannel(string serverAddress)
    {
        channels.TryGetValue(serverAddress, out IChannel channelToServer);
        if (channelToServer != null)
        {
            channelToServer = await GetExistAliveChannel(channelToServer, serverAddress);
            if (channelToServer != null)
            {
                return channelToServer;
            }
        }
        if (Logger().IsEnabled(LogLevel.Information))
        {
            Logger().LogInformation("will connect to " + serverAddress);
        }

        //var channel = await DoConnect(serverAddress);
        //return channel;
        var lockObj = channelLocks.GetOrAdd(serverAddress, (key) => new object());

        lock (lockObj)
        {
            return DoConnect(serverAddress).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Release channel to pool if necessary.
    /// </summary>
    /// <param name="channel"> channel </param>
    /// <param name="serverAddress"> server address </param>
    internal virtual async Task ReleaseChannel(IChannel channel, string serverAddress)
    {
        if (channel == null || serverAddress == null)
        {
            return;
        }
        await Task.CompletedTask;
        var lockObj = channelLocks.GetOrAdd(serverAddress, (key) => new object());
        lock (lockObj)
        {
            channels.TryGetValue(serverAddress, out IChannel ch);
            if (ch == null)
            {
                nettyClientKeyPool.ReturnObject(poolKeyMap[serverAddress], channel);
                return;
            }
            if (ch.CompareTo(channel) == 0)
            {
                if (Logger().IsEnabled(LogLevel.Information))
                {
                    Logger().LogInformation($"return to pool, rm channel:{channel}");
                }

                DestroyChannel(serverAddress, channel).GetAwaiter().GetResult();
            }
            else
            {
                nettyClientKeyPool.ReturnObject(poolKeyMap[serverAddress], channel);
            }
        }
    }

    /// <summary>
    /// Destroy channel.
    /// </summary>
    /// <param name="serverAddress"> server address </param>
    /// <param name="channel"> channel </param>
    internal virtual async Task DestroyChannel(string serverAddress, IChannel channel)
    {
        if (channel == null)
        {
            return;
        }
        try
        {
            if (channels.TryGetValue(serverAddress,out IChannel cacheCh) && channel.Equals(cacheCh))
            {
                channels.TryRemove(serverAddress, out _);
            }
            nettyClientKeyPool.ReturnObject(poolKeyMap[serverAddress], channel);
            await Task.CompletedTask;
        }
        catch (Exception exx)
        {
            Logger().LogError(exx, $"return channel to rmPool error:{exx.Message}");
        }
    }

    /// <summary>
    /// Reconnect to remote server of current transaction service group.
    /// </summary>
    /// <param name="transactionServiceGroup"> transaction service group </param>
    internal virtual async Task Reconnect(string transactionServiceGroup)
    {
        IList<string> availList = null;
        try
        {
            availList = GetAvailServerList(transactionServiceGroup);
        }
        catch (Exception exx)
        {
            Logger().LogError(exx, $"Failed to get available servers:{exx.Message}");
        }

        if ((availList?.Count ?? 0) <= 0)
        {
            //var registryService = RegistryFactory.Instance;
            //string clusterName = registryService.GetServiceGroup(transactionServiceGroup);

            //if (string.IsNullOrWhiteSpace(clusterName))
            //{
            //    Logger().LogError($"can not get cluster name in registry config '{Constant.ConfigurationKeys.SERVICE_GROUP_MAPPING_PREFIX}{transactionServiceGroup}', please make sure registry config correct");
            //    return;
            //}
            //if (!(registryService is FileRegistryServiceImpl))
            //{
            //    Logger().LogError($"no available service found in cluster '{clusterName}', please make sure registry config correct and keep your seata server running");
            //}
            if (Logger().IsEnabled(LogLevel.Debug))
            {
                Logger().LogDebug("availList is empty");
            }
            return;
        }

        foreach (string serverAddress in availList)
        {
            try
            {
                await AcquireChannel(serverAddress);
            }
            catch (Exception e)
            {
                Logger().LogError(e, $"{FrameworkErrorCode.NetConnect.GetErrCode()} can not connect to {serverAddress} cause:{e.Message}");
            }
        }
    }

    internal virtual async Task InvalidateObject(string serverAddress, IChannel channel)
    {
        //nettyClientKeyPool.InvalidateObject(poolKeyMap[serverAddress], channel);
        await nettyClientKeyPool.InvalidateObject(poolKeyMap[serverAddress], channel);
    }

    internal virtual void RegisterChannel(string serverAddress, IChannel channel)
    {
        if (channels.TryGetValue(serverAddress, out IChannel channelToServer) && channelToServer.Active) 
        {
            return;
        }

        channels.TryAdd(serverAddress, channel);
    }

    private async Task<IChannel> DoConnect(string serverAddress)
    {
        if (channels.TryGetValue(serverAddress, out IChannel channelToServer) && channelToServer.Active)
        {
            return channelToServer;
        }
        IChannel channelFromPool;
        try
        {
            NettyPoolKey currentPoolKey = poolKeyFunction(serverAddress);
            NettyPoolKey previousPoolKey = poolKeyMap.GetValueOrDefault(serverAddress, null);
            poolKeyMap.GetOrAdd(serverAddress, (key)=>currentPoolKey);
            channelFromPool = await nettyClientKeyPool.BorrowObject(poolKeyMap[serverAddress]);
            channels[serverAddress] = channelFromPool;
        }
        catch (Exception exx)
        {
            Logger().LogError(exx, $"{FrameworkErrorCode.RegisterRM.GetErrCode()} register RM failed.");
            throw new FrameworkException("can not register RM,err:" + exx.Message);
        }
        return channelFromPool;
    }


    private IList<string> GetAvailServerList(string transactionServiceGroup)
    {
        var availList = new List<string>
        {
            "127.0.0.1:8091"
        };
        return availList;

        //IList<IPEndPoint> availInetSocketAddressList = RegistryFactory.Instance.Lookup(transactionServiceGroup);
        //if ((availInetSocketAddressList?.Count ?? 0) <= 0)
        //{
        //    return Array.Empty<string>();
        //}
        //return availInetSocketAddressList.Select(w => NetUtil.ToStringAddress(w)).ToList();
    }

    private async Task<IChannel> GetExistAliveChannel(IChannel rmChannel, string serverAddress)
    {
        if (rmChannel.Active)
        {
            return rmChannel;
        }
        else
        {
            int i = 0;
            for (; i < NettyClientConfig.MaxCheckAliveRetry; i++)
            {
                try
                {
                    Thread.Sleep(NettyClientConfig.CheckAliveInterval);
                }
                catch (Exception exx)
                {
                    Logger().LogError(exx, exx.Message);
                }

                if (channels.TryGetValue(serverAddress, out rmChannel) 
                    && rmChannel != null && rmChannel.Active)
                {
                    return rmChannel;
                }
            }
            if (i == NettyClientConfig.MaxCheckAliveRetry)
            {
                Logger().LogWarning($"channel {rmChannel} is not active after long wait, close it.");
                await ReleaseChannel(rmChannel, serverAddress);
                return null;
            }
        }
        return null;
    }
}
