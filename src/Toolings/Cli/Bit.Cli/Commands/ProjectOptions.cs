using System.CommandLine;
using Bit.Cli.Projects;

namespace Bit.Cli.Commands;

public sealed class ProjectOptions
{
    public const string ProjectName = "--project";

    private readonly HashSet<Option> added = [];

    public Option<string?> Project { get; } = new(ProjectName) { Description = "The project's folder. Default: the current folder.", HelpName = "folder" };

    public Option<string?> ServerApi { get; } = new("--server-api") { Description = "Server.Api's .csproj. Default: src/Server/<name>.Server.Api, or the *.Server.Api.csproj the project's .slnx lists.", HelpName = "csproj" };

    public Option<string[]> ServerWeb { get; } = new("--server-web") { Description = "Server.Web's .csproj, the server the app opens at. Repeat it for a project with more than one. Default: src/Server/<name>.Server.Web, or the *.Server.Web.csproj the project's .slnx lists.", HelpName = "csproj" };

    public Option<string?> AppHost { get; } = new("--apphost") { Description = "The Aspire AppHost's .csproj. Default: src/Server/<name>.Server.AppHost, or the *.Server.AppHost.csproj the project's .slnx lists.", HelpName = "csproj" };

    public Option<string?> ClientCore { get; } = new("--client-core") { Description = "Client.Core's folder, whose package.json names the project's Node.js version. Default: src/Client/<name>.Client.Core, or the folder of the *.Core.csproj the project's .slnx lists under src/Client.", HelpName = "folder" };

    public Option<string[]> Web { get; } = new("--web") { Description = "A Blazor WebAssembly app's .csproj, read for whether it builds native WebAssembly. Repeat it for more. Default: every one under src/Client.", HelpName = "csproj" };

    public Option<string?> Maui { get; } = new("--maui") { Description = "Client.Maui's .csproj, the Android, iOS and macOS app. Default: src/Client/<name>.Client.Maui, or the *.Maui.csproj the project's .slnx lists under src/Client.", HelpName = "csproj" };

    public Option<string?> Windows { get; } = new("--windows") { Description = "Client.Windows' .csproj. Default: src/Client/<name>.Client.Windows, or the *.Windows.csproj the project's .slnx lists under src/Client.", HelpName = "csproj" };

    public Option<string?> Workbench { get; } = new("--workbench") { Description = "The Workbench's .csproj. Default: src/Workbench/<name>.Workbench.csproj, or the *.Workbench.csproj the project's .slnx lists.", HelpName = "csproj" };

    public Option<string?> LandingPage { get; } = new("--landing-page") { Description = "The landing page's server .csproj. Default: src/LandingPage/<name>.LandingPage.Server, or the *.LandingPage.Server.csproj the project's .slnx lists.", HelpName = "csproj" };

    public Option<string[]> WellKnown { get; } = new("--well-known") { Description = "A wwwroot/.well-known folder, whose assetlinks.json and apple-app-site-association.json name the app. Repeat it for a project that serves more than one web app. Default: every one under src/Client that has one of the two.", HelpName = "folder" };

    public ProjectOptions AddTo(Command command, params Option[] options)
    {
        foreach (var option in options)
        {
            command.Options.Add(option);
            added.Add(option);
        }

        return this;
    }

    public ProjectOptions SearchedAbove()
    {
        Project.Description = "A folder of the project. Default: the current folder. The project is the nearest folder with a *.Web.slnf or *.slnx, it or one above it.";
        return this;
    }

    public string Folder(ParseResult parseResult, CliServices cli) => Path.GetFullPath(added.Contains(Project) && parseResult.GetValue(Project) is { Length: > 0 } given ? given : ".", cli.Environment.CurrentDirectory);

    public bool IsGiven(ParseResult parseResult) => added.Contains(Project) && parseResult.GetValue(Project) is { Length: > 0 };

    public string? ProjectDirectory(ParseResult parseResult, CliServices cli, out string folder)
    {
        folder = Folder(parseResult, cli);
        return Directory.Exists(folder) ? ProjectContext.FindProjectDirectory(folder) : null;
    }

    public static string NotInAProject(string folder) => Directory.Exists(folder)
        ? $"{folder} isn't in a bit Boilerplate project: neither it nor a folder above it has a *.Web.slnf or *.slnx."
        : $"{folder} isn't there.";

    public (ProjectParts Parts, string? Error) Read(ParseResult parseResult, CliServices cli, string projectDirectory)
    {
        string? error = null;

        string? Resolve(Option option, string? value, bool folder)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var path = PathOf(value, cli, projectDirectory);

            if (Exists(path) is false)
            {
                error ??= $"{option.Name}: {path} isn't there.";
                return null;
            }

            if (folder)
            {
                if (Directory.Exists(path))
                    return path;

                error ??= $"{option.Name} takes a folder, and {path} is a file.";
                return null;
            }

            if (File.Exists(path))
            {
                if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    return path;

                error ??= $"{option.Name} takes a .csproj or its folder, and {path} is neither.";
                return null;
            }

            var inside = ProjectContext.ProjectFilesIn(path);
            var named = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));

            if (inside.Count == 1 || inside.Any(file => string.Equals(Path.GetFileNameWithoutExtension(file), named, StringComparison.OrdinalIgnoreCase)))
                return ProjectContext.ProjectFileIn(path);

            error ??= inside.Count == 0
                ? $"{option.Name} takes a .csproj, and {path} has none."
                : $"{option.Name} takes a .csproj, and {path} has {inside.Count} of them: name the one you mean.";
            return null;
        }

        string? One(Option<string?> option, bool folder = false) => added.Contains(option) ? Resolve(option, parseResult.GetValue(option), folder) : null;

        IReadOnlyList<string> Many(Option<string[]> option, bool folder = false) => added.Contains(option)
            ? [.. (parseResult.GetValue(option) ?? []).Select(value => Resolve(option, value, folder)).OfType<string>().Distinct(StringComparer.Ordinal)]
            : [];

        var parts = new ProjectParts
        {
            ServerApi = One(ServerApi),
            ServerWeb = Many(ServerWeb),
            AppHost = One(AppHost),
            ClientCore = One(ClientCore, folder: true),
            Web = Many(Web),
            Maui = One(Maui),
            Windows = One(Windows),
            Workbench = One(Workbench),
            LandingPage = One(LandingPage),
            WellKnown = Many(WellKnown, folder: true)
        };

        return (parts, error);
    }

    public static string PathOf(string value, CliServices cli, string projectDirectory)
    {
        var path = Path.GetFullPath(value, cli.Environment.CurrentDirectory);

        if (Exists(path) is false && Path.IsPathRooted(value) is false && Path.GetFullPath(value, projectDirectory) is var inProject && Exists(inProject))
        {
            path = inProject;
        }

        return Exists(path) ? AsOnDisk(Path.TrimEndingDirectorySeparator(path)) : path;
    }

    private static string AsOnDisk(string path)
    {
        try
        {
            var entry = new DirectoryInfo(path);

            if (entry.Parent is not { } parent || entry.Name.AsSpan().IndexOfAny('*', '?') >= 0)
                return path;

            var names = parent.EnumerateFileSystemInfos(entry.Name, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, AttributesToSkip = 0 }).Select(e => e.Name).ToList();
            var name = names.Count == 1 && names.Contains(entry.Name, StringComparer.Ordinal) is false ? names[0] : entry.Name;

            return Path.Combine(AsOnDisk(parent.FullName), name);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return path;
        }
    }

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);
}
