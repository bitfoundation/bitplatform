using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Reads the source checkout, for the tests that pin a contract a bUnit render cannot see: what a component's
/// stylesheet does, what its demo page documents, what its C# and its script agree on by name, what the theme
/// stylesheets declare.
/// </summary>
/// <remarks>
/// A test that reads a source file reads it through this class rather than working out the path on its own or having
/// the csproj copy the file beside the binaries: every path is written relative to the src/BlazorUI folder, and this
/// is the one file that knows where that folder is, so a test file can move to any depth without counting the levels
/// again and no source file is ever read from a stale copy. The same goes for picking a stylesheet apart -
/// <see cref="StripScssComments"/>, <see cref="GetScssBlock"/> and <see cref="GetScssDeclarations"/> share one reading
/// of what is a comment, a string and a brace, so a fix to it reaches every test at once.
/// </remarks>
internal static class SourceFiles
{
    /// <summary>The folders of Bit.BlazorUI.Extras/Styles that hold its packaged presets.</summary>
    private static readonly string[] ExtrasPresetFolders = ["Fluent2", "Material", "Cupertino"];

    /// <summary>
    /// The src/BlazorUI folder: the one the build recorded in the BlazorUISourceRoot assembly metadata, or else the
    /// first folder above the test binaries that holds Bit.BlazorUI/Bit.BlazorUI.csproj.
    /// </summary>
    /// <remarks>
    /// Neither depends on the path the compiler records for a source file, which a PathMap or a deterministic CI build
    /// rewrites to one that exists on no disk.
    /// </remarks>
    public static string Root { get; } = FindRoot();

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
    /// The full path of a theme stylesheet, given by its path relative to the core Bit.BlazorUI/Styles folder. The
    /// presets Bit.BlazorUI.Extras packages (Fluent2, Material, Cupertino) are stylesheets of the same theming system,
    /// so they are addressed as folders beside Fluent and resolved to Bit.BlazorUI.Extras/Styles.
    /// </summary>
    public static string GetThemeStylesheetPath(params string[] segments)
    {
        var relative = Path.Combine(segments);
        var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        var project = ExtrasPresetFolders.Contains(first, StringComparer.Ordinal) ? "Bit.BlazorUI.Extras" : "Bit.BlazorUI";

        return GetPath(project, "Styles", relative);
    }

    /// <summary>Reads a theme stylesheet, given as <see cref="GetThemeStylesheetPath"/> takes it.</summary>
    public static string ReadThemeStylesheet(params string[] segments)
    {
        return ReadFullPath(GetThemeStylesheetPath(segments));
    }

    /// <summary>
    /// Every theme stylesheet matching <paramref name="searchPattern"/>: the whole core Bit.BlazorUI/Styles tree and
    /// the packaged presets of Bit.BlazorUI.Extras.
    /// </summary>
    public static IEnumerable<string> EnumerateThemeStylesheets(string searchPattern = "*.scss")
    {
        return Directory.EnumerateFiles(GetDirectory("Bit.BlazorUI", "Styles"), searchPattern, SearchOption.AllDirectories)
                        .Concat(ExtrasPresetFolders.SelectMany(folder => Directory.EnumerateFiles(GetDirectory("Bit.BlazorUI.Extras", "Styles", folder), searchPattern)));
    }

    /// <summary>
    /// Drops the comments of a stylesheet, // and /* */ alike, so an assertion matches what the stylesheet does rather
    /// than what its comments say about it (the header comment documenting the public variables, above all). A //
    /// inside a string or an unquoted url() is not a comment and stays; a block comment leaves its line breaks behind,
    /// so every rule stays on the line it was on.
    /// </summary>
    public static string StripScssComments(string stylesheet)
    {
        var kinds = Classify(stylesheet);
        var stripped = new StringBuilder(stylesheet.Length);

        for (var i = 0; i < stylesheet.Length; i++)
        {
            if (kinds[i] is not ScssChar.Comment || stylesheet[i] == '\n')
            {
                stripped.Append(stylesheet[i]);
            }
        }

        return stripped.ToString();
    }

    /// <summary>
    /// The rule that starts at the first occurrence of <paramref name="start"/> outside a comment (its selector,
    /// at-rule or mixin header), from there through the brace that closes it. Braces are counted, so the nested rules
    /// (&amp;:hover, @media, ...) are part of the block and the block ends with the rule itself, never earlier or later;
    /// braces in comments and strings are skipped.
    /// </summary>
    public static string GetScssBlock(string stylesheet, string start)
    {
        var (index, _, close, _) = FindBlock(stylesheet, start);

        return stylesheet[index..(close + 1)];
    }

    /// <summary>
    /// The rule <see cref="GetScssBlock"/> finds, less its nested rules (&amp;:hover, @media, ...): its header and the
    /// declarations it makes itself. What a test means by "this rule sets X" - which a nested rule setting X, applying
    /// only in its own state, must not satisfy.
    /// </summary>
    public static string GetScssDeclarations(string stylesheet, string start)
    {
        var (index, open, close, kinds) = FindBlock(stylesheet, start);

        var declarations = new StringBuilder(stylesheet[index..(open + 1)]);
        var statementStart = declarations.Length;
        var interpolation = 0;

        for (var i = open + 1; i < close; i++)
        {
            var c = stylesheet[i];

            if (kinds[i] is ScssChar.Code && c == '{' && stylesheet[i - 1] != '#' && interpolation == 0)
            {
                // A nested rule: drop its header, which is the statement written so far, and skip to its closing brace.
                declarations.Length = statementStart;
                i = FindClose(stylesheet, kinds, i);
                statementStart = declarations.Length;
                continue;
            }

            declarations.Append(c);

            if (kinds[i] is not ScssChar.Code) continue;

            if (c == '{') interpolation++;
            else if (c == '}') interpolation--;
            else if (c == ';' && interpolation == 0) statementStart = declarations.Length;
        }

        return declarations.Append('}').ToString();
    }

    private static (int Index, int Open, int Close, ScssChar[] Kinds) FindBlock(string stylesheet, string start)
    {
        var kinds = Classify(stylesheet);

        var index = stylesheet.IndexOf(start, StringComparison.Ordinal);
        while (index >= 0 && Array.IndexOf(kinds, ScssChar.Comment, index, start.Length) >= 0)
        {
            index = stylesheet.IndexOf(start, index + 1, StringComparison.Ordinal);
        }
        Assert.IsTrue(index >= 0, $"The stylesheet has no \"{start.Trim()}\" block.");

        // The block is the one the first brace opens - the outer one when `start` reaches into a nested rule
        // ("@media ... {\n    .bit-foo {") - and an interpolation in the selector (#{$x}) is not taken for it.
        var open = index;
        while (open < stylesheet.Length && (kinds[open] is not ScssChar.Code || stylesheet[open] != '{' || (open > 0 && stylesheet[open - 1] == '#')))
        {
            open++;
        }
        Assert.IsTrue(open < stylesheet.Length, $"The \"{start.Trim()}\" block has no opening brace.");

        var close = FindClose(stylesheet, kinds, open);
        Assert.IsTrue(close < stylesheet.Length, $"The \"{start.Trim()}\" block is not closed.");

        return (index, open, close, kinds);
    }

    // The brace closing the one at `open`, counting only the braces outside comments and strings; the length of the
    // stylesheet when there is none.
    private static int FindClose(string stylesheet, ScssChar[] kinds, int open)
    {
        var depth = 0;

        for (var i = open; i < stylesheet.Length; i++)
        {
            if (kinds[i] is not ScssChar.Code) continue;

            if (stylesheet[i] == '{')
            {
                depth++;
            }
            else if (stylesheet[i] == '}' && --depth == 0)
            {
                return i;
            }
        }

        return stylesheet.Length;
    }

    private enum ScssChar : byte { Code, Comment, String }

    // What each character of a stylesheet is: code, part of a // or /* */ comment, or part of a string - a quoted one,
    // or the inside of an unquoted url(), whose // is a protocol rather than a comment.
    private static ScssChar[] Classify(string stylesheet)
    {
        var kinds = new ScssChar[stylesheet.Length];
        var i = 0;

        while (i < stylesheet.Length)
        {
            var from = i;
            var c = stylesheet[i];
            var next = i + 1 < stylesheet.Length ? stylesheet[i + 1] : '\0';
            ScssChar kind;

            if (c == '/' && next == '/')
            {
                i = IndexOrEnd(stylesheet, stylesheet.IndexOf('\n', i));
                kind = ScssChar.Comment;
            }
            else if (c == '/' && next == '*')
            {
                var end = stylesheet.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? stylesheet.Length : end + 2;
                kind = ScssChar.Comment;
            }
            else if (c is '"' or '\'')
            {
                i = EndOfString(stylesheet, i);
                kind = ScssChar.String;
            }
            else if (IsUnquotedUrl(stylesheet, i))
            {
                var end = stylesheet.IndexOf(')', i);
                i = end < 0 ? stylesheet.Length : end + 1;
                kind = ScssChar.String;
            }
            else
            {
                i++;
                kind = ScssChar.Code;
            }

            kinds.AsSpan(from, i - from).Fill(kind);
        }

        return kinds;
    }

    // Past the quote closing the string that opens at `start`; a string runs to the end of its line at the most.
    private static int EndOfString(string text, int start)
    {
        var quote = text[start];

        for (var i = start + 1; i < text.Length; i++)
        {
            if (text[i] == '\\') i++;
            else if (text[i] == quote) return i + 1;
            else if (text[i] == '\n') return i;
        }

        return text.Length;
    }

    private static bool IsUnquotedUrl(string text, int index)
    {
        if (string.Compare(text, index, "url(", 0, 4, StringComparison.OrdinalIgnoreCase) != 0) return false;
        if (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] is '-' or '_')) return false;

        var argument = index + 4;
        while (argument < text.Length && char.IsWhiteSpace(text[argument])) argument++;

        return argument < text.Length && text[argument] is not ('"' or '\'');
    }

    private static int IndexOrEnd(string text, int index) => index < 0 ? text.Length : index;

    private static string FindRoot()
    {
        var recorded = typeof(SourceFiles).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                                   .FirstOrDefault(a => a.Key == "BlazorUISourceRoot")?.Value;

        if (IsRoot(recorded)) return Path.GetFullPath(recorded!);

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (IsRoot(directory.FullName)) return directory.FullName;
        }

        // Nothing found: every read then fails with the path it looked for.
        return recorded ?? AppContext.BaseDirectory;
    }

    private static bool IsRoot(string? directory)
    {
        return string.IsNullOrEmpty(directory) is false && File.Exists(Path.Combine(directory, "Bit.BlazorUI", "Bit.BlazorUI.csproj"));
    }
}
