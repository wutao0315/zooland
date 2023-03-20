namespace Zooyard.Rpc.Route.Tag.Model;

public record Tag
{
    public string Name { get; set; } = string.Empty;

    public List<string> Addresses { get; set; } = new();

    public static Tag ParseFromMap(Dictionary<string, object> map)
    {
        var tag = new Tag();
        if (map.TryGetValue("name", out object? nameObj))
        {
            tag.Name = nameObj.ToString()!;
        }

        if (map.TryGetValue("addresses", out object? addressesObj) && addressesObj is List<string> addresses)
        {
            tag.Addresses = addresses;
        }
        return tag;
    }
}
