using System.Text.Json;
using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Spectre.Console;

namespace Bit.Cli.Projects;

public sealed record DiffRequest
{
    public string? From { get; init; }

    public string? To { get; init; }

    public string? Project { get; init; }

    public string? Name { get; init; }

    public string? Output { get; init; }

    public string? TemplatePackage { get; init; }

    public IReadOnlyDictionary<string, string> TemplateValues { get; init; } = new Dictionary<string, string>();
}

public sealed record ProjectRecord(string? Version, string? Name, IReadOnlyDictionary<string, string> Options);

public sealed class TemplateDiff(CliServices cli)
{
    public async Task<int> RunAsync(DiffRequest request, CancellationToken cancellationToken)
    {
        NewWorkflow.WriteHeader(cli);

        var directory = Path.GetFullPath(request.Project ?? ".", cli.Environment.CurrentDirectory);
        var record = ReadProject(directory);
        var name = request.Name ?? (Directory.Exists(directory) ? ProjectContext.FindProjectName(directory) : null) ?? record.Name;
        var from = request.From ?? record.Version;
        var to = request.To ?? (request.TemplatePackage is null ? BuildInfo.Version : TemplateSource.FolderVersion);

        if (name is null)
            return UsageError($"{directory} isn't a bit Boilerplate project. Run bit diff in the project's folder, or pass --name.");

        if (from is null)
            return UsageError($"{directory} has no bit.json that says which bit Boilerplate version made the project. Pass it: bit diff <from> [to].");

        var options = new Dictionary<string, string>(record.Options, StringComparer.OrdinalIgnoreCase);

        foreach (var (option, value) in request.TemplateValues)
        {
            options[option] = value;
        }

        var fromOptions = new Dictionary<string, string>(options, StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(directory) && ProjectContext.ReadTargetFramework(directory, name) is { } framework)
        {
            fromOptions["framework"] = framework;

            if (request.TemplateValues.ContainsKey("framework") is false)
            {
                options["framework"] = framework;
            }
        }

        var frameworkChanges = fromOptions.TryGetValue("framework", out var fromFramework) && options.TryGetValue("framework", out var toFramework)
            && string.Equals(fromFramework, toFramework, StringComparison.OrdinalIgnoreCase) is false;

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase) && request.TemplatePackage is null && frameworkChanges is false)
        {
            cli.Console.Out.MarkupLine($"  [green]{Markup.Escape(name)} is already on bit Boilerplate {Markup.Escape(to)}.[/]");
            return CliApp.ExitOk;
        }

        var output = Path.GetFullPath(request.Output ?? Path.Combine(Path.GetTempPath(), "bit-diff", $"{name}-{from}-{to}"), cli.Environment.CurrentDirectory);

        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
        {
            if (request.Output is not null)
                return UsageError($"{output} already exists and isn't empty. Pick another folder with -o.");

            FolderRemover.Delete(output);
        }

        Directory.CreateDirectory(output);

        cli.Console.Out.MarkupLine($"  Comparing bit Boilerplate [bold]{Markup.Escape(from)}[/] with [bold]{Markup.Escape(to)}[/] for [bold]{Markup.Escape(name)}[/]");
        cli.Console.Out.MarkupLine($"  [grey]{(options.Count == 0 ? "the template's default options" : string.Join(" ", options.Select(o => o.Value is "true" ? $"--{o.Key}" : $"--{o.Key} {o.Value}")))}[/]");

        if (frameworkChanges)
        {
            cli.Console.Out.MarkupLine($"  [grey]from {Markup.Escape(fromOptions["framework"])} to {Markup.Escape(options["framework"])}[/]");
        }
        cli.Console.Out.WriteLine();

        var steps = new StepRunner(cli);
        var source = new TemplateSource(cli.Environment, cli.Runner);

        await steps.RunAsync("git", "Creating a git repository", async (_, ct) =>
        {
            var init = await Git(output, ["init", "-q", "-b", "bit-boilerplate"], ct);

            foreach (var (key, value) in new[] { ("user.name", "bit diff"), ("user.email", "diff@bitplatform.dev"), ("core.autocrlf", "false"), ("core.safecrlf", "false"), ("commit.gpgsign", "false") })
            {
                await Git(output, ["config", key, value], ct);
            }

            return StepResult.FromProcess(init, "Created a git repository", "Couldn't create a git repository", null, output);
        }, cancellationToken);

        if (steps.AnyFailed is false)
        {
            await SnapshotAsync(steps, "from", source, from, null, name, output, fromOptions, cancellationToken);
        }

        if (steps.AnyFailed is false)
        {
            await steps.RunAsync("clear", $"Clearing bit Boilerplate {from}", (_, _) =>
            {
                var failures = new DirectoryInfo(output).EnumerateFileSystemInfos().Where(entry => entry.Name is not ".git").SelectMany(entry => Remove(entry)).ToList();

                return Task.FromResult(failures.Count == 0
                    ? StepResult.Succeeded($"Cleared bit Boilerplate {from}")
                    : StepResult.Failed($"Couldn't clear bit Boilerplate {from}", $"{failures.Count} files are in use", hint: failures[0]));
            }, cancellationToken);
        }

        if (steps.AnyFailed is false)
        {
            await SnapshotAsync(steps, "to", source, to, request.TemplatePackage, name, output, options, cancellationToken);
        }

        if (steps.AnyFailed)
            return CliApp.ExitFailed;

        var patch = Path.Combine(output, "bit-diff.patch");
        await Git(output, ["diff", "--binary", $"--output={patch}", "HEAD~1", "HEAD"], cancellationToken);
        File.AppendAllText(Path.Combine(output, ".git", "info", "exclude"), "\n/bit-diff.patch\n");
        var stat = (await Git(output, ["diff", "--shortstat", "HEAD~1", "HEAD"], cancellationToken)).Output.Trim();

        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine($"  [green bold]bit Boilerplate {Markup.Escape(from)} → {Markup.Escape(to)}[/][grey]: {Markup.Escape(stat.Length == 0 ? "no changes" : stat)}[/]");
        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine($"  [bold]Repository[/]  {Markup.Escape(output)}  [grey]one commit per version[/]");
        cli.Console.Out.MarkupLine($"  [bold]Patch[/]       {Markup.Escape(patch)}");
        cli.Console.Out.MarkupLine($"  [bold]Files[/]       git -C {Markup.Escape(ProcessSpec.Quote(output))} diff --stat HEAD~1");
        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine($"  [grey]Both have the same name and options{(frameworkChanges ? ", apart from the .NET version" : "")}. What the template picks at random on every run, such as ports, GUIDs, dates and the seeded accounts' password, differs between them, and so does what bit new adds.[/]");
        cli.Console.Out.MarkupLine($"  [grey]Delete it when you're done: bit remove {Markup.Escape(ProcessSpec.Quote(output))} --yes[/]");
        cli.Console.Out.WriteLine();

        return CliApp.ExitOk;
    }

    private async Task SnapshotAsync(StepRunner steps, string id, TemplateSource source, string version, string? packagePath, string name, string output, IReadOnlyDictionary<string, string> options, CancellationToken cancellationToken)
    {
        await steps.RunAsync($"create-{id}", $"Creating {name} with bit Boilerplate {version}", async (progress, ct) =>
        {
            var (package, installed) = await source.EnsureInstalledAsync(packagePath is null ? version : null, packagePath, progress, ct);

            if (package is null)
                return StepResult.FromProcess(installed.Succeeded ? installed with { ExitCode = 1 } : installed, "", $"Couldn't get bit Boilerplate {version}", packagePath is null ? $"dotnet new install Bit.Boilerplate::{version}" : null);

            var manifest = TemplateSource.ReadManifest(package.PackagePath) ?? TemplateManifest.Embedded;
            var (arguments, unknown) = TemplateArguments(manifest, options);
            var created = await source.CreateAsync(package, manifest, name, output, arguments, progress, ct);

            if (created.Succeeded is false)
                return StepResult.FromProcess(created, "", $"Couldn't create {name} with bit Boilerplate {version}", null);

            var solution = Path.Combine(output, $"{name}.slnx");
            var legacySolution = Path.ChangeExtension(solution, ".sln");

            if (File.Exists(solution) && File.Exists(legacySolution))
            {
                File.Delete(legacySolution);
            }

            await Git(output, ["add", "-A"], ct);
            var commit = await Git(output, ["commit", "-q", "--allow-empty", "-m", $"bit Boilerplate {version}"], ct);

            return StepResult.FromProcess(commit, $"Created {name} with bit Boilerplate {version}", $"Couldn't commit bit Boilerplate {version}", null,
                unknown.Count == 0 ? null : $"without {string.Join(", ", unknown)}, which it doesn't have");
        }, cancellationToken);
    }

    public static (List<string> Arguments, List<string> Unknown) TemplateArguments(TemplateManifest manifest, IReadOnlyDictionary<string, string> options)
    {
        var arguments = new List<string>();
        var unknown = new List<string>();

        foreach (var (option, value) in options)
        {
            if (manifest.Find(option) is not { } parameter || parameter.Normalize(value) is not { } normalized)
            {
                unknown.Add($"--{option} {value}");
                continue;
            }

            arguments.AddRange([$"--{parameter.Name}", normalized]);
        }

        return (arguments, unknown);
    }

    public static ProjectRecord ReadProject(string directory)
    {
        var bitJson = Path.Combine(directory, "bit.json");
        var record = File.Exists(bitJson) && ReadBitJson(File.ReadAllText(bitJson)) is { } read
            ? read
            : new ProjectRecord(null, null, new Dictionary<string, string>());

        if (record.Options.ContainsKey("framework") || Directory.Exists(directory) is false
            || (record.Name ?? ProjectContext.FindProjectName(directory)) is not { } name
            || ProjectContext.ReadTargetFramework(directory, name) is not { } framework)
            return record;

        return record with { Options = new Dictionary<string, string>(record.Options, StringComparer.OrdinalIgnoreCase) { ["framework"] = framework } };
    }

    public static ProjectRecord? ReadBitJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object)
                return null;

            string? Text(string name) => root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("options", out var values) && values.ValueKind is JsonValueKind.Object)
            {
                foreach (var option in values.EnumerateObject())
                {
                    if (option.Value.ValueKind is JsonValueKind.String or JsonValueKind.True or JsonValueKind.False)
                    {
                        options[option.Name] = option.Value.ValueKind is JsonValueKind.String ? option.Value.GetString()! : option.Value.GetBoolean() ? "true" : "false";
                    }
                }
            }

            return new ProjectRecord(Text("version"), Text("name"), options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<string> Remove(FileSystemInfo entry)
    {
        if (entry is DirectoryInfo)
            return FolderRemover.Delete(entry.FullName).Failures.Select(f => f.Path);

        try
        {
            entry.Attributes = FileAttributes.Normal;
            entry.Delete();
            return [];
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return [entry.FullName];
        }
    }

    private Task<ProcessResult> Git(string directory, string[] arguments, CancellationToken cancellationToken)
    {
        return cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = arguments, WorkingDirectory = directory, Timeout = TimeSpan.FromMinutes(5) }, cancellationToken);
    }

    private int UsageError(string message)
    {
        cli.Console.Fail(message);
        return CliApp.ExitUsage;
    }
}
