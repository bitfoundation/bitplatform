using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Reads the source checkout, for the tests that pin a contract a bUnit render cannot see: what a component's
/// stylesheet does, what its demo page documents, what its C# and its script agree on by name.
/// </summary>
/// <remarks>
/// A test that reads a source file reads it through this class rather than working out the path on its own: every
/// path is written relative to the src/BlazorUI folder, and this is the one file that knows where that folder is, so a
/// test file can move to any depth without counting the levels again. The same goes for picking a stylesheet apart -
/// <see cref="StripScssComments"/> and <see cref="GetScssBlock"/> are the one way to do it, so a fix to either reaches
/// every test at once.
/// </remarks>
internal static class SourceFiles
{
    /// <summary>The src/BlazorUI folder, two levels above this file.</summary>
    public static string Root { get; } = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetThisFile())!, "..", ".."));

    /// <summary>The full path of a file or folder, given by its path relative to the src/BlazorUI folder.</summary>
    public static string GetPath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine([Root, .. segments]));
    }

    /// <summary>
    /// The full path of a folder, given by its path relative to the src/BlazorUI folder; fails when it is missing.
    /// </summary>
    public static string GetDirectory(params string[] segments)
    {
        var path = GetPath(segments);

        Assert.IsTrue(Directory.Exists(path), $"Missing {path}; this test reads the source tree, so it must run from a source checkout.");

        return path;
    }

    /// <summary>
    /// Reads a file, given by its path relative to the src/BlazorUI folder, with its line endings normalized to \n;
    /// fails when it is missing.
    /// </summary>
    public static string Read(params string[] segments)
    {
        return ReadFullPath(GetPath(segments));
    }

    /// <summary>Reads a file by its full path, with its line endings normalized to \n; fails when it is missing.</summary>
    public static string ReadFullPath(string path)
    {
        Assert.IsTrue(File.Exists(path), $"Missing {path}; this test reads the source tree, so it must run from a source checkout.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    /// <summary>
    /// Drops the // comments of a stylesheet, whole-line and trailing alike, so an assertion matches what the
    /// stylesheet does rather than what its comments say about it (the header comment documenting the public
    /// variables, above all). A // right after a colon is the protocol of a url, not a comment, and stays.
    /// </summary>
    public static string StripScssComments(string stylesheet)
    {
        return Regex.Replace(stylesheet, @"(?<!:)//[^\n]*", string.Empty);
    }

    /// <summary>
    /// The rule that starts at the first occurrence of <paramref name="start"/> (its selector, at-rule or mixin
    /// header), from there through the brace that closes it. Braces are counted, so the nested rules (&amp;:hover,
    /// @media, ...) are part of the block and the block ends with the rule itself, never earlier or later; braces in
    /// // and /* */ comments are skipped.
    /// </summary>
    public static string GetScssBlock(string stylesheet, string start)
    {
        var index = stylesheet.IndexOf(start, StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"The stylesheet has no \"{start.Trim()}\" block.");

        // The block is the one the first brace opens - the outer one when `start` reaches into a nested rule
        // ("@media ... {\n    .bit-foo {") - and an interpolation in the selector (#{$x}) is not taken for it.
        var open = index;
        while (open < stylesheet.Length && (stylesheet[open] != '{' || (open > 0 && stylesheet[open - 1] == '#')))
        {
            open++;
        }
        Assert.IsTrue(open < stylesheet.Length, $"The \"{start.Trim()}\" block has no opening brace.");

        var depth = 0;
        for (var i = open; i < stylesheet.Length; i++)
        {
            var c = stylesheet[i];

            if (c == '/' && i + 1 < stylesheet.Length)
            {
                var next = stylesheet[i + 1];

                // A // that follows a colon is a protocol in a url(), not a comment.
                if (next == '/' && (i == 0 || stylesheet[i - 1] != ':'))
                {
                    i = SkipTo(stylesheet, "\n", i) - 1;
                    continue;
                }

                if (next == '*')
                {
                    i = SkipTo(stylesheet, "*/", i) + 1;
                    continue;
                }
            }

            if (c == '{')
            {
                depth++;
            }
            else if (c == '}' && --depth == 0)
            {
                return stylesheet[index..(i + 1)];
            }
        }

        Assert.Fail($"The \"{start.Trim()}\" block is not closed.");
        return string.Empty;
    }

    private static int SkipTo(string text, string end, int from)
    {
        var index = text.IndexOf(end, from + 2, StringComparison.Ordinal);

        return index < 0 ? text.Length : index;
    }

    private static string GetThisFile([CallerFilePath] string thisFile = "") => thisFile;
}
