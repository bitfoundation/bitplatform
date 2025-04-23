using System.Diagnostics;

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
            return;

        var clientCorePath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Core");

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

        var toolPath = Path.Combine(clientCorePath, "node_modules/.bin/sass.cmd");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found", toolPath);
            return;
        }

        var clientMauiDirPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Maui");
        var clientWindowsDirPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Windows");
        var clientWebDirPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Web");

        await Task.WhenAll(WatchProject(app, clientCorePath, logger, toolPath, ".:. Styles/app.scss:wwwroot/styles/app.css --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientMauiDirPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientWindowsDirPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientWebDirPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"));
    }

    private static async Task WatchProject(WebApplication app, string projectDir, ILogger<ScssCompilerService> logger, string toolPath, string command)
    {
        if (Directory.Exists(projectDir) is false)
        {
            logger.LogWarning("{ProjectDirectory} not found", projectDir);
            return;
        }

        using var watchScssFilesProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = projectDir,
                FileName = toolPath,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        watchScssFilesProcess.OutputDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogInformation(e.Data); };
        watchScssFilesProcess.ErrorDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogError(e.Data); };

        logger.LogInformation("Running {toolPath} for {ProjectDirectory}", toolPath, projectDir);
        if (watchScssFilesProcess.Start() is false)
        {
            logger.LogError("Failed to start {toolPath} for {ProjectDirectory}", toolPath, projectDir);
            return;
        }

        watchScssFilesProcess.BeginOutputReadLine();
        watchScssFilesProcess.BeginErrorReadLine();

        app.Lifetime.ApplicationStopping.Register(() =>
        {
            if (watchScssFilesProcess.HasExited is false)
            {
                watchScssFilesProcess.Kill();
                watchScssFilesProcess.WaitForExit(5000);
            }
        });

        await app.WaitForShutdownAsync();
    }
}
