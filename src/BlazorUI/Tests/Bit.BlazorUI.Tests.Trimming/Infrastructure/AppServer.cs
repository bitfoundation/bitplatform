using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Bit.BlazorUI.Tests.Trimming.Infrastructure;

/// <summary>A published app served on a port of its own, for a browser to open.</summary>
public abstract class AppServer : IAsyncDisposable
{
    public abstract string Url { get; }

    public abstract ValueTask DisposeAsync();

    /// <summary>Serves a published WebAssembly app's wwwroot the way a static host would, with the given page as its host page.</summary>
    public static async Task<AppServer> StartStaticAsync(string webRoot, string hostPage = "index.html")
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { WebRootPath = webRoot, ContentRootPath = webRoot });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        var types = new FileExtensionContentTypeProvider();
        types.Mappings[".wasm"] = "application/wasm";
        types.Mappings[".dat"] = "application/octet-stream";
        types.Mappings[".blat"] = "application/octet-stream";
        types.Mappings[".webcil"] = "application/octet-stream";

        var files = new PhysicalFileProvider(webRoot);
        var defaults = new DefaultFilesOptions { FileProvider = files };
        defaults.DefaultFileNames.Clear();
        defaults.DefaultFileNames.Add(hostPage);
        app.UseDefaultFiles(defaults);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = files, ContentTypeProvider = types, ServeUnknownFileTypes = true });
        app.MapFallbackToFile(hostPage, new StaticFileOptions { FileProvider = files });

        await app.StartAsync();
        return new StaticServer(app, app.Urls.First());
    }

    /// <summary>
    /// Runs a published server app as it runs in production - its own process, its static web assets served off the
    /// endpoints manifest the publish wrote - and waits until it answers.
    /// </summary>
    public static async Task<AppServer> StartPublishedAsync(string publishDirectory, string assemblyName)
    {
        var url = $"http://127.0.0.1:{FreePort()}";
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = publishDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        info.ArgumentList.Add(Path.Combine(publishDirectory, assemblyName + ".dll"));
        info.ArgumentList.Add("--urls");
        info.ArgumentList.Add(url);
        info.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        var process = Process.Start(info)!;
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var server = new ProcessServer(process, url, output);
        using var client = new HttpClient();
        for (var deadline = DateTime.UtcNow.AddSeconds(60); DateTime.UtcNow < deadline; await Task.Delay(250))
        {
            if (process.HasExited) break;
            try
            {
                if ((await client.GetAsync(url)).IsSuccessStatusCode) return server;
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
        }

        await server.DisposeAsync();
        throw new InvalidOperationException($"The published app at {publishDirectory} did not answer on {url}:\n{output}");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class StaticServer(WebApplication app, string url) : AppServer
    {
        public override string Url { get; } = url;

        public override async ValueTask DisposeAsync() => await app.DisposeAsync();
    }

    private sealed class ProcessServer(Process process, string url, StringBuilder output) : AppServer
    {
        public override string Url { get; } = url;

        public string Output { get { lock (output) return output.ToString(); } }

        public override async ValueTask DisposeAsync()
        {
            if (process.HasExited is false)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            process.Dispose();
        }
    }
}
