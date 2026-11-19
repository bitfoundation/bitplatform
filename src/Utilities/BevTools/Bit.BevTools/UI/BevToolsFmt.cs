using System.Globalization;

namespace Bit.BevTools;

/// <summary>Formatting helpers for the BevTools UI.</summary>
public static class BevToolsFmt
{
    public static string Ms(double? ms) => ms is null ? "–" : ms.Value switch
    {
        < 0.01 => "<0.01 ms",
        < 10 => ms.Value.ToString("0.00", CultureInfo.InvariantCulture) + " ms",
        < 1000 => ms.Value.ToString("0.0", CultureInfo.InvariantCulture) + " ms",
        _ => (ms.Value / 1000).ToString("0.00", CultureInfo.InvariantCulture) + " s",
    };

    public static string Time(DateTimeOffset t) => t.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    public static string Rel(DateTimeOffset t, DateTimeOffset start) => "+" + (t - start).TotalSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "s";

    public static string Ago(DateTimeOffset? t) => t is null ? "never" : (DateTimeOffset.UtcNow - t.Value) switch
    {
        { TotalSeconds: < 1 } => "just now",
        { TotalSeconds: < 60 } s => ((int)s.TotalSeconds) + " s ago",
        { TotalMinutes: < 60 } m => ((int)m.TotalMinutes) + " min ago",
        var h => ((int)h.TotalHours) + " h ago",
    };

    public static string Bytes(long? bytes) => bytes is null ? "–" : BevToolsHttpMessageHandler.FormatBytes(bytes.Value);

    public static string Num(double value) => value.ToString(value >= 100 ? "0" : "0.#", CultureInfo.InvariantCulture);

    public static string KindClass(BevToolsEventKind kind) => "bev-kind bev-kind-" + kind.ToString().ToLowerInvariant();

    public static string KindLabel(BevToolsEventKind kind) => kind switch
    {
        BevToolsEventKind.RenderBatch => "batch",
        BevToolsEventKind.UiEvent => "event",
        BevToolsEventKind.StateChange => "state",
        BevToolsEventKind.JsInterop => "js",
        BevToolsEventKind.Navigation => "nav",
        BevToolsEventKind.Diagnostic => "diag",
        _ => kind.ToString().ToLowerInvariant(),
    };

    public static string SevClass(BevToolsSeverity severity) => "bev-sev-" + severity.ToString().ToLowerInvariant();

    public static string Lifetime(Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) => lifetime.ToString().ToLowerInvariant();

    public static string ComponentFile(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick > 0)
        {
            name = name[..tick];
        }

        return name + ".razor";
    }
}
