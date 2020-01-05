using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zooyard.Rpc.GrpcImpl
{
    public abstract class ServerInterceptor: Interceptor
    {
    }
    public abstract class ClientInterceptor : Interceptor
    {
    }
}
