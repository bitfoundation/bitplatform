using System.Diagnostics.Metrics;

namespace Boilerplate.Shared.Services;

/// <summary>
/// Open telemetry activity source for the application.
/// </summary>
public class AppActivitySource
{
    public static ActivitySource CurrentActivity = new("Boilerplate", typeof(AppActivitySource).Assembly.GetName().Version!.ToString());

    public static Meter CurrentMeter = new("Boilerplate", typeof(AppActivitySource).Assembly.GetName().Version!.ToString());
}
