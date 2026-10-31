using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bit.BlazeDb.Demo.Server.Dtos;
using ModelContextProtocol;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Runs <see cref="BlazeDbTableInspector"/> in a child process of this server, so what a submitted source
/// costs to compile is bounded where it is spent instead of guessed at from its syntax.
/// <para>
/// The compiler decides what a source costs, and the ways to make it expensive are open-ended: constants that
/// fold into gigabytes, spelled through names no syntax check can resolve, generic types that expand without
/// end, overload resolution that goes exponential. Recognizing each shape up front is a race against the
/// compiler that a check written from the syntax loses sooner or later. A child process bounds every one of
/// them at once: its GC heap is capped with <c>DOTNET_GCHeapHardLimit</c>, so a source that needs more fails
/// with an out-of-memory error in the child, and a child still busy when the inspection times out is killed.
/// Neither reaches the server.
/// </para>
/// <para>
/// A worker serves one inspection at a time and stays up for the next, so the compiler is loaded and warmed
/// once per worker rather than once per call. One that timed out, ran out of memory or failed is discarded,
/// and the next call starts a fresh one. A worker also ends on its own: when its input closes - which the
/// operating system does when the server exits, however it exits - and when one inspection runs far past the
/// time the server gives it.
/// </para>
/// </summary>
internal sealed class BlazeDbInspectionWorker : IDisposable
{
    /// <summary>The argument this server is started with to serve as a worker instead; see Program.cs.</summary>
    public const string Argument = "--blazedb-inspection-worker";

    /// <summary>
    /// A worker's GC heap: four times what compiling the MCP tests' whole model file takes, generated code
    /// included, and a fraction of what the server has.
    /// </summary>
    public const long MemoryLimit = 256L * 1024 * 1024;

    // The worker's own deadline for one inspection, past the server's: what ends a worker whose server is gone.
    private static readonly TimeSpan _ownTimeout = TimeSpan.FromMinutes(1);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly ConcurrentBag<BlazeDbInspectionWorker> _idle = [];

    private readonly Process _process;

    private BlazeDbInspectionWorker(Process process) => _process = process;

    /// <summary>
    /// Inspects <paramref name="source"/> in an idle worker, or a new one. A refusal - the source past a limit,
    /// or past the memory a worker has - is thrown as an <see cref="McpException"/> with the reason in it.
    /// </summary>
    public static async Task<BlazeDbTableInspectionDto> InspectAsync(string? source, bool includeGeneratedCode, CancellationToken cancellationToken)
    {
        var worker = TakeIdle() ?? Start();
        var reusable = false;

        try
        {
            // Pipes to a child process cannot be relied on to stop when a token is cancelled, so the wait is what
            // is cancelled; the worker is killed below, which ends the read and the write with it.
            var request = JsonSerializer.Serialize(new Request(source, includeGeneratedCode), _json);
            await worker._process.StandardInput.WriteLineAsync(request).WaitAsync(cancellationToken);
            await worker._process.StandardInput.FlushAsync().WaitAsync(cancellationToken);

            var line = await worker._process.StandardOutput.ReadLineAsync().WaitAsync(cancellationToken)
                // A crash rather than an out-of-memory error the worker could report: the runtime gives up
                // outright when it runs out of memory where it cannot throw.
                ?? throw new McpException($"The source was not inspected: compiling it stopped the inspector, most likely by needing more than {MemoryLimit / (1024 * 1024)} MB. " +
                                          "A table type needs nothing near that - send the type on its own.");

            var response = JsonSerializer.Deserialize<Response>(line, _json)!;
            reusable = response.Exiting is false;

            if (response.Inspection is { } inspection) return inspection;

            throw response.Refusal is { } refusal
                ? new McpException(refusal)
                : new InvalidOperationException("The inspection failed; the inspector wrote why to the server's error output.");
        }
        finally
        {
            if (reusable)
            {
                _idle.Add(worker);
            }
            else
            {
                worker.Dispose();
            }
        }
    }

    /// <summary>
    /// The worker's side: reads one request per line from standard input and writes one response per line to
    /// standard output, until the input closes.
    /// </summary>
    public static async Task ServeAsync()
    {
        // Standard output carries the responses and nothing else, so anything that writes to the console
        // writes to standard error instead.
        await using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        Console.SetOut(Console.Error);
        using var input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));

        while (await input.ReadLineAsync() is { } line)
        {
            var request = JsonSerializer.Deserialize<Request>(line, _json)!;
            Response response;

            using (new Timer(_ => Environment.Exit(1), null, _ownTimeout, Timeout.InfiniteTimeSpan))
            {
                try
                {
                    response = new(BlazeDbTableInspector.Inspect(request.Source, request.IncludeGeneratedCode, CancellationToken.None), null, Exiting: false);
                }
                catch (McpException refusal)
                {
                    response = new(null, refusal.Message, Exiting: false);
                }
                catch (OutOfMemoryException)
                {
                    // The heap is whole - only the allocation that would have gone past the limit failed - but a
                    // fresh worker is cheaper to trust than whatever the compiler cached halfway.
                    response = new(null, $"The source was not inspected: compiling it needed more than {MemoryLimit / (1024 * 1024)} MB. " +
                                         "A table type needs nothing near that - send the type on its own.", Exiting: true);
                }
                catch (Exception failure)
                {
                    await Console.Error.WriteLineAsync($"InspectBlazeDbTable failed: {failure}");
                    response = new(null, null, Exiting: true);
                }
            }

            await output.WriteLineAsync(JsonSerializer.Serialize(response, _json));

            if (response.Exiting) return;
        }
    }

    private static BlazeDbInspectionWorker? TakeIdle()
    {
        while (_idle.TryTake(out var worker))
        {
            if (worker._process.HasExited is false) return worker;

            worker.Dispose();
        }

        return null;
    }

    private static BlazeDbInspectionWorker Start()
    {
        var start = new ProcessStartInfo(DotnetHost())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false)
        };

        // This app's own assembly, on the runtime and the dependencies of the process that starts it: the
        // server's own when it is the server, those of whatever hosts it otherwise - a test project, which
        // can target a newer runtime than the server and carry newer builds of the assemblies it shares.
        var entry = (Assembly.GetEntryAssembly() ?? typeof(BlazeDbInspectionWorker).Assembly).Location;
        var dependencies = Path.ChangeExtension(entry, ".deps.json");

        start.ArgumentList.Add("exec");
        start.ArgumentList.Add("--runtimeconfig");
        start.ArgumentList.Add(Path.ChangeExtension(entry, ".runtimeconfig.json"));
        if (File.Exists(dependencies))
        {
            start.ArgumentList.Add("--depsfile");
            start.ArgumentList.Add(dependencies);
        }
        start.ArgumentList.Add(typeof(BlazeDbInspectionWorker).Assembly.Location);
        start.ArgumentList.Add(Argument);

        start.Environment["DOTNET_GCHeapHardLimit"] = $"0x{MemoryLimit:X}";
        // One heap for one inspection at a time, with the whole limit to itself, instead of the server GC an
        // ASP.NET Core app is configured with.
        start.Environment["DOTNET_gcServer"] = "0";

        return new BlazeDbInspectionWorker(Process.Start(start) ?? throw new InvalidOperationException("The inspector's process did not start."));
    }

    /// <summary>The <c>dotnet</c> host that starts a worker - the one this server runs on.</summary>
    private static string DotnetHost()
    {
        // Set by the SDK for whatever it starts - dotnet run, dotnet test.
        if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } hostPath && File.Exists(hostPath)) return hostPath;

        // `dotnet Bit.BlazeDb.Demo.Server.dll`, which is how a published server is started.
        if (Environment.ProcessPath is { } processPath && Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) return processPath;

        // Started through its own executable instead: the host of the installation the runtime is part of,
        // whose shared frameworks live under <root>/shared/Microsoft.NETCore.App/<version>/.
        var root = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));

        return Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
    }

    public void Dispose()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // It has exited already, or is exiting.
        }

        _process.Dispose();
    }

    private sealed record Request(string? Source, bool IncludeGeneratedCode);

    /// <summary>An inspection, or the refusal to send the caller, or neither when it failed; <c>Exiting</c> when the worker ends after it.</summary>
    private sealed record Response(BlazeDbTableInspectionDto? Inspection, string? Refusal, bool Exiting);
}
