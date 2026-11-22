using System.Text.Json;
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

    public ProjectParts Parts { get; init; } = ProjectParts.None;

    public static ProjectContext At(string directory, string name, ProjectParts? parts = null) => new() { Name = name, Directory = directory, Platforms = new HashSet<Platform>(), Parts = parts ?? ProjectParts.None };

    public string Solution => Path.Combine(Directory, $"{Name}.slnx");

    public string WebSolutionFilter => Path.Combine(Directory, $"{Name}.Web.slnf");

    public bool BuildsSolution => Templates.Platforms.BuildsSolution(Platforms);

    public string WorkbenchProject => Part(Parts.Workbench, Templates.Platforms.WorkbenchProject(Name), ".Workbench.csproj");

    public string WorkbenchDirectory => Path.GetDirectoryName(WorkbenchProject)!;

    public string LandingPageProject => Part(Parts.LandingPage, Templates.Platforms.LandingPageProject(Name), ".LandingPage.Server.csproj");

    public string LandingPageDirectory => Path.GetDirectoryName(LandingPageProject)!;

    public IReadOnlyList<string> ProjectsOutsideWebFilter
    {
        get
        {
            var built = ReadFilterProjects(WebSolutionFilter).Select(FullProjectPath).Append(WorkbenchProject).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return [.. ReadSolutionProjects(Solution).Where(project => built.Contains(FullProjectPath(project)) is false)];
        }
    }

    public string ServerApiProject => Part(Parts.ServerApi, $"src/Server/{Name}.Server.Api/{Name}.Server.Api.csproj", ".Server.Api.csproj");

    public string ServerApiDirectory => Path.GetDirectoryName(ServerApiProject)!;

    public IReadOnlyList<string> ServerWebProjects => Parts.ServerWeb.Count > 0 ? Parts.ServerWeb : [Part(null, $"src/Server/{Name}.Server.Web/{Name}.Server.Web.csproj", ".Server.Web.csproj")];

    public string ServerWebDirectory => Path.GetDirectoryName(ServerWebProjects[0])!;

    public string ClientCoreDirectory => Parts.ClientCore ?? Path.GetDirectoryName(FullProjectPath(FindClientProject(Directory, Name, $"src/Client/{Name}.Client.Core/{Name}.Client.Core.csproj", ".Core.csproj")))!;

    public string AppHostProject => Part(Parts.AppHost, $"src/Server/{Name}.Server.AppHost/{Name}.Server.AppHost.csproj", ".Server.AppHost.csproj");

    public string AppHostDirectory => Path.GetDirectoryName(AppHostProject)!;

    public bool Exists => System.IO.Directory.Exists(Directory) && (File.Exists(Solution) || File.Exists(WebSolutionFilter));

    public string? Database => Template?.Database ?? ReadDatabaseFromProject();

    public bool NeedsContainers => Template?.NeedsContainers ?? AppHostNeedsContainers(AppHostDirectory);

    public bool IsApiStandalone => Template is not null
        ? string.Equals(Template["api"], "Standalone", StringComparison.OrdinalIgnoreCase)
        : System.IO.Directory.Exists(ServerApiDirectory) && ServerWebReferencesServerApi() is false;

    public string? SeedPassword { get; set; }

    public string MauiProject => Parts.Maui is { } given ? Relative(given) : FindMauiProject(Directory, Name);

    public string WindowsProject => Parts.Windows is { } given ? Relative(given) : FindWindowsProject(Directory, Name);

    public string TargetFrameworkVersion => (Parts.ClientCore is { } core ? ReadTargetFrameworkOf(ProjectFileIn(core)) : ReadTargetFramework(Directory, Name)) ?? $"net{Environment.Version.Major}.0";

    public string? AppId => AppIdRegex().Match(ReadText(FullProjectPath(MauiProject))) is { Success: true } match ? match.Groups["id"].Value.Trim() : null;

    public IReadOnlyList<string> WebProjects => Parts.Web.Count > 0 ? Parts.Web : [.. FilesUnder(Path.Combine(Directory, "src", "Client"), "*.csproj").Where(IsWebProject)];

    public bool UsesNativeWebAssembly => WebProjects.Any(csproj => NativeWebAssemblyRegex().IsMatch(ReadText(csproj)));

    public IEnumerable<string> PartsNotFound(bool settingUp)
    {
        if (File.Exists(Path.Combine(ClientCoreDirectory, "package.json")) is false)
            yield return $"bit doesn't know which Node.js {Name} needs: {Relative(ClientCoreDirectory)}/package.json isn't there. --client-core names Client.Core's folder.";

        if (WebProjects.Count == 0)
            yield return $"bit didn't check whether {Name} builds native WebAssembly: it found no Blazor WebAssembly project under src/Client. --web names its .csproj.";

        if (settingUp && System.IO.Directory.Exists(ServerApiDirectory) is false)
            yield return $"bit didn't look at the migrations of {Name}'s database: {Relative(ServerApiDirectory)} isn't there. --server-api names Server.Api's .csproj.";
    }

    public static string FindMauiProject(string directory, string name) => FindClientProject(directory, name, Templates.Platforms.MauiProject(name), ".Maui.csproj");

    public static string FindWindowsProject(string directory, string name) => FindClientProject(directory, name, Templates.Platforms.WindowsProject(name), ".Windows.csproj");

    /// <summary>
    /// The path the template gives a client project, or, when a project has moved it, the one its solution lists:
    /// a project split into two apps may keep src/Client/Shop.Client.Operations.Maui, for example.
    /// </summary>
    private static string FindClientProject(string directory, string name, string templatePath, string fileEnding) => FindProject(directory, name, templatePath, fileEnding, "src/Client/");

    private static string FindProject(string directory, string name, string templatePath, string fileEnding, string under = "") =>
        File.Exists(Path.Combine(directory, templatePath))
            ? templatePath
            : ReadSolutionProjects(Path.Combine(directory, $"{name}.slnx")).FirstOrDefault(project => project.StartsWith(under, StringComparison.OrdinalIgnoreCase) && project.EndsWith(fileEnding, StringComparison.OrdinalIgnoreCase)) ?? templatePath;

    private string Part(string? given, string templatePath, string fileEnding) => given ?? FullProjectPath(FindProject(Directory, Name, templatePath, fileEnding));

    public string Relative(string path) => Path.GetRelativePath(Directory, path).Replace('\\', '/');

    public static IReadOnlyList<string> ProjectFilesIn(string folder)
    {
        try
        {
            return System.IO.Directory.Exists(folder) ? [.. System.IO.Directory.EnumerateFiles(folder, "*.csproj").Order(StringComparer.Ordinal)] : [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public static string? ProjectFileIn(string folder)
    {
        var files = ProjectFilesIn(folder);
        var named = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));

        return files.FirstOrDefault(file => string.Equals(Path.GetFileNameWithoutExtension(file), named, StringComparison.OrdinalIgnoreCase)) ?? files.FirstOrDefault();
    }

    public static string? ReadTargetFramework(string directory, string name) => ReadTargetFrameworkOf(Path.Combine(directory, FindClientProject(directory, name, $"src/Client/{name}.Client.Core/{name}.Client.Core.csproj", ".Core.csproj")));

    private static string? ReadTargetFrameworkOf(string? core) => core is not null && TargetFrameworkRegex().Match(ReadText(core)) is { Success: true } match ? match.Groups["tfm"].Value : null;

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
        try
        {
            var filter = System.IO.Directory.EnumerateFiles(directory, "*.Web.slnf").FirstOrDefault();

            if (filter is not null)
                return Path.GetFileName(filter)[..^".Web.slnf".Length];

            var solution = System.IO.Directory.EnumerateFiles(directory, "*.slnx").FirstOrDefault();
            return solution is null ? null : Path.GetFileNameWithoutExtension(solution);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return null;
        }
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

    private string FullProjectPath(string project) => Path.GetFullPath(Path.Combine(Directory, project));

    private static readonly string[] skippedFolders = ["bin", "obj", "node_modules"];

    public static IEnumerable<string> FilesUnder(string folder, string pattern)
    {
        var pending = new Stack<string>([folder]);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            string[] files, children;

            try
            {
                files = System.IO.Directory.GetFiles(current, pattern);
                children = System.IO.Directory.GetDirectories(current);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.Order(StringComparer.Ordinal))
            {
                yield return file;
            }

            foreach (var child in children.OrderDescending(StringComparer.Ordinal).Where(child => skippedFolders.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase) is false))
            {
                pending.Push(child);
            }
        }
    }

    private static bool IsWebProject(string csproj) => csproj.EndsWith(".Client.Web.csproj", StringComparison.OrdinalIgnoreCase) || ReadText(csproj).Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.Ordinal);

    public static string ReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : "";
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }

    private static IEnumerable<string> ReadSolutionProjects(string solution)
    {
        try
        {
            return File.Exists(solution)
                ? [.. XDocument.Load(solution).Descendants().Where(e => e.Name.LocalName == "Project").Select(e => (string?)e.Attribute("Path")).OfType<string>().Select(path => path.Replace('\\', '/'))]
                : [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return [];
        }
    }

    private static IEnumerable<string> ReadFilterProjects(string filter)
    {
        try
        {
            if (File.Exists(filter) is false)
                return [];

            using var document = JsonDocument.Parse(File.ReadAllText(filter), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            return document.RootElement.ValueKind is JsonValueKind.Object && document.RootElement.TryGetProperty("solution", out var solution) && solution.ValueKind is JsonValueKind.Object
                && solution.TryGetProperty("projects", out var projects) && projects.ValueKind is JsonValueKind.Array
                ? [.. projects.EnumerateArray().Where(p => p.ValueKind is JsonValueKind.String).Select(p => p.GetString()!.Replace('\\', '/'))]
                : [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    private string? ReadDatabaseFromProject()
    {
        var api = ServerApiProject;

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
        var web = ServerWebProjects[0];

        try
        {
            return File.Exists(web) is false || File.ReadAllText(web).Contains(Path.GetFileName(ServerApiProject), StringComparison.OrdinalIgnoreCase);
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

    [GeneratedRegex(@"<TargetFramework>(?<tfm>net\d+\.\d+)</TargetFramework>")]
    private static partial Regex TargetFrameworkRegex();

    [GeneratedRegex(@"<ApplicationId>(?<id>[^<]*)</ApplicationId>")]
    private static partial Regex AppIdRegex();

    [GeneratedRegex(@"<WasmBuildNative[^>]*>\s*true\s*</WasmBuildNative>", RegexOptions.IgnoreCase)]
    private static partial Regex NativeWebAssemblyRegex();

    [GeneratedRegex(@"\.Add(Azure)?(SqlServer|Postgres\w*|PostgreSQL|MySql|ManagedRedis|Redis\w*)\(")]
    private static partial Regex ContainerServerRegex();
}
