using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Bit.Cli.Infrastructure;

public sealed record ProcessSpec
{
    public required string FileName { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string? WorkingDirectory { get; init; }

    public IReadOnlyDictionary<string, string?>? Environment { get; init; }

    public bool Interactive { get; init; }

    public TimeSpan? Timeout { get; init; }

    public Action<string>? OnOutputLine { get; init; }

    public string CommandLine => string.Join(' ', new[] { FileName }.Concat(Arguments).Select(Quote));

    public static string Quote(string value)
    {
        if (value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '/' or '\\' or ':' or '=' or ',' or '@' or '+'))
            return value;

        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }
}

public sealed record ProcessResult
{
    public required int ExitCode { get; init; }

    public string Output { get; init; } = "";

    public TimeSpan Duration { get; init; }

    public bool NotFound { get; init; }

    public bool TimedOut { get; init; }

    public bool Succeeded => ExitCode == 0 && NotFound is false && TimedOut is false;

    public IEnumerable<string> OutputLines => Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'));
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default);

    bool StartDetached(ProcessSpec spec);

    string? FindExecutable(string name);
}

public sealed class ProcessRunner(CliEnvironment environment, CliLog log) : IProcessRunner
{
    private const int MaxCapturedCharacters = 512 * 1024;

    public string? FindExecutable(string name)
    {
        if (Path.IsPathRooted(name))
            return File.Exists(name) ? name : null;

        var extensions = environment.IsWindows
            ? (environment.GetVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries).Prepend("")
            : [""];

        var directories = (environment.GetVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in directories)
        {
            foreach (var extension in extensions)
            {
                if (environment.IsWindows && extension.Length == 0 && Path.HasExtension(name) is false)
                    continue;

                try
                {
                    var candidate = Path.Combine(directory.Trim('"'), name + extension);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                }
            }
        }

        return null;
    }

    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var executable = FindExecutable(spec.FileName) ?? spec.FileName;

        log.Write($"> {spec.CommandLine}{(spec.WorkingDirectory is null ? "" : $"   (in {spec.WorkingDirectory})")}");

        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = spec.WorkingDirectory ?? environment.CurrentDirectory,
            RedirectStandardOutput = spec.Interactive is false,
            RedirectStandardError = spec.Interactive is false,
            RedirectStandardInput = spec.Interactive is false,
            StandardOutputEncoding = spec.Interactive ? null : Encoding.UTF8,
            StandardErrorEncoding = spec.Interactive ? null : Encoding.UTF8,
            CreateNoWindow = spec.Interactive is false
        };

        PrepareStart(startInfo, spec);

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var outputLock = new object();

        void OnData(string? line)
        {
            if (line is null)
                return;

            lock (outputLock)
            {
                if (output.Length < MaxCapturedCharacters)
                {
                    output.AppendLine(line);
                }
            }

            log.Write(line);
            spec.OnOutputLine?.Invoke(line);
        }

        process.OutputDataReceived += (_, e) => OnData(e.Data);
        process.ErrorDataReceived += (_, e) => OnData(e.Data);

        try
        {
            if (process.Start() is false)
                return NotFound(spec, stopwatch);
        }
        catch (Win32Exception)
        {
            return NotFound(spec, stopwatch);
        }

        if (spec.Interactive is false)
        {
            process.StandardInput.Close();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        using var timeoutSource = spec.Timeout is { } timeout ? new CancellationTokenSource(timeout) : new CancellationTokenSource();
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);

            if (cancellationToken.IsCancellationRequested)
                throw;

            log.Write($"  timed out after {stopwatch.Elapsed}");
            return new ProcessResult { ExitCode = -1, Output = output.ToString(), Duration = stopwatch.Elapsed, TimedOut = true };
        }

        log.Write($"  exited with {process.ExitCode} in {stopwatch.Elapsed.TotalSeconds:0.0}s");

        lock (outputLock)
        {
            return new ProcessResult { ExitCode = process.ExitCode, Output = output.ToString(), Duration = stopwatch.Elapsed };
        }
    }

    public bool StartDetached(ProcessSpec spec)
    {
        log.Write($"> {spec.CommandLine}   (detached)");

        var startInfo = new ProcessStartInfo(FindExecutable(spec.FileName) ?? spec.FileName)
        {
            UseShellExecute = false,
            WorkingDirectory = spec.WorkingDirectory ?? environment.CurrentDirectory,
            CreateNoWindow = true
        };

        PrepareStart(startInfo, spec);

        try
        {
            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (Win32Exception)
        {
            log.Write($"  {spec.FileName} was not found");
            return false;
        }
    }

    private void PrepareStart(ProcessStartInfo startInfo, ProcessSpec spec)
    {
        foreach (var argument in spec.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var variable in CliEnvironment.InProcessOnlyVariables)
        {
            if (environment.Variables.TryGetValue(variable, out var original))
            {
                startInfo.Environment[variable] = original;
            }
            else
            {
                startInfo.Environment.Remove(variable);
            }
        }

        if (spec.Environment is not null)
        {
            foreach (var (key, value) in spec.Environment)
            {
                if (value is null)
                {
                    startInfo.Environment.Remove(key);
                }
                else
                {
                    startInfo.Environment[key] = value;
                }
            }
        }
    }

    private ProcessResult NotFound(ProcessSpec spec, Stopwatch stopwatch)
    {
        log.Write($"  {spec.FileName} was not found");
        return new ProcessResult { ExitCode = -1, NotFound = true, Duration = stopwatch.Elapsed };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (process.HasExited is false)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exp) when (exp is InvalidOperationException or Win32Exception or NotSupportedException)
        {
        }
    }
}
