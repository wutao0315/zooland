using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;
using System.Diagnostics;
using Zooyard.Diagnositcs;

namespace Zooyard.Instrumentation.OpenTelemetry;

internal class DiagnosticListener : IObserver<KeyValuePair<string, object?>>
{
    public const string SourceName = "Zooyard.OpenTelemetry";
    private static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
    private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

    private readonly ConcurrentDictionary<string, ActivityContext> _contexts = new();

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> evt)
    {
        switch (evt.Key)
        {
            case Diagnositcs.Constant.ConsumerBefore:
                {
                    var eventData = (EventDataStore)evt.Value!;

                    var parentContext = Propagator.Extract(default, eventData.TransportMessage, (msg, key) =>
                    {
                        //if (msg.Headers.TryGetValue(key, out string? value))
                        //{
                        //    return new[] { value };
                        //}
                        return Enumerable.Empty<string>();
                    });

                    var activity = ActivitySource.StartActivity(eventData.Operation,
                        ActivityKind.Consumer,
                        parentContext.ActivityContext);

                    if (activity != null)
                    {
                        activity.SetTag("rpc.destination", eventData.Operation);

                        activity.AddEvent(new ActivityEvent("rpc message persistence start...",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value)));

                        //_contexts[eventData.TransportMessage.GetId() + eventData.TransportMessage.GetGroup()] = activity.Context;
                    }
                }
                break;
            case Diagnositcs.Constant.ConsumerAfter:
                {
                    var eventData = (EventDataStore)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        activity.AddEvent(new ActivityEvent("rpc message persistence succeeded!",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.OperationTimestamp!.Value),
                            new ActivityTagsCollection { new("prc.duration", eventData.ElapsedTimeMs) }));

                        activity.Stop();
                    }
                }
                break;
            case Diagnositcs.Constant.ConsumerError:
                {
                    var eventData = (EventDataStore)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        var exception = eventData.Exception!;
                        activity.SetStatus(Status.Error.WithDescription(exception.Message));
                        activity.RecordException(exception);
                        activity.Stop();
                    }
                }
                break;
        }
    }
}