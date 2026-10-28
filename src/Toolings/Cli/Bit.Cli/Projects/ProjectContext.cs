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

    public IReadOnlyList<string> BuildProperties { get; init; } = [];

    public string Solution => Path.Combine(Directory, $"{Name}.slnx");

    public string WebSolutionFilter => Path.Combine(Directory, $"{Name}.Web.slnf");

    public bool BuildsSolution => Templates.Platforms.BuildsSolution(Platforms);

    public string BuildPath => Templates.Platforms.BuildPath(Name, Platforms);

    public string ServerApiDirectory => Path.Combine(Directory, "src", "Server", $"{Name}.Server.Api");

    public string ServerWebDirectory => Path.Combine(Directory, "src", "Server", $"{Name}.Server.Web");

    public string ClientCoreDirectory => Path.Combine(Directory, "src", "Client", $"{Name}.Client.Core");

    public string AppHostDirectory => Path.Combine(Directory, "src", "Server", $"{Name}.Server.AppHost");

    public bool Exists => System.IO.Directory.Exists(Directory) && (File.Exists(Solution) || File.Exists(WebSolutionFilter));

    public string? Database => Template?.Database ?? ReadDatabaseFromProject();

    public bool NeedsContainers => Template?.NeedsContainers ?? AppHostNeedsContainers(AppHostDirectory);

    public bool IsApiStandalone => Template is not null
        ? string.Equals(Template["api"], "Standalone", StringComparison.OrdinalIgnoreCase)
        : System.IO.Directory.Exists(ServerApiDirectory) && ServerWebReferencesServerApi() is false;

    public string? SeedPassword { get; set; }

    public string TargetFrameworkVersion => ReadTargetFramework(Directory, Name) ?? $"net{Environment.Version.Major}.0";

    public static string? ReadTargetFramework(string directory, string name)
    {
        var core = Path.Combine(directory, "src", "Client", $"{name}.Client.Core", $"{name}.Client.Core.csproj");

        try
        {
            if (File.Exists(core) && TargetFrameworkRegex().Match(File.ReadAllText(core)) is { Success: true } match)
                return match.Groups["tfm"].Value;
        }
        catch (IOException)
        {
        }

        return null;
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

    public static string? FindProjectDirectory(string directory)
    {
        for (var folder = new DirectoryInfo(directory); folder is not null; folder = folder.Parent)
        {
            try
            {
                if (folder.Exists && FindProjectName(folder.FullName) is not null)
                    return folder.FullName;
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }

        return null;
    }

    public static string? ReadAppId(string directory, string name)
    {
        var maui = Path.Combine(directory, Templates.Platforms.MauiProject(name));

        try
        {
            return File.Exists(maui) && AppIdRegex().Match(File.ReadAllText(maui)) is { Success: true } match ? match.Groups["id"].Value.Trim() : null;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return null;
        }
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

    private bool ServerWebReferencesServerApi()
    {
        var web = Path.Combine(Directory, "src", "Server", $"{Name}.Server.Web", $"{Name}.Server.Web.csproj");

        try
        {
            return File.Exists(web) is false || File.ReadAllText(web).Contains($"{Name}.Server.Api.csproj", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    public static bool AppHostNeedsContainers(string appHostDirectory)
    {
        try
        {
            return System.IO.Directory.Exists(appHostDirectory)
                && System.IO.Directory.EnumerateFiles(appHostDirectory, "*.cs", SearchOption.AllDirectories)
                    .Where(file => file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false && file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) is false)
                    .Any(file => ContainerServerRegex().IsMatch(File.ReadAllText(file)));
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return true;
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

    [GeneratedRegex(@"<ApplicationId>(?<id>[^<]*)</ApplicationId>")]
    private static partial Regex AppIdRegex();

    [GeneratedRegex(@"<WasmBuildNative[^>]*>\s*true\s*</WasmBuildNative>", RegexOptions.IgnoreCase)]
    private static partial Regex NativeWebAssemblyRegex();

    [GeneratedRegex(@"\.Add(Azure)?(SqlServer|Postgres\w*|PostgreSQL|MySql|ManagedRedis|Redis\w*)\(")]
    private static partial Regex ContainerServerRegex();
}
