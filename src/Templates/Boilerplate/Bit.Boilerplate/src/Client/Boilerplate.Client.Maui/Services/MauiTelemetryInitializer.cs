using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Boilerplate.Client.Maui.Services;

public partial class MauiTelemetryInitializer : ITelemetryInitializer
{
    public static string? AuthenticatedUserId { get; set; }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.User.AuthenticatedUserId = AuthenticatedUserId;
    }
}
