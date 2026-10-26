using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.AotProfiling;

public sealed class DevToolsException(string message) : Exception(message);

public sealed class Chromium : IAsyncDisposable
{
    private readonly Process process;
    private readonly string userDataDirectory;

    private Chromium(Process process, string userDataDirectory, DevToolsConnection connection)
    {
        this.process = process;
        this.userDataDirectory = userDataDirectory;
        Connection = connection;
    }

    public DevToolsConnection Connection { get; }

    public static string? Find(CliEnvironment environment, Func<string, string?> findOnPath)
    {
        foreach (var browser in PlaywrightBrowsers(environment).Concat(InstalledBrowsers(environment)))
        {
            if (File.Exists(browser))
                return browser;
        }

        return environment.IsLinux
            ? new[] { "google-chrome", "google-chrome-stable", "chromium", "chromium-browser", "microsoft-edge", "microsoft-edge-stable" }.Select(findOnPath).FirstOrDefault(p => p is not null)
            : null;
    }

    public static IEnumerable<string> PlaywrightBrowsers(CliEnvironment environment)
    {
        var folder = environment.GetVariable("PLAYWRIGHT_BROWSERS_PATH") is { Length: > 0 } custom && custom != "0"
            ? custom
            : environment.Os switch
            {
                HostOs.Windows => Path.Combine(environment.GetVariable("LOCALAPPDATA") ?? Path.Combine(environment.HomeDirectory, "AppData", "Local"), "ms-playwright"),
                HostOs.MacOS => Path.Combine(environment.HomeDirectory, "Library", "Caches", "ms-playwright"),
                _ => Path.Combine(environment.HomeDirectory, ".cache", "ms-playwright")
            };

        if (Directory.Exists(folder) is false)
            yield break;

        string[] executables = environment.Os switch
        {
            HostOs.Windows => [@"chrome-win64\chrome.exe", @"chrome-win\chrome.exe", @"chrome-headless-shell-win64\chrome-headless-shell.exe", @"chrome-win\headless_shell.exe"],
            HostOs.MacOS => ["chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing", "chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing", "chrome-mac/Chromium.app/Contents/MacOS/Chromium", "chrome-headless-shell-mac-arm64/chrome-headless-shell", "chrome-headless-shell-mac-x64/chrome-headless-shell"],
            _ => ["chrome-linux64/chrome", "chrome-linux/chrome", "chrome-headless-shell-linux64/chrome-headless-shell", "chrome-linux/headless_shell"]
        };

        var installs = Directory.EnumerateDirectories(folder, "chromium*")
            .OrderBy(d => Path.GetFileName(d).Contains("headless", StringComparison.Ordinal))
            .ThenByDescending(d => int.TryParse(Path.GetFileName(d).Split('-').Last(), out var revision) ? revision : 0);

        foreach (var install in installs)
        {
            foreach (var executable in executables)
            {
                yield return Path.Combine(install, executable);
            }
        }
    }

    private static IEnumerable<string> InstalledBrowsers(CliEnvironment environment)
    {
        if (environment.IsMacOS)
            return ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Chromium.app/Contents/MacOS/Chromium", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"];

        if (environment.IsWindows is false)
            return [];

        var roots = new[] { "ProgramFiles", "ProgramFiles(x86)", "LOCALAPPDATA" }.Select(environment.GetVariable).OfType<string>().ToList();

        return roots.Select(r => Path.Combine(r, "Google", "Chrome", "Application", "chrome.exe"))
            .Concat(roots.Select(r => Path.Combine(r, "Microsoft", "Edge", "Application", "msedge.exe")));
    }

    public static async Task<Chromium> LaunchAsync(string executable, CliLog log, CancellationToken cancellationToken)
    {
        var userDataDirectory = Path.Combine(Path.GetTempPath(), "bit-aot-profile", "browser-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(userDataDirectory);

        var headless = Path.GetFileName(executable).Contains("headless", StringComparison.OrdinalIgnoreCase) ? "--headless" : "--headless=new";
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in new[] { headless, "--remote-debugging-port=0", $"--user-data-dir={userDataDirectory}", "--no-first-run", "--no-default-browser-check",
                     "--no-sandbox", "--disable-dev-shm-usage", "--disable-extensions", "--disable-background-networking", "--disable-component-update", "--disable-sync",
                     "--mute-audio", "--hide-scrollbars", "--window-size=1280,800", "about:blank" })
        {
            start.ArgumentList.Add(argument);
        }

        log.Write($"> {executable} {string.Join(' ', start.ArgumentList)}");
        var process = Process.Start(start) ?? throw new DevToolsException($"Couldn't start {executable}.");
        var errors = new StringBuilder();
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (errors) errors.AppendLine(e.Data); };
        process.OutputDataReceived += (_, e) => { };
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var portFile = Path.Combine(userDataDirectory, "DevToolsActivePort");
        var waited = Stopwatch.StartNew();
        string? endpoint;

        while ((endpoint = ReadEndpoint(portFile)) is null)
        {
            if (process.HasExited || waited.Elapsed > TimeSpan.FromSeconds(60))
            {
                Kill(process);
                string output;
                lock (errors) output = errors.ToString().Trim();
                throw new DevToolsException($"{Path.GetFileName(executable)} didn't open its DevTools port. {output}");
            }

            await Task.Delay(100, cancellationToken);
        }

        var connection = await DevToolsConnection.ConnectAsync(new Uri(endpoint), cancellationToken);
        return new Chromium(process, userDataDirectory, connection);
    }

    private static string? ReadEndpoint(string portFile)
    {
        try
        {
            return File.Exists(portFile) && File.ReadAllLines(portFile) is [var port, var path, ..] && port.Length > 0 && path.Length > 0 ? $"ws://127.0.0.1:{port}{path}" : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public async Task<DevToolsPage> OpenPageAsync(CancellationToken cancellationToken)
    {
        var target = await Connection.SendAsync("Target.createTarget", new { url = "about:blank" }, cancellationToken: cancellationToken);
        var attached = await Connection.SendAsync("Target.attachToTarget", new { targetId = target.GetProperty("targetId").GetString(), flatten = true }, cancellationToken: cancellationToken);
        return new DevToolsPage(Connection, attached.GetProperty("sessionId").GetString()!);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await Connection.SendAsync("Browser.close", cancellationToken: timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (Exception exp) when (exp is OperationCanceledException or DevToolsException or IOException or WebSocketException)
        {
        }

        Kill(process);
        await Connection.DisposeAsync();
        process.Dispose();

        for (var attempt = 0; attempt < 10 && Directory.Exists(userDataDirectory); attempt++)
        {
            try
            {
                Directory.Delete(userDataDirectory, recursive: true);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(300);
            }
        }
    }

    private static void Kill(Process process)
    {
        try
        {
            if (process.HasExited is false)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception exp) when (exp is InvalidOperationException or System.ComponentModel.Win32Exception or AggregateException)
        {
        }
    }
}

public sealed class DevToolsConnection : IAsyncDisposable
{
    private static readonly JsonSerializerOptions serializerOptions = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    private readonly ClientWebSocket socket = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> pending = new();
    private readonly SemaphoreSlim sending = new(1, 1);
    private readonly CancellationTokenSource closing = new();
    private Task receiving = Task.CompletedTask;
    private int lastId;

    public event Action<string, string?, JsonElement>? EventReceived;

    public static async Task<DevToolsConnection> ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        var connection = new DevToolsConnection();
        connection.socket.Options.KeepAliveInterval = TimeSpan.Zero;
        await connection.socket.ConnectAsync(uri, cancellationToken);
        connection.receiving = Task.Run(connection.ReceiveAsync, CancellationToken.None);
        return connection;
    }

    public async Task<JsonElement> SendAsync(string method, object? parameters = null, string? sessionId = null, CancellationToken cancellationToken = default)
    {
        var id = Interlocked.Increment(ref lastId);
        var message = new JsonObject { ["id"] = id, ["method"] = method };
        if (parameters is not null)
            message["params"] = JsonSerializer.SerializeToNode(parameters, parameters.GetType(), serializerOptions);
        if (sessionId is not null)
            message["sessionId"] = sessionId;

        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = completion;
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await sending.WaitAsync(cancellationToken);
        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(message.ToJsonString()), WebSocketMessageType.Text, true, cancellationToken);
        }
        finally
        {
            sending.Release();
        }

        try
        {
            return await completion.Task;
        }
        finally
        {
            pending.TryRemove(id, out _);
        }
    }

    private async Task ReceiveAsync()
    {
        var buffer = new byte[64 * 1024];
        using var message = new MemoryStream();

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, closing.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                message.Write(buffer, 0, result.Count);
                if (result.EndOfMessage is false)
                    continue;

                Dispatch(message.ToArray());
                message.SetLength(0);
            }
        }
        catch (Exception exp) when (exp is OperationCanceledException or WebSocketException or IOException)
        {
        }
        finally
        {
            foreach (var completion in pending.Values)
            {
                completion.TrySetException(new DevToolsException("The browser closed its DevTools connection."));
            }
        }
    }

    private void Dispatch(byte[] data)
    {
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;

        if (root.TryGetProperty("id", out var id))
        {
            if (pending.TryGetValue(id.GetInt32(), out var completion))
            {
                if (root.TryGetProperty("error", out var error))
                    completion.TrySetException(new DevToolsException(error.TryGetProperty("message", out var text) ? text.GetString() ?? "" : error.ToString()));
                else
                    completion.TrySetResult(root.TryGetProperty("result", out var result) ? result.Clone() : default);
            }

            return;
        }

        if (root.TryGetProperty("method", out var method))
        {
            EventReceived?.Invoke(method.GetString()!, root.TryGetProperty("sessionId", out var session) ? session.GetString() : null,
                root.TryGetProperty("params", out var parameters) ? parameters.Clone() : default);
        }
    }

    public async ValueTask DisposeAsync()
    {
        closing.Cancel();
        await receiving;
        socket.Dispose();
        closing.Dispose();
        sending.Dispose();
    }
}

public sealed class DevToolsPage
{
    private readonly DevToolsConnection connection;

    public DevToolsPage(DevToolsConnection connection, string sessionId)
    {
        this.connection = connection;
        SessionId = sessionId;
        connection.EventReceived += (method, session, parameters) =>
        {
            if (session == SessionId)
                EventReceived?.Invoke(method, parameters);
        };
    }

    public string SessionId { get; }

    public event Action<string, JsonElement>? EventReceived;

    public Task<JsonElement> SendAsync(string method, object? parameters = null, CancellationToken cancellationToken = default)
        => connection.SendAsync(method, parameters, SessionId, cancellationToken);

    public async Task<JsonElement> EvaluateAsync(string function, object? argument = null, CancellationToken cancellationToken = default)
    {
        var expression = $"({function})({(argument is null ? "" : JsonSerializer.Serialize(argument, argument.GetType()))})";
        var result = await SendAsync("Runtime.evaluate", new { expression, awaitPromise = true, returnByValue = true }, cancellationToken);

        if (result.TryGetProperty("exceptionDetails", out var details))
            throw new DevToolsException(details.TryGetProperty("exception", out var exception) && exception.TryGetProperty("description", out var description)
                ? description.GetString() ?? details.ToString()
                : details.GetProperty("text").GetString() ?? details.ToString());

        return result.GetProperty("result").TryGetProperty("value", out var value) ? value : default;
    }
}
