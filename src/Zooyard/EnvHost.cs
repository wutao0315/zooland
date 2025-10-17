using System.Diagnostics;

namespace Zooyard;

public static class EnvHost
{
    public static ActivitySource CreateActivitySource<T>()
    {
        var tp = typeof(T);
        var scopeName = tp.FullName ?? "Zooyard.EnvHost";
        var version = tp.Assembly.GetName().Version?.ToString();
        var activitySource = CreateActivitySource(scopeName, version);
        return activitySource;
    }

    public static ActivitySource CreateActivitySource(string scopeName, string? version = "")
    {
        var activitySource = new ActivitySource(scopeName, version);
        return activitySource;
    }

    public static Activity? StartActivity(this ActivitySource activitySource, string spanName, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = activitySource.StartActivity(spanName, kind);
        return activity;
    }

    public static ActivitySource CreateActivitySource(this Type type)
    {
        var scopeName = type.FullName ?? "Zooyard.EnvHost";
        var version = type.Assembly.GetName().Version?.ToString();
        var result = new ActivitySource(scopeName, version);
        return result;
    }
}
