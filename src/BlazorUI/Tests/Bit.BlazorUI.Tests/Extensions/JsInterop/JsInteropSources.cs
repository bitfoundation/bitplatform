using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Locates the library sources the JS interop contract tests read from disk.
/// </summary>
/// <remarks>
/// These tests scan the real <c>.cs</c> and <c>.ts</c> files rather than the compiled assemblies, because what
/// they check - that a C# call site names a JavaScript function that exists, and that it is synchronous - is
/// written in the sources and in nothing else.
/// </remarks>
internal static class JsInteropSources
{
    /// <summary>
    /// The BlazorUI source root: the directory holding both library projects, or <see langword="null"/> when
    /// the source tree isn't present (tests running from packaged binaries), so a caller can report the test
    /// inconclusive instead of failing.
    /// </summary>
    public static string? TryFindBlazorUiRoot([CallerFilePath] string callerFilePath = "")
    {
        var directoryName = Path.GetDirectoryName(callerFilePath);
        if (string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName)) return null;

        var dir = new DirectoryInfo(directoryName);

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI.Extras")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// The directory the calling test file lives in, or <see langword="null"/> when the source tree isn't
    /// present. Used to reach test assets that are read from the source tree rather than copied to the
    /// output, the way the rest of these contract tests read the library's sources.
    /// </summary>
    public static string? TryFindCallerDirectory([CallerFilePath] string callerFilePath = "")
    {
        var directoryName = Path.GetDirectoryName(callerFilePath);

        return string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName) ? null : directoryName;
    }

    /// <summary>The two library projects whose sources declare the interop contract.</summary>
    public static string[] ProjectDirectories(string blazorUiRoot) =>
    [
        Path.Combine(blazorUiRoot, "Bit.BlazorUI"),
        Path.Combine(blazorUiRoot, "Bit.BlazorUI.Extras"),
    ];

    /// <summary>
    /// Every source file of <paramref name="root"/> matching <paramref name="pattern"/>, less build outputs
    /// and the npm dependencies. Both projects keep a <c>node_modules</c> folder beside their sources (the
    /// stylesheet and TypeScript toolchain), and its thousands of files are neither part of the library nor
    /// something a drift there should be read out of - a class named like one of ours in there would answer
    /// for it.
    /// </summary>
    public static IEnumerable<string> EnumerateSourceFiles(string root, string pattern)
    {
        if (!Directory.Exists(root)) yield break;

        var separator = Path.DirectorySeparatorChar;

        foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            if (file.Contains($"{separator}bin{separator}") ||
                file.Contains($"{separator}obj{separator}") ||
                file.Contains($"{separator}node_modules{separator}"))
            {
                continue;
            }

            yield return file;
        }
    }
}
