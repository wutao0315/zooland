using System;
using System.Collections.Generic;
using System.Text;

namespace Zooyard
{
    public class ZooyardOption
    {
        public string RegisterUrl { get; set; }
        public IDictionary<string, ZooyardClientOption> Clients { get; set; }
        public IDictionary<string, string> Mergers { get; set; }

        public override string ToString()
        {
            var clientBuilder = new StringBuilder("");
            foreach (var item in Clients)
            {
                clientBuilder.Append($@"{item.Key}:[
                                    version:{item.Value.Version},
                                    service:{item.Value.Service},
                                    poolType:{item.Value.PoolType},
                                    urls:[{string.Join(",", item.Value.Urls)}]]");
            }
            var mergerBuilder = new StringBuilder("");
            foreach (var item in Mergers)
            {
                mergerBuilder.Append(item.Key);
                mergerBuilder.Append(":");
                mergerBuilder.Append(item.Value);
            }
            return $"[RegisterUrl:{RegisterUrl},Clients:[{clientBuilder}],Mergers:[{mergerBuilder}]]";
        }
    }
    public class ZooyardClientOption
    {
        public string Version { get; set; }
        public string ServiceType { get; set; }
        public IEnumerable<string> Urls { get; set; }
        public string PoolType { get; set; }

        private Type _service;
        public Type Service { get { return _service == null ? _service = Type.GetType(ServiceType) : _service; } }
    }
}
