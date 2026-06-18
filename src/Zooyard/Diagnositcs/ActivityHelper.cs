using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace Zooyard.Diagnositcs;

internal class ActivityHelper
{
    internal static readonly ActivitySource ActivitySource = new("Zooyard.Rpc");
    internal const string RequestIdHeaderName = "Request-Id";
    internal const string TraceParentHeaderName = "traceparent";
    internal const string TraceStateHeaderName = "tracestate";
    internal const string CorrelationContextHeaderName = "Correlation-Context";

    public static Activity? Start(string activityName, IDictionary<string, string?>? headers, ActivityKind kind)
    {
        var activity = StartActivity(activityName, headers, kind);
        return activity;
    }
    private static Activity? StartActivity(string activityName, IDictionary<string, string?>? headers, ActivityKind kind)
    {
        string? parentId = null;
        ActivityContext? activityContext = null;
        string? traceStateString = null;

        if (headers?.Count > 0) 
        {
            headers.TryGetValue(TraceStateHeaderName, out traceStateString);

            if (headers.TryGetValue(TraceParentHeaderName, out var traceParentHeaderName)
                && !string.IsNullOrWhiteSpace(traceParentHeaderName))
            {
                activityContext = ActivityContext.Parse(traceParentHeaderName, traceStateString);
            }

            if (headers.TryGetValue(RequestIdHeaderName, out var requestId))
            {
                parentId = requestId!;
            }
        }
       
        var activity = activityContext == null ? ActivitySource.StartActivity(activityName, kind, parentId) : ActivitySource.StartActivity(activityName, kind, activityContext.Value);
        return activity;
    }

    public static void InjectHeaders(Activity? currentActivity, IDictionary<string, string?> headers)
    {
        if (currentActivity == null) 
        {
            return;
        }
        if (currentActivity.IdFormat == ActivityIdFormat.W3C)
        {
            if (!headers.ContainsKey(TraceParentHeaderName))
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
            if (!headers.ContainsKey(RequestIdHeaderName))
            {
                headers.Add(RequestIdHeaderName, currentActivity.Id);
            }
        }

        // we expect baggage to be empty or contain a few items
        using (IEnumerator<KeyValuePair<string, string?>> e = currentActivity.Baggage.GetEnumerator())
        {
            if (e.MoveNext())
            {
                var baggage = new List<string>();
                do
                {
                    KeyValuePair<string, string?> item = e.Current;
                    baggage.Add(new NameValueHeaderValue(WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)).ToString());
                }
                while (e.MoveNext());
                headers.Add(CorrelationContextHeaderName, string.Join(',', baggage));
            }
        }
    }
}
