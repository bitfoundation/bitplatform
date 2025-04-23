using System.Diagnostics;

namespace Boilerplate.Server.Web.Services;

public class ScssCompilerService
{
    internal static async Task WatchScssFiles(WebApplication app)
    {
        if (app.Environment.IsDevelopment() is false)
            return;

        var clientCorePath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Core");

        var logger = app.Services.GetRequiredService<ILogger<ScssCompilerService>>();

        var toolPath = Path.Combine(clientCorePath, "node_modules/.bin/", AppPlatform.IsWindows ? "sass.cmd" : "sass");

        if (File.Exists(toolPath) is false)
        {
            logger.LogWarning("{SassTool} not found", toolPath);
            return;
        }

        if (AppPlatform.IsWindows is false)
        {
            try
            {
                File.SetUnixFileMode(toolPath, UnixFileMode.UserExecute | UnixFileMode.UserRead);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set executable permissions on {ToolPath}", toolPath);
                return;
            }
        }

        var clientMauiPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Maui");
        var clientWindowsPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Windows");
        var clientWebPath = Path.Combine(Environment.CurrentDirectory, "../../Client/Boilerplate.Client.Web");

        await Task.WhenAll(WatchProject(app, clientCorePath, logger, toolPath, ".:. Styles/app.scss:wwwroot/styles/app.css --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientMauiPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientWindowsPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"),
            WatchProject(app, clientWebPath, logger, toolPath, ".:. --style compressed --load-path=. --silence-deprecation=import --update --watch"));
    }

    private static async Task WatchProject(WebApplication app, string projectPath, ILogger<ScssCompilerService> logger, string toolPath, string command)
    {
        if (Directory.Exists(projectPath) is false)
        {
            logger.LogWarning("{ClientCoreDirectory} not found", projectPath);
            return;
        }

        using var watchScssFilesProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = projectPath,
                FileName = toolPath,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        watchScssFilesProcess.OutputDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogInformation(e.Data); };
        watchScssFilesProcess.ErrorDataReceived += (_, e) => { if (string.IsNullOrEmpty(e.Data) is false) logger.LogError(e.Data); };

        logger.LogInformation("Running {toolPath}", toolPath);
        if (watchScssFilesProcess.Start() is false)
        {
            logger.LogError("Failed to start {toolPath}", toolPath);
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
