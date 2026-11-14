using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Projects;

/// <summary>
/// Runs the project's .github/workflows/ci.yml with act on this machine itself, not in a container. The workflow builds and
/// tests with the .NET SDK, workloads, NuGet packages, Playwright's browsers and Android SDK that bit setup installed here,
/// so a run downloads nothing they already have, and the tests reach Docker or Podman the way they do outside CI. act runs
/// it on a copy of the project's files, without what .gitignore leaves out such as bin and obj, so every run builds from
/// scratch while the app can keep running from the project's folder.
/// </summary>
public sealed partial class LocalCi(CliServices cli, string directory)
{
    private const string WorkflowPath = ".github/workflows/ci.yml";

    public string Workflow => ProjectContext.CiWorkflow(directory);

    /// <summary>bit's own executable, first on the PATH of the workflow's steps so they run the bit that runs them; null when it runs through dotnet.</summary>
    public string? Self { get; init; } = Environment.ProcessPath is { } path && Path.GetFileNameWithoutExtension(path).Equals("bit", StringComparison.OrdinalIgnoreCase) ? path : null;

    /// <summary>
    /// The runner labels of the workflow's jobs, such as ubuntu-24.04, which act runs on this machine: each runs-on is a label
    /// or a list of them. Null for any other runs-on, such as an expression: act would skip that job as a platform it doesn't
    /// know, and the run would pass without running it.
    /// </summary>
    public static IReadOnlyList<string>? Labels(string workflow)
    {
        var labels = new List<string>();

        foreach (Match runsOn in RunsOnRegex().Matches(workflow))
        {
            var value = runsOn.Groups["value"].Value.Trim();
            string[] items = value.StartsWith('[') && value.EndsWith(']') ? value[1..^1].Split(',') : [value];

            foreach (var item in items.Select(i => i.Trim().Trim('\'', '"')))
            {
                if (LabelRegex().IsMatch(item) is false)
                    return null;

                labels.Add(item);
            }
        }

        return labels.Count == 0 ? null : labels.Distinct().ToList();
    }

    [GeneratedRegex("""^\s*runs-on:(?<value>[^#\r\n]*)""", RegexOptions.Multiline)]
    private static partial Regex RunsOnRegex();

    [GeneratedRegex("""^[\w.-]+$""")]
    private static partial Regex LabelRegex();

    public async Task<int> RunAsync(IReadOnlyList<string> actArguments, CancellationToken cancellationToken)
    {
        var steps = new StepRunner(cli);
        string? act = null;
        var labels = Labels(await File.ReadAllTextAsync(Workflow, cancellationToken))!;

        await steps.RunAsync("act", "Looking for act", (_, _) =>
        {
            act = FindAct();
            return Task.FromResult(act is null
                ? StepResult.Failed("act isn't installed", "bit ci runs the workflow with it", followUp: "bit setup --tools act installs it, or see https://nektosact.com/installation/")
                : StepResult.Succeeded("Found act", act));
        }, cancellationToken);

        // GitHub's runners have PowerShell 7, which bit's setup action runs in, and act runs the workflow's steps in it on Windows.
        await steps.RunAsync("pwsh", "Looking for PowerShell 7", (_, _) => Task.FromResult(cli.Runner.FindExecutable("pwsh") is { } pwsh
            ? StepResult.Succeeded("Found PowerShell 7", pwsh)
            : StepResult.Failed("PowerShell 7 isn't installed", "the workflow's steps run in it, as on GitHub's runners", followUp: "See https://aka.ms/install-powershell")), cancellationToken);

        if (steps.AnyFailed is false)
        {
            await steps.RunAsync("ci", $"Running {WorkflowPath} with act", async (_, token) =>
            {
                var result = await cli.Runner.RunAsync(new ProcessSpec
                {
                    FileName = act!,
                    Arguments = ActArguments(labels, actArguments),
                    WorkingDirectory = directory,
                    Environment = Self is null ? null : new Dictionary<string, string?> { ["PATH"] = Path.GetDirectoryName(Self) + Path.PathSeparator + cli.Environment.GetVariable("PATH") },
                    Interactive = true
                }, token);

                return result.Succeeded
                    ? StepResult.Succeeded("CI passed")
                    : StepResult.Failed("CI failed", result.NotFound ? "act couldn't start" : $"act exited with {result.ExitCode}", followUp: "act's log above says which step failed and why.");
            }, cancellationToken, needsTerminal: true);
        }

        foreach (var followUp in steps.Reports.Select(r => r.Result.FollowUp).OfType<string>())
        {
            cli.Console.Line($"    {Markup.Escape(followUp)}");
        }

        return steps.AnyFailed ? CliApp.ExitFailed : CliApp.ExitOk;
    }

    public static IReadOnlyList<string> ActArguments(IReadOnlyList<string> labels, IReadOnlyList<string> extra)
    {
        return
        [
            "workflow_dispatch",
            "--workflows", WorkflowPath,
            // Each job runs on this machine, with what bit setup installed here, rather than in a container.
            .. labels.SelectMany(label => new[] { "--platform", $"{label}=-self-hosted" }),
            // The actions are fetched once, not again on every run.
            "--action-offline-mode",
            .. extra
        ];
    }

    private string? FindAct()
    {
        if (cli.Runner.FindExecutable("act") is { } act)
            return act;

        // winget puts act's link here, which a terminal opened before it was installed doesn't have on its PATH.
        var linked = Path.Combine(cli.Environment.GetVariable("LOCALAPPDATA") ?? Path.Combine(cli.Environment.HomeDirectory, "AppData", "Local"), "Microsoft", "WinGet", "Links", "act.exe");
        return cli.Environment.IsWindows && File.Exists(linked) ? linked : null;
    }
}
