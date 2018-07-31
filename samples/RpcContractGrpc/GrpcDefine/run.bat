
echo 生成csharp文件到项目GrpcImpls项目中，编译该项目即可；

protoc.exe -I ./ --csharp_out ../gen-csharp  helloService.proto --grpc_out ../gen-csharp --plugin=protoc-gen-grpc=grpc_csharp_plugin.exe

echo 生成完成
pause

