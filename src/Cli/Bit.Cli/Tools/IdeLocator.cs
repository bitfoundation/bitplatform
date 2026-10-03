using System.Diagnostics;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public sealed record Ide(string Id, string Name, string Executable);

public static class IdeLocator
{
    public const string VsCode = "code";
    public const string VisualStudio = "vs";
    public const string Rider = "rider";
    public const string None = "none";

    public static IReadOnlyList<Ide> FindAll(CliEnvironment environment, IProcessRunner runner)
    {
        return new[] { FindVsCode(environment, runner), FindVisualStudio(environment, runner), FindRider(environment, runner) }
            .OfType<Ide>()
            .ToArray();
    }

    public static Ide? Find(string id, CliEnvironment environment, IProcessRunner runner) => id switch
    {
        VsCode => FindVsCode(environment, runner),
        VisualStudio => FindVisualStudio(environment, runner),
        Rider => FindRider(environment, runner),
        _ => null
    };

    public static Ide? FindVsCode(CliEnvironment environment, IProcessRunner runner)
    {
        var local = environment.GetVariable("LOCALAPPDATA") ?? "";
        var programFiles = environment.GetVariable("ProgramFiles") ?? @"C:\Program Files";

        var candidates = environment.Os switch
        {
            HostOs.Windows => new[]
            {
                runner.FindExecutable("code"),
                Path.Combine(local, "Programs", "Microsoft VS Code", "bin", "code.cmd"),
                Path.Combine(programFiles, "Microsoft VS Code", "bin", "code.cmd"),
                runner.FindExecutable("code-insiders"),
                Path.Combine(local, "Programs", "Microsoft VS Code Insiders", "bin", "code-insiders.cmd")
            },
            HostOs.MacOS => new[]
            {
                runner.FindExecutable("code"),
                "/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code",
                Path.Combine(environment.HomeDirectory, "Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"),
                runner.FindExecutable("code-insiders")
            },
            _ => new[] { runner.FindExecutable("code"), "/usr/bin/code", "/usr/share/code/bin/code", "/snap/bin/code", runner.FindExecutable("code-insiders") }
        };

        var executable = candidates.FirstOrDefault(c => string.IsNullOrEmpty(c) is false && File.Exists(c));
        return executable is null ? null : new Ide(VsCode, executable.Contains("insiders", StringComparison.OrdinalIgnoreCase) ? "VS Code Insiders" : "VS Code", executable);
    }

    public static Ide? FindVisualStudio(CliEnvironment environment, IProcessRunner runner)
    {
        if (environment.IsWindows is false)
            return null;

        var vswhere = Path.Combine(environment.GetVariable("ProgramFiles(x86)") ?? @"C:\Program Files (x86)", "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (File.Exists(vswhere) is false)
            return null;

        try
        {
            using var process = Process.Start(new ProcessStartInfo(vswhere, ["-latest", "-prerelease", "-products", "*", "-property", "productPath"])
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            var path = process?.StandardOutput.ReadToEnd().Trim().Split('\n').FirstOrDefault()?.Trim();
            process?.WaitForExit(10_000);

            return string.IsNullOrEmpty(path) is false && File.Exists(path) ? new Ide(VisualStudio, "Visual Studio", path) : null;
        }
        catch (Exception exp) when (exp is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    public static Ide? FindRider(CliEnvironment environment, IProcessRunner runner)
    {
        var candidates = environment.Os switch
        {
            HostOs.Windows => new[]
            {
                runner.FindExecutable("rider64.exe"),
                runner.FindExecutable("rider"),
                Path.Combine(environment.GetVariable("LOCALAPPDATA") ?? "", "JetBrains", "Toolbox", "scripts", "rider.cmd")
            },
            HostOs.MacOS => new[]
            {
                runner.FindExecutable("rider"),
                "/Applications/Rider.app/Contents/MacOS/rider",
                Path.Combine(environment.HomeDirectory, "Applications/Rider.app/Contents/MacOS/rider")
            },
            _ => new[] { runner.FindExecutable("rider"), Path.Combine(environment.HomeDirectory, ".local/share/JetBrains/Toolbox/scripts/rider") }
        };

        var executable = candidates.FirstOrDefault(c => string.IsNullOrEmpty(c) is false && File.Exists(c));
        return executable is null ? null : new Ide(Rider, "Rider", executable);
    }
}
