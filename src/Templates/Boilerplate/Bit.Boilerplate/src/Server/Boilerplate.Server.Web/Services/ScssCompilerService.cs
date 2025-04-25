using System.Diagnostics;
using Meziantou.Framework.Win32;

namespace Boilerplate.Server.Web.Services;

/// <summary>
/// This compiles SCSS files into CSS in real-time during development, no extensions required.
/// </summary>
public class ScssCompilerService
{
    internal static async Task WatchScssFiles(WebApplication app)
    {
        if (app.Environment.IsDevelopment() is false)
            return;

        if (Environment.GetEnvironmentVariable("IN_APP_SCSS_COMPILER_ENABLED") is not "true")
            return; // Checkout Visual Studio's launchSettings.json

        var clientCorePath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Core");

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

        var toolPath = Path.Combine(clientCorePath, "node_modules/.bin/sass.cmd");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found", toolPath);
            return;
        }

        var sassPathsToWatch = new List<string>
        {
            ".:.", "Styles/app.scss:wwwroot/styles/app.css"
        };

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Maui")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Maui:../../Client/Boilerplate.Client.Maui");

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Windows")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Windows:../../Client/Boilerplate.Client.Windows");

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Web")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Web:../../Client/Boilerplate.Client.Web");

        var command = $"{string.Join(" ", sassPathsToWatch)} --style compressed --load-path=. --silence-deprecation=import --update --watch";

        // Create a job object to ensure the child process terminates with the parent
        using var job = new JobObject();
        job.SetLimits(new JobObjectLimits
        {
            Flags = JobObjectLimitFlags.KillOnJobClose // Terminate process when job is closed
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

        // Assign the process to the job
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
