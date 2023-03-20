using System.Text;

namespace Zooyard;

public record ZooyardOption
{
    public string Address { get; set; } = String.Empty;
    public Dictionary<string, string> Meta { get; set; } = new();
    public List<string> Mergers { get; set; } = new();
    public Dictionary<string, ZooyardServiceOption> Services { get; set; } = new();
}

public record ZooyardServiceOption
{
    public Dictionary<string, string> Meta { get; set; } = new();
    public List<ZooyardInstanceOption> Instances { get; set; } = new();
}
public record ZooyardInstanceOption
{
    public string Host { get; set; } = String.Empty;
    public int Port { get; set; }
    public Dictionary<string, string> Meta { get; set; } = new ();
}
