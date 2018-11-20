﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Zooyard.Rpc.Support;

namespace Zooyard.Rpc.WcfImpl
{
    public class WcfServer : AbstractServer
    {
        private readonly ILogger _logger;
        private readonly WcfService _server;
        public WcfServer(WcfService server, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WcfServer>();
            _server = server;
        }

        public IList<WcfService> Services { get; set; } = new List<WcfService>();
        public override void DoExport()
        {
            //open service
            _server.Open();


            // Step 3 of the hosting procedure: Add a service endpoint.

            Console.WriteLine($"Starting the wcf server ...");
           
        }

        public override void DoDispose()
        {
            _server.Dispose();
        }
    }
}
