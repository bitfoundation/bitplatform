using System.Diagnostics;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.AotProfiling;

public sealed record AotProfileRequest(
    string? Project,
    string? Output,
    IReadOnlyList<string> Properties,
    string? Origin,
    string StartPath,
    IReadOnlyList<string> Steps,
    int MaxPages,
    IReadOnlyList<string> Include,
    string? Browser);

public sealed class AotProfileRun(CliServices cli)
{
    public static readonly string[] ProfilerProperties = ["RunAOTCompilation=false", "WasmProfilers=aot", "WasmBuildNative=true", "WasmEnableWebcil=false"];

    private const int MaxReloads = 5;

    public async Task<int> RunAsync(AotProfileRequest request, CancellationToken cancellationToken)
    {
        var current = cli.Environment.CurrentDirectory;

        if (FindProject(request.Project, current) is not { } project)
        {
            return cli.UsageError("project-missing", request.Project is null
                ? "No project here. Run bit aot-profile in the folder of the Blazor WebAssembly app's project, or pass its .csproj."
                : $"No project at {request.Project}. Pass a .csproj, or a folder with one .csproj.");
        }

        Uri? origin = null;
        if (request.Origin is not null && (Uri.TryCreate(request.Origin, UriKind.Absolute, out origin) is false || origin.Scheme is not ("http" or "https")))
        {
            return cli.UsageError("origin-invalid", $"--origin {request.Origin} isn't an http or https URL.");
        }

        if (request.Steps.FirstOrDefault(s => ProfileCollector.IsValidStep(s) is false) is { } invalid)
        {
            return cli.UsageError("step-invalid", $"--step {invalid} isn't a step. Use a path like /settings, click:<css selector>, key:<key> or wait:<milliseconds>.");
        }

        var browser = request.Browser ?? Chromium.Find(cli.Environment, cli.Runner.FindExecutable);
        if (browser is null || File.Exists(browser) is false)
        {
            return cli.UsageError("browser-missing", request.Browser is null
                ? "No Chromium, Chrome or Edge found. bit setup installs Playwright's Chromium for a bit Boilerplate project's UI tests, or pass --browser <path>."
                : $"No browser at {request.Browser}.");
        }

        var output = Path.GetFullPath(request.Output ?? $"{Path.GetFileNameWithoutExtension(project)}.aotprofile", current);
        var work = Path.Combine(Path.GetTempPath(), "bit-aot-profile", Guid.NewGuid().ToString("N")[..8]);
        var publish = Path.Combine(work, "publish");

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var published = await cli.Console.RunWithStatusAsync("Publishing with the AOT profiler", progress => cli.Runner.RunAsync(new ProcessSpec
            {
                FileName = "dotnet",
                Arguments = ["publish", project, "-c", "Release", "-o", publish, .. request.Properties.Select(p => $"-p:{p}"), .. ProfilerProperties.Select(p => $"-p:{p}")],
                WorkingDirectory = Path.GetDirectoryName(project),
                OnOutputLine = progress
            }, cancellationToken));

            if (published.Succeeded is false)
            {
                cli.Console.Step(StepStatus.Failed, "Publish with the AOT profiler", Path.GetFileName(project), published.Duration);
                foreach (var line in published.OutputLines.Where(l => l.Contains(": error ", StringComparison.Ordinal)).Distinct().Take(10))
                {
                    cli.Console.StepHint(line.Trim());
                }

                return CliApp.ExitFailed;
            }

            cli.Console.Step(StepStatus.Succeeded, "Published with the AOT profiler", Path.GetFileName(project), published.Duration);

            if (origin is not null && PublishedApp.FindServer(publish) is not null)
                cli.Console.StepHint("--origin is for apps without a server; this one runs its own.");

            var framework = Path.Combine(publish, "wwwroot", "_framework");
            var assemblies = (Directory.Exists(framework) ? Directory.EnumerateFiles(framework, "*.dll") : [])
                .Select(path => (Path: path, Name: AssemblyMethods.ReadName(path)))
                .Where(a => a.Name is not null)
                .ToList();

            if (request.Include.FirstOrDefault(name => assemblies.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) is false) is { } unknown)
            {
                return cli.UsageError("include-unknown", $"--include {unknown}: the app has no assembly by that name.");
            }

            stopwatch.Restart();
            await using var app = await PublishedApp.StartAsync(publish, origin, cli.Log, cancellationToken);
            await using var chromium = await Chromium.LaunchAsync(browser, cli.Log, cancellationToken);
            var page = await chromium.OpenPageAsync(cancellationToken);
            var collector = new ProfileCollector(page, app, cli.Log);
            await collector.StartAsync(cancellationToken);

            var skipped = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> visited = [];
            byte[] data;

            while (true)
            {
                try
                {
                    visited = await cli.Console.RunWithStatusAsync($"Using the app at {app.Origin}", progress => collector.VisitAsync(request.StartPath, Math.Max(1, request.MaxPages), skipped, progress, cancellationToken));

                    if (request.Steps.Count > 0)
                        await collector.RunStepAsync(request.StartPath, cancellationToken);

                    foreach (var step in request.Steps)
                    {
                        if (await collector.RunStepAsync(step, cancellationToken) is false)
                            cli.Console.StepWarning($"--step {step}: nothing on the page matches it.");
                    }

                    data = await collector.WriteAsync(cancellationToken);
                    break;
                }
                catch (AppReloadedException reload) when (skipped.Count < MaxReloads)
                {
                    cli.Console.StepWarning($"{reload.Path} reloads the app, which starts the profile over. Using the app again without it.");
                    skipped.Add(reload.Path);
                }
            }

            cli.Console.Step(StepStatus.Succeeded, "Used the app", $"{visited.Count} page{(visited.Count == 1 ? "" : "s")}{(request.Steps.Count > 0 ? $", {request.Steps.Count} step{(request.Steps.Count == 1 ? "" : "s")}" : "")}", stopwatch.Elapsed);
            cli.Console.StepHint(string.Join(" ", visited));

            if (collector.WrittenEarly)
                cli.Console.StepWarning("The profile was written before the last page: something read a .NET exception's stack trace in the browser, which is the moment the profiler writes.");

            foreach (var error in collector.Errors.Take(5))
            {
                cli.Console.StepHint($"page error: {error}");
            }

            var profile = AotProfile.Read(data);
            var collected = profile.Methods.Count;
            var unmanaged = assemblies.Sum(a => profile.AddRange(AssemblyMethods.Read(a.Path, unmanagedCallersOnly: true)));
            var included = request.Include.Sum(name => profile.AddRange(AssemblyMethods.Read(assemblies.First(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)).Path, unmanagedCallersOnly: false)));

            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await File.WriteAllBytesAsync(output, profile.ToArray(), cancellationToken);

            var detail = $"{collected:N0} methods run, {unmanaged:N0} [UnmanagedCallersOnly]{(request.Include.Count > 0 ? $", {included:N0} of {string.Join(", ", request.Include)}" : "")}";
            cli.Console.Step(StepStatus.Succeeded, $"Wrote {Path.GetRelativePath(current, output)}", detail);
            cli.Console.Line($"[grey]Publish with[/] -p:WasmAotProfilePath={Markup.Escape(output)} [grey]and the same -p properties.[/]");
            return CliApp.ExitOk;
        }
        catch (Exception exp) when (exp is DevToolsException or InvalidOperationException or InvalidDataException or AppReloadedException)
        {
            cli.Log.Write(exp.ToString());
            cli.Console.Fail(exp.Message);
            return CliApp.ExitFailed;
        }
        finally
        {
            try
            {
                if (Directory.Exists(work))
                    Directory.Delete(work, recursive: true);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                cli.Log.Write($"Couldn't delete {work}: {exp.Message}");
            }
        }
    }

    public static string? FindProject(string? project, string currentDirectory)
    {
        var path = Path.GetFullPath(project ?? ".", currentDirectory);

        if (File.Exists(path))
            return path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ? path : null;

        return Directory.Exists(path) && Directory.GetFiles(path, "*.csproj") is [var only] ? only : null;
    }
}
