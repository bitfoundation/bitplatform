using System.CommandLine;
using Bit.Cli.Infrastructure;
using Bit.Cli.Trust;
using Spectre.Console;

namespace Bit.Cli.Commands;

public static class TrustCommand
{
    public static Command Create(Func<CliServices> services)
    {
        var pathArgument = new Argument<string?>("path") { Description = "The folder to trust. Default: the git repository around the current folder, or the folder itself.", Arity = ArgumentArity.ZeroOrOne };

        var command = new Command("trust", "Mark a project folder as trusted for VS Code, Claude Code, Copilot CLI, Codex and Gemini CLI, so its tasks and MCP servers run without prompts.")
        {
            pathArgument
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var cli = services();
            var folder = Path.GetFullPath(parseResult.GetValue(pathArgument) ?? ".", cli.Environment.CurrentDirectory);

            if (Directory.Exists(folder) is false)
            {
                cli.Console.Fail($"{folder} doesn't exist.");
                return CliApp.ExitUsage;
            }

            if (parseResult.GetValue(pathArgument) is null)
            {
                var gitRoot = await cli.Runner.RunAsync(new ProcessSpec { FileName = "git", Arguments = ["rev-parse", "--show-toplevel"], WorkingDirectory = folder }, cancellationToken);

                if (gitRoot.Succeeded && Directory.Exists(gitRoot.Output.Trim()))
                {
                    folder = Path.GetFullPath(gitRoot.Output.Trim());
                }
            }

            cli.Console.Out.MarkupLine($"Trusting [bold]{Markup.Escape(folder)}[/]");

            var context = new TrustContext(cli.Environment, cli.Runner);
            var failed = false;

            foreach (var writer in TrustWriters.All)
            {
                var outcome = writer.Trust(context, folder);
                cli.Log.Write($"trust {outcome.Tool}: {outcome.Kind} {outcome.Detail}");

                var (status, text) = outcome.Kind switch
                {
                    TrustResultKind.Trusted => (StepStatus.Succeeded, $"Trusted for {outcome.Tool}"),
                    TrustResultKind.AlreadyTrusted => (StepStatus.Succeeded, $"Already trusted for {outcome.Tool}"),
                    TrustResultKind.NotInstalled => (StepStatus.Skipped, $"{outcome.Tool} isn't installed"),
                    TrustResultKind.Skipped => (StepStatus.Warning, $"Skipped {outcome.Tool}"),
                    _ => (StepStatus.Failed, $"Couldn't trust for {outcome.Tool}")
                };

                failed |= outcome.Kind is TrustResultKind.Failed;
                cli.Console.Step(status, text, outcome.Kind is TrustResultKind.Trusted ? null : outcome.Detail);
            }

            cli.Console.Out.MarkupLine("[grey]VS Code keeps its own trust store while it runs; if it was skipped, close VS Code and run bit trust again, or click Trust once.[/]");
            return failed ? CliApp.ExitFailed : CliApp.ExitOk;
        });

        return command;
    }
}
