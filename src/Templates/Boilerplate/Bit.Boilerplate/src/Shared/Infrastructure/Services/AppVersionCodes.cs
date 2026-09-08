namespace Boilerplate.Shared.Infrastructure.Services;

/// <summary>
/// Encodes an application version as a single sortable number, and back, because text does not sort as a version does
/// (<c>"1.10.0" &lt; "1.9.0"</c>). Same formula as <c>ApplicationVersion</c> in Boilerplate.Client.Maui.csproj.
/// <code>
/// 0.0.1     |             1
/// 0.1.0     |         1_000
/// 1.0.0     |     1_000_000
/// 1.9.0     |     1_009_000
/// 1.10.0    |     1_010_000
/// 2.5.123   |     2_005_123
/// 2100.0.0  | 2_100_000_000 (<see cref="MaxCode"/>)
/// 1.0.0-rc1 |          null (unparseable)
/// 1.1000.0  |          null (past <see cref="MaxComponent"/>)
/// </code>
/// </summary>
public static class AppVersionCodes
{
    /// <summary>Three digits each for minor and patch, so the major keeps its full range under <see cref="MaxCode"/>.</summary>
    public const int MaxComponent = 999;

    /// <summary>Highest code Google Play accepts, which makes 2100.0.0 the highest version - 1 is the lowest.</summary>
    public const long MaxCode = 2_100_000_000;

    private const long MajorFactor = 1_000_000;
    private const long MinorFactor = 1_000;

    /// <summary>
    /// Null when <paramref name="version"/> cannot be represented (unparseable, a component past
    /// <see cref="MaxComponent"/>, or a code outside 1 to <see cref="MaxCode"/>). A fourth component is dropped.
    /// </summary>
    public static long? TryEncode(string? version)
    {
        return Version.TryParse(version, out var parsed) ? TryEncode(parsed) : null;
    }

    /// <inheritdoc cref="TryEncode(string?)"/>
    public static long? TryEncode(Version? version)
    {
        if (version is null)
            return null;

        // Build and Revision are -1 when the version has fewer than three or four components.
        var (major, minor, patch) = (version.Major, version.Minor, Math.Max(version.Build, 0));

        if (major < 0 || minor is < 0 or > MaxComponent || patch > MaxComponent)
            return null;

        var code = major * MajorFactor + minor * MinorFactor + patch;

        // No versionCode the stores would take.
        return code is < 1 or > MaxCode ? null : code;
    }

    /// <summary>Renders a code back as <c>major.minor.patch</c>; null in, null out.</summary>
    public static string? Decode(long? code)
    {
        if (code is null or < 0)
            return null;

        var value = code.Value;

        return string.Create(CultureInfo.InvariantCulture,
            $"{value / MajorFactor}.{value / MinorFactor % MinorFactor}.{value % MinorFactor}");
    }
}
