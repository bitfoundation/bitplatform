//+:cnd:noEmit
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiAppInsightsTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (ITelemetryContext.Current is not null)
        {
            telemetry.Context.Session.Id = ITelemetryContext.Current.AppSessionId.ToString();
            telemetry.Context.Component.Version = ITelemetryContext.Current.AppVersion;
            telemetry.Context.Device.OperatingSystem = ITelemetryContext.Current.Platform;
            telemetry.Context.User.AuthenticatedUserId = ITelemetryContext.Current.UserId?.ToString();

            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.UserSessionId)] = ITelemetryContext.Current.UserSessionId?.ToString();
            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.WebView)] = ITelemetryContext.Current.WebView;
            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.TimeZone)] = ITelemetryContext.Current.TimeZone;
            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.Culture)] = ITelemetryContext.Current.Culture;
            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.IsOnline)] = ITelemetryContext.Current.IsOnline?.ToString().ToLowerInvariant();
        }

        telemetry.Context.Session.IsFirst = VersionTracking.IsFirstLaunchEver;
        telemetry.Context.Device.OemName = DeviceInfo.Current.Manufacturer;
        telemetry.Context.Device.Model = DeviceInfo.Current.Model;
        telemetry.Context.Device.Type = DeviceInfo.Idiom.ToString();

        if (AppPlatform.IsIosOnMacOS)
        {
            telemetry.Context.GlobalProperties["IsiOSApplicationOnMac"] = "true";
        }
    }
}
