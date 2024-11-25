using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using Thrift.Processor;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport.Client;

namespace Zooyard.ThriftImpl.Header;

public class THeaderProcessor : ITAsyncProcessor
{
    internal const string ActivityName = "Zooyard.Hosting.HttpRequestIn";
    private const string ActivityStartKey = ActivityName + ".Start";
    private const string ActivityStopKey = ActivityName + ".Stop";

    private const string DeprecatedDiagnosticsBeginRequestKey = "Zooyard.Hosting.BeginRequest";
    private const string DeprecatedDiagnosticsEndRequestKey = "Zooyard.Hosting.EndRequest";
    private const string DiagnosticsUnhandledExceptionKey = "Zooyard.Hosting.UnhandledException";



    private readonly ActivitySource _activitySource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly DistributedContextPropagator _propagator;
    private readonly ITAsyncProcessor _realProcessor;
    private readonly ILogger _logger;
    public THeaderProcessor(ILogger logger, ITAsyncProcessor realProcessor,
        DiagnosticListener diagnosticListener,
        ActivitySource activitySource,
        DistributedContextPropagator propagator)
    {
        _logger = logger;
        _realProcessor = realProcessor;
        _diagnosticListener = diagnosticListener;
        _activitySource = activitySource;
        _propagator = propagator;
    }

    public async Task<bool> ProcessAsync(TProtocol iprot, TProtocol oprot, CancellationToken cancellationToken = default)
    {
        long startTimestamp = 0;
        IDictionary<string, string>? headInfo = null;
        var methodName = "";
        var hostAddress = "";
        if (iprot.Transport is TSocketTransport socket)
        {
            hostAddress = socket.Host.MapToIPv4().ToString();
        }
        else if (iprot.Transport is TTlsSocketTransport socketTls)
        {
            hostAddress = socketTls.Host.MapToIPv4().ToString();
        }
        else if (iprot.Transport is THttpTransport http)
        {
            hostAddress = http.RequestHeaders.Host!;
        }
        //else if (iprot.Transport is TStreamTransport stream)
        //{
        //}
        //else if (iprot.Transport is TNamedPipeTransport pipe)
        //{
        //}
        //else if (iprot.Transport is TMemoryBufferTransport memory)
        //{
        //}

        if (iprot is TBinaryHeaderServerProtocol serverProtocolBinary)
        {
            serverProtocolBinary.MarkTFramedTransport(iprot);
            TMessage tMessage = await serverProtocolBinary.ReadMessageBeginAsync(cancellationToken);
            await serverProtocolBinary.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolBinary.Head;

            methodName = tMessage.Name;
            serverProtocolBinary.ResetTFramedTransport(iprot);
        } 
        else if (iprot is TCompactHeaderServerProtocol serverProtocolCompact) 
        {
            serverProtocolCompact.MarkTFramedTransport(iprot);
            TMessage tMessage = await serverProtocolCompact.ReadMessageBeginAsync(cancellationToken);
            await serverProtocolCompact.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolCompact.Head;

            methodName = tMessage.Name;
           
            serverProtocolCompact.ResetTFramedTransport(iprot);
        }
        else if (iprot is TJsonHeaderServerProtocol serverProtocolJson) 
        {
            serverProtocolJson.MarkTFramedTransport(iprot);
            TMessage tMessage = await serverProtocolJson.ReadMessageBeginAsync(cancellationToken);
            await serverProtocolJson.ReadFieldZero(cancellationToken);
            headInfo = serverProtocolJson.Head;

            methodName = tMessage.Name;

            serverProtocolJson.ResetTFramedTransport(iprot);
        }


        //string parentSpanId = headInfo.get(PARENT_SPAN_ID.getValue());
        //string isSampled = headInfo.get(IS_SAMPLED.getValue());
        //bool sampled = isSampled == null || bool.Parse(isSampled);

        //if (traceId != null && parentSpanId != null)
        //{
        //    //TraceUtils.startLocalTracer("rpc.thrift receive", traceId, parentSpanId, sampled);
        //    string methodName = tMessage.Name;
        //    //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_METHOD, methodName);
        //    TTransport transport = iprot.Transport;

        //    //string hostAddress = ((TSocket)transport).getSocket().getRemoteSocketAddress().toString();
        //    //TraceUtils.submitAdditionalAnnotation(Constants.TRACE_THRIFT_SERVER, hostAddress);
        //}

        var diagnosticListenerEnabled = _diagnosticListener.IsEnabled();
        var diagnosticListenerActivityCreationEnabled = (diagnosticListenerEnabled && _diagnosticListener.IsEnabled(ActivityName));
        var loggingEnabled = _logger.IsEnabled(LogLevel.Critical);
        Activity? activity = null;
        bool hasDiagnosticListener = false;
        if (headInfo != null && (loggingEnabled || diagnosticListenerActivityCreationEnabled || _activitySource.HasListeners()))
        {
            activity = StartActivity(headInfo, loggingEnabled, diagnosticListenerActivityCreationEnabled, out hasDiagnosticListener);
            Activity.Current = activity;
        }

        if (headInfo != null && diagnosticListenerEnabled)
        {
            if (_diagnosticListener.IsEnabled(DeprecatedDiagnosticsBeginRequestKey))
            {
                if (startTimestamp == 0)
                {
                    startTimestamp = Stopwatch.GetTimestamp();
                }

                RecordBeginRequestDiagnostics(headInfo, startTimestamp);
            }
        }

        bool result = await _realProcessor.ProcessAsync(iprot, oprot, cancellationToken);

        if (activity is not null) 
        {
            StopActivity(activity, hasDiagnosticListener);
            //TraceUtils.endAndSendLocalTracer();
        }
        
        return result;
    }

    private Activity? StartActivity(IDictionary<string, string> headers, bool loggingEnabled, bool diagnosticListenerActivityCreationEnabled, out bool hasDiagnosticListener)
    {
        hasDiagnosticListener = false;

        var activity = ActivityCreator.CreateFromRemote(
            _activitySource,
            _propagator,
            headers,
            static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = default;
                var headers = (IHeaderDictionary)carrier!;
                fieldValue = headers[fieldName];
            },
            ActivityName,
            ActivityKind.Server,
            tags: null,
            links: null,
            loggingEnabled || diagnosticListenerActivityCreationEnabled);
        if (activity is null)
        {
            return null;
        }

        _diagnosticListener.OnActivityImport(activity, headers);

        if (_diagnosticListener.IsEnabled(ActivityStartKey))
        {
            hasDiagnosticListener = true;
            activity.Start();
            WriteDiagnosticEvent(_diagnosticListener, ActivityStartKey, headers);
        }
        else
        {
            activity.Start();
        }

        return activity;
    }

    private void StopActivity(Activity activity, bool hasDiagnosticListener)
    {
        if (hasDiagnosticListener)
        {
            // Stop sets the end time if it was unset, but we want it set before we issue the write
            // so we do it now.
            if (activity.Duration == TimeSpan.Zero)
            {
                activity.SetEndTime(DateTime.UtcNow);
            }
            activity.Stop();    // Resets Activity.Current (we want this after the Write)
        }
        else
        {
            activity.Stop();
        }
    }

    private static void WriteDiagnosticEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(
        DiagnosticSource diagnosticSource, string name, TValue value)
    {
        diagnosticSource.Write(name, value);
    }
    private void RecordBeginRequestDiagnostics(IDictionary<string, string> header, long startTimestamp)
    {
        WriteDiagnosticEvent(
            _diagnosticListener,
            DeprecatedDiagnosticsBeginRequestKey,
            new DeprecatedProcessData(header, startTimestamp));
    }

}

sealed class DeprecatedProcessData
{
    internal DeprecatedProcessData(IDictionary<string, string> header, long timestamp)
    {
        this.header = header;
        this.timestamp = timestamp;
    }

    // Compatibility with anonymous object property names
    public IDictionary<string,string> header { get; }
    public long timestamp { get; }

    public override string ToString() => $"{{ {nameof(header)} = {header}, {nameof(timestamp)} = {timestamp} }}";
}
internal static class ActivityCreator
{
    /// <summary>
    /// Create an activity with details received from a remote source.
    /// </summary>
    public static Activity? CreateFromRemote(
        ActivitySource activitySource,
        DistributedContextPropagator propagator,
        object distributedContextCarrier,
        DistributedContextPropagator.PropagatorGetterCallback propagatorGetter,
        string activityName,
        ActivityKind kind,
        IEnumerable<KeyValuePair<string, object?>>? tags,
        IEnumerable<ActivityLink>? links,
        bool diagnosticsOrLoggingEnabled)
    {
        Activity? activity = null;
        string? requestId = null;
        string? traceState = null;

        if (activitySource.HasListeners())
        {
            propagator.ExtractTraceIdAndState(
                distributedContextCarrier,
                propagatorGetter,
                out requestId,
                out traceState);

            if (ActivityContext.TryParse(requestId, traceState, isRemote: true, out ActivityContext context))
            {
                // The requestId used the W3C ID format. Unfortunately, the ActivitySource.CreateActivity overload that
                // takes a string parentId never sets HasRemoteParent to true. We work around that by calling the
                // ActivityContext overload instead which sets HasRemoteParent to parentContext.IsRemote.
                // https://github.com/dotnet/aspnetcore/pull/41568#discussion_r868733305
                activity = activitySource.CreateActivity(activityName, kind, context, tags: tags, links: links);
            }
            else
            {
                // Pass in the ID we got from the headers if there was one.
                activity = activitySource.CreateActivity(activityName, kind, string.IsNullOrEmpty(requestId) ? null : requestId, tags: tags, links: links);
            }
        }

        if (activity is null)
        {
            // CreateActivity didn't create an Activity (this is an optimization for the
            // case when there are no listeners). Let's create it here if needed.
            if (diagnosticsOrLoggingEnabled)
            {
                // Note that there is a very small chance that propagator has already been called.
                // Requires that the activity source had listened, but it didn't create an activity.
                // Can only happen if there is a race between HasListeners and CreateActivity calls,
                // and someone removing the listener.
                //
                // The only negative of calling the propagator twice is a small performance hit.
                // It's small and unlikely so it's not worth trying to optimize.
                propagator.ExtractTraceIdAndState(
                    distributedContextCarrier,
                    propagatorGetter,
                    out requestId,
                    out traceState);

                activity = new Activity(activityName);
                if (!string.IsNullOrEmpty(requestId))
                {
                    activity.SetParentId(requestId);
                }
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity.AddTag(tag.Key, tag.Value);
                    }
                }
                if (links != null)
                {
                    foreach (var link in links)
                    {
                        activity.Links.Append(link);
                        //activity.AddLink(link);
                    }
                }
            }
            else
            {
                return null;
            }
        }

        // The trace id was successfully extracted, so we can set the trace state
        // https://www.w3.org/TR/trace-context/#tracestate-header
        if (!string.IsNullOrEmpty(requestId))
        {
            if (!string.IsNullOrEmpty(traceState))
            {
                activity.TraceStateString = traceState;
            }
        }

        // Baggage can be used regardless of whether a distributed trace id was present on the inbound request.
        // https://www.w3.org/TR/baggage/#abstract
        var baggage = propagator.ExtractBaggage(distributedContextCarrier, propagatorGetter);

        // AddBaggage adds items at the beginning  of the list, so we need to add them in reverse to keep the same order as the client
        // By contract, the propagator has already reversed the order of items so we need not reverse it again
        // Order could be important if baggage has two items with the same key (that is allowed by the contract)
        if (baggage is not null)
        {
            foreach (var baggageItem in baggage)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }
        }

        return activity;
    }
}
