### Zooyard 是一个分布式微服务框架,提供高性能RPC远程服务调用和兼容各种现有rpc框架，集成了哈希，随机，轮询，最后调用优先作为负载均衡的算法，同时集群支持 广播，FailBack，FailFast，FailOver,FailSafe,Forking,Mergeable，用于满足业务上的各种rpc调用需求，RPC采用netty、thrift、grpc、http、akka、wcf等作为基础通信框架。

<br />

### 架构特点
本微服务架构的设计特点是不需要自己实现通信层，利用各家大厂已经开发并在产品中验证过的通信框架，如grpc,thrift,akk,wcf等，在他们基础上实现容错，负载均衡，支持 opentracing,调用的失败自动隔离,错误自动重试等，支持通信框架混用，支持部分代码是.net core,部分代码是.net framework，跨平台，让历史项目可以更加平滑的过度到微服务框架下，同时提供统一的调用接口和封装，保证业务的统一和独立。同时为了保证性能在底层实现自有的连接池，通过一套统一的机制充分利用系统资源。


### 如何安装

nuget:Install-Package Zooyard.Extensions -Version  版本号

nuget:Install-Package Zooyard.Rpc.Extensions -Version  版本号

nuget:Install-Package Zooyard.GrpcImpl.Extensions -Version  版本号

nuget:Install-Package Zooyard.ThriftImpl.Extensions -Version  版本号

nuget:Install-Package Zooyard.Rpc.AkkaImpl.Extensions -Version  版本号

nuget:Install-Package Zooyard.HttpImpl.Extensions -Version  版本号

nuget:Install-Package Zooyard.Rpc.NettyImpl.Extensions -Version  版本号

nuget:Install-Package Zooyard.Rpc.WcfImpl.Extensions -Version  版本号
