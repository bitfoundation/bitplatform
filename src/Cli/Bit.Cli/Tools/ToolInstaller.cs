using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Bit.Cli.Infrastructure;
using Spectre.Console;

namespace Bit.Cli.Tools;

public sealed class ToolInstaller(CliServices cli, StepRunner steps)
{
    public async Task InstallAsync(IReadOnlyList<ToolCheck> selected, ToolContext context, CancellationToken cancellationToken)
    {
        var withActions = selected.Where(c => c.Action is not null).ToList();

        foreach (var manual in selected.Where(c => c.Action is null))
        {
            steps.Add(manual.Tool.Id, StepResult.Warning($"Install {manual.Tool.Name} yourself", manual.Manual ?? "bit can't install it on this machine", manual.Manual));
        }

        if (withActions.Count == 0)
            return;

        if (cli.Environment.IsWindows)
        {
            var adminActions = withActions.Select(c => c.Action!).Where(a => a.Elevation is Elevation.Admin).ToList();

            if (adminActions.Count > 0)
            {
                await RunWindowsAdministratorBatchAsync(adminActions, cancellationToken);
            }

            foreach (var action in withActions.Select(c => c.Action!).Where(a => a.Elevation is not Elevation.Admin))
            {
                await RunActionAsync(action, useSudo: false, cancellationToken);
            }
        }
        else
        {
            var ordered = withActions.OrderByDescending(c => c.Tool.Id is "homebrew").ToList();
            var sudoReady = ordered.Any(c => c.Action!.Elevation is Elevation.Sudo) is false || await PrepareSudoAsync(ordered.Select(c => c.Action!).Where(a => a.Elevation is Elevation.Sudo).ToList(), cancellationToken);

            foreach (var check in ordered)
            {
                var action = check.Action!;

                if (check.Tool.Id is not "homebrew" && context.Environment.IsMacOS && cli.Runner.FindExecutable("brew") is null)
                {
                    RefreshPath();
                    var replanned = check.Tool.PlanInstall(new ToolContext(context.Environment, context.Runner, context.Needs, PackageManagers.Detect(context.Environment, context.Runner)), check.Status);
                    action = replanned ?? action;
                }

                if (action.Elevation is Elevation.Sudo && sudoReady is false)
                {
                    var commands = string.Join(" && ", action.Commands.Select(c => "sudo " + c.CommandLine));
                    steps.Add(action.ToolId, StepResult.Warning($"{action.Title} needs sudo", "run it yourself", commands, commands));
                    continue;
                }

                await RunActionAsync(action, useSudo: action.Elevation is Elevation.Sudo && cli.Environment.IsElevated is false, cancellationToken);

                if (action.ToolId is "homebrew")
                {
                    RefreshPath();
                }
            }
        }

        RefreshPath();
    }

    private async Task RunActionAsync(ToolAction action, bool useSudo, CancellationToken cancellationToken)
    {
        var interactive = action.Interactive || action.Commands.Any(c => c.Interactive);

        await steps.RunAsync(action.ToolId, action.Title, async (progress, ct) =>
        {
            ProcessResult? last = null;

            foreach (var command in action.Commands)
            {
                var spec = command with { Interactive = interactive, OnOutputLine = progress };

                if (useSudo)
                {
                    spec = spec with { FileName = "sudo", Arguments = ["-n", command.FileName, .. command.Arguments] };
                }

                last = await cli.Runner.RunAsync(spec, ct);

                if (action.SuccessExitCodes.Contains(last.ExitCode) is false || last.NotFound || last.TimedOut)
                    break;
            }

            var succeeded = last is not null && action.SuccessExitCodes.Contains(last.ExitCode) && last.NotFound is false && last.TimedOut is false;
            var done = DoneTitle(action.Title);

            return succeeded
                ? StepResult.Succeeded(done, hint: action.AfterInstall) with { Status = action.AfterInstall is null ? StepStatus.Succeeded : StepStatus.Warning, Detail = action.AfterInstall }
                : StepResult.FromProcess(last ?? new ProcessResult { ExitCode = -1 }, done, $"Couldn't {char.ToLowerInvariant(action.Title[0])}{action.Title[1..]}", string.Join(" && ", action.Commands.Select(c => (useSudo ? "sudo " : "") + c.CommandLine)));
        }, cancellationToken, needsTerminal: interactive);
    }

    private async Task<bool> PrepareSudoAsync(IReadOnlyList<ToolAction> actions, CancellationToken cancellationToken)
    {
        if (cli.Environment.IsElevated)
            return true;

        if (cli.Runner.FindExecutable("sudo") is null)
            return false;

        var passwordless = await cli.Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-n", "true"], Timeout = TimeSpan.FromSeconds(10) }, cancellationToken);

        if (passwordless.Succeeded)
            return true;

        if (cli.HasTerminal is false)
            return false;

        cli.Console.Out.WriteLine();
        cli.Console.Out.MarkupLine("[bold]These run with sudo; your password is asked once:[/]");

        foreach (var command in actions.SelectMany(a => a.Commands))
        {
            cli.Console.Out.MarkupLine($"  [grey]sudo {Markup.Escape(command.CommandLine)}[/]");
        }

        var authenticated = await cli.Runner.RunAsync(new ProcessSpec { FileName = "sudo", Arguments = ["-v"], Interactive = true }, cancellationToken);
        cli.Console.Out.WriteLine();
        return authenticated.Succeeded;
    }

    private async Task RunWindowsAdministratorBatchAsync(IReadOnlyList<ToolAction> actions, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), "bit-cli", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var scriptPath = Path.Combine(directory, "install.ps1");
        var logPath = Path.Combine(directory, "install.log");
        var resultPath = Path.Combine(directory, "results.json");

        File.WriteAllText(scriptPath, BuildWindowsScript(actions, logPath, resultPath), new UTF8Encoding(true));
        cli.Log.Write($"Administrator script {scriptPath}:{Environment.NewLine}{File.ReadAllText(scriptPath)}");

        var declined = false;
        var title = cli.Environment.IsElevated
            ? $"Running {actions.Count} administrator step(s)"
            : "Waiting for administrator permission (Windows will ask once)";

        if (cli.Environment.IsElevated is false && cli.HasTerminal is false && cli.Environment.IsCI is false)
        {
            foreach (var action in actions)
            {
                var commands = string.Join(" && ", action.Commands.Select(c => c.CommandLine));
                steps.Add(action.ToolId, StepResult.Warning($"{action.Title} needs administrator rights", "run it in an elevated terminal", commands, commands));
            }

            return;
        }

        await cli.Console.RunWithStatusAsync(title, async progress =>
        {
            try
            {
                using var tail = StartTail(logPath, progress);

                if (cli.Environment.IsElevated)
                {
                    await cli.Runner.RunAsync(new ProcessSpec { FileName = "powershell.exe", Arguments = ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath], Timeout = TimeSpan.FromHours(2) }, cancellationToken);
                }
                else
                {
                    using var process = Process.Start(new ProcessStartInfo("powershell.exe")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\""
                    });

                    if (process is not null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                    }
                }
            }
            catch (Win32Exception exp) when (exp.NativeErrorCode == 1223)
            {
                declined = true;
            }

            return 0;
        });

        if (File.Exists(logPath))
        {
            cli.Log.Write(File.ReadAllText(logPath));
        }

        var exitCodes = ReadResults(resultPath);

        foreach (var action in actions)
        {
            var done = DoneTitle(action.Title);
            var followUp = string.Join(" && ", action.Commands.Select(c => c.CommandLine));

            if (declined)
            {
                steps.Add(action.ToolId, StepResult.Warning($"{action.Title} was skipped", "administrator permission was declined", followUp));
            }
            else if (exitCodes.TryGetValue(action.ToolId, out var exitCode) && action.SuccessExitCodes.Contains(exitCode))
            {
                steps.Add(action.ToolId, action.AfterInstall is null
                    ? StepResult.Succeeded(done)
                    : StepResult.Warning(done, action.AfterInstall));
            }
            else
            {
                steps.Add(action.ToolId, StepResult.Failed($"Couldn't {char.ToLowerInvariant(action.Title[0])}{action.Title[1..]}", exitCodes.TryGetValue(action.ToolId, out var code) ? $"exit code {code}" : "it didn't run", followUp));
            }
        }
    }

    public static string BuildWindowsScript(IReadOnlyList<ToolAction> actions, string logPath, string resultPath)
    {
        static string Quote(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

        var script = new StringBuilder();
        script.AppendLine("$ErrorActionPreference = 'Continue'");
        script.AppendLine("$ProgressPreference = 'SilentlyContinue'");
        script.AppendLine("$results = [ordered]@{}");
        script.AppendLine($"function Write-BitLog([string]$text) {{ Add-Content -LiteralPath {Quote(logPath)} -Value $text -Encoding UTF8 }}");

        foreach (var action in actions)
        {
            var successCodes = string.Join(", ", action.SuccessExitCodes);
            script.AppendLine($"Write-BitLog {Quote("== " + action.Title)}");
            script.AppendLine("$code = 0");
            script.AppendLine("try {");

            foreach (var command in action.Commands)
            {
                script.AppendLine($"    if (@({successCodes}) -contains $code) {{");
                script.AppendLine($"        & {Quote(command.FileName)} {string.Join(' ', command.Arguments.Select(Quote))} 2>&1 | ForEach-Object {{ Write-BitLog \"$_\" }}");
                script.AppendLine("        $code = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }");
                script.AppendLine("    }");
            }

            script.AppendLine("} catch { Write-BitLog \"$_\"; $code = -1 }");
            script.AppendLine($"$results[{Quote(action.ToolId)}] = $code");
        }

        script.AppendLine($"$results | ConvertTo-Json | Set-Content -LiteralPath {Quote(resultPath)} -Encoding UTF8");
        return script.ToString();
    }

    private static Dictionary<string, int> ReadResults(string resultPath)
    {
        try
        {
            if (File.Exists(resultPath) is false)
                return [];

            using var document = JsonDocument.Parse(File.ReadAllText(resultPath).TrimStart('﻿'));
            return document.RootElement.EnumerateObject()
                .Where(p => p.Value.ValueKind is JsonValueKind.Number)
                .ToDictionary(p => p.Name, p => p.Value.GetInt32());
        }
        catch (Exception exp) when (exp is JsonException or IOException)
        {
            return [];
        }
    }

    private static IDisposable StartTail(string path, Action<string> progress)
    {
        var cancellation = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            long position = 0;

            while (cancellation.IsCancellationRequested is false)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        stream.Seek(position, SeekOrigin.Begin);
                        using var reader = new StreamReader(stream);
                        var text = await reader.ReadToEndAsync();
                        position = stream.Position;

                        var last = text.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
                        if (last is not null)
                        {
                            progress(last);
                        }
                    }
                }
                catch (IOException)
                {
                }

                await Task.Delay(500);
            }
        });

        return cancellation;
    }

    private void RefreshPath()
    {
        var directories = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            foreach (var target in new[] { EnvironmentVariableTarget.Machine, EnvironmentVariableTarget.User })
            {
                directories.AddRange((Environment.GetEnvironmentVariable("Path", target) ?? "")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Environment.ExpandEnvironmentVariables));
            }
        }
        else
        {
            directories.AddRange(["/opt/homebrew/bin", "/usr/local/bin"]);
        }

        directories.Add(Path.Combine(cli.Environment.HomeDirectory, ".dotnet", "tools"));
        cli.Environment.PrependToPath(directories);
    }

    private static string DoneTitle(string title)
    {
        foreach (var (present, past) in new[] { ("Install ", "Installed "), ("Update ", "Updated "), ("Start ", "Started "), ("Enable ", "Enabled "), ("Trust ", "Trusted "), ("Turn on ", "Turned on ") })
        {
            if (title.StartsWith(present, StringComparison.Ordinal))
                return past + title[present.Length..];
        }

        return title;
    }
}
