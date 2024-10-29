using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Windows.Services;

public partial class WindowsTelemetryInitializer : ITelemetryInitializer
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
    }
}
