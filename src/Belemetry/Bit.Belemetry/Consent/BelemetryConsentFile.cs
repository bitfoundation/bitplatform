using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Remembers the consent decision of a Hybrid app in a file, so it survives restarts even when the app never shows a
/// web page that could keep it in <c>localStorage</c>. Recording the choice itself is strictly necessary, so this
/// does not depend on <see cref="BelemetryFeatures.Persistence"/>.
/// </summary>
internal static class BelemetryConsentFile
{
    public static bool IsSupported(BelemetrySettings settings)
    {
        return settings.Options.Consent.Persist && settings.IsBrowser is false && settings.IsServer is false;
    }

    public static BelemetryConsentRecord? Load(BelemetrySettings settings)
    {
        if (IsSupported(settings) is false) return null;

        try
        {
            var path = GetPath(settings);
            if (File.Exists(path) is false) return null;
            return JsonSerializer.Deserialize(File.ReadAllText(path), BelemetryJsonContext.Default.BelemetryConsentRecord);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(BelemetrySettings settings, BelemetryFeatures features)
    {
        if (IsSupported(settings) is false) return;

        try
        {
            var path = GetPath(settings);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var record = new BelemetryConsentRecord { Features = (int)features, Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            File.WriteAllText(path, JsonSerializer.Serialize(record, BelemetryJsonContext.Default.BelemetryConsentRecord));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The decision still applies for this run.
        }
    }

    private static string GetPath(BelemetrySettings settings)
    {
        var directory = settings.Options.Persistence.Directory ?? BelemetryFileRequestStore.DefaultDirectory(settings.ServiceName);
        return Path.Combine(directory, "consent.json");
    }
}
