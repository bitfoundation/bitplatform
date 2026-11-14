using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.AotProfiling;

public sealed class PublishedApp : IAsyncDisposable
{
    public static readonly Uri DefaultOrigin = new("http://localhost:5000/");

    private readonly Process? server;

    private PublishedApp(Uri origin, string webRoot, Process? server)
    {
        Origin = origin;
        WebRoot = webRoot;
        this.server = server;
    }

    public Uri Origin { get; }

    public string WebRoot { get; }

    public bool IsStatic => server is null;

    public static string? FindServer(string publishFolder)
    {
        foreach (var config in Directory.EnumerateFiles(publishFolder, "*.runtimeconfig.json"))
        {
            var app = config[..^".runtimeconfig.json".Length];
            JsonNode? options;

            try
            {
                options = JsonNode.Parse(File.ReadAllText(config))?["runtimeOptions"];
            }
            catch (JsonException)
            {
                continue;
            }

            if (options is null || File.Exists(app + ".dll") is false)
                continue;

            if (options["framework"] is not null || options["frameworks"] is not null)
                return app + ".dll";

            var apphost = OperatingSystem.IsWindows() ? app + ".exe" : app;
            if (options["includedFrameworks"] is not null && options["wasmHostProperties"] is null && File.Exists(apphost))
                return apphost;
        }

        return null;
    }

    public static async Task<PublishedApp> StartAsync(string publishFolder, Uri? origin, CliLog log, CancellationToken cancellationToken)
    {
        var webRoot = Path.Combine(publishFolder, "wwwroot");

        if (FindServer(publishFolder) is not { } entry)
        {
            if (File.Exists(Path.Combine(webRoot, "index.html")) is false)
                throw new InvalidOperationException("The publish has neither a server to start nor a wwwroot/index.html to serve.");

            return new PublishedApp(origin ?? DefaultOrigin, webRoot, null);
        }

        var dll = entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        var address = new Uri($"http://127.0.0.1:{FreePort()}/");
        var start = new ProcessStartInfo(dll ? "dotnet" : entry) { WorkingDirectory = publishFolder, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        if (dll)
            start.ArgumentList.Add(entry);

        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(address.ToString().TrimEnd('/'));

        log.Write($"> {(dll ? "dotnet " : "")}{Path.GetFileName(entry)} --urls {address}");
        var process = Process.Start(start) ?? throw new InvalidOperationException($"Couldn't start {Path.GetFileName(entry)}.");
        var output = new StringBuilder();
        void Collect(string? line)
        {
            if (line is null)
                return;

            log.Write(line);
            lock (output) output.AppendLine(line);
        }

        process.OutputDataReceived += (_, e) => Collect(e.Data);
        process.ErrorDataReceived += (_, e) => Collect(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var app = new PublishedApp(address, webRoot, process);
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var waited = Stopwatch.StartNew();

        try
        {
            while (true)
            {
                try
                {
                    using var response = await http.GetAsync(address, cancellationToken);
                    return app;
                }
                catch (Exception exp) when (exp is HttpRequestException or TaskCanceledException && cancellationToken.IsCancellationRequested is false)
                {
                }

                if (process.HasExited || waited.Elapsed > TimeSpan.FromMinutes(2))
                {
                    await app.DisposeAsync();
                    string text;
                    lock (output) text = output.ToString().Trim();
                    throw new InvalidOperationException($"{Path.GetFileName(entry)} didn't start. {text}");
                }

                await Task.Delay(250, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        if (server is null)
            return;

        try
        {
            if (server.HasExited is false)
                server.Kill(entireProcessTree: true);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await server.WaitForExitAsync(timeout.Token);
        }
        catch (Exception exp) when (exp is InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception or AggregateException)
        {
        }

        server.Dispose();
    }
}
