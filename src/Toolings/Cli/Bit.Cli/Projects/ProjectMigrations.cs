using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Projects;

public sealed partial class ProjectMigrations(CliServices cli, ProjectContext project)
{
    public const string MigrationsFolder = "Infrastructure/Data/Migrations";

    public const string OfflineContext = "AppOfflineDbContext";

    private const string CompiledModelFolder = "Infrastructure/Data/CompiledModel";

    private const string MissingProviderText = "Install and configure any database supported by ef core";

    private static readonly string[] diagnostics = ["--verbose", "--no-color", "--prefix-output"];

    public string Tool => (project.PackageVersion("Microsoft.EntityFrameworkCore.Design") ?? project.PackageVersion("Microsoft.EntityFrameworkCore")) is { } version ? $"dotnet-ef@{version}" : "dotnet-ef";

    public bool HasOfflineDatabase => File.Exists(Path.Combine(project.ClientCoreDirectory, "Infrastructure", "Data", $"{OfflineContext}.cs")) && Directory.Exists(project.ServerWebDirectory);

    public bool HasDatabaseProvider => ReadText(Path.Combine(project.ServerApiDirectory, "Program.Services.cs")).Contains(MissingProviderText, StringComparison.Ordinal) is false;

    private string[] OfflineArguments => ["--context", OfflineContext, "--project", $"../../Client/{project.Name}.Client.Core/{project.Name}.Client.Core.csproj"];

    public Task<ProcessResult> RunAsync(string directory, IReadOnlyList<string> arguments, Action<string> progress, CancellationToken cancellationToken)
    {
        return cli.Runner.RunAsync(new ProcessSpec
        {
            FileName = "dotnet",
            Arguments = ["dnx", Tool, "--", .. arguments],
            WorkingDirectory = directory,
            OnOutputLine = progress,
            Timeout = TimeSpan.FromMinutes(30)
        }, cancellationToken);
    }

    public string FollowUp(string directory, IReadOnlyList<string> arguments) => $"cd {ProcessSpec.Quote(directory)} && dotnet {string.Join(' ', new[] { "dnx", Tool, "--" }.Concat(arguments).Select(ProcessSpec.Quote))}";

    public string Attempt(string directory, IReadOnlyList<string> arguments)
        => $"bit was running EF Core's command-line tool, {Tool.Replace("@", " ", StringComparison.Ordinal)}, in {directory}: dotnet {string.Join(' ', new[] { "dnx", Tool, "--" }.Concat(arguments).Select(ProcessSpec.Quote))}";

    public IReadOnlySet<string> MigrationFiles(bool offline)
    {
        var folder = Path.Combine(Project(offline), MigrationsFolder);

        try
        {
            return Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*.cs").ToHashSet(StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return new HashSet<string>();
        }
    }

    public async Task<StepResult> FormatAsync(bool offline, IReadOnlySet<string> before, Action<string> progress, CancellationToken cancellationToken)
    {
        var directory = Project(offline);
        var added = MigrationFiles(offline).Where(f => before.Contains(f) is false).Select(f => Path.GetRelativePath(directory, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToList();

        if (added.Count == 0)
            return StepResult.Skipped("Skipped dotnet format", "the migration added no files");

        string[] arguments = ["format", $"{Path.GetFileName(directory)}.csproj", "--no-restore", "--include", .. added];
        var result = await cli.Runner.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = arguments, WorkingDirectory = directory, OnOutputLine = progress, Timeout = TimeSpan.FromMinutes(15) }, cancellationToken);
        var followUp = $"cd {ProcessSpec.Quote(directory)} && dotnet {string.Join(' ', arguments.Select(ProcessSpec.Quote))}";

        return result.Succeeded
            ? StepResult.Succeeded("Formatted the migration", string.Join(", ", added.Select(Path.GetFileName)))
            : StepResult.FromProcess(result, "", "Couldn't format the migration", followUp) with { Status = StepStatus.Warning };
    }

    public Task<StepResult> AddAsync(string name, bool offline, Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["migrations", "add", name, "--output-dir", MigrationsFolder, .. offline ? OfflineArguments : Array.Empty<string>()];
        return StepAsync(Startup(offline), arguments, [], $"Added the {name} migration", $"Couldn't add the {name} migration", Where(offline), progress, cancellationToken);
    }

    public async Task<StepResult> RemoveAsync(bool offline, Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["migrations", "remove", .. offline ? OfflineArguments : Array.Empty<string>()];
        var result = await RunAsync(Startup(offline), [.. arguments, .. diagnostics], progress, cancellationToken);
        var removed = RemovedMigrationRegex().Match(result.Output) is { Success: true } match ? match.Groups["name"].Value : Where(offline);

        return Step(result, Startup(offline), arguments, arguments, "Removed the last migration", "Couldn't remove the last migration", removed);
    }

    public Task<StepResult> OptimizeAsync(bool build, Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["dbcontext", "optimize", "--output-dir", CompiledModelFolder, "--namespace", $"{project.Name}.Client.Core.Infrastructure.Data", .. OfflineArguments];
        return StepAsync(project.ServerWebDirectory, arguments, build ? [] : ["--no-build"], "Regenerated the compiled model", "Couldn't regenerate the compiled model", Where(offline: true), progress, cancellationToken);
    }

    public Task<StepResult> BundleAsync(string runtime, Action<string> progress, CancellationToken cancellationToken)
    {
        string[] arguments = ["migrations", "bundle", "--configuration", "Release", "--self-contained", "--target-runtime", runtime, "--output", BundleFile(runtime), "--force"];
        return StepAsync(project.ServerApiDirectory, arguments, [], "Built the migrations bundle", "Couldn't build the migrations bundle", $"for {runtime}", progress, cancellationToken);
    }

    public string BundlePath(string runtime) => Path.GetRelativePath(project.Directory, Path.Combine(project.ServerApiDirectory, BundleFile(runtime)));

    private static string BundleFile(string runtime) => runtime.StartsWith("win", StringComparison.OrdinalIgnoreCase) ? "efbundle.exe" : "efbundle";

    private string Startup(bool offline) => offline ? project.ServerWebDirectory : project.ServerApiDirectory;

    private string Project(bool offline) => offline ? project.ClientCoreDirectory : project.ServerApiDirectory;

    private string Where(bool offline) => offline ? "offline database, Client.Core" : $"{project.Database ?? "the"} database, Server.Api";

    private async Task<StepResult> StepAsync(string directory, string[] arguments, string[] runOnly, string succeeded, string failed, string detail, Action<string> progress, CancellationToken cancellationToken)
    {
        var result = await RunAsync(directory, [.. arguments, .. runOnly, .. diagnostics], progress, cancellationToken);
        return Step(result, directory, arguments, [.. arguments, .. runOnly], succeeded, failed, detail);
    }

    private StepResult Step(ProcessResult result, string directory, string[] arguments, string[] ran, string succeeded, string failed, string detail)
    {
        var step = StepResult.FromProcess(result, succeeded, failed, FollowUp(directory, [.. arguments, "--verbose"]), detail);

        if (step.Status is StepStatus.Failed)
            return step with { Hint = Errors(result.Output) ?? step.Hint, Attempt = Attempt(directory, ran) };

        return Warnings(result.Output) is { } warnings ? StepResult.Warning(succeeded, detail, hint: warnings) : step;
    }

    private string? Errors(string output)
    {
        var root = Path.TrimEndingDirectorySeparator(project.Directory) + Path.DirectorySeparatorChar;
        var lines = output.Split('\n').Select(l => l.Trim()).ToArray();

        var build = lines
            .Select(l => l.StartsWith("verbose:", StringComparison.Ordinal) ? l["verbose:".Length..].Trim() : l)
            .Where(l => BuildErrorRegex().IsMatch(l))
            .Select(l => ProjectSuffixRegex().Replace(l, "").Replace(root, "", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .Take(5);

        var tool = lines
            .Where(l => l.StartsWith("error:", StringComparison.Ordinal))
            .Select(l => l["error:".Length..].Trim())
            .Where(l => l.Length > 0);

        var errors = build.Concat(tool).Distinct(StringComparer.Ordinal).ToList();
        return errors.Count == 0 ? null : string.Join('\n', errors);
    }

    private static string? Warnings(string output)
    {
        var warnings = output.Split('\n')
            .Select(l => DataLossWarningRegex().Match(l.TrimEnd()))
            .Where(m => m.Success)
            .Select(m => m.Groups["text"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return warnings.Count == 0 ? null : string.Join('\n', warnings);
    }

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

    [GeneratedRegex(@":\s+error\s+[A-Z]{2,8}\d{3,6}\s*:", RegexOptions.CultureInvariant)]
    private static partial Regex BuildErrorRegex();

    [GeneratedRegex(@"\s\[[^\]]+proj\]$", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectSuffixRegex();

    [GeneratedRegex(@"^warn: {4}(?<text>\S.*\bloss of data\b.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex DataLossWarningRegex();

    [GeneratedRegex(@"Removing migration '(?<name>[^']+)'", RegexOptions.CultureInvariant)]
    private static partial Regex RemovedMigrationRegex();
}
