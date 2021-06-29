using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;
using Thrift.Transport.Server;
using Zooyard.Core;
using Zooyard.Core.Logging;
using Zooyard.Rpc.Support;
using Zooyard.Rpc.ThriftImpl.Header;

namespace Zooyard.Rpc.ThriftImpl
{
    public class ThriftServer : AbstractServer
    {
        private TServer _server;
        private readonly IOptionsMonitor<ThriftServerOption> _thriftServerOption;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IEnumerable<ITAsyncProcessor> _asyncProcessorList;
        public ThriftServer(//TServer server,
            IEnumerable<ITAsyncProcessor> asyncProcessorList,
            ILogger<ThriftServer> logger,
            ILoggerFactory loggerFactory,
            IOptionsMonitor<ThriftServerOption> thriftServerOption,
            IRegistryService registryService)
            :base(registryService)
        {
            _asyncProcessorList = asyncProcessorList;
            _thriftServerOption = thriftServerOption;
            _logger  = logger;
            _loggerFactory = loggerFactory;
            //_server = server;
        }
        
        
        public override async Task DoExport(CancellationToken cancellationToken)
        {
            //_server.Start();

            //run the server

            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        await _server.ServeAsync(cancellationToken);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger().LogError(ex);
            //        throw ex;
            //    }
            //});

            //await _server.ServeAsync(cancellationToken);
            await RunAsync(cancellationToken);
            _logger.LogInformation("Started the thrift server ...");
            Console.WriteLine("Started the thrift server ...");
        }

        public override async Task DoDispose()
        {
            //unregiste from register center
            await Task.CompletedTask;
            _server.Stop();
            _logger.LogInformation("stoped the thrift server ...");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var selectedTransport = GetTransport();
            var selectedBuffering = GetBuffering();
            var selectedProtocol = GetProtocol();
            var multiplex = GetMultiplex();

            if (selectedTransport == Transport.Http)
            {
                if (multiplex)
                    throw new Exception("This semple code does not yet allow multiplex over http (although Thrift itself of course does)");

                var asyncProcessor = _asyncProcessorList.FirstOrDefault();
                await new HttpServerSample(asyncProcessor).Run(cancellationToken);
            }
            else
            {
                await RunSelectedConfigurationAsync(selectedTransport, selectedBuffering, selectedProtocol, multiplex, cancellationToken);
            }
        }


        private bool GetMultiplex()
        {
            var mplex = _thriftServerOption.CurrentValue.Multiplex;
            return mplex;
        }

        private Protocol GetProtocol()
        {
            var protocol = _thriftServerOption.CurrentValue.Protocol;
            if (string.IsNullOrEmpty(protocol))
                return Protocol.Binary;

            protocol = protocol.Substring(0, 1).ToUpperInvariant() + protocol.Substring(1).ToLowerInvariant();
            if (Enum.TryParse(protocol, true, out Protocol selectedProtocol))
                return selectedProtocol;
            else
                return Protocol.Binary;
        }

        private Buffering GetBuffering()
        {
            var buffering = _thriftServerOption.CurrentValue.Buffering;
            if (string.IsNullOrEmpty(buffering))
                return Buffering.None;

            buffering = buffering.Substring(0, 1).ToUpperInvariant() + buffering.Substring(1).ToLowerInvariant();
            if (Enum.TryParse<Buffering>(buffering, out var selectedBuffering))
                return selectedBuffering;
            else
                return Buffering.None;
        }

        private Transport GetTransport()
        {
            var transport = _thriftServerOption.CurrentValue.Transport;
            if (string.IsNullOrEmpty(transport))
                return Transport.Tcp;

            transport = transport.Substring(0, 1).ToUpperInvariant() + transport.Substring(1).ToLowerInvariant();
            if (Enum.TryParse(transport, true, out Transport selectedTransport))
                return selectedTransport;
            else
                return Transport.Tcp;
        }

        private async Task RunSelectedConfigurationAsync(Transport transport, Buffering buffering, Protocol protocol, bool multiplex, CancellationToken cancellationToken)
        {
            var port = _thriftServerOption.CurrentValue.Port;
            var configuration = _thriftServerOption.CurrentValue.Configuration;
            TServerTransport serverTransport = transport switch
            {
                Transport.Tcp => new TServerSocketTransport(port, configuration),
                Transport.NamedPipe => new TNamedPipeServerTransport(".test", configuration),//, NamedPipeClientFlags.None),
                Transport.TcpTls => new TTlsServerSocketTransport(9090, configuration, GetCertificate(), ClientCertValidator, LocalCertificateSelectionCallback),
                _ => throw new ArgumentException("unsupported value $transport", nameof(transport)),
            };

            TTransportFactory transportFactory = buffering switch
            {
                Buffering.Buffered => new TBufferedTransport.Factory(),
                Buffering.Framed => new TFramedTransport.Factory(),
                // layered transport(s) are optional
                Buffering.None => null,
                _ => throw new ArgumentException("unsupported value $buffering", nameof(buffering)),
            };

            TProtocolFactory protocolFactory = protocol switch
            {
                Protocol.Binary => new TBinaryProtocol.Factory(),
                Protocol.Compact => new TCompactProtocol.Factory(),
                Protocol.Json => new TJsonProtocol.Factory(),
                Protocol.BinaryHeader => new TBinaryHeaderServerProtocol.Factory(),
                Protocol.CompactHeader => new TCompactHeaderServerProtocol.Factory(),
                Protocol.JsonHeader => new TJsonHeaderServerProtocol.Factory(),
                _ => throw new ArgumentException("unsupported value $protocol", nameof(protocol)),
            };

            //var handler = new CalculatorAsyncHandler();
            //ITAsyncProcessor processor = new Calculator.AsyncProcessor(handler);
            ITAsyncProcessor processor = _asyncProcessorList.FirstOrDefault();
            if (multiplex)
            {
                var multiplexedProcessor = new TMultiplexedProcessor();
                foreach (var item in _asyncProcessorList)
                {
                    multiplexedProcessor.RegisterProcessor(item.GetType().FullName, item);
                }

                processor = multiplexedProcessor;
            }


            try
            {
                _logger.LogInformation(
                    string.Format(
                        "TSimpleAsyncServer with \n{0} transport\n{1} buffering\nmultiplex = {2}\n{3} protocol",
                        transport,
                        buffering,
                        multiplex ? "yes" : "no",
                        protocol
                        ));


                _server = new TSimpleAsyncServer(
                    itProcessorFactory: new TSingletonProcessorFactory(processor),
                    serverTransport: serverTransport,
                    inputTransportFactory: transportFactory,
                    outputTransportFactory: transportFactory,
                    inputProtocolFactory: protocolFactory,
                    outputProtocolFactory: protocolFactory,
                    logger: _loggerFactory.CreateLogger<TSimpleAsyncServer>());

                //var threadConfig = new TThreadPoolAsyncServer.Configuration();
                //var server = new TThreadPoolAsyncServer(
                //     processorFactory: new TSingletonProcessorFactory(processor),
                //     serverTransport: serverTransport,
                //     inputTransportFactory: transportFactory,
                //     outputTransportFactory: transportFactory,
                //     inputProtocolFactory: protocolFactory,
                //     outputProtocolFactory: protocolFactory,
                //     threadConfig: threadConfig,
                //     logger: loggerFactory.CreateLogger<TThreadPoolAsyncServer>());

                _logger.LogInformation("Starting the server...");

                await _server.ServeAsync(cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogInformation(x.ToString());
            }
        }

        private X509Certificate2 GetCertificate()
        {
            // due to files location in net core better to take certs from top folder
            var certFile = GetCertPath(Directory.GetParent(Directory.GetCurrentDirectory()));
            return new X509Certificate2(certFile, "ThriftTest");
        }

        private string GetCertPath(DirectoryInfo di, int maxCount = 6)
        {
            var topDir = di;
            var certFile =
                topDir.EnumerateFiles("ThriftTest.pfx", SearchOption.AllDirectories)
                    .FirstOrDefault();
            if (certFile == null)
            {
                if (maxCount == 0)
                    throw new FileNotFoundException("Cannot find file in directories");
                return GetCertPath(di.Parent, maxCount - 1);
            }

            return certFile.FullName;
        }

        private X509Certificate LocalCertificateSelectionCallback(object sender,
            string targetHost, X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return GetCertificate();
        }

        private bool ClientCertValidator(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private enum Transport
        {
            Tcp,
            NamedPipe,
            Http,
            TcpTls,
        }

        private enum Buffering
        {
            None,
            Buffered,
            Framed,
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

        public class HttpServerSample
        {
            private readonly ITAsyncProcessor _asyncProcessor;
            public HttpServerSample(ITAsyncProcessor asyncProcessor) 
            {
                _asyncProcessor = asyncProcessor;
            }
            public async Task Run(CancellationToken cancellationToken)
            {
                var config = new ConfigurationBuilder()
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .Build();

                var host = new WebHostBuilder()
                    .UseConfiguration(config)
                    .UseKestrel()
                    .UseUrls("http://localhost:9090")
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureServices(services => {
                        services.AddTransient(w=>_asyncProcessor);
                    })
                    .UseStartup<Startup>()
                    .ConfigureLogging((ctx, logging) => ConfigureLogging(logging))
                    .Build();

                await host.RunAsync(cancellationToken);
            }

            private void ConfigureLogging(ILoggingBuilder logging)
            {
                logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                logging.AddConsole();
                logging.AddDebug();
            }

            public class Startup
            {
                public Startup(IHostingEnvironment env)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(env.ContentRootPath)
                        .AddEnvironmentVariables();

                    Configuration = builder.Build();
                }

                public IConfigurationRoot Configuration { get; }

                // This method gets called by the runtime. Use this method to add services to the container.
                public void ConfigureServices(IServiceCollection services)
                {
                    // NOTE: this is not really the recommended way to do it
                    // because the HTTP server cannot be configured properly to e.g. accept framed or multiplex

                    //services.AddTransient<Calculator.IAsync, CalculatorAsyncHandler>();
                    //services.AddTransient<ITAsyncProcessor, _asyncProcessor>();
                    services.AddTransient<THttpServerTransport, THttpServerTransport>();
                }

                // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
                public void Configure(IApplicationBuilder app)//, IHostingEnvironment env, ILoggerFactory loggerFactory)
                {
                    app.UseMiddleware<THttpServerTransport>();
                }
            }
        }

    }

    public class ThriftServerOption
    {
        public string Transport { get; set; }
        public string Buffering { get; set; }
        public string Protocol { get; set; }
        public bool Multiplex { get; set; } = false;
        public int Port { get; set; }
        public TConfiguration Configuration { get; set; } = new TConfiguration();
        public int ClientTimeOut { get; set; }
    }
}
