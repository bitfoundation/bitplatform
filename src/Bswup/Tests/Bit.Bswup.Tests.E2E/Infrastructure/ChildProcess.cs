using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// A process the suite starts and owns: its output is drained (a full pipe would stall it) into a
/// bounded buffer that failure messages quote, and disposing kills the whole process tree.
/// </summary>
public sealed class ChildProcess : IAsyncDisposable
{
    private const int KeptOutputLines = 200;

    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output = new();

    private ChildProcess(Process process) => _process = process;

    public bool HasExited => _process.HasExited;

    /// <summary>The last lines the process wrote, for failure messages.</summary>
    public string RecentOutput => string.Join(Environment.NewLine, _output);

    public static ChildProcess Start(string fileName, IEnumerable<string> arguments, string workingDirectory, IDictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        foreach (var (name, value) in environment ?? new Dictionary<string, string>()) startInfo.Environment[name] = value;

        var child = new ChildProcess(new Process { StartInfo = startInfo });
        child._process.OutputDataReceived += (_, e) => child.Keep(e.Data);
        child._process.ErrorDataReceived += (_, e) => child.Keep(e.Data);

        if (child._process.Start() is false)
            throw new InvalidOperationException($"Failed to start {fileName}.");

        child._process.BeginOutputReadLine();
        child._process.BeginErrorReadLine();
        return child;
    }

    /// <summary>Runs a process to completion and throws, quoting its output, when it fails.</summary>
    public static async Task RunToCompletionAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout)
    {
        await using var child = Start(fileName, arguments, workingDirectory);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await child._process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"{fileName} {string.Join(' ', arguments)} did not finish within {timeout}.{Environment.NewLine}{child.RecentOutput}");
        }

        if (child._process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} {string.Join(' ', arguments)} exited with {child._process.ExitCode}.{Environment.NewLine}{child.RecentOutput}");
    }

    /// <summary>Polls <paramref name="url"/> until it answers with a success status.</summary>
    public async Task WaitForHttpAsync(string url, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (HasExited)
                throw new InvalidOperationException($"The process exited before {url} answered.{Environment.NewLine}{RecentOutput}");

            try
            {
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            catch (TaskCanceledException) { /* not answering yet */ }

            await Task.Delay(250);
        }

        throw new TimeoutException($"{url} did not answer within {timeout}.{Environment.NewLine}{RecentOutput}");
    }

    public static int FreePort()
    {
        // Bind to port 0 and release it: the OS hands back a free ephemeral port.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_process.HasExited is false)
            {
                _process.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _process.WaitForExitAsync(cts.Token);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            // Best-effort: the process may already be gone, or refuse to die before the run ends.
        }
        finally
        {
            _process.Dispose();
        }
    }

    private void Keep(string? line)
    {
        if (line is null) return;

        _output.Enqueue(line);
        while (_output.Count > KeptOutputLines) _output.TryDequeue(out _);
    }
}
