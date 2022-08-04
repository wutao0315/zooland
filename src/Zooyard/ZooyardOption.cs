using System.Text;

namespace Zooyard;

public class ZooyardOption
{
    public string RegisterUrl { get; set; } = string.Empty;
    public Dictionary<string, ZooyardClientOption> Clients { get; set; } = new();
    public Dictionary<string, string> Mergers { get; set; } = new();

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
            mergerBuilder.Append(':');
            mergerBuilder.Append(item.Value);
        }
        return $"[RegisterUrl:{RegisterUrl},Clients:[{clientBuilder}],Mergers:[{mergerBuilder}]]";
    }
}
public class ZooyardClientOption
{
    public string Version { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public List<string> Urls { get; set; } = new();
    public string PoolType { get; set; } = string.Empty;

    private Type? _service;
    public Type Service { get { return _service == null ? _service = Type.GetType(ServiceType)! : _service; } }
}

//public class ZooyardOption
//{
//    public Dictionary<string, ZooyardClientOption> Clients { get; set; } = new ();
//}
//public class ZooyardClientOption
//{
//    public Dictionary<string, string> Meta { get; set; } = new ();
//    public List<ZooyardInstanceOption> Instances { get; set; } = new ();
//}
public class ZooyardInstanceOption
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public Dictionary<string, string> Meta { get; set; } = new ();
}
