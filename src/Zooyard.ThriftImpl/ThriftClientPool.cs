﻿using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Thrift.Transport.Client;
using Zooyard.Rpc;
using Zooyard.Rpc.Support;
using Zooyard.ThriftImpl.Header;
using Zooyard.Utils;

namespace Zooyard.ThriftImpl;

public class ThriftClientPool(ILogger<ThriftClientPool> logger) : AbstractClientPool(logger)
{
    public const string MAXFRAMESIZE_KEY = "MaxFrameSize";
    public const string MAXMESSAGESIZE_KEY = "MaxMessageSize";
    public const string RECURSIONLIMIT_KEY = "RecursionLimit";
    public const string MULTIPLEX_KEY = "multiplex";
    public const string TRANSPORT_KEY = "tr";
    public const string BUFFERING_KEY = "bf";

    public const string RPC_TIMEOUT_KEY = "rpc_timeout";
    public const int DEFAULT_RPC_TIMEOUT = 5000;

    public const string CHECK_TIMEOUT_KEY = "check_timeout";
    public const int DEFAULT_CHECK_TIMEOUT = 1000;

    protected override async Task<IClient> CreateClient(URL url)
    {
        //实例化TheTransport
        //获得transport参数,用于反射实例化
        
        var config = new TConfiguration();
        config.MaxFrameSize = url.GetParameter(MAXFRAMESIZE_KEY, config.MaxFrameSize);
        config.MaxMessageSize = url.GetParameter(MAXMESSAGESIZE_KEY, config.MaxMessageSize);
        config.RecursionLimit = url.GetParameter(RECURSIONLIMIT_KEY, config.RecursionLimit);

        var transport = GetTransport(url);
        _logger.LogInformation($"Selected client transport: {transport}");

        var protocol = MakeProtocol(url, MakeTransport(url, config));
        _logger.LogInformation($"Selected client protocol: {GetProtocol(url)}");

        var mplex = GetMultiplex(url);
        _logger.LogInformation("Multiplex " + (mplex ? "yes" : "no"));

        if (ProxyType == null)
        {
            throw new RpcException($"not find the proxy thrift client {url.ToFullString()}");
        }

        if (mplex)
            protocol = new TMultiplexedProtocol(protocol, ServiceName);

        //instance ThriftClient
        var client = (TBaseClient)Activator.CreateInstance(ProxyType, protocol)!;

        await Task.CompletedTask;
        return new ThriftClient(_logger, client, url);
    }



    private bool GetMultiplex(URL url)
    {
        var mplex = url.GetParameter(MULTIPLEX_KEY,"");
        return !string.IsNullOrEmpty(mplex);
    }

    private Protocol GetProtocol(URL url)
    {
        var protocol = url.Protocol;
        if (string.IsNullOrEmpty(protocol))
            return Protocol.Binary;

        protocol = protocol[..1].ToUpperInvariant() + protocol.Substring(1).ToLowerInvariant();
        if (Enum.TryParse(protocol, true, out Protocol selectedProtocol))
            return selectedProtocol;
        else
            return Protocol.Binary;
    }

    private Buffering GetBuffering(URL url)
    {
        var buffering = url.GetParameter(BUFFERING_KEY, "");
        if (string.IsNullOrEmpty(buffering))
            return Buffering.None;

        buffering = buffering[..1].ToUpperInvariant() + buffering.Substring(1).ToLowerInvariant();
        if (Enum.TryParse<Buffering>(buffering, out var selectedBuffering))
            return selectedBuffering;
        else
            return Buffering.None;
    }

    private Transport GetTransport(URL url)
    {
        var transport = url.GetParameter(TRANSPORT_KEY, "");
        if (string.IsNullOrEmpty(transport))
            return Transport.Tcp;

        transport = transport[..1].ToUpperInvariant() + transport.Substring(1).ToLowerInvariant();
        if (Enum.TryParse(transport, true, out Transport selectedTransport))
            return selectedTransport;
        else
            return Transport.Tcp;
    }

    private TTransport MakeTransport(URL url, TConfiguration configuration)
    {
        var ipaddress = IPAddress.Loopback;
        if (!NetUtil.IsAnyHost(url.Host) && !NetUtil.IsLocalHost(url.Host))
        {
            ipaddress = IPAddress.Parse(url.Host);
        }
        // construct endpoint transport
        TTransport? transport = null;
        Transport selectedTransport = GetTransport(url);
        {
            switch (selectedTransport)
            {
                case Transport.Tcp:
                    transport = new TSocketTransport(ipaddress, url.Port, configuration);
                    break;

                case Transport.NamedPipe:
                    transport = new TNamedPipeTransport(".test", configuration);
                    break;

                case Transport.Http:
                    transport = new THttpTransport(new Uri($"http://{url.Host}:{url.Port}"), configuration);
                    break;

                case Transport.TcpTls:
                    transport = new TTlsSocketTransport(ipaddress, url.Port, configuration,
                        GetCertificate(), CertValidator, LocalCertificateSelectionCallback);
                    break;

                default:
                    Console.WriteLine("unhandled case");
                    break;
            }
        }

        // optionally add layered transport(s)
        Buffering selectedBuffering = GetBuffering(url);
        switch (selectedBuffering)
        {
            case Buffering.Buffered:
                transport = new TBufferedTransport(transport);
                break;

            case Buffering.Framed:
                transport = new TFramedTransport(transport);
                break;

            default: // layered transport(s) are optional
                if (selectedBuffering != Buffering.None) 
                {
                    Console.WriteLine("unhandled case");
                }
                break;
        }

        return transport!;
    }
        
    private X509Certificate2 GetCertificate()
    {
        // due to files location in net core better to take certs from top folder
        var certFile = GetCertPath(Directory.GetParent(Directory.GetCurrentDirectory()));
        return new X509Certificate2(certFile, "ThriftTest");
    }

    private string GetCertPath(DirectoryInfo? di, int maxCount = 6)
    {
        var certFile =di?.EnumerateFiles("ThriftTest.pfx", SearchOption.AllDirectories)?.FirstOrDefault();
        if (certFile == null)
        {
            if (maxCount == 0)
                throw new FileNotFoundException("Cannot find file in directories");
            return GetCertPath(di?.Parent, maxCount - 1);
        }

        return certFile.FullName;
    }

    private X509Certificate LocalCertificateSelectionCallback(object sender,
        string targetHost, X509CertificateCollection localCertificates,
        X509Certificate? remoteCertificate, string[] acceptableIssuers)
    {
        return GetCertificate();
    }

    private static bool CertValidator(object sender, X509Certificate? certificate,
        X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    private TProtocol MakeProtocol(URL url, TTransport transport)
    {
        Protocol selectedProtocol = GetProtocol(url);
        return selectedProtocol switch
        {
            Protocol.Binary => new TBinaryProtocol(transport),
            Protocol.Compact => new TCompactProtocol(transport),
            Protocol.Json => new TJsonProtocol(transport),
            Protocol.BinaryHeader => new TBinaryHeaderProtocol(transport),
            Protocol.CompactHeader => new TCompactHeaderProtocol(transport),
            Protocol.JsonHeader => new TJsonHeaderProtocol(transport),
            _ => throw new Exception("unhandled protocol"),
        };
    }

    private enum Buffering
    {
        None,
        Buffered,
        Framed
    }
    private enum Transport
    {
        Tcp,
        NamedPipe,
        Http,
        TcpBuffered,
        Framed,
        TcpTls
    }

    private enum Protocol
    {
        Binary,
        Compact,
        Json,
        BinaryHeader,
        CompactHeader,
        JsonHeader,
    }
}

