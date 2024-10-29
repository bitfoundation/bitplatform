using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (ITelemetryContext.Current is not null)
        {
            telemetry.Context.Session.Id = ITelemetryContext.Current.AppSessionId.ToString();
            telemetry.Context.Component.Version = ITelemetryContext.Current.AppVersion;
            telemetry.Context.Device.OperatingSystem = ITelemetryContext.Current.OS;
            telemetry.Context.User.Id = ITelemetryContext.Current.UserId?.ToString();

            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.UserSessionId)] = ITelemetryContext.Current.UserSessionId?.ToString();
            telemetry.Context.GlobalProperties[nameof(ITelemetryContext.WebView)] = ITelemetryContext.Current.WebView;
        }

        telemetry.Context.Session.IsFirst = VersionTracking.IsFirstLaunchEver;
        telemetry.Context.Device.OemName = DeviceInfo.Current.Manufacturer;
        telemetry.Context.Device.Model = DeviceInfo.Current.Model;

        if (AppPlatform.IsIosOnMacOS)
        {
            telemetry.Context.GlobalProperties["IsiOSApplicationOnMac"] = "true";
        }
    }
}
