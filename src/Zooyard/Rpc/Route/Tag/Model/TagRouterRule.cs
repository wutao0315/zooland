namespace Zooyard.Rpc.Route.Tag.Model;

public class TagRouterRule: AbstractRouterRule
{
    private List<Tag> tags;
    private readonly Dictionary<string, List<string>> addressToTagnames = new();
    private readonly Dictionary<string, List<string>> tagnameToAddresses = new();

    public static TagRouterRule parseFromMap(Dictionary<string, object> map)
    {
        var tagRouterRule = new TagRouterRule();
        tagRouterRule.ParseFromMapInner(map);

        if (map.TryGetValue(Constants.TAGS_KEY, out object? tags) && tags != null)
        {
            //tagRouterRule.Tags
        }
        //if (tags != null && List.class.isAssignableFrom(tags.getClass())) 
        //{
        //    tagRouterRule.setTags(((List<Map<String, Object>>) tags).stream()
        //            .map(Tag::parseFromMap).collect(Collectors.toList()));
        //}

        return tagRouterRule;
    }

    public void init()
    {
        if (!Valid)
        {
            return;
        }
        //tags.stream().filter(tag->CollectionUtils.isNotEmpty(tag.getAddresses())).forEach(tag-> {
        //    tagnameToAddresses.put(tag.getName(), tag.getAddresses());
        //    tag.getAddresses().forEach(addr-> {
        //        List<String> tagNames = addressToTagnames.computeIfAbsent(addr, k-> new ArrayList<>());
        //        tagNames.add(tag.getName());
        //    });
        //    });
        //}
    }

    public List<string> Addresses
    {
        get => null;
        //return tags.stream()
        //        .filter(tag->CollectionUtils.isNotEmpty(tag.getAddresses()))
        //        .flatMap(tag->tag.getAddresses().stream())
        //        .collect(Collectors.toList());
    }

    public List<string> getTagNames()
    {
        return null;
        //return tags.stream().map(Tag::getName).collect(Collectors.toList());
    }

    public Dictionary<string, List<string>> getAddressToTagnames()
    {
        return addressToTagnames;
    }


    public Dictionary<string, List<string>> getTagnameToAddresses()
    {
        return tagnameToAddresses;
    }

    public List<Tag> Tags { get; set; }
}
