using System.Diagnostics;
using Meziantou.Framework.Win32;

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

        if (OperatingSystem.IsWindows() is false)
            return; // The JobObject used below (to terminate sass alongside the server) is Windows-only.

        var clientCorePath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../Client/Bit.BlazorUI.Demo.Client.Core"));

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

        var toolPath = Path.Combine(clientCorePath, "node_modules/.bin/sass.cmd");

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

        // Create a job object to ensure the sass process terminates with the server process.
        using var job = new JobObject();
        job.SetLimits(new JobObjectLimits
        {
            Flags = JobObjectLimitFlags.KillOnJobClose
        });

        using var watchScssFilesProcess = new Process
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

        try
        {
            job.AssignProcess(watchScssFilesProcess);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to assign process to job for {toolPath}", toolPath);
        }

        watchScssFilesProcess.BeginOutputReadLine();
        watchScssFilesProcess.BeginErrorReadLine();

        await app.WaitForShutdownAsync();
    }
}
