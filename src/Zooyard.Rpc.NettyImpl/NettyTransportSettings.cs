﻿using DotNetty.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Zooyard.Rpc.NettyImpl;

/// <summary>
/// INTERNAL API.
/// 
/// Defines the settings for the <see cref="DotNettyTransport"/>.
/// </summary>
internal sealed class NettyTransportSettings
{
    public static NettyTransportSettings Create(URL config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config), "DotNetty HOCON config was not found (default path: `akka.remote.dot-netty`)");
        
        var host = config.GetParameter("hostname");
        if (string.IsNullOrEmpty(host)) host = IPAddress.Any.ToString();
        var publicHost = config.GetParameter<string>("public-hostname", null);
        var publicPort = config.GetParameter<int>("public-port", 0);

        var byteOrderString = config.GetParameter("byte-order", "little-endian").ToLowerInvariant();
        var order = byteOrderString switch
        {
            "little-endian" => ByteOrder.LittleEndian,
            "big-endian" => ByteOrder.BigEndian,
            _ => throw new ArgumentException($"Unknown byte-order option [{byteOrderString}]. Supported options are: big-endian, little-endian."),
        };
        return new NettyTransportSettings(
            enableSsl: config.GetParameter("enable-ssl", false),
            connectTimeout: TimeSpan.FromSeconds(config.GetParameter("connection-timeout", 15)),
            hostname: host,
            publicHostname: !string.IsNullOrEmpty(publicHost) ? publicHost : host,
            port: config.Port <= 0 ? 2552 : config.Port,
            publicPort: publicPort > 0 ? publicPort : (int?)null,
            serverSocketWorkerPoolSize: ComputeWorkerPoolSize(config),//.GetConfig("server-socket-worker-pool")),
            clientSocketWorkerPoolSize: ComputeWorkerPoolSize(config),//.GetConfig("client-socket-worker-pool")),
            maxFrameSize: config.GetParameter("maximum-frame-size", 128000),
            ssl: config.HasParameter("ssl") ? SslSettings.Create(config) : SslSettings.Empty,
            dnsUseIpv6: config.GetParameter("dns-use-ipv6", false),
            tcpReuseAddr: config.GetParameter("tcp-reuse-addr", true),
            tcpKeepAlive: config.GetParameter("tcp-keepalive", true),
            tcpNoDelay: config.GetParameter("tcp-nodelay", true),
            backlog: config.GetParameter("backlog", 4096),
            enforceIpFamily: (Type.GetType("Mono.Runtime") != null) || config.GetParameter("enforce-ip-family", false),
            receiveBufferSize: config.GetParameter("receive-buffer-size", 256000),
            sendBufferSize: config.GetParameter("send-buffer-size", 256000),
            writeBufferHighWaterMark: ToNullableInt(config.GetParameter<int>("write-buffer-high-water-mark")),
            writeBufferLowWaterMark: ToNullableInt(config.GetParameter<int>("write-buffer-low-water-mark")),
            backwardsCompatibilityModeEnabled: config.GetParameter("enable-backwards-compatibility", false),
            logTransport: config.HasParameter("log-transport") && config.GetParameter<bool>("log-transport"),
            byteOrder: order,
            enableBufferPooling: config.GetParameter("enable-pooling", true));
    }

    private static int? ToNullableInt(long? value) => value.HasValue && value.Value > 0 ? (int?)value.Value : null;

    private static int ComputeWorkerPoolSize(URL config)
    {
        if (config == null) return ThreadPoolConfig.ScaledPoolSize(2, 1.0, 2);

        return ThreadPoolConfig.ScaledPoolSize(
            floor: config.GetParameter("pool-size-min",2),
            scalar: config.GetParameter("pool-size-factor",1.0D),
            ceiling: config.GetParameter("pool-size-max",2));
    }

    /// <summary>
    /// If set to true, a Secure Socket Layer will be established
    /// between remote endpoints. They need to share a X509 certificate
    /// which path is specified in `akka.remote.dot-netty.tcp.ssl.certificate.path`
    /// </summary>
    public readonly bool EnableSsl;

    /// <summary>
    /// Sets a connection timeout for all outbound connections 
    /// i.e. how long a connect may take until it is timed out.
    /// </summary>
    public readonly TimeSpan ConnectTimeout;

    /// <summary>
    /// The hostname or IP to bind the remoting to.
    /// </summary>
    public readonly string Hostname;

    /// <summary>
    /// If this value is set, this becomes the public address for the actor system on this
    /// transport, which might be different than the physical ip address (hostname)
    /// this is designed to make it easy to support private / public addressing schemes
    /// </summary>
    public readonly string PublicHostname;

    /// <summary>
    /// The default remote server port clients should connect to.
    /// Default is 2552 (AKKA), use 0 if you want a random available port
    /// This port needs to be unique for each actor system on the same machine.
    /// </summary>
    public readonly int Port;

    /// <summary>
    /// If this value is set, this becomes the public port for the actor system on this
    /// transport, which might be different than the physical port
    /// this is designed to make it easy to support private / public addressing schemes
    /// </summary>
    public readonly int? PublicPort;

    public readonly int ServerSocketWorkerPoolSize;
    public readonly int ClientSocketWorkerPoolSize;
    public readonly int MaxFrameSize;
    public readonly SslSettings Ssl;

    /// <summary>
    /// If set to true, we will use IPv6 addresses upon DNS resolution for 
    /// host names. Otherwise IPv4 will be used.
    /// </summary>
    public readonly bool DnsUseIpv6;

    /// <summary>
    /// Enables SO_REUSEADDR, which determines when an ActorSystem can open
    /// the specified listen port (the meaning differs between *nix and Windows).
    /// </summary>
    public readonly bool TcpReuseAddr;

    /// <summary>
    /// Enables TCP Keepalive, subject to the O/S kernel's configuration.
    /// </summary>
    public readonly bool TcpKeepAlive;

    /// <summary>
    /// Enables the TCP_NODELAY flag, i.e. disables Nagle's algorithm
    /// </summary>
    public readonly bool TcpNoDelay;

    /// <summary>
    /// If set to true, we will enforce usage of IPv4 or IPv6 addresses upon DNS 
    /// resolution for host names. If true, we will use IPv6 enforcement. Otherwise, 
    /// we will use IPv4.
    /// </summary>
    public readonly bool EnforceIpFamily;

    /// <summary>
    /// Sets the size of the connection backlog.
    /// </summary>
    public readonly int Backlog;

    /// <summary>
    /// Sets the default receive buffer size of the Sockets.
    /// </summary>
    public readonly int? ReceiveBufferSize;

    /// <summary>
    /// Sets the default send buffer size of the Sockets.
    /// </summary>
    public readonly int? SendBufferSize;
    public readonly int? WriteBufferHighWaterMark;
    public readonly int? WriteBufferLowWaterMark;

    /// <summary>
    /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
    /// </summary>
    public readonly bool BackwardsCompatibilityModeEnabled;

    /// <summary>
    /// When set to true, it will enable logging of DotNetty user events 
    /// and message frames.
    /// </summary>
    public readonly bool LogTransport;

    /// <summary>
    /// Byte order used by DotNetty, either big or little endian.
    /// By default a little endian is used to achieve compatibility with Helios.
    /// </summary>
    public readonly ByteOrder ByteOrder;

    /// <summary>
    /// Used mostly as a work-around for https://github.com/akkadotnet/akka.net/issues/3370
    /// on .NET Core on Linux. Should always be left to <c>true</c> unless running DotNetty v0.4.6
    /// on Linux, which can accidentally release buffers early and corrupt frames. Turn this setting
    /// to <c>false</c> to disable pooling and work-around this issue at the cost of some performance.
    /// </summary>
    public readonly bool EnableBufferPooling;

    public NettyTransportSettings(bool enableSsl, TimeSpan connectTimeout, string hostname, string publicHostname,
        int port, int? publicPort, int serverSocketWorkerPoolSize, int clientSocketWorkerPoolSize, int maxFrameSize, SslSettings ssl,
        bool dnsUseIpv6, bool tcpReuseAddr, bool tcpKeepAlive, bool tcpNoDelay, int backlog, bool enforceIpFamily,
        int? receiveBufferSize, int? sendBufferSize, int? writeBufferHighWaterMark, int? writeBufferLowWaterMark, bool backwardsCompatibilityModeEnabled, bool logTransport, ByteOrder byteOrder, bool enableBufferPooling)
    {
        if (maxFrameSize < 32000) throw new ArgumentException("maximum-frame-size must be at least 32000 bytes", nameof(maxFrameSize));
        
        EnableSsl = enableSsl;
        ConnectTimeout = connectTimeout;
        Hostname = hostname;
        PublicHostname = publicHostname;
        Port = port;
        PublicPort = publicPort;
        ServerSocketWorkerPoolSize = serverSocketWorkerPoolSize;
        ClientSocketWorkerPoolSize = clientSocketWorkerPoolSize;
        MaxFrameSize = maxFrameSize;
        Ssl = ssl;
        DnsUseIpv6 = dnsUseIpv6;
        TcpReuseAddr = tcpReuseAddr;
        TcpKeepAlive = tcpKeepAlive;
        TcpNoDelay = tcpNoDelay;
        Backlog = backlog;
        EnforceIpFamily = enforceIpFamily;
        ReceiveBufferSize = receiveBufferSize;
        SendBufferSize = sendBufferSize;
        WriteBufferHighWaterMark = writeBufferHighWaterMark;
        WriteBufferLowWaterMark = writeBufferLowWaterMark;
        BackwardsCompatibilityModeEnabled = backwardsCompatibilityModeEnabled;
        LogTransport = logTransport;
        ByteOrder = byteOrder;
        EnableBufferPooling = enableBufferPooling;
    }
}

internal sealed class SslSettings
{
    public static readonly SslSettings Empty = new SslSettings();
    public static SslSettings Create(URL config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config), "DotNetty SSL HOCON config was not found (default path: `akka.remote.dot-netty.Ssl`)");



        if (config.GetParameter("certificate.use-thumprint-over-file", false))
        {
            return new SslSettings(config.GetParameter("certificate.thumbprint"),
                config.GetParameter("certificate.store-name"),
                ParseStoreLocationName(config.GetParameter("certificate.store-location")),
                    config.GetParameter("suppress-validation", false));

        }
        else
        {
            var flagsRaw = config.GetParameter("certificate.flags",new[] { ""}).ToList();
            var flags = flagsRaw.Aggregate(X509KeyStorageFlags.DefaultKeySet, (flag, str) => flag | ParseKeyStorageFlag(str));

            return new SslSettings(
                certificatePath: config.GetParameter("certificate.path"),
                certificatePassword: config.GetParameter("certificate.password"),
                flags: flags,
                suppressValidation: config.GetParameter("suppress-validation", false));
        }

    }

    private static StoreLocation ParseStoreLocationName(string str)
    {
        switch (str)
        {
            case "local-machine": return StoreLocation.LocalMachine;
            case "current-user": return StoreLocation.CurrentUser;
            default: throw new ArgumentException($"Unrecognized flag in X509 certificate config [{str}]. Available flags: local-machine | current-user");
        }
    }

    private static X509KeyStorageFlags ParseKeyStorageFlag(string str)
    {
        return str switch
        {
            "default-key-set" => X509KeyStorageFlags.DefaultKeySet,
            "exportable" => X509KeyStorageFlags.Exportable,
            "machine-key-set" => X509KeyStorageFlags.MachineKeySet,
            "persist-key-set" => X509KeyStorageFlags.PersistKeySet,
            "user-key-set" => X509KeyStorageFlags.UserKeySet,
            "user-protected" => X509KeyStorageFlags.UserProtected,
            _ => throw new ArgumentException($"Unrecognized flag in X509 certificate config [{str}]. Available flags: default-key-set | exportable | machine-key-set | persist-key-set | user-key-set | user-protected"),
        };
    }

    /// <summary>
    /// X509 certificate used to establish Secure Socket Layer (SSL) between two remote endpoints.
    /// </summary>
    public readonly X509Certificate2 Certificate;

    /// <summary>
    /// Flag used to suppress certificate validation - use true only, when on dev machine or for testing.
    /// </summary>
    public readonly bool SuppressValidation;

    public SslSettings()
    {
        Certificate = null;
        SuppressValidation = false;
    }

    public SslSettings(string certificateThumbprint, string storeName, StoreLocation storeLocation, bool suppressValidation)
    {

        var store = new X509Store(storeName, storeLocation);
        try
        {
            store.Open(OpenFlags.ReadOnly);


            var find = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, !suppressValidation);
            if (find.Count == 0)
            {
                throw new ArgumentException(
                    "Could not find Valid certificate for thumbprint (by default it can be found under `akka.remote.dot-netty.tcp.ssl.certificate.thumpbrint`. Also check akka.remote.dot-netty.tcp.ssl.certificate.store-name and akka.remote.dot-netty.tcp.ssl.certificate.store-location)");
            }

            Certificate = find[0];
            SuppressValidation = suppressValidation;
        }
        finally
        {
#if  NET45 //netstandard1.6 doesn't have close on store.
            store.Close();
#else
#endif

        }

    }

    public SslSettings(string certificatePath, string certificatePassword, X509KeyStorageFlags flags, bool suppressValidation)
    {
        if (string.IsNullOrEmpty(certificatePath))
            throw new ArgumentNullException(nameof(certificatePath), "Path to SSL certificate was not found (by default it can be found under `akka.remote.dot-netty.tcp.ssl.certificate.path`)");

        Certificate = new X509Certificate2(certificatePath, certificatePassword, flags);
        SuppressValidation = suppressValidation;
    }
}
