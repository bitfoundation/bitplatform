using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public sealed record ProjectBit(string FilePath, string? MinVersion, string? MaxVersion)
{
    public const string FileName = "bit.json";

    public bool Fits(string version) => NeedsNewerThan(version) is false && NeedsOlderThan(version) is false;

    public bool NeedsNewerThan(string version) => MinVersion is not null && SelfUpdate.CompareVersions(version, MinVersion) < 0;

    public bool NeedsOlderThan(string version) => MaxVersion is not null && SelfUpdate.CompareVersions(version, MaxVersion) > 0;

    public static ProjectBit? Find(string directory)
    {
        try
        {
            for (var folder = new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
            {
                var file = Path.Combine(folder.FullName, FileName);

                if (File.Exists(file))
                    return Read(file);
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
        }

        return null;
    }

    private static ProjectBit? Read(string file)
    {
        if (TemplateDiff.ReadBitJson(File.ReadAllText(file)) is not { } record)
            return null;

        var min = Version(record.CliMinVersion);
        var max = Version(record.CliMaxVersion);

        if (min is null && max is null)
            return null;

        if (min is not null && max is not null && SelfUpdate.CompareVersions(min, max) > 0)
            return null;

        return new ProjectBit(file, min, max);
    }

    private static string? Version(string? value) => value?.Trim() is { } version && SelfUpdate.IsValidVersion(version) ? version : null;
}
