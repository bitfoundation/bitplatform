using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public sealed record OldestProject(string SinceBit, string Template);

public sealed record ProjectBit(string FilePath, string? MinVersion, string? MaxVersion)
{
    public const string FileName = "bit.json";

    public static readonly IReadOnlyList<OldestProject> OldestProjects = [];

    public string? TemplateVersion { get; init; }

    public string? TooOldFor { get; init; }

    public bool Fits(string version) => NeedsNewerThan(version) is false && NeedsOlderThan(version) is false;

    public bool NeedsNewerThan(string version) => MinVersion is not null && SelfUpdate.CompareVersions(version, MinVersion) < 0;

    public bool NeedsOlderThan(string version) => (MaxVersion is not null && SelfUpdate.CompareVersions(version, MaxVersion) > 0) || IsTooOldFor(version);

    public bool IsTooOldFor(string version) => TooOldFor is not null && SelfUpdate.CompareVersions(version, TooOldFor) >= 0;

    public static ProjectBit? Find(string directory, IReadOnlyList<OldestProject>? oldestProjects = null)
    {
        try
        {
            for (var folder = new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
            {
                var file = Path.Combine(folder.FullName, FileName);

                if (File.Exists(file))
                    return Read(file, oldestProjects ?? OldestProjects);
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
        }

        return null;
    }

    private static ProjectBit? Read(string file, IReadOnlyList<OldestProject> oldestProjects)
    {
        if (TemplateDiff.ReadBitJson(File.ReadAllText(file)) is not { } record)
            return null;

        var version = Version(record.Version);
        var max = Version(record.CliMaxVersion);
        var min = version is not null && max is not null && SelfUpdate.CompareVersions(version, max) > 0 ? null : version;

        var tooOldFor = version is null ? null : oldestProjects
            .Where(o => SelfUpdate.IsValidVersion(o.SinceBit) && SelfUpdate.IsValidVersion(o.Template))
            .Where(o => SelfUpdate.CompareVersions(version, o.Template) < 0 && SelfUpdate.CompareVersions(version, o.SinceBit) < 0)
            .Select(o => o.SinceBit)
            .Order(Comparer<string>.Create(SelfUpdate.CompareVersions))
            .FirstOrDefault();

        if (min is null && max is null && tooOldFor is null)
            return null;

        return new ProjectBit(file, min, max) { TemplateVersion = version, TooOldFor = tooOldFor };
    }

    private static string? Version(string? value) => value?.Trim() is { } version && SelfUpdate.IsValidVersion(version) ? version : null;
}
