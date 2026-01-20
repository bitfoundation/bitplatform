using Meziantou.Framework.Win32;

namespace Boilerplate.Server.Web.Infrastructure.Services;

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
            return; // Check out Visual Studio's launchSettings.json

        if (OperatingSystem.IsWindows() is false)
            return;

        var clientCorePath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Core");

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

        var toolPath = Path.Combine(clientCorePath, "node_modules/.bin/sass.cmd");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found", toolPath);
            return;
        }

        // The ScssCompilerService operates from the Client.Core directory.
        // Using .:., it locates all .scss files in the `Styles` and `Components` folders of Client.Core (see --load-path),
        // compiles them to CSS, and saves them alongside the .scss files.
        // It also compiles app.scss to app.css and places it in the wwwroot/styles folder.
        // For Client.Web, Client.Maui, or Client.Windows, if present, the process is similar,
        // but uses ../../Client/Boilerplate.Client.Web:../../Client/Boilerplate.Client.Web instead of .:.,
        // as the working directory remains Client.Core.

        var sassPathsToWatch = new List<string>
        {
            "Components:Components", "Styles/app.scss:wwwroot/styles/app.css"
        };

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Maui/Components")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Maui/Components:../../Client/Boilerplate.Client.Maui/Components");

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Windows/Components")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Windows/Components:../../Client/Boilerplate.Client.Windows/Components");

        if (Path.Exists(Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Web/Components")))
            sassPathsToWatch.Add("../../Client/Boilerplate.Client.Web/Components:../../Client/Boilerplate.Client.Web/Components");

        var command = $"{string.Join(" ", sassPathsToWatch)} --style compressed --silence-deprecation=import --update --watch --color";

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
