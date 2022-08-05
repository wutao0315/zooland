using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zooyard.Rpc.Route.Tag.Model;

public class Tag
{
    public static Tag parseFromMap(Dictionary<string, object> map)
    {
        Tag tag = new Tag();
        if (map.TryGetValue("name", out object? nameObj))
        {
            tag.Name = nameObj.ToString();
        }

        if (map.TryGetValue("addresses", out object? addressesObj) && addressesObj is List<string> addresses)
        {
            tag.Addresses = addresses;
        }
        return tag;
    }

    //if (addresses != null && List.class.isAssignableFrom(addresses.getClass())) 
    //{
    //    //tag.Addresses = ((List<Object>) addresses).stream().map(String::valueOf).collect(Collectors.toList()));
    //    return tag;
    //}

    public string Name { get; set; } = string.Empty;

    public List<String> Addresses { get; set; } = new();
}
