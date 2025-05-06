using System.Text;

namespace Zooyard;

public record ZooyardOption
{
    public string Protocol { get; set; } = "http";
    public IReadOnlyDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    //public List<string> Mergers { get; set; } = new();
    public Dictionary<string, ZooyardServiceOption> Services { get; set; } = [];
}

public record ZooyardServiceOption
{
    public Dictionary<string, string> Meta { get; set; } = [];
    public List<ZooyardInstanceOption> Instances { get; set; } = [];
}
public record ZooyardInstanceOption
{
    public string Host { get; set; } = String.Empty;
    public int Port { get; set; }
    public Dictionary<string, string> Meta { get; set; } = [];
}
