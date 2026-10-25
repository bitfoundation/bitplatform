using System.Diagnostics;
using OpenTelemetry;

namespace Bit.Cli.Telemetry;

public sealed class AllowListProcessor(TelemetryLevel level) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.TagObjects.ToArray())
        {
            if (TelemetryFields.IsAllowed(tag.Key) is false || (level is not TelemetryLevel.All && TelemetryFields.IsUsage(tag.Key)))
            {
                activity.SetTag(tag.Key, null);
            }
        }
    }
}
