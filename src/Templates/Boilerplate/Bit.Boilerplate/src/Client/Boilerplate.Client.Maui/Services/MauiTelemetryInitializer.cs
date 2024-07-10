using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiTelemetryInitializer : ITelemetryInitializer
{
    private string sessionId { get; } = Guid.NewGuid().ToString();

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Session.Id = sessionId;
        telemetry.Context.Session.IsFirst = VersionTracking.IsFirstLaunchEver;

        telemetry.Context.Device.OperatingSystem = DeviceInfo.Current.Platform.ToString();
        telemetry.Context.Device.OemName = DeviceInfo.Current.Manufacturer;
        telemetry.Context.Device.Model = DeviceInfo.Current.Model;

        telemetry.Context.Component.Version = AppInfo.Current.VersionString;

#if iOS
        telemetry.Context.GlobalProperties["IsiOSApplicationOnMac"] = Foundation.NSProcessInfo.ProcessInfo.IsiOSApplicationOnMac.ToString();
#endif
    }
}
