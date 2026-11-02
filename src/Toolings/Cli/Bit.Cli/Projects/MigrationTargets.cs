using System.Text.RegularExpressions;

namespace Bit.Cli.Projects;

public sealed record MigrationTarget(string Context, bool ContextFound, string ProjectFile, string StartupDirectory, string MigrationsFolder, string CompiledModelFolder, string Namespace)
{
    public string ProjectDirectory => Path.GetDirectoryName(ProjectFile)!;

    public bool StartsItself => string.Equals(Path.TrimEndingDirectorySeparator(ProjectDirectory), Path.TrimEndingDirectorySeparator(StartupDirectory), StringComparison.OrdinalIgnoreCase);
}

public sealed record MigrationLookup(MigrationTarget? Target, string Code = "", string Problem = "");

public sealed partial class MigrationTargets(ProjectContext project, Func<string> tool)
{
    public const string ServerContext = "AppDbContext";

    public const string OfflineContext = "AppOfflineDbContext";

    private static readonly string[] skippedFolders = ["bin", "obj", "node_modules"];

    private readonly Dictionary<bool, MigrationLookup> found = [];

    private List<string>? files;

    private Dictionary<string, (bool IsWeb, IReadOnlyList<string> References)>? projects;

    public MigrationLookup Find(bool offline)
    {
        if (found.TryGetValue(offline, out var lookup) is false)
        {
            found[offline] = lookup = Resolve(offline);
        }

        return lookup;
    }

    private MigrationLookup Resolve(bool offline)
    {
        var context = offline ? OfflineContext : ServerContext;
        var usual = offline ? project.ClientCoreDirectory : project.ServerApiDirectory;
        var file = ContextFile(context, usual);

        if (file is null)
        {
            if (offline)
                return new(null, "offline-db-missing", $"{project.Name} has no offline database: none of its .cs files declares the {context} class, which bit adds the offline database's migrations to. Only a project created with --offlineDb true has one.");

            if (Directory.Exists(usual))
                return new(Target(context, contextFound: false, Path.Combine(usual, $"{Path.GetFileName(usual)}.csproj"), usual, Path.Combine(usual, "Infrastructure", "Data"), contextFile: null));

            return new(null, "server-api-missing", $"{project.Name} has no src/Server/{project.Name}.Server.Api folder and none of its .cs files declares the {context} class, so bit doesn't know which project has the database's migrations. "
                + $"EF Core's tools can do it by hand, from the project's folder: dotnet dnx {tool()} -- migrations add <name> --context <your DbContext> --project <the .csproj that has it> --startup-project <the app's .csproj>");
        }

        if (OwningProject(file) is not { } projectFile)
            return new(null, "context-project-missing", $"{Relative(file)} declares {context}, but none of the folders above it has a .csproj, so EF Core's tools have no project to add its migrations to.");

        if (Startup(projectFile, offline ? project.ServerWebDirectory : usual) is not { } startup)
            return new(null, "startup-project-missing", $"{context} is in {Relative(projectFile)}, but no ASP.NET Core project (Microsoft.NET.Sdk.Web) is it or references it, so EF Core's tools have no app to start it with. "
                + $"Reference it from the app that registers {context}, or run EF Core's tools by hand with that app, from the project's folder: dotnet dnx {tool()} -- migrations add <name> --context {context} --project {Relative(projectFile).Replace('\\', '/')} --startup-project <the app's .csproj>");

        return new(Target(context, contextFound: true, projectFile, startup, Path.GetDirectoryName(file)!, file));
    }

    private MigrationTarget Target(string context, bool contextFound, string projectFile, string startup, string contextFolder, string? contextFile)
    {
        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        var snapshot = ProjectFiles(projectDirectory).FirstOrDefault(f => Path.GetFileName(f).Equals($"{context}ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));
        var model = ProjectFiles(projectDirectory).FirstOrDefault(f => Path.GetFileName(f).Equals($"{context}Model.cs", StringComparison.OrdinalIgnoreCase));
        var relative = ProjectRelative(projectDirectory, contextFolder);

        var codeNamespace = (model is null ? null : Namespace(model))
            ?? (contextFile is null ? null : Namespace(contextFile))
            ?? string.Join('.', new[] { Path.GetFileNameWithoutExtension(projectFile) }.Concat(relative.Split('/', StringSplitOptions.RemoveEmptyEntries)));

        return new MigrationTarget(
            context,
            contextFound,
            projectFile,
            startup,
            snapshot is null ? Join(relative, "Migrations") : ProjectRelative(projectDirectory, Path.GetDirectoryName(snapshot)!),
            model is null ? Join(relative, "CompiledModel") : ProjectRelative(projectDirectory, Path.GetDirectoryName(model)!),
            codeNamespace);
    }

    private string? ContextFile(string context, string usual)
    {
        var conventional = Path.Combine(usual, "Infrastructure", "Data", $"{context}.cs");

        if (File.Exists(conventional))
            return conventional;

        var declaration = new Regex($@"\bclass\s+{Regex.Escape(context)}\b", RegexOptions.CultureInvariant);
        var sources = Files().Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
        var named = sources.Where(f => Path.GetFileName(f).Equals($"{context}.cs", StringComparison.OrdinalIgnoreCase)).ToList();

        return named.FirstOrDefault(f => declaration.IsMatch(ReadText(f)))
            ?? sources.Except(named).FirstOrDefault(f => declaration.IsMatch(ReadText(f)))
            ?? named.FirstOrDefault();
    }

    private string? OwningProject(string file)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(project.Directory));

        for (var folder = Path.GetDirectoryName(file); folder is not null && folder.Length >= root.Length; folder = Path.GetDirectoryName(folder))
        {
            try
            {
                if (Directory.EnumerateFiles(folder, "*.csproj").Order(StringComparer.Ordinal).FirstOrDefault() is { } csproj)
                    return csproj;
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }

        return null;
    }

    private string? Startup(string projectFile, string usual)
    {
        var graph = Projects();
        var target = Path.GetFullPath(projectFile);

        var candidates = graph
            .Where(p => p.Value.IsWeb && (string.Equals(p.Key, target, StringComparison.OrdinalIgnoreCase) || References(p.Key, target, graph)))
            .Select(p => Path.GetDirectoryName(p.Key)!)
            .Order(StringComparer.Ordinal)
            .ToList();

        return candidates.FirstOrDefault(c => string.Equals(c, Path.GetFullPath(usual), StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(c => string.Equals(c, Path.GetDirectoryName(target), StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault();
    }

    private static bool References(string from, string target, Dictionary<string, (bool IsWeb, IReadOnlyList<string> References)> graph)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { from };
        var pending = new Queue<string>([from]);

        while (pending.Count > 0)
        {
            if (graph.TryGetValue(pending.Dequeue(), out var node) is false)
                continue;

            foreach (var reference in node.References)
            {
                if (string.Equals(reference, target, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (seen.Add(reference))
                {
                    pending.Enqueue(reference);
                }
            }
        }

        return false;
    }

    private Dictionary<string, (bool IsWeb, IReadOnlyList<string> References)> Projects()
    {
        if (projects is not null)
            return projects;

        projects = new(StringComparer.OrdinalIgnoreCase);

        foreach (var csproj in Files().Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            var path = Path.GetFullPath(csproj);
            var text = ReadText(path);
            var references = ProjectReferenceRegex().Matches(text)
                .Select(m => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, m.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar))))
                .ToList();

            projects.TryAdd(path, (WebSdkRegex().IsMatch(text), references));
        }

        return projects;
    }

    private IEnumerable<string> ProjectFiles(string projectDirectory)
    {
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory)) + Path.DirectorySeparatorChar;
        return Files().Where(f => Path.GetFullPath(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private List<string> Files()
    {
        if (files is not null)
            return files;

        files = [];
        var pending = new Stack<string>([project.Directory]);

        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            string[] inside, children;

            try
            {
                inside = Directory.GetFiles(folder);
                children = Directory.GetDirectories(folder);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            files.AddRange(inside.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)));

            foreach (var child in children.Where(c => Path.GetFileName(c) is var name && name.StartsWith('.') is false && skippedFolders.Contains(name, StringComparer.OrdinalIgnoreCase) is false))
            {
                pending.Push(child);
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private string Relative(string path) => Path.GetRelativePath(project.Directory, path);

    private static string ProjectRelative(string projectDirectory, string folder)
    {
        var relative = Path.GetRelativePath(projectDirectory, folder).Replace('\\', '/');
        return relative == "." ? "" : relative;
    }

    private static string Join(string folder, string name) => folder.Length == 0 ? name : $"{folder}/{name}";

    private static string? Namespace(string file) => NamespaceRegex().Match(ReadText(file)) is { Success: true } match ? match.Groups["name"].Value : null;

    private static string ReadText(string path)
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

    [GeneratedRegex(@"^\s*namespace\s+(?<name>[A-Za-z_][\w.]*)", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(@"<ProjectReference\b[^>]*\bInclude\s*=\s*""(?<path>[^""]+\.csproj)""", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectReferenceRegex();

    [GeneratedRegex(@"\bSdk\s*=\s*""Microsoft\.NET\.Sdk\.Web(/[^""]*)?""", RegexOptions.CultureInvariant)]
    private static partial Regex WebSdkRegex();
}
