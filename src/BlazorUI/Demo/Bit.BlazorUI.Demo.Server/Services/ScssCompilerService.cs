using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Server.Services;

/// <summary>
/// Compiles SCSS files into CSS in real-time during development, no extensions required.
/// Combined with the Watch items of the client/library csproj files, this lets dotnet watch
/// hot-apply style changes to the browser without a rebuild.
/// (Mirrors the ScssCompilerService of the bit Boilerplate template.)
/// </summary>
public class ScssCompilerService
{
    internal static async Task WatchScssFiles(WebApplication app)
    {
        if (app.Environment.IsDevelopment() is false)
            return;

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

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

        var command = $"{string.Join(" ", sassPathsToWatch)} --style compressed --silence-deprecation=import --update --watch --color";

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

            watchScssFilesProcess.OutputDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogInformation(e.Data); };
            watchScssFilesProcess.ErrorDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogError(e.Data); };

            logger.LogInformation("Running {toolPath} for {ProjectDirectory}", toolPath, clientCorePath);
            if (watchScssFilesProcess.Start() is false)
            {
                logger.LogError("Failed to start {toolPath} for {ProjectDirectory}", toolPath, clientCorePath);
                return;
            }

            watchScssFilesProcess.BeginOutputReadLine();
            watchScssFilesProcess.BeginErrorReadLine();

            // dart-sass --watch runs until killed, so tie its lifetime to the server: kill the whole
            // process tree when the host starts shutting down (dotnet watch restart, Ctrl+C) so no
            // orphaned sass watcher survives. The finally is a backstop for exceptions before shutdown.
            app.Lifetime.ApplicationStopping.Register(() => KillSassProcess(watchScssFilesProcess, logger));

            await app.WaitForShutdownAsync();
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to terminate the scss watch process.");
        }
    }
}
