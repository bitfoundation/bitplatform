using Bit.Cli.Infrastructure;
using Bit.Cli.Templates;
using Bit.Cli.Tools;
using Microsoft.Win32;
using Spectre.Console;

namespace Bit.Cli.Projects;

public static class AdministratorNeeds
{
    public static async Task<IReadOnlyList<string>> FindAsync(CliServices cli, IReadOnlyList<ToolCheck> tools, ToolNeeds needs, bool setup, CancellationToken cancellationToken)
    {
        if (cli.Environment.IsWindows is false || cli.Environment.IsElevated || cli.HasTerminal is false)
            return [];

        var reasons = tools.Where(t => t.Action?.Elevation is Elevation.Admin).Select(t => t.Tool.Name).ToList();

        if (setup && await BuildSupportNeedsItAsync(cli, tools, needs, cancellationToken))
        {
            reasons.Add(Platforms.BuildSupport(needs.Platforms));
        }

        if (setup && Platforms.BuildsSolution(needs.Platforms) && AndroidSdkNeedsIt(cli.Environment))
        {
            reasons.Add("the Android SDK");
        }

        return reasons;
    }

    public static async Task StartAsync(CliServices cli, IReadOnlyList<string> reasons, CancellationToken cancellationToken)
    {
        if (reasons.Count == 0)
            return;

        var (session, declined) = await cli.Console.RunWithStatusAsync("Waiting for administrator permission (Windows asks once)", _ => AdministratorSession.StartAsync(cli, cancellationToken));

        cli.Administrator = session;
        cli.AdministratorDeclined = declined;

        if (declined)
        {
            cli.Console.Out.MarkupLine($"  [yellow]{cli.Console.WarnSymbol}[/] Administrator permission was declined, so these steps run without it: {Markup.Escape(string.Join(", ", reasons))}.");
        }
    }

    public static async Task EndAsync(CliServices cli)
    {
        if (cli.Administrator is { } session)
        {
            cli.Administrator = null;
            await session.DisposeAsync();
        }
    }

    public static bool DotnetNeedsIt(CliServices cli) => ToolCatalog.DotnetRoot(cli.Runner) is { } root && Directory.Exists(root) && CanWrite(root) is false;

    public static bool AndroidSdkNeedsIt(CliEnvironment environment) => AndroidSdkDirectory(environment) is { } directory && CanWrite(directory) is false;

    public static string? AndroidSdkDirectory(CliEnvironment environment)
    {
        foreach (var variable in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
        {
            if (environment.GetVariable(variable) is { Length: > 0 } fromVariable && Directory.Exists(fromVariable))
                return fromVariable;
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (var (hive, key, value) in new[] { (Registry.CurrentUser, @"SOFTWARE\Xamarin\MonoAndroid", "PrivateAndroidSdkPath"), (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Android SDK Tools", "Path"), (Registry.LocalMachine, @"SOFTWARE\Android SDK Tools", "Path") })
            {
                using var opened = hive.OpenSubKey(key);

                if (opened?.GetValue(value) is string fromRegistry && Directory.Exists(fromRegistry))
                    return fromRegistry;
            }
        }

        foreach (var candidate in new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk") })
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        return null;
    }

    public static bool CanWrite(string directory)
    {
        try
        {
            using (File.Create(Path.Combine(directory, $".bit-write-check-{Guid.NewGuid():N}"), 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch (Exception exp) when (exp is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    private static async Task<bool> BuildSupportNeedsItAsync(CliServices cli, IReadOnlyList<ToolCheck> tools, ToolNeeds needs, CancellationToken cancellationToken)
    {
        if (tools.Any(t => t.Tool.Id is "dotnet-sdk" && t.Action?.Elevation is Elevation.Admin))
            return true;

        if (needs.Sdk is null || DotnetNeedsIt(cli) is false)
            return false;

        var folder = Directory.CreateTempSubdirectory("bit-workloads-").FullName;

        try
        {
            File.WriteAllText(Path.Combine(folder, "global.json"), needs.Sdk.GlobalJson);
            var list = await cli.Runner.RunAsync(new ProcessSpec { FileName = "dotnet", Arguments = ["workload", "list"], WorkingDirectory = folder, Timeout = TimeSpan.FromMinutes(2) }, cancellationToken);
            var installed = ProjectSteps.ParseInstalledWorkloads(list.Output);

            return list.Succeeded is false || Platforms.Workloads(needs.Platforms, cli.Environment.Os).Any(w => installed.Contains(w) is false);
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
