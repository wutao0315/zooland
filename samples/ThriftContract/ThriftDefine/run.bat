
echo 生成csharp文件到项目ThriftImpls项目中的TService目录下，编译该项目即可；

thrift-0.14.2.exe --gen netstd -o ../ shared.thrift
thrift-0.14.2.exe --gen netstd -o ../ tutorial.thrift

echo 生成完成
pause