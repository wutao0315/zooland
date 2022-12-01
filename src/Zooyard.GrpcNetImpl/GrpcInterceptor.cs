using Grpc.Core;
using Grpc.Core.Interceptors;
using Zooyard.Rpc;

namespace Zooyard.GrpcNetImpl;

public abstract class ClientInterceptor : Interceptor
{
}

public class ClientGrpcHeaderInterceptor : ClientInterceptor
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata();
        var attachments = RpcContext.GetContext().Attachments;
        foreach (var item in attachments)
        {
            metadata.Add(item.Key, item.Value);
        } 
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);
        return response;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata();
        var attachments = RpcContext.GetContext().Attachments;
        foreach (var item in attachments)
        {
            metadata.Add(item.Key, item.Value);
        }
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);

        var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
        return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

    }
}