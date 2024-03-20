using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Zooyard.Rpc;

namespace Zooyard.GrpcNetImpl;

public abstract class ClientInterceptor : Interceptor
{
}


public class ClientGrpcHeaderInterceptor : ClientInterceptor
{
    public const string DiagnosticListenerName = "GrpcHandlerDiagnosticListener";
    public const string ActivityName = "Zooyard.GrpcNetImpl.RequestOut";
    public const string TraceParentHeaderName = "traceparent";
    public const string TraceStateHeaderName = "tracestate";
    public const string RequestIdHeaderName = "Request-Id";
    public const string CorrelationContextHeaderName = "Correlation-Context";
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata();
        var attachments = RpcContext.GetContext().Attachments;
        foreach (var item in attachments)
        {
            metadata.Add(item.Key, item.Value.ToString()!);
        }

        Activity? activity = Activity.Current;

        if (activity == null)
        {
            activity = new Activity(ActivityName);
            activity.Start();
            InjectHeaders(activity, metadata);
            var options = context.Options.WithHeaders(metadata);
            try
            {
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
                var response = continuation(request, context);
                return response;
            }
            finally
            {
                activity.Stop();
            }
        }
        else 
        {
            InjectHeaders(activity, metadata);
            var options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            var response = continuation(request, context);
            return response;
        }
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = new Metadata();
        var attachments = RpcContext.GetContext().Attachments;
        foreach (var item in attachments)
        {
            metadata.Add(item.Key, item.Value.ToString()!);
        }
        var options = context.Options.WithHeaders(metadata);
        context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        var response = continuation(request, context);

        var responseAsync = response.ResponseAsync.ContinueWith<TResponse>((r) => r.Result);
        return new AsyncUnaryCall<TResponse>(responseAsync, response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);

    }

    private void InjectHeaders(Activity currentActivity, Metadata headers)
    {
        if (currentActivity.IdFormat == ActivityIdFormat.W3C)
        {
            if (headers.Get(TraceParentHeaderName) == null)
            {
                headers.Add(TraceParentHeaderName, currentActivity.Id);
                if (currentActivity.TraceStateString != null)
                {
                    headers.Add(TraceStateHeaderName, currentActivity.TraceStateString);
                }
            }
        }
        else
        {
            if (headers.Get(RequestIdHeaderName) == null)
            {
                headers.Add(RequestIdHeaderName, currentActivity.Id);
            }
        }

        // we expect baggage to be empty or contain a few items
        using (IEnumerator<KeyValuePair<string, string>> e = currentActivity.Baggage.GetEnumerator())
        {
            if (e.MoveNext())
            {
                var baggage = new List<string>();
                do
                {
                    KeyValuePair<string, string> item = e.Current;
                    baggage.Add(new NameValueHeaderValue(WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)).ToString());
                }
                while (e.MoveNext());
                headers.Add(CorrelationContextHeaderName, string.Join(',',baggage));
            }
        }
    }
}