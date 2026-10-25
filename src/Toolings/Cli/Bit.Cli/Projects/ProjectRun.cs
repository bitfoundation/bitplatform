using System.Runtime.InteropServices;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Projects;

public enum ProjectRunMode
{
    None,
    Once,
    UntilClosed
}

public static class ProjectRun
{
    public static ProjectRunMode Mode(CliServices cli, bool aspire, bool noBuild, bool noRun)
    {
        if (noBuild || noRun || cli.Environment.IsCI)
            return ProjectRunMode.None;

        if (cli.HasTerminal && cli.Environment.CodingAgent is null && cli.Environment.IsDevContainer is false)
            return ProjectRunMode.UntilClosed;

        return aspire ? ProjectRunMode.Once : ProjectRunMode.None;
    }

    public static string Title(ProjectRunMode mode, bool aspire) => mode is ProjectRunMode.Once
        ? "Starting the project once with Aspire"
        : aspire ? "Starting the project with Aspire" : "Starting the project";

    public static string PlanText(ProjectRunMode mode, bool aspire) => mode switch
    {
        ProjectRunMode.Once => "a first Aspire start",
        _ => aspire ? "start it with Aspire and open the dashboard and the app" : "start it and open it in the browser"
    };

    public static async Task RunProjectAsync(CliServices cli, StepRunner steps, ProjectContext context, ProjectSteps projectSteps, ProjectRunMode mode, bool aspire, CancellationToken cancellationToken)
    {
        var title = Title(mode, aspire);

        if (steps.Reports.Any(r => r.Id is "build" && r.Result.Status is StepStatus.Failed))
        {
            steps.Add("run", StepResult.Skipped("Didn't start the project", "the build failed"));
            return;
        }

        await NewWorkflow.RunProjectStepAsync(steps, context, "run", title, aspire
            ? (progress, ct) => projectSteps.AspireStartAsync(mode is ProjectRunMode.UntilClosed, progress, ct)
            : projectSteps.DotnetRunAsync, cancellationToken);
    }

    public static async Task KeepRunningAsync(CliServices cli, RunningProject running, CancellationToken cancellationToken)
    {
        var urls = new[] { running.DashboardUrl, running.AppUrl }.OfType<string>().ToList();

        foreach (var url in urls)
        {
            OpenInBrowser(cli, url);
        }

        cli.Console.Out.WriteLine();

        for (var i = 0; i < urls.Count; i++)
        {
            var label = urls[i] == running.DashboardUrl ? "  [grey]Aspire dashboard[/]" : "  [grey]The app[/]";
            cli.Console.Out.MarkupLine($"{(i == 0 ? "  [bold]Running[/]  " : "           ")}{Markup.Escape(urls[i])}{label}");
        }

        cli.Console.Out.MarkupLine("  [grey]It keeps running while this terminal is open. Close it, or press Ctrl+C, to stop the project.[/]");
        cli.Console.Out.WriteLine();

        using var closed = new CancellationTokenSource();
        var terminalClosed = false;

        using var hangUp = PosixSignalRegistration.Create(PosixSignal.SIGHUP, signal =>
        {
            signal.Cancel = true;
            terminalClosed = true;
            running.StopAsync(cli.Environment.IsWindows).Wait(TimeSpan.FromSeconds(cli.Environment.IsWindows ? 4 : 60));
            closed.Cancel();
        });

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, closed.Token);

        var waiting = Task.Delay(Timeout.Infinite, stop.Token);

        if (await Task.WhenAny(waiting, running.Exited ?? waiting) != waiting && stop.IsCancellationRequested is false)
        {
            cli.Console.Out.MarkupLine("  [yellow]The project stopped.[/]");
            await running.StopAsync(false);
            return;
        }

        if (terminalClosed)
            return;

        cli.Console.Out.MarkupLine("  [grey]Stopping the project[/]");
        await running.StopAsync(true);
    }

    public static void OpenInBrowser(CliServices cli, string url)
    {
        ProcessSpec? open = cli.Environment.Os switch
        {
            HostOs.Windows => new ProcessSpec { FileName = "rundll32.exe", Arguments = ["url.dll,FileProtocolHandler", url] },
            HostOs.MacOS => new ProcessSpec { FileName = "open", Arguments = [url] },
            _ when cli.Runner.FindExecutable("xdg-open") is not null => new ProcessSpec { FileName = "xdg-open", Arguments = [url] },
            _ => null
        };

        if (open is not null)
        {
            cli.Runner.StartDetached(open);
        }
    }
}
