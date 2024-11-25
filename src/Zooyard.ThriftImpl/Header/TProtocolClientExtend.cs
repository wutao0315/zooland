using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Thrift.Protocol;

namespace Zooyard.ThriftImpl.Header;

public static class TProtocolClientExtend
{
    public const string TraceParentHeaderName = "traceparent";
    public const string TraceStateHeaderName = "tracestate";
    public const string RequestIdHeaderName = "Request-Id";
    public const string CorrelationContextHeaderName = "Correlation-Context";

    public static void InjectHeaders(this TProtocol protocol, Activity currentActivity, IDictionary<string, string> headers)
    {
        if (currentActivity.IdFormat == ActivityIdFormat.W3C)
        {
            if (!headers.TryGetValue(TraceParentHeaderName, out var traceparent) && !string.IsNullOrWhiteSpace(currentActivity.Id))
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
            if (!headers.TryGetValue(RequestIdHeaderName, out var traceparent) && !string.IsNullOrWhiteSpace(currentActivity.Id))
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
