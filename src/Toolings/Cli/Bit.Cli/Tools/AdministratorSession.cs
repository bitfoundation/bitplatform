using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public sealed class AdministratorSession : IAsyncDisposable
{
    public const string CommandName = "admin-helper";

    private readonly CliServices cli;
    private readonly NamedPipeServerStream pipe;
    private readonly Process helper;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;
    private readonly SemaphoreSlim running = new(1, 1);
    private readonly SemaphoreSlim writing = new(1, 1);

    private AdministratorSession(CliServices cli, NamedPipeServerStream pipe, Process helper)
    {
        this.cli = cli;
        this.pipe = pipe;
        this.helper = helper;
        reader = new StreamReader(pipe);
        writer = new StreamWriter(pipe) { AutoFlush = true };
    }

    internal static async Task<AdministratorSession?> AttachAsync(CliServices cli, NamedPipeServerStream pipe, Process helper, CancellationToken cancellationToken)
    {
        var session = new AdministratorSession(cli, pipe, helper);
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        waiting.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            if (await session.reader.ReadLineAsync(waiting.Token) is { } line && JsonSerializer.Deserialize<AdministratorResponse>(line) is { Ready: true })
            {
                cli.Log.Write($"admin-helper: process {helper.Id} is ready");
                return session;
            }
        }
        catch (Exception exp) when (exp is IOException or JsonException || exp is OperationCanceledException && cancellationToken.IsCancellationRequested is false)
        {
        }

        cli.Log.Write($"admin-helper: process {helper.Id} stopped before it was ready; its own log is in {cli.Environment.LogsDirectory}");
        await session.DisposeAsync();
        return null;
    }

    public static async Task<(AdministratorSession? Session, bool Declined)> StartAsync(CliServices cli, CancellationToken cancellationToken)
    {
        var name = $"bit-cli-admin-{Guid.NewGuid():N}";
        var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.FirstPipeInstance | PipeOptions.CurrentUserOnly);
        Process? helper = null;

        try
        {
            helper = Process.Start(HelperStartInfo(name));

            if (helper is null)
            {
                await pipe.DisposeAsync();
                return (null, false);
            }

            using var connecting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connecting.CancelAfter(TimeSpan.FromMinutes(2));
            await pipe.WaitForConnectionAsync(connecting.Token);

            if (GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var clientId) is false || clientId != helper.Id)
            {
                cli.Log.Write($"admin-helper: process {clientId} connected instead of {helper.Id}");
                throw new IOException("Another process answered the administrator helper's pipe.");
            }

            return (await AttachAsync(cli, pipe, helper, cancellationToken), false);
        }
        catch (Win32Exception exp) when (exp.NativeErrorCode == 1223)
        {
            await pipe.DisposeAsync();
            return (null, true);
        }
        catch (Exception exp) when (exp is IOException or OperationCanceledException && cancellationToken.IsCancellationRequested is false)
        {
            cli.Log.Write($"admin-helper: {exp.Message}");
            Stop(helper);
            await pipe.DisposeAsync();
            return (null, false);
        }
    }

    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken)
    {
        await running.WaitAsync(cancellationToken);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var output = new System.Text.StringBuilder();

            var environment = new Dictionary<string, string?>(spec.Environment ?? new Dictionary<string, string?>()) { ["PATH"] = cli.Environment.GetVariable("PATH") };
            var fileName = cli.Runner.FindExecutable(spec.FileName) ?? spec.FileName;

            await SendAsync(new AdministratorRequest { Run = new AdministratorCommand(fileName, [.. spec.Arguments], spec.WorkingDirectory, environment, (int?)spec.Timeout?.TotalSeconds) });

            using var cancelling = cancellationToken.Register(() => _ = SendAsync(new AdministratorRequest { Cancel = true }));

            while (await reader.ReadLineAsync(CancellationToken.None) is { } line)
            {
                var response = JsonSerializer.Deserialize<AdministratorResponse>(line);

                if (response?.Line is { } text)
                {
                    output.AppendLine(text);
                    spec.OnOutputLine?.Invoke(text);
                }
                else if (response?.Exit is { } exitCode)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new ProcessResult { ExitCode = exitCode, Output = output.ToString(), Duration = stopwatch.Elapsed, NotFound = response.NotFound, TimedOut = response.TimedOut, Spec = spec };
                }
            }

            return new ProcessResult { ExitCode = -1, Output = output + "The administrator helper stopped.", Duration = stopwatch.Elapsed, Spec = spec };
        }
        finally
        {
            running.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await SendAsync(new AdministratorRequest { Quit = true });
            await helper.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception exp) when (exp is IOException or TimeoutException or ObjectDisposedException)
        {
            Stop(helper);
        }

        helper.Dispose();
        await pipe.DisposeAsync();
    }

    public static async Task<int> ServeAsync(CliServices cli, string pipeName, int parentId, CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(TimeSpan.FromSeconds(30), cancellationToken);

        if (GetNamedPipeServerProcessId(pipe.SafePipeHandle, out var serverId) is false || serverId != parentId)
        {
            cli.Log.Write($"admin-helper: the pipe belongs to process {serverId}, not {parentId}");
            return CliApp.ExitFailed;
        }

        if (!OperatingSystem.IsWindows() || !OwnedByThisUser(pipe))
        {
            cli.Log.Write("admin-helper: the pipe belongs to another user");
            return CliApp.ExitFailed;
        }

        using var parent = Process.GetProcessById(parentId);
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = parent.WaitForExitAsync(stopping.Token).ContinueWith(_ =>
        {
            try
            {
                stopping.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }, TaskScheduler.Default);

        using var reader = new StreamReader(pipe);
        var writer = new StreamWriter(pipe) { AutoFlush = true };
        var writing = new Lock();
        CancellationTokenSource? cancel = null;
        Task? current = null;

        void Send(AdministratorResponse response)
        {
            lock (writing)
            {
                try
                {
                    writer.WriteLine(JsonSerializer.Serialize(response));
                }
                catch (IOException)
                {
                }
            }
        }

        Send(new AdministratorResponse { Ready = true });

        try
        {
            while (await reader.ReadLineAsync(stopping.Token) is { } line && JsonSerializer.Deserialize<AdministratorRequest>(line) is { Quit: false } request)
            {
                if (request.Cancel)
                {
                    cancel?.Cancel();
                }
                else if (request.Run is { } command)
                {
                    if (current is not null)
                    {
                        await current;
                    }

                    cancel?.Dispose();
                    cancel = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
                    var token = cancel.Token;

                    current = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await cli.Runner.RunAsync(new ProcessSpec
                            {
                                FileName = command.FileName,
                                Arguments = command.Arguments,
                                WorkingDirectory = command.WorkingDirectory,
                                Environment = command.Environment,
                                Timeout = command.TimeoutSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
                                OnOutputLine = text => Send(new AdministratorResponse { Line = text })
                            }, token);

                            Send(new AdministratorResponse { Exit = result.ExitCode, NotFound = result.NotFound, TimedOut = result.TimedOut });
                        }
                        catch (OperationCanceledException)
                        {
                            Send(new AdministratorResponse { Exit = ToolInstaller.CancelledCode });
                        }
                    }, CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancel?.Cancel();

            if (current is not null)
            {
                await current;
            }

            cancel?.Dispose();
            await stopping.CancelAsync();
        }

        return CliApp.ExitOk;
    }

    private static ProcessStartInfo HelperStartInfo(string pipeName)
    {
        var host = Environment.ProcessPath ?? "dotnet";
        var arguments = $"{CommandName} --pipe {pipeName} --parent {Environment.ProcessId}";

        if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            arguments = $"{ProcessSpec.Quote(typeof(AdministratorSession).Assembly.Location)} {arguments}";
        }

        return new ProcessStartInfo(host) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden, Arguments = arguments };
    }

    private Task SendAsync(AdministratorRequest request) => SendAsync(writer, writing, request);

    private static async Task SendAsync(StreamWriter writer, SemaphoreSlim writing, AdministratorRequest request)
    {
        await writing.WaitAsync();

        try
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(request));
        }
        catch (Exception exp) when (exp is IOException or ObjectDisposedException)
        {
        }
        finally
        {
            writing.Release();
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool OwnedByThisUser(PipeStream pipe)
    {
        using var identity = WindowsIdentity.GetCurrent();
        return pipe.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) is SecurityIdentifier owner && owner == identity.User;
    }

    private static void Stop(Process? process)
    {
        try
        {
            if (process is { HasExited: false })
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exp) when (exp is Win32Exception or InvalidOperationException or NotSupportedException)
        {
        }
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out int clientProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out int serverProcessId);
}

public sealed record AdministratorCommand(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory, IReadOnlyDictionary<string, string?>? Environment, int? TimeoutSeconds);

public sealed record AdministratorRequest
{
    public AdministratorCommand? Run { get; init; }

    public bool Cancel { get; init; }

    public bool Quit { get; init; }
}

public sealed record AdministratorResponse
{
    public bool Ready { get; init; }

    public string? Line { get; init; }

    public int? Exit { get; init; }

    public bool NotFound { get; init; }

    public bool TimedOut { get; init; }
}
