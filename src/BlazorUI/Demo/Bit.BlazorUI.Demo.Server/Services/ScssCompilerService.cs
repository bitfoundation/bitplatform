using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Server.Services;

/// <summary>
/// Compiles SCSS files into CSS in real-time during development, no extensions required.
/// Combined with the Watch items of the client/library csproj files, this lets dotnet watch
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

        var clientCorePath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../Client/Bit.BlazorUI.Demo.Client.Core"));

        // The npm-installed dart-sass shim: sass.cmd on Windows, the sass shell script elsewhere.
        var toolPath = Path.Combine(clientCorePath, OperatingSystem.IsWindows() ? "node_modules/.bin/sass.cmd" : "node_modules/.bin/sass");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found. Run a build of Bit.BlazorUI.Demo.Client.Core once (npm install) to enable real-time scss compilation.", toolPath);
            return;
        }

        // The sass process operates from the Client.Core directory. Folder:Folder pairs compile the
        // isolated .razor.scss files in place; file:file pairs compile the global style bundles.
        // The BlazorUI library styles are included as well, so editing a component's scss
        // (e.g. BitButton.scss) instantly refreshes _content/Bit.BlazorUI/styles/*.css.
        var sassPathsToWatch = new List<string>
        {
            "Pages:Pages", "Components:Components", "Shared:Shared",
            "Styles/app.scss:wwwroot/styles/app.css",
            "../../../Bit.BlazorUI/Styles/bit.blazorui.scss:../../../Bit.BlazorUI/wwwroot/styles/bit.blazorui.css",
            "../../../Bit.BlazorUI/Styles/Fluent/bit.blazorui.fluent.scss:../../../Bit.BlazorUI/wwwroot/styles/bit.blazorui.fluent.css",
            "../../../Bit.BlazorUI/Styles/Fluent/bit.blazorui.fluent-dark.scss:../../../Bit.BlazorUI/wwwroot/styles/bit.blazorui.fluent-dark.css",
            "../../../Bit.BlazorUI/Styles/Fluent/bit.blazorui.fluent-light.scss:../../../Bit.BlazorUI/wwwroot/styles/bit.blazorui.fluent-light.css",
            "../../../Bit.BlazorUI.Extras/Styles/bit.blazorui.extras.scss:../../../Bit.BlazorUI.Extras/wwwroot/styles/bit.blazorui.extras.css",
            "../../../Bit.BlazorUI.Icons/Styles/bit.blazorui.icons.scss:../../../Bit.BlazorUI.Icons/wwwroot/styles/bit.blazorui.icons.css",
            "../../../Bit.BlazorUI.Assets/Styles/bit.blazorui.assets.scss:../../../Bit.BlazorUI.Assets/wwwroot/styles/bit.blazorui.assets.css",
        };

        // dart-sass --watch only watches files that live under a watched root: the directory of each
        // compiled entry point plus every --load-path directory. The library bundle entry points sit in
        // each project's Styles/ folder, but their component partials are imported from a sibling
        // Components/ tree (e.g. bit.blazorui.scss -> ... -> Styles/components.scss -> ../Components/Buttons/
        // ActionButton/BitActionButton.scss). Those partials are outside every watched root, so editing one
        // would NOT retrigger a compile and the css would go stale on hot reload. Adding each Components/
        // folder as a load-path brings it under a watched root. Do not remove these: without them, editing a
        // component's scss (BitButton.scss, etc.) silently stops hot-reloading its styles.
        var loadPaths = new List<string>
        {
            "--load-path=.",
            "--load-path=../../../Bit.BlazorUI/Components",
            "--load-path=../../../Bit.BlazorUI.Extras/Components",
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
                    WorkingDirectory = clientCorePath,
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

            logger.LogInformation("Running {toolPath} for {ProjectDirectory}", toolPath, clientCorePath);
            if (watchScssFilesProcess.Start() is false)
            {
                logger.LogError("Failed to start {toolPath} for {ProjectDirectory}", toolPath, clientCorePath);
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
