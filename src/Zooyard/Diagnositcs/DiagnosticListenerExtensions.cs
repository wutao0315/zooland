using System.Diagnostics;
using System.Xml.Linq;

namespace Zooyard.Diagnositcs;

public class Constant
{
    public const string DiagnosticListenerName = "ZooyardDiagnosticListener";

    public const string ZooyardPrefix = "Zooyard.Rpc.";

    public const string ConsumerBefore = ZooyardPrefix + nameof(ConsumerBefore);
    public const string ConsumerAfter = ZooyardPrefix + nameof(ConsumerAfter);
    public const string ConsumerError = ZooyardPrefix + nameof(ConsumerError);

    public const string ProviderBefore = ZooyardPrefix + nameof(ProviderBefore);
    public const string ProviderAfter = ZooyardPrefix + nameof(ProviderAfter);
    public const string ProviderError = ZooyardPrefix + nameof(ProviderError);
}
public static class DiagnosticListenerExtensions
{
    public static void WriteConsumerBefore(this DiagnosticSource _this, string system, string clusterName, URL url, IInvocation invocation)
    {
        if (!_this.IsEnabled(Constant.ConsumerBefore))
        {
            return;
        }
        var eventData = new EventDataStore(system, clusterName, url, invocation);
        _this.Write(Constant.ConsumerBefore, eventData);
    }
    public static void WriteConsumerAfter<T>(this DiagnosticSource _this, string system, string clusterName, URL url, IInvocation invocation, IResult<T> result)
    {
        if (!_this.IsEnabled(Constant.ConsumerAfter))
        {
            return;
        }
        var eventData = new EventDataStore(system, clusterName, url, invocation) { Elapsed = result.ElapsedMilliseconds, Result = result };
        _this.Write(Constant.ConsumerAfter, eventData);
    }
    public static void WriteConsumerError(this DiagnosticSource _this, string system, string clusterName, URL url, IInvocation invocation, Exception exception, long elapsed)
    {
        if (!_this.IsEnabled(Constant.ConsumerAfter))
        {
            return;
        }
        var eventData = new EventDataStore(system, clusterName, url, invocation) { Elapsed = elapsed,  Exception = exception };
        _this.Write(Constant.ConsumerAfter, eventData);
    }
}
