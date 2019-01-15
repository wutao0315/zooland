rem clear old packages
for %%i in (src\Zooyard.Core\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Core.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.AkkaImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.AkkaImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.GrpcImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.GrpcImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.HttpImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.HttpImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.NettyImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.NettyImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.ThriftImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.ThriftImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.WcfImpl\bin\Release\*.nupkg) do del /q/a/f/s %%i
for %%i in (src\Zooyard.Rpc.WcfImpl.Extensions\bin\Release\*.nupkg) do del /q/a/f/s %%i
rem build
dotnet build Zooland.sln -c Release
rem upload new packages
for %%i in (src\Zooyard.Core\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Core.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.AkkaImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.AkkaImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.GrpcImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.GrpcImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.HttpImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.HttpImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.NettyImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.NettyImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.ThriftImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.ThriftImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.WcfImpl\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package
for %%i in (src\Zooyard.Rpc.WcfImpl.Extensions\bin\Release\*.nupkg) do nuget push %%i -Source https://www.nuget.org/api/v2/package