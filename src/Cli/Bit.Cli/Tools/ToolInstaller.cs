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

            if (succeeded)
                return StepResult.Succeeded(done, hint: action.AfterInstall) with { Status = action.AfterInstall is null ? StepStatus.Succeeded : StepStatus.Warning, Detail = action.AfterInstall };

            if (action.Partial is { } partial && last is { NotFound: false, TimedOut: false } && last.ExitCode == partial.ExitCode)
                return StepResult.Succeeded(done, partial.Detail, partial.Hint);

            var failed = StepResult.FromProcess(last ?? new ProcessResult { ExitCode = -1 }, done, $"Couldn't {char.ToLowerInvariant(action.Title[0])}{action.Title[1..]}", string.Join(" && ", action.Commands.Select(c => (useSudo ? "sudo " : "") + c.CommandLine)));
            return action.Optional ? failed with { Status = StepStatus.Warning } : failed;
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
        var cancelPath = Path.Combine(directory, "cancel");
        var script = Encoding.UTF8.GetBytes(BuildWindowsScript(actions, logPath, resultPath, cancelPath));

        File.WriteAllBytes(scriptPath, script);
        cli.Log.Write($"Administrator script {scriptPath}:{Environment.NewLine}{Encoding.UTF8.GetString(script)}");

        var arguments = $"-NoProfile -NonInteractive -EncodedCommand {EncodeBootstrap(scriptPath, script)}";

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

        var timeout = TimeSpan.FromTicks(actions.Sum(a => a.Commands.Sum(c => (c.Timeout ?? TimeSpan.FromMinutes(30)).Ticks))) + TimeSpan.FromMinutes(5);

        await cli.Console.RunWithStatusAsync(title, async progress =>
        {
            using var tail = new CancellationTokenSource();
            var tailing = TailAsync(logPath, progress, tail.Token);
            using var cancelled = cancellationToken.Register(() => TryWriteCancel(cancelPath));

            try
            {
                if (cli.Environment.IsElevated)
                {
                    await cli.Runner.RunAsync(new ProcessSpec { FileName = "powershell.exe", Arguments = ["-NoProfile", "-NonInteractive", "-EncodedCommand", EncodeBootstrap(scriptPath, script)], Timeout = timeout }, cancellationToken);
                }
                else
                {
                    using var process = Process.Start(new ProcessStartInfo("powershell.exe")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Minimized,
                        Arguments = arguments
                    });

                    if (process is not null)
                    {
                        try
                        {
                            await process.WaitForExitAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
                        }
                        catch (Exception exp) when (exp is OperationCanceledException or TimeoutException)
                        {
                            TryWriteCancel(cancelPath);

                            try
                            {
                                await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromMinutes(1), CancellationToken.None);
                            }
                            catch (TimeoutException)
                            {
                            }

                            if (exp is OperationCanceledException)
                                throw;
                        }
                    }
                }
            }
            catch (Win32Exception exp) when (exp.NativeErrorCode == 1223)
            {
                declined = true;
            }
            finally
            {
                await tail.CancelAsync();
                await tailing;
            }

            return 0;
        });

        if (File.Exists(logPath))
        {
            cli.Log.Write(File.ReadAllText(logPath));
        }

        var exitCodes = ReadResults(resultPath);

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }

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
                var detail = exitCodes.TryGetValue(action.ToolId, out var code)
                    ? code switch { TimedOutCode => "it took too long and was stopped", CancelledCode => "it was cancelled", _ => $"exit code {code}" }
                    : "it didn't run";

                steps.Add(action.ToolId, StepResult.Failed($"Couldn't {char.ToLowerInvariant(action.Title[0])}{action.Title[1..]}", detail, followUp));
            }
        }
    }

    public const int TimedOutCode = -2;

    public const int CancelledCode = -3;

    public static string BuildWindowsScript(IReadOnlyList<ToolAction> actions, string logPath, string resultPath, string cancelPath)
    {
        static string Quote(string value) => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";

        var script = new StringBuilder();
        script.AppendLine("$ErrorActionPreference = 'Continue'");
        script.AppendLine("$ProgressPreference = 'SilentlyContinue'");
        script.AppendLine("$results = [ordered]@{}");
        script.AppendLine($"$cancelPath = {Quote(cancelPath)}");
        script.AppendLine($"$stoppedPath = {Quote(cancelPath + ".stopped")}");
        script.AppendLine($"function Write-BitLog([string]$text) {{ Add-Content -LiteralPath {Quote(logPath)} -Value $text -Encoding UTF8 }}");
        script.AppendLine("function Start-BitWatchdog([int]$Seconds) {");
        script.AppendLine("    Start-Job -ArgumentList $PID, $Seconds, $cancelPath, $stoppedPath -ScriptBlock {");
        script.AppendLine("        param($ParentId, $Seconds, $CancelPath, $StoppedPath)");
        script.AppendLine("        $deadline = (Get-Date).AddSeconds($Seconds)");
        script.AppendLine("        while ((Get-Date) -lt $deadline -and -not (Test-Path -LiteralPath $CancelPath)) { Start-Sleep -Milliseconds 500 }");
        script.AppendLine("        Set-Content -LiteralPath $StoppedPath -Value $(if (Test-Path -LiteralPath $CancelPath) { 'cancelled' } else { 'timed out' })");
        script.AppendLine("        Get-CimInstance Win32_Process -Filter \"ParentProcessId=$ParentId\" | Where-Object { $_.ProcessId -ne $PID -and $_.Name -ne 'conhost.exe' } | ForEach-Object { & taskkill.exe /T /F /PID $_.ProcessId | Out-Null }");
        script.AppendLine("    }");
        script.AppendLine("}");

        foreach (var action in actions)
        {
            var successCodes = string.Join(", ", action.SuccessExitCodes);
            script.AppendLine($"Write-BitLog {Quote("== " + action.Title)}");
            script.AppendLine($"$code = if (Test-Path -LiteralPath $cancelPath) {{ {CancelledCode} }} else {{ 0 }}");
            script.AppendLine("try {");

            foreach (var command in action.Commands)
            {
                var seconds = (int)(command.Timeout ?? TimeSpan.FromMinutes(30)).TotalSeconds;

                script.AppendLine($"    if (@({successCodes}) -contains $code) {{");
                script.AppendLine($"        $watchdog = Start-BitWatchdog {seconds}");
                script.AppendLine($"        & {Quote(command.FileName)} {string.Join(' ', command.Arguments.Select(Quote))} 2>&1 | ForEach-Object {{ Write-BitLog \"$_\" }}");
                script.AppendLine("        $code = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }");
                script.AppendLine("        Stop-Job $watchdog; Remove-Job $watchdog -Force");
                script.AppendLine("        if (Test-Path -LiteralPath $stoppedPath) {");
                script.AppendLine("            $why = (Get-Content -LiteralPath $stoppedPath -Raw).Trim(); Remove-Item -LiteralPath $stoppedPath");
                script.AppendLine($"            Write-BitLog \"stopped: $why\"; $code = if ($why -eq 'cancelled') {{ {CancelledCode} }} else {{ {TimedOutCode} }}");
                script.AppendLine("        }");
                script.AppendLine("    }");
            }

            script.AppendLine("} catch { Write-BitLog \"$_\"; $code = -1 }");
            script.AppendLine($"$results[{Quote(action.ToolId)}] = $code");
        }

        script.AppendLine($"$results | ConvertTo-Json | Set-Content -LiteralPath {Quote(resultPath)} -Encoding UTF8");
        return script.ToString();
    }

    public static string EncodeBootstrap(string scriptPath, byte[] script)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(script));
        var bootstrap = $"$b = [IO.File]::ReadAllBytes('{scriptPath.Replace("'", "''", StringComparison.Ordinal)}'); " +
            $"if ([BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($b)).Replace('-', '') -ne '{hash}') {{ exit 87 }}; " +
            "& ([scriptblock]::Create([Text.Encoding]::UTF8.GetString($b)))";

        return Convert.ToBase64String(Encoding.Unicode.GetBytes(bootstrap));
    }

    private static void TryWriteCancel(string cancelPath)
    {
        try
        {
            File.WriteAllText(cancelPath, "cancel");
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
        }
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

    private static async Task TailAsync(string path, Action<string> progress, CancellationToken cancellationToken)
    {
        long position = 0;

        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    stream.Seek(position, SeekOrigin.Begin);
                    using var reader = new StreamReader(stream);
                    var text = await reader.ReadToEndAsync(CancellationToken.None);
                    position = stream.Position;

                    var last = text.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
                    if (last is not null)
                    {
                        progress(last);
                    }
                }

                await Task.Delay(500, cancellationToken);
            }
            catch (IOException)
            {
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
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
