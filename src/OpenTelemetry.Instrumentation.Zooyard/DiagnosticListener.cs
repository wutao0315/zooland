using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System.Collections.Concurrent;
using System.Diagnostics;
using Zooyard.Diagnositcs;

namespace OpenTelemetry.Instrumentation.Zooyard;

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
            case Constant.ConsumerBefore:
                {
                    var eventData = (EventDataStore)evt.Value!;

                    var parentContext = Propagator.Extract(default, eventData.Invocation, (msg, key) =>
                    {
                        if (msg.GetAttributes().TryGetValue(key, out object? value))
                        {
                            return new[] { value.ToString() };
                        }
                        return Enumerable.Empty<string>();
                    });

                    var activity = ActivitySource.StartActivity(eventData.ClusterName,
                        ActivityKind.Consumer,
                        parentContext.ActivityContext);

                    if (activity != null)
                    {
                        activity.SetTag("rpc.system", eventData.Url);
                        activity.SetTag("rpc.url", eventData.Url);
                        activity.SetTag("rpc.cluster.name", eventData.ClusterName);
                        activity.SetTag("rpc.id", eventData.Invocation.Id);

                        activity.AddEvent(new ActivityEvent("rpc message persistence start...",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.ActiveTimestamp)));

                        if (parentContext != default)
                        {
                            _contexts[eventData.Invocation.Id] = Activity.Current!.Context;
                        }

                        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), eventData.Invocation,
                            (msg, key, value) =>
                            {
                                msg.SetAttachment(key, value);
                            });
                    }
                }
                break;
            case Constant.ConsumerAfter:
                {
                    var eventData = (EventDataStore)evt.Value!;
                    if (Activity.Current is { } activity)
                    {
                        activity.AddEvent(new ActivityEvent("rpc message persistence succeeded!",
                            DateTimeOffset.FromUnixTimeMilliseconds(eventData.ActiveTimestamp),
                            new ActivityTagsCollection { new("rpc.client.duration", eventData.Elapsed) }));

                        activity.Stop();
                    }
                }
                break;
            case Constant.ConsumerError:
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