using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Cuts a font down on Node.js: fonttools' pyftsubset, on Pyodide (@web-alchemy/fonttools), installed once per machine
/// from the package-lock.json the package ships beside this task (tasks/fonttools) with npm ci - every byte of it the one
/// that lockfile pins, and none of its install scripts run.
/// </summary>
public static class NodeFontSubsetter
{
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SubsetTimeout = TimeSpan.FromMinutes(2);

    // npm's own defaults retry a request it cannot make for over two minutes before giving up, which an offline build
    // would pay on every publish: one quick retry, then the .NET cut takes over.
    private static readonly Dictionary<string, string> NpmEnvironment = new(StringComparer.Ordinal)
    {
        ["npm_config_fetch_retries"] = "1",
        ["npm_config_fetch_retry_mintimeout"] = "2000",
        ["npm_config_fetch_retry_maxtimeout"] = "5000",
        ["npm_config_fetch_timeout"] = "60000",
        ["npm_config_prefer_offline"] = "true",
        ["npm_config_audit"] = "false",
        ["npm_config_fund"] = "false",
        ["npm_config_update_notifier"] = "false",
    };

    /// <summary>What a tools folder pins: the packages its package.json names, installed from its package-lock.json.</summary>
    public sealed class Pin(string directory, string tool, string hash)
    {
        /// <summary>The folder holding package.json and package-lock.json.</summary>
        public string Directory { get; } = directory;

        /// <summary>What it runs, as package.json names it ("@web-alchemy/fonttools@1.4.0"), for the lines saying so.</summary>
        public string Tool { get; } = tool;

        /// <summary>The lockfile's SHA-256: what the folder it is installed into is named after, and a cut is stamped with.</summary>
        public string Hash { get; } = hash;

        /// <summary>The pin of a tools folder, or null where it has none to read, with the reason.</summary>
        public static Pin? Read(string toolsDirectory, out string? error)
        {
            error = null;
            var lockFile = Path.Combine(toolsDirectory, "package-lock.json");
            var packageFile = Path.Combine(toolsDirectory, "package.json");
            if (File.Exists(lockFile) is false || File.Exists(packageFile) is false)
            {
                error = $"no package.json and package-lock.json in {toolsDirectory}";
                return null;
            }

            try
            {
                var package = Json.AsObject(Json.Parse(File.ReadAllText(packageFile, Encoding.UTF8)), packageFile);
                var dependencies = Json.AsObject(package.TryGetValue("dependencies", out var value) ? value : null, packageFile + " dependencies");
                return new(toolsDirectory, string.Join(", ", dependencies.Select(d => $"{d.Key}@{d.Value}")), IconFontSubsetter.Hash(File.ReadAllBytes(lockFile)));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
            {
                error = e.Message;
                return null;
            }
        }
    }

    /// <summary>
    /// The node a cut runs on: the one named, or the first on the PATH; null where there is none. A cut is stamped with it,
    /// so one the .NET cut stood in for is made again once Node.js is there.
    /// </summary>
    public static string? Locate(string? nodePath)
    {
        if (nodePath is not null) return nodePath;

        var name = Path.DirectorySeparatorChar == '\\' ? "node.exe" : "node";
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim().Trim('"'), name);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    /// <summary>
    /// Writes the WOFF2 of the font's glyphs for the codepoints to <paramref name="output"/>, or says why it could not.
    /// </summary>
    /// <param name="nodePath">The node executable, or null for the one on the PATH (npm is the one beside it).</param>
    public static bool TrySubset(string font, IReadOnlyCollection<int> codepoints, string output, Pin pin, string? nodePath, out string? error)
    {
        var tool = Install(pin, nodePath, out error);
        if (tool is null) return false;

        var unicodes = output + ".unicodes";
        try
        {
            File.WriteAllText(unicodes, string.Join("\n", codepoints.Select(c => "U+" + c.ToString("X4", System.Globalization.CultureInfo.InvariantCulture))));
            if (File.Exists(output)) File.Delete(output);

            var script = Path.Combine(tool, "node_modules", "@web-alchemy", "fonttools", "bin", "pyftsubset");
            var (exitCode, log, failure) = Run(nodePath ?? "node", [script, font, $"--unicodes-file={unicodes}", "--flavor=woff2", $"--output-file={output}"],
                                               Path.GetDirectoryName(output)!, SubsetTimeout, environment: null);
            if (failure is not null || exitCode != 0 || File.Exists(output) is false)
            {
                error = failure ?? $"pyftsubset exited with {exitCode}: {Tail(log)}";
                return false;
            }
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            error = e.Message;
            return false;
        }
        finally
        {
            try { File.Delete(unicodes); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    // The tool, installed into a folder of the user's named after the lockfile's hash, so every project and every version
    // of the packages pinning the same one shares it. Installed into a folder of its own first and moved into place once
    // complete, so two builds racing each other never run a half-installed one.
    private static string? Install(Pin pin, string? nodePath, out string? error)
    {
        error = null;

        // Never the temp folder in its place: every user of the machine shares that one, and a tool another user put at
        // the path below would be run as this one's.
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(root))
        {
            error = "this user has no local application data folder to install it into (is HOME set?)";
            return null;
        }

        var folder = Path.Combine(root, "bit-blazorui", "fonttools", pin.Hash.Substring(0, 16));
        var complete = Path.Combine(folder, ".complete");
        if (File.Exists(complete)) return folder;

        // An install that failed is not tried again on the same node: a machine with no way to the npm registry pays its
        // timeouts once, rather than on every publish whose icons changed, in every project. Until the file saying so is
        // deleted, or the node changes (one installed, BitBlazorUINodePath set).
        var node = Locate(nodePath) ?? "no node";
        var failed = folder + ".failed";
        if (ReadFailure(failed, node) is { } earlier)
        {
            error = $"{earlier} (on an earlier publish, not tried again until {failed} is deleted)";
            return null;
        }

        var staging = folder + "." + Guid.NewGuid().ToString("N").Substring(0, 8);
        try
        {
            Directory.CreateDirectory(staging);
            File.Copy(Path.Combine(pin.Directory, "package.json"), Path.Combine(staging, "package.json"));
            File.Copy(Path.Combine(pin.Directory, "package-lock.json"), Path.Combine(staging, "package-lock.json"));

            var (exitCode, log, failure) = RunNpm(nodePath, ["ci", "--ignore-scripts", "--no-audit", "--no-fund", "--loglevel=error"], staging);
            if (failure is not null || exitCode != 0)
            {
                error = failure ?? $"npm ci of {pin.Tool} exited with {exitCode}: {Tail(log)}";
                WriteFailure(failed, node, error);
                return null;
            }
            File.WriteAllText(Path.Combine(staging, ".complete"), pin.Tool);

            if (Directory.Exists(folder) && File.Exists(complete) is false) Directory.Delete(folder, recursive: true);
            if (Directory.Exists(folder) is false) Directory.Move(staging, folder);
            return folder;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Another build moved its own copy into place first; it is as complete as this one.
            if (File.Exists(complete)) return folder;
            error = $"{pin.Tool} could not be installed into {folder}: {e.Message}";
            return null;
        }
        finally
        {
            try { if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    // Why an earlier install on the same node failed (the second line of the file it left), or null where none did.
    private static string? ReadFailure(string path, string node)
    {
        try
        {
            if (File.Exists(path) is false) return null;
            var lines = File.ReadAllLines(path);
            return lines.Length > 1 && lines[0] == node ? lines[1] : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // A failure not written down is only tried again: what the install said stands either way.
    private static void WriteFailure(string path, string node, string error)
    {
        try { File.WriteAllLines(path, new[] { node, error.Replace('\r', ' ').Replace('\n', ' ') }); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // npm is a script beside node: npm.cmd on Windows, which only cmd.exe runs, and npm elsewhere - which starts with
    // #!/usr/bin/env node, and so runs on the node the PATH has. The folder of the node named leads the PATH it is given.
    private static (int ExitCode, string Log, string? Failure) RunNpm(string? nodePath, string[] arguments, string workingDirectory)
    {
        var windows = Path.DirectorySeparatorChar == '\\';
        var directory = nodePath is null ? null : Path.GetDirectoryName(Path.GetFullPath(nodePath));
        var npm = directory is null ? "npm" : Path.Combine(directory, windows ? "npm.cmd" : "npm");

        var environment = new Dictionary<string, string>(NpmEnvironment, StringComparer.Ordinal);
        if (directory is not null) environment["PATH"] = directory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");

        // The arguments are npm's own words, never a path, so there is nothing cmd.exe could read as one of its own. npm is
        // quoted only as a path: a batch file called by a quoted bare name ("npm") reads the working directory as its own
        // folder (%~dp0), and npm.cmd then looks for npm itself there.
        var command = directory is null ? npm : $"\"{npm}\"";
        return windows
            ? Run("cmd.exe", ["/d", "/s", "/c", $"\"{command} {string.Join(" ", arguments)}\""], workingDirectory, InstallTimeout, environment, rawArguments: true)
            : Run(npm, arguments, workingDirectory, InstallTimeout, environment);
    }

    private static (int ExitCode, string Log, string? Failure) Run(string fileName, string[] arguments, string workingDirectory, TimeSpan timeout, Dictionary<string, string>? environment, bool rawArguments = false)
    {
        var info = new ProcessStartInfo(fileName, rawArguments ? string.Join(" ", arguments) : string.Join(" ", arguments.Select(Quote)))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory,
        };
        if (environment is not null) foreach (var pair in environment) info.Environment[pair.Key] = pair.Value;

        var log = new StringBuilder();
        try
        {
            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit((int)timeout.TotalMilliseconds) is false)
            {
                KillTree(process);
                return (-1, log.ToString(), $"{Path.GetFileName(fileName)} did not finish in {timeout.TotalMinutes} minutes");
            }

            process.WaitForExit();
            lock (log) return (process.ExitCode, log.ToString(), null);
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or IOException)
        {
            return (-1, string.Empty, $"{fileName} could not be started ({e.Message}) - is Node.js installed, and on the PATH or named by BitBlazorUINodePath?");
        }
    }

    // On Windows the process is cmd.exe or node, and npm's node a child of the first: taskkill takes the tree down with it.
    private static void KillTree(Process process)
    {
        try
        {
            if (Path.DirectorySeparatorChar == '\\')
            {
                using var kill = Process.Start(new ProcessStartInfo("taskkill", $"/T /F /PID {process.Id}") { UseShellExecute = false, CreateNoWindow = true });
                kill?.WaitForExit(10_000);
            }
            else
            {
                process.Kill();
            }
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException) { }
    }

    // An argument as the Windows command line and .NET's own parsing on Unix read it back.
    private static string Quote(string argument) => argument.Length > 0 && argument.IndexOfAny([' ', '\t', '"']) < 0 ? argument : "\"" + argument.Replace("\"", "\\\"") + "\"";

    private static string Tail(string log)
    {
        var lines = log.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", lines.Skip(Math.Max(0, lines.Length - 5)));
    }
}
