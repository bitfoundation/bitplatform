using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bit.Cli.Templates;

namespace Bit.Cli.Projects;

public sealed partial class ProjectContext
{
    public required string Name { get; init; }

    public required string Directory { get; init; }

    public required IReadOnlySet<Platform> Platforms { get; init; }

    public TemplateSelection? Template { get; init; }

    public bool GitReady { get; set; }

    public string Solution => Path.Combine(Directory, $"{Name}.slnx");

    public string WebSolutionFilter => Path.Combine(Directory, $"{Name}.Web.slnf");

    public bool BuildsSolution => Templates.Platforms.BuildsSolution(Platforms);

    public string BuildPath => Templates.Platforms.BuildPath(Name, Platforms);

    public string ServerApiDirectory => Path.Combine(Directory, "src", "Server", $"{Name}.Server.Api");

    public bool Exists => System.IO.Directory.Exists(Directory) && (File.Exists(Solution) || File.Exists(WebSolutionFilter));

    public string? Database => Template?.Database ?? ReadDatabaseFromProject();

    public string TargetFrameworkVersion
    {
        get
        {
            var core = Path.Combine(Directory, "src", "Client", $"{Name}.Client.Core", $"{Name}.Client.Core.csproj");

            try
            {
                if (File.Exists(core) && TargetFrameworkRegex().Match(File.ReadAllText(core)) is { Success: true } match)
                    return match.Groups["tfm"].Value;
            }
            catch (IOException)
            {
            }

            return $"net{Environment.Version.Major}.0";
        }
    }

    public string? PackageVersion(string packageId)
    {
        var props = Path.Combine(Directory, "src", "Directory.Packages.props");

        try
        {
            if (File.Exists(props) is false)
                return null;

            return XDocument.Load(props).Descendants()
                .Where(e => e.Name.LocalName == "PackageVersion" && string.Equals((string?)e.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase))
                .Select(e => (string?)e.Attribute("Version"))
                .FirstOrDefault(v => string.IsNullOrEmpty(v) is false);
        }
        catch (Exception exp) when (exp is IOException or System.Xml.XmlException)
        {
            return null;
        }
    }

    public static string? FindProjectName(string directory)
    {
        var filter = System.IO.Directory.EnumerateFiles(directory, "*.Web.slnf").FirstOrDefault();

        if (filter is not null)
            return Path.GetFileName(filter)[..^".Web.slnf".Length];

        var solution = System.IO.Directory.EnumerateFiles(directory, "*.slnx").FirstOrDefault();
        return solution is null ? null : Path.GetFileNameWithoutExtension(solution);
    }

    private string? ReadDatabaseFromProject()
    {
        var api = Path.Combine(ServerApiDirectory, $"{Name}.Server.Api.csproj");

        try
        {
            if (File.Exists(api) is false)
                return null;

            var text = File.ReadAllText(api);
            return text.Contains("Npgsql", StringComparison.Ordinal) ? "PostgreSQL"
                : text.Contains("EntityFrameworkCore.SqlServer", StringComparison.Ordinal) ? "SqlServer"
                : text.Contains("MySql.EntityFrameworkCore", StringComparison.Ordinal) ? "MySql"
                : text.Contains("EntityFrameworkCore.Sqlite", StringComparison.Ordinal) ? "Sqlite"
                : "Other";
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static bool UsesNativeWebAssembly(string directory)
    {
        try
        {
            return System.IO.Directory.EnumerateFiles(Path.Combine(directory, "src", "Client"), "*.Client.Web.csproj", SearchOption.AllDirectories)
                .Any(csproj => NativeWebAssemblyRegex().IsMatch(File.ReadAllText(csproj)));
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"<TargetFramework>(?<tfm>net\d+\.\d+)</TargetFramework>")]
    private static partial Regex TargetFrameworkRegex();

    [GeneratedRegex(@"<WasmBuildNative[^>]*>\s*true\s*</WasmBuildNative>", RegexOptions.IgnoreCase)]
    private static partial Regex NativeWebAssemblyRegex();
}
