namespace Bit.Belemetry;

/// <summary>A parsed Application Insights connection string.</summary>
internal sealed record AzureMonitorConnectionString(string InstrumentationKey, Uri IngestionEndpoint, Uri LiveEndpoint)
{
    public const string DefaultIngestionEndpoint = "https://dc.services.visualstudio.com/";
    public const string DefaultLiveEndpoint = "https://rt.services.visualstudio.com/";

    /// <summary>Parses <c>Key=Value;...</c> pairs (keys are case-insensitive). Throws on a missing or malformed instrumentation key.</summary>
    public static AzureMonitorConnectionString Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0) values[part[..eq].Trim()] = part[(eq + 1)..].Trim();
        }

        if (values.TryGetValue("InstrumentationKey", out var ikey) is false || Guid.TryParse(ikey, out _) is false)
        {
            throw new FormatException("The Application Insights connection string has no valid InstrumentationKey.");
        }

        var ingestion = values.TryGetValue("IngestionEndpoint", out var i) && Uri.TryCreate(i, UriKind.Absolute, out var iu) ? iu : null;
        var live = values.TryGetValue("LiveEndpoint", out var l) && Uri.TryCreate(l, UriKind.Absolute, out var lu) ? lu : null;

        // Sovereign clouds publish only an EndpointSuffix; the endpoints are derived from it as the Azure SDKs do.
        if (values.TryGetValue("EndpointSuffix", out var suffix) && string.IsNullOrWhiteSpace(suffix) is false)
        {
            var location = values.TryGetValue("Location", out var loc) && string.IsNullOrWhiteSpace(loc) is false ? loc + "." : string.Empty;
            ingestion ??= new Uri($"https://{location}dc.{suffix.Trim('.')}/");
            live ??= new Uri($"https://{location}live.{suffix.Trim('.')}/");
        }

        return new AzureMonitorConnectionString(ikey, ingestion ?? new Uri(DefaultIngestionEndpoint), live ?? new Uri(DefaultLiveEndpoint));
    }

    /// <summary>The track endpoint, <c>{IngestionEndpoint}/v2.1/track</c>.</summary>
    public string TrackUrl => Combine(IngestionEndpoint, "v2.1/track");

    internal static string Combine(Uri root, string path)
    {
        var text = root.ToString();
        return text.EndsWith('/') ? text + path : text + "/" + path;
    }
}
