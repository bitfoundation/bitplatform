using OpenTelemetry;

namespace Boilerplate.Server.Shared.Services;

public class AppOpenTelemetryProcessor : BaseProcessor<Activity>
{
    public override void OnStart(Activity activity)
    {
        if (activity.DisplayName.Contains("Microsoft.AspNetCore.Components.Server.ComponentHub"))
        {
            activity.IsAllDataRequested = false; // Prevents Blazor Server's SignalR from being exported
        }
        else if (activity.OperationName is "Microsoft.AspNetCore.Components.HandleEvent")
        {
            activity.IsAllDataRequested = false; // Prevents Blazor's events from being exported.
        }
    }
}
