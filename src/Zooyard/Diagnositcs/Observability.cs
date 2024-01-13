using System.Diagnostics;

namespace Zooyard.Diagnositcs;

internal static class Observability
{
    public static readonly ActivitySource YarpActivitySource = new ("Zooyard");

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
