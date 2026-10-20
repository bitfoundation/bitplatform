using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Build;

internal static class IconFontSubsetter
{
    public const string Tool = "@web-alchemy/fonttools@1.4.0";

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private static readonly Regex Content = new(@"content:\s*""(?:\\(?<hex>[0-9A-Fa-f]{1,6})\s?|(?<char>[\uD800-\uDBFF][\uDC00-\uDFFF]|[^""\\]))", RegexOptions.CultureInvariant);

    public static AssetComposer.Output? Subset(PackageAssets package, IconSet set, AssetComposer.IconGlyphs glyphs, AssetSelection selection, string fontPath, string outputRoot, out string? error)
    {
        error = null;

        var kept = glyphs.Rules.Where(g => selection.Icons.Contains(g.Key)).ToList();
        if (kept.Count == glyphs.Rules.Count) return null;

        var codepoints = new SortedSet<int>(kept.Select(g => Codepoint(g.Value)).Where(c => c.HasValue).Select(c => c!.Value));
        var unicodes = string.Join("\n", codepoints.Select(c => "U+" + c.ToString("X4", CultureInfo.InvariantCulture)));

        var output = Path.Combine(outputRoot, package.PackageId, set.Font!.Replace('/', Path.DirectorySeparatorChar));
        var unicodesPath = Path.Combine(outputRoot, $"{package.PackageId}.{Path.GetFileName(output)}.unicodes");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

        var upToDate = File.Exists(output)
            && File.Exists(unicodesPath)
            && File.ReadAllText(unicodesPath) == unicodes
            && File.GetLastWriteTimeUtc(output) >= File.GetLastWriteTimeUtc(fontPath);

        if (upToDate is false)
        {
            if (File.Exists(output)) File.Delete(output);
            File.WriteAllText(unicodesPath, unicodes);

            if (Run(fontPath, unicodesPath, output, outputRoot, out error) is false) return null;
        }

        return new AssetComposer.Output(package.PackageId, set.Font, output, new FileInfo(fontPath).Length, new FileInfo(output).Length, kept.Count, glyphs.Rules.Count);
    }

    private static int? Codepoint(string rule)
    {
        var match = Content.Match(rule);
        if (match.Success is false) return null;

        if (match.Groups["hex"].Success) return int.Parse(match.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return char.ConvertToUtf32(match.Groups["char"].Value, 0);
    }

    private static bool Run(string font, string unicodes, string output, string workingDirectory, out string? error)
    {
        error = null;

        if (new[] { font, unicodes, output }.Any(p => p.IndexOf('"') >= 0))
        {
            error = "a path has a double quote in it";
            return false;
        }

        var arguments = $"--yes --package {Tool} pyftsubset \"{font}\" \"--unicodes-file={unicodes}\" --flavor=woff2 \"--output-file={output}\"";
        var windows = Path.DirectorySeparatorChar == '\\';

        var info = new ProcessStartInfo(windows ? "cmd.exe" : "npx", windows ? $"/d /s /c \"npx {arguments}\"" : arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory,
        };

        var log = new StringBuilder();

        try
        {
            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit((int)Timeout.TotalMilliseconds) is false)
            {
                try { process.Kill(); } catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { }
                error = $"npx {Tool} did not finish in {Timeout.TotalMinutes} minutes";
                return false;
            }

            process.WaitForExit();

            if (process.ExitCode != 0 || File.Exists(output) is false)
            {
                error = $"npx {Tool} pyftsubset exited with {process.ExitCode}: {Tail(log)}";
                return false;
            }
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            error = $"npx could not be started: {e.Message}";
            return false;
        }

        return true;
    }

    private static string Tail(StringBuilder log)
    {
        string text;
        lock (log) text = log.ToString();

        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", lines.Skip(Math.Max(0, lines.Length - 5)));
    }
}
