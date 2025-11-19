//+:cnd:noEmit
using OpenTelemetry;

namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// OpenTelemetry processor that enriches telemetry data with MAUI-specific context.
/// </summary>
public partial class MauiTelemetryEnrichmentProcessor : BaseProcessor<Activity>
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

        if (ITelemetryContext.Current is not null)
        {
            // Set standard OpenTelemetry semantic convention attributes
            activity.SetTag("app.session.id", ITelemetryContext.Current.AppSessionId);
            activity.SetTag("app.version", ITelemetryContext.Current.AppVersion);
            activity.SetTag("os.description", ITelemetryContext.Current.Platform);

            activity.SetTag("enduser.id", ITelemetryContext.Current.UserId);

            // Add custom context properties
            activity.SetTag("user.session.id", ITelemetryContext.Current.UserSessionId);

            activity.SetTag("webview.version", ITelemetryContext.Current.WebView);

            activity.SetTag("user.timezone", ITelemetryContext.Current.TimeZone);

            activity.SetTag("user.culture", ITelemetryContext.Current.Culture);

            activity.SetTag("network.online", ITelemetryContext.Current.IsOnline);
        }

        activity.SetTag("device.first.launch", VersionTracking.IsFirstLaunchEver);
        activity.SetTag("device.manufacturer", DeviceInfo.Current.Manufacturer);
        activity.SetTag("device.model", DeviceInfo.Current.Model);
        activity.SetTag("device.type", DeviceInfo.Idiom);

        if (AppPlatform.IsIosOnMacOS)
        {
            activity.SetTag("device.ios.on.mac", "true");
        }

        base.OnStart(activity);
    }
}
