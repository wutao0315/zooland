using System.Collections.Generic;
using System.Reflection;

namespace Zooyard.Rpc.Route.Tag.Model;

public record TagRouterRule: AbstractRouterRule
{
    private readonly Dictionary<string, List<string>> addressToTagnames = new();
    private readonly Dictionary<string, List<string>> tagnameToAddresses = new();

    public static TagRouterRule ParseFromMap(Dictionary<string, object> map)
    {
        var tagRouterRule = new TagRouterRule();
        tagRouterRule.ParseFromMapInner(map);

        if (map.TryGetValue(Constants.TAGS_KEY, out object? tags) 
            && tags != null
            && typeof(List<Dictionary<string, object>>).IsAssignableFrom(tags.GetType()))
        {
            var tagList = (List<Dictionary<string, object>>)tags;
            var tagResult = new List<Tag>();
            foreach (var tag in tagList)
            {
                var item = Tag.ParseFromMap(tag);
                tagResult.Add(item);
            }
            tagRouterRule.Tags = tagResult;
        }

        return tagRouterRule;
    }

    public void Init()
    {
        if (!Valid)
        {
            return;
        }
        foreach (var tag in Tags.Where(w => w.Addresses.Count > 0))
        {
            tagnameToAddresses.Add(tag.Name, tag.Addresses);
            foreach (var addr in tag.Addresses)
            {
                addressToTagnames.TryGetValue(addr, out var tagNames);
                tagNames ??= new List<string>();
                tagNames.Add(tag.Name);
                addressToTagnames[addr] = tagNames;
            }
        }
    }

    public List<string> Addresses
    {

        get
        {
            var result = new List<string>();
            foreach (var addresses in Tags.Where(w => w.Addresses.Count > 0).Select(w => w.Addresses))
            {
                foreach (var addr in addresses) 
                {
                    result.Add(addr);
                }
            }
            return result;
        }
    }

    public List<string> GetTagNames()
    {
        return Tags.Select(w => w.Name).ToList();
    }

    public Dictionary<string, List<string>> AddressToTagnames=> addressToTagnames;

    public Dictionary<string, List<string>> TagnameToAddresses => tagnameToAddresses;

    public List<Tag> Tags { get; set; } = new();
}
