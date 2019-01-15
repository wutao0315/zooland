rem build
dotnet build Zooland.sln -c Release
cd %cd%\nuget
rem clear old nuget packages
for %%i in (*.nupkg) do del /q/a/f/s %%i
rem create nuget packages
nuget pack Zooyard.Core.nuspec
nuget pack Zooyard.Core.Extensions.nuspec
nuget pack Zooyard.Rpc.nuspec
nuget pack Zooyard.Rpc.Extensions.nuspec
nuget pack Zooyard.Rpc.AkkaImpl.nuspec
nuget pack Zooyard.Rpc.AkkaImpl.Extensions.nuspec
nuget pack Zooyard.Rpc.GrpcImpl.nuspec
nuget pack Zooyard.Rpc.GrpcImpl.Extensions.nuspec
nuget pack Zooyard.Rpc.HttpImpl.nuspec
nuget pack Zooyard.Rpc.HttpImpl.Extensions.nuspec
nuget pack Zooyard.Rpc.NettyImpl.nuspec
nuget pack Zooyard.Rpc.NettyImpl.Extensions.nuspec
nuget pack Zooyard.Rpc.ThriftImpl.nuspec
nuget pack Zooyard.Rpc.ThriftImpl.Extensions.nuspec
nuget pack Zooyard.Rpc.WcfImpl.nuspec
nuget pack Zooyard.Rpc.WcfImpl.Extensions.nuspec
cd ..