using System.Diagnostics;

namespace Bit.Websites.Platform.Server.Services;

/// <summary>
/// Compiles SCSS files into CSS in real-time during development, no extensions required.
/// Combined with the Watch items of the client csproj file, this lets dotnet watch
/// hot-apply style changes to the browser without a rebuild.
/// (Mirrors the ScssCompilerService of the bit Boilerplate template.)
/// </summary>
public static class ScssCompilerService
{
    internal static async Task WatchScssFiles(WebApplication app)
    {
        if (app.Environment.IsDevelopment() is false)
            return;

        // A static type can't be an ILogger<T> category, so create the logger by name via the factory.
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ScssCompilerService));

        var clientPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../Bit.Websites.Platform.Client"));

        // The npm-installed dart-sass shim: sass.cmd on Windows, the sass shell script elsewhere.
        var toolPath = Path.Combine(clientPath, OperatingSystem.IsWindows() ? "node_modules/.bin/sass.cmd" : "node_modules/.bin/sass");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found. Run a build of Bit.Websites.Platform.Client once (npm install) to enable real-time scss compilation.", toolPath);
            return;
        }

        // The sass process operates from the client directory. Folder:Folder pairs compile the
        // isolated .razor.scss files in place; the file:file pair compiles the global style bundle.
        var sassPathsToWatch = new List<string>
        {
            "Pages:Pages", "Shared:Shared",
            "Styles/app.scss:wwwroot/styles/app.css",
        };

        var loadPaths = new List<string>
        {
            "--load-path=.",
        };

        // --no-source-map is required for style hot reload to work. dart-sass emits a *.css.map next to
        // every *.css; dotnet watch watches those too and pushes each change to the browser. The injected
        // aspnetcore-browser-refresh.js only does a live, in-place stylesheet swap for paths ending in
        // ".css" - for any other static file (including ".css.map") it falls back to a full location.reload().
        // With source maps on, every scss edit therefore fires a burst of full-page reloads that race with
        // (and clobber) the clean css swaps, so the page never reliably shows the change. Dropping the maps
        // leaves only ".css" updates, which hot-swap cleanly without reloading the page.
        var command = $"{string.Join(" ", sassPathsToWatch)} {string.Join(" ", loadPaths)} --style compressed --no-source-map --silence-deprecation=import --update --watch --color";

        // Scss watching is best-effort development tooling: any failure below must not fault this task,
        // which would otherwise surface as an unobserved error in Program.cs's Task.WhenAll at shutdown.
        Process? watchScssFilesProcess = null;
        try
        {
            watchScssFilesProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = clientPath,
                    FileName = toolPath,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // Pass the sass output as a logging argument, not as the message template: scss snippets in
            // sass's error output contain braces that ILogger would parse as format placeholders.
            watchScssFilesProcess.OutputDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogInformation("{SassOutput}", e.Data); };
            watchScssFilesProcess.ErrorDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogError("{SassError}", e.Data); };

            logger.LogInformation("Running {toolPath} for {ProjectDirectory}", toolPath, clientPath);
            if (watchScssFilesProcess.Start() is false)
            {
                logger.LogError("Failed to start {toolPath} for {ProjectDirectory}", toolPath, clientPath);
                return;
            }

            watchScssFilesProcess.BeginOutputReadLine();
            watchScssFilesProcess.BeginErrorReadLine();

            // dart-sass --watch runs until killed, so tie its lifetime to the server: park until the
            // host begins shutting down (dotnet watch restart, Ctrl+C), then the finally kills the whole
            // process tree so no orphaned sass watcher survives - Program.cs awaits this task, so the
            // cleanup always completes before the process exits. Waiting on ApplicationStopping (instead
            // of app.WaitForShutdownAsync, which itself calls host.StopAsync) keeps Program.cs's
            // WaitForShutdownAsync as the single code path that drives host shutdown.
            try
            {
                await Task.Delay(Timeout.Infinite, app.Lifetime.ApplicationStopping);
            }
            catch (OperationCanceledException)
            {
                // The normal shutdown signal, not a failure.
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to watch scss files with {toolPath}", toolPath);
        }
        finally
        {
            KillSassProcess(watchScssFilesProcess, logger);
            watchScssFilesProcess?.Dispose();
        }
    }

    private static void KillSassProcess(Process? process, ILogger logger)
    {
        try
        {
            if (process is not null && process.HasExited is false)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process never started or is already disposed - nothing to terminate.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to terminate the scss watch process.");
        }
    }
}
