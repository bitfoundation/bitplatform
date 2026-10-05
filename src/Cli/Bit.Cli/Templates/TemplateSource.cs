using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Templates;

public sealed record TemplatePackage(string Version, string PackagePath, string HiveDirectory);

public sealed class TemplateSource(CliEnvironment environment, IProcessRunner runner)
{
    public const string PackageId = "Bit.Boilerplate";

    public const string FolderVersion = "local";

    private const string FolderMarker = "folder.txt";

    private string TemplatesDirectory => Path.Combine(environment.BitDirectory, "templates");

    public async Task<(TemplatePackage? Package, ProcessResult Result)> EnsureInstalledAsync(string? version, string? packagePath, Action<string>? onOutput, CancellationToken cancellationToken)
    {
        if (packagePath is not null)
        {
            var fullPath = Path.GetFullPath(packagePath, environment.CurrentDirectory);
            var isFolder = Directory.Exists(fullPath);

            if (isFolder is false && File.Exists(fullPath) is false)
                return (null, new ProcessResult { ExitCode = -1, Output = $"No template package or folder at {fullPath}." });

            var changed = isFolder ? File.GetLastWriteTimeUtc(Path.Combine(fullPath, ".template.config", "template.json")) : File.GetLastWriteTimeUtc(fullPath);
            var stamp = $"{fullPath}|{changed.Ticks}";
            var localHive = Path.Combine(TemplatesDirectory, "local-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(stamp)))[..12]);
            var alreadyInstalled = isFolder ? File.Exists(Path.Combine(localHive, FolderMarker)) : FindPackage(localHive) is not null;
            var localResult = alreadyInstalled
                ? new ProcessResult { ExitCode = 0, Output = "Already installed." }
                : await InstallAsync(fullPath, localHive, onOutput, cancellationToken);

            if (isFolder && alreadyInstalled is false && localResult.Succeeded)
            {
                File.WriteAllText(Path.Combine(localHive, FolderMarker), fullPath);
            }

            return localResult.Succeeded
                ? (new TemplatePackage(isFolder ? FolderVersion : ReadPackageVersion(fullPath) ?? "0.0.0", fullPath, localHive), localResult)
                : (null, localResult);
        }

        version ??= BuildInfo.Version;
        var hive = Path.Combine(TemplatesDirectory, version);
        var cached = FindPackage(hive);

        if (cached is not null)
            return (new TemplatePackage(version, cached, hive), new ProcessResult { ExitCode = 0, Output = "Already installed." });

        var result = await InstallAsync($"{PackageId}::{version}", hive, onOutput, cancellationToken);
        var installed = FindPackage(hive);

        return result.Succeeded && installed is not null
            ? (new TemplatePackage(version, installed, hive), result)
            : (null, result);
    }

    public Task<ProcessResult> CreateAsync(TemplatePackage package, TemplateManifest manifest, string name, string outputDirectory, IEnumerable<string> templateArguments, Action<string>? onOutput, CancellationToken cancellationToken)
    {
        return runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["new", manifest.ShortName, "--name", name, "--output", outputDirectory, "--debug:custom-hive", package.HiveDirectory, .. templateArguments],
            WorkingDirectory = environment.CurrentDirectory,
            OnOutputLine = onOutput,
            Timeout = TimeSpan.FromMinutes(10)
        }, cancellationToken);
    }

    public static TemplateManifest? ReadManifest(string packagePath)
    {
        return ReadEntry(packagePath, ".template.config/template.json") is { } json ? TemplateManifest.Parse(json) : null;
    }

    public static string? ReadEntry(string packagePath, string pathSuffix)
    {
        if (Directory.Exists(packagePath))
            return ReadFolderEntry(packagePath, pathSuffix);

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var entry = archive.Entries
                .Where(e => e.FullName.Replace('\\', '/').EndsWith(pathSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.FullName.Length)
                .FirstOrDefault();

            if (entry is null)
                return null;

            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }
        catch (Exception exp) when (exp is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string? ReadFolderEntry(string folder, string pathSuffix)
    {
        var parts = pathSuffix.Split('/');

        try
        {
            for (var i = 0; i < parts.Length; i++)
            {
                var candidate = Path.Combine([folder, .. parts[i..]]);

                if (File.Exists(candidate))
                    return File.ReadAllText(candidate);
            }
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static string? ReadPackageVersion(string packagePath)
    {
        try
        {
            var nuspec = ReadEntry(packagePath, ".nuspec");
            return nuspec is null ? null : XDocument.Parse(nuspec).Descendants().FirstOrDefault(e => e.Name.LocalName == "version")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? FindPackage(string hive)
    {
        var packages = Path.Combine(hive, "packages");

        return Directory.Exists(packages)
            ? Directory.EnumerateFiles(packages, $"{PackageId}.*.nupkg").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;
    }

    private Task<ProcessResult> InstallAsync(string package, string hive, Action<string>? onOutput, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(hive);

        return runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["new", "install", package, "--debug:custom-hive", hive, "--force"],
            WorkingDirectory = environment.CurrentDirectory,
            OnOutputLine = onOutput,
            Timeout = TimeSpan.FromMinutes(5)
        }, cancellationToken);
    }
}
