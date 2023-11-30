using System.Diagnostics;

namespace Zooyard.Diagnositcs;

internal static class Observability
{
    public static readonly ActivitySource YarpActivitySource = new ActivitySource("Zooyard");

    //public static Activity? GetYardActivity(this HttpContext context)
    //{
    //    return context.Features[typeof(YarpActivity)] as Activity;
    //}

    //public static void SetYarpActivity(this HttpContext context, Activity? activity)
    //{
    //    if (activity is not null)
    //    {
    //        context.Features[typeof(YarpActivity)] = activity;
    //    }
    //}

    public static void AddError(this Activity activity, string message, string description)
    {
        if (activity is not null)
        {
            var tagsCollection = new ActivityTagsCollection
        {
            { "error", message },
            { "description", description }
        };

            activity.AddEvent(new ActivityEvent("Error", default, tagsCollection));
        }
    }

    private class YarpActivity
    {
    }
}
