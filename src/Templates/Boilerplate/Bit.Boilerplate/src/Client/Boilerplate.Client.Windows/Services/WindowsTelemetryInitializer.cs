using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Windows.Services;
public partial class WindowsTelemetryInitializer : ITelemetryInitializer
{
    public static string? AuthenticatedUserId { get; set; }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.User.AuthenticatedUserId = AuthenticatedUserId;
    }
}
