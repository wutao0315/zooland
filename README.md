### Zooyard 是一个分布式微服务框架,提供高性能RPC远程服务调用和兼容各种现有rpc框架
#### ILoadBalance:
集成了哈希(ConsistentHash)，随机（Random），轮询（RoundRobin），最后调用优先(LeastActive)作为负载均衡的算法;
<br />
#### ICluster:
同时集群支持 广播(Broadcast)，(失败重试)FailBack，（失败直接返回）FailFast，（失败按次重试）FailOver,（失败忽略）FailSafe,（并行）Forking,（合并）Mergeable;
<br />
#### 通信协议及框架：
netty、thrift、grpc、http
<br />

### 架构特点
本微服务架构的设计特点是不需要自己实现通信层，利用各家大厂已经开发并在产品中验证过的通信框架，如grpc,thrift,http，在他们基础上实现容错，负载均衡，支持 OpenTelemetry,调用的失败自动隔离,错误自动重试等，支持通信框架混用，提供统一的调用接口和封装，保证业务的统一和独立。同时为了保证性能在底层实现自有的连接池，通过一套统一的机制充分利用系统资源。


### RPC
请求->路由->集群->负载均衡
通过下面meta数据，可以很方便的实现蓝绿和灰度发布和为流量做泳道设计

请求携带meta：token(app|user),user(id|name)
接口描述meta：serviceName,routeRule,routeContent,cluster,loadbance
实例描述meta: routeRule,routeContent,cluster,loadbance,weight

### 如何安装

nuget:Install-Package Zooyard -Version  版本号-Version

nuget:Install-Package Zooyard.GrpcNetImpl -Version  版本号

nuget:Install-Package Zooyard.ThriftImpl -Version  版本号

nuget:Install-Package Zooyard.HttpImpl -Version  版本号

nuget:Install-Package Zooyard.DotNettyImpl -Version  版本号

nuget:Install-Package Zooyard.SourceGenerator -Version  版本号

### 统一接口规范处理
例如:http统一返回 {"code":0,"msg":"", data:[]}
可以通过下面的过滤器，配合AspNetCore 的全局异常可以统一处理和捕获全局异常
```c#
/// <summary>
/// 路径过滤器
/// </summary>
public class RpcInterceptor : IInterceptor
{
    public async Task<string> UrlCall(string url, ProxyMethodResolverContext context)
    {
        //路径过滤
        await Task.CompletedTask;
        return url;
    }

    public async Task BeforeCall(IInvocation invocation, RpcContext context)
    {
        //token传递
        await Task.CompletedTask;
    }

    public async Task AfterCall<T>(IInvocation invocation, RpcContext context, IResult<T>? obj)
    {
        //统一异常处理
        if (obj != null && !obj.HasException && obj.Value is ResponseDataResult baseObj && baseObj.Code != 0)
        {
            throw new Exception(baseObj.Code, baseObj.Msg) { Trace = baseObj.Trace };
        }

        if (obj != null && !obj.HasException && obj.Value is ResponseMessage repObj && repObj.Code != 0)
        {
            throw new Exception(repObj.Code, repObj.Msg) { Trace = repObj.Trace };
        }
        await Task.CompletedTask;
    }

    public int Order => 1;

}
``` 

### 接口协议自动忽略code和msg的格式，配合过滤器设置如果code异常直接返回异常，没有异常直接返回data下的数据值；再也不用一堆的code和msg判断

```json
{"code":0,"msg":"ok", data:["test"]}
```

```C#
[ZooyardHttp("test-host", Url = "/test")]
public interface ITestService
{
    [GetMapping("getlist")]
    Task<List<string>> GetList();
}
```

### 在调用端集成
负载均衡、集群、数据合并、本地缓存、路由工厂
ILoadBalance、ICluster、IMerger、ICache、IStateRouterFactory
Zooyard.SourceGenerator 自动扫码接口协议，自动生成调用端源码

