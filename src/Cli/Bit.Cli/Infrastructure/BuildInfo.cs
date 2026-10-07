using System.Reflection;

namespace Bit.Cli.Infrastructure;

public static class BuildInfo
{
    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public const string PackageId = "Bit.Cli";

    private static readonly Assembly assembly = typeof(BuildInfo).Assembly;

    private static readonly string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    public static string Version { get; } = Metadata("BitCliVersion") ?? informationalVersion.Split('+')[0];

    public static string? Commit { get; } = informationalVersion.Split('+') is [_, var commit, ..] && commit.Length >= 7 ? commit : null;

    public static string? ShortCommit => Commit?[..7];

    public static string? RunUrl { get; } = Metadata("BitCliBuildRunUrl");

    public static string? TelemetryConnectionString { get; } = Metadata("BitCliTelemetryConnectionString");

    public static bool IsOfficialBuild => RunUrl is not null;

    public static string SourceUrl => $"{RepositoryUrl}/tree/{Commit?[..12] ?? "develop"}/src/Cli";

    public static string PackageUrl => $"https://www.nuget.org/packages/{PackageId}/{Version}";

    private static string? Metadata(string key)
    {
        var value = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
