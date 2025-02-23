using Zooyard.Rpc;

namespace Zooyard.Attributes;

public abstract class AbstractAttribute : Attribute
{
    /// <summary>
    /// 指定request中必须包含某些参数值，才让该方法处理。
    /// </summary>
    public string Params { get; init; } = string.Empty;
    /// <summary>
    /// 获取Params
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetParams()
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(Params))
        {
            return result;
        }

        var headerList = Params.Split(['&', ','], StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in headerList)
        {
            var kvList = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (kvList.Length <= 1)
            {
                continue;
            }
            result[kvList[0]] = kvList[1];
        }

        return result;
    }
    /// <summary>
    /// 指定request中必须包含某些指定的header值，才让该方法处理请求
    /// </summary>
    public string Headers { get; init; } = string.Empty;
    /// <summary>
    /// 获取Headers
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetHeaders()
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(Headers))
        {
            return result;
        }

        var headerList = Headers.Split(['&', ','], StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in headerList)
        {
            var kvList = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (kvList.Length <= 1)
            {
                continue;
            }
            result[kvList[0]] = kvList[1];
        }

        return result;
    }
    /// <summary>
    /// 集群
    /// </summary>
    public string Cluster { get; init; } = string.Empty;
    /// <summary>
    /// 负载均衡
    /// </summary>
    public string Loadbance { get; init; } = string.Empty;
    /// <summary>
    /// 缓存
    /// </summary>
    public string Cache { get; init; } = string.Empty;
    /// <summary>
    /// 路由
    /// </summary>
    public string Route { get; init; } = string.Empty;
    /// <summary>
    /// 摘要配置
    /// </summary>
    public string Metadatas { get; init; } = string.Empty;
    /// <summary>
    /// 获取Headers
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, string> GetMetadatas()
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(Metadatas))
        {
            return result;
        }

        var headerList = Metadatas.Split(['&', ','], StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in headerList)
        {
            var kvList = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (kvList.Length <= 1)
            {
                continue;
            }
            result[kvList[0]] = kvList[1];
        }

        if (!string.IsNullOrWhiteSpace(Cluster))
        {
            result[ZooyardPools.CLUSTER_KEY] = Cluster;
        }
        if (!string.IsNullOrWhiteSpace(Cluster))
        {
            result[ZooyardPools.LOADBANCE_KEY] = Loadbance;
        }
        if (!string.IsNullOrWhiteSpace(Cache))
        {
            result[ZooyardPools.CACHE_KEY] = Cache;
        }
        if (!string.IsNullOrWhiteSpace(Route))
        {
            result[ZooyardPools.ROUTE_KEY] = Route;
        }
        return result;
    }
}
