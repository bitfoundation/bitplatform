using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards the "FastInvoke targets must be synchronous JavaScript" contract.
///
/// On Blazor WebAssembly, <c>FastInvoke</c>/<c>FastInvokeVoid</c> run through <c>IJSInProcessRuntime</c>
/// synchronously. If the target JavaScript function is asynchronous (declared <c>async</c>, annotated with a
/// <c>Promise</c> return type, or returning a Promise from its body), the call becomes fire-and-forget:
/// callers proceed before the work completes and any error is lost. Since the interop is keyed by string
/// identifiers, this test statically links every <c>FastInvoke</c> call site to its TypeScript definition
/// and fails if any target returns a Promise.
/// </summary>
[TestClass]
public class FastInvokeSyncContractTests
{
    // Matches FastInvoke("id"... / FastInvokeVoid("id"... / FastInvoke<T>("id"... capturing the JS identifier.
    private static readonly Regex FastInvokeCallRegex =
        new(@"FastInvoke(?:Void)?\s*(?:<[^>]+>)?\s*\(\s*""(?<id>[^""]+)""", RegexOptions.Compiled);

    // Matches a TypeScript class declaration.
    private static readonly Regex TsClassRegex =
        new(@"\bclass\s+(?<class>\w+)", RegexOptions.Compiled);

    // Matches a static method header up to (and including) its opening parameter parenthesis, e.g.
    // "static async parseAsync(" or "static init<T>(". The trailing "\(" (with only whitespace/generics
    // allowed before it) excludes static fields and arrow-function properties like "static foo = () => {}".
    private static readonly Regex TsStaticMethodHeaderRegex =
        new(@"\bstatic\s+(?<async>async\s+)?(?<method>\w+)\s*(?:<[^>]+>)?\s*\(", RegexOptions.Compiled);

    // Detects a Promise-returning method body, e.g. "return new Promise(" or "return Promise.resolve(".
    private static readonly Regex TsReturnsPromiseRegex =
        new(@"\breturn\s+(?:new\s+Promise\b|Promise\s*\.)", RegexOptions.Compiled);

    [TestMethod]
    public void FastInvoke_CallSites_ShouldNotTargetAsyncJavaScriptFunctions()
    {
        var blazorUiRoot = FindBlazorUiRoot();

        var csharpDirs = new[]
        {
            Path.Combine(blazorUiRoot, "Bit.BlazorUI"),
            Path.Combine(blazorUiRoot, "Bit.BlazorUI.Extras"),
        };

        // 1. Collect every FastInvoke target identifier (reduced to its "Class.method" tail) and where it lives.
        var fastInvokeTargets = new List<(string ClassMethod, string Identifier, string File)>();
        foreach (var dir in csharpDirs)
        {
            foreach (var file in EnumerateSourceFiles(dir, "*.cs"))
            {
                var text = File.ReadAllText(file);
                foreach (Match match in FastInvokeCallRegex.Matches(text))
                {
                    var identifier = match.Groups["id"].Value;
                    var classMethod = LastTwoSegments(identifier);
                    if (classMethod is null) continue;

                    fastInvokeTargets.Add((classMethod, identifier, file));
                }
            }
        }

        Assert.IsTrue(fastInvokeTargets.Count > 0,
            "Expected to find FastInvoke call sites to validate, but none were found. " +
            "The scanning logic in this test is likely broken or the source layout changed.");

        // 2. Collect every async/Promise-returning static JS method as "Class.method" across all TypeScript sources.
        var asyncJsMethods = CollectAsyncJsMethods(csharpDirs);

        Assert.IsTrue(asyncJsMethods.Count > 0,
            "Expected to find async/Promise-returning TypeScript methods (e.g. BitBlazorUI.PdfReader.renderPage), but none were found. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

        // 3. A FastInvoke target that returns a Promise on the JS side violates the synchronous-only contract.
        var violations = fastInvokeTargets
            .Where(t => asyncJsMethods.Contains(t.ClassMethod))
            .Select(t => $"  - '{t.Identifier}' (async/Promise-returning JS) invoked via FastInvoke in {Path.GetFileName(t.File)}")
            .Distinct()
            .ToList();

        Assert.AreEqual(0, violations.Count,
            "FastInvoke/FastInvokeVoid must only target synchronous JavaScript functions. " +
            "On Blazor WebAssembly these calls run synchronously and discard the returned Promise (fire-and-forget), " +
            "so the following async targets must use the regular asynchronous invocation instead:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    private static HashSet<string> CollectAsyncJsMethods(IEnumerable<string> roots)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in roots)
        foreach (var file in EnumerateSourceFiles(root, "*.ts"))
        {
            // Skip type-declaration files; they contain signatures, not implementations.
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            var text = File.ReadAllText(file);

            // Index every class declaration so each method can be mapped to its enclosing class.
            var classMatches = TsClassRegex.Matches(text)
                .Select(m => (Name: m.Groups["class"].Value, Index: m.Index))
                .OrderBy(c => c.Index)
                .ToList();

            foreach (Match header in TsStaticMethodHeaderRegex.Matches(text))
            {
                // The header match ends at the opening parameter parenthesis.
                var openParen = header.Index + header.Length - 1;
                var closeParen = FindMatching(text, openParen, '(', ')');
                if (closeParen < 0) continue;

                // Read the return-type annotation (if any) between ')' and the method body's '{'.
                var bodyStart = SkipReturnAnnotation(text, closeParen + 1, out var returnAnnotation);

                var declaredAsync = header.Groups["async"].Success;
                var annotatedPromise = returnAnnotation.Contains("Promise", StringComparison.Ordinal);

                var returnsPromiseInBody = false;
                if (!declaredAsync && !annotatedPromise && bodyStart >= 0 && bodyStart < text.Length && text[bodyStart] == '{')
                {
                    var bodyEnd = FindMatching(text, bodyStart, '{', '}');
                    if (bodyEnd > bodyStart)
                    {
                        var body = text.Substring(bodyStart, bodyEnd - bodyStart + 1);
                        returnsPromiseInBody = TsReturnsPromiseRegex.IsMatch(body);
                    }
                }

                if (!declaredAsync && !annotatedPromise && !returnsPromiseInBody) continue;

                var owningClass = classMatches.LastOrDefault(c => c.Index < header.Index);
                if (owningClass.Name is null) continue;

                result.Add($"{owningClass.Name}.{header.Groups["method"].Value}");
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the index of the bracket that matches the opener at <paramref name="openIndex"/>, honoring
    /// strings, template literals, and comments so brackets inside them are ignored. Returns -1 if unmatched.
    /// </summary>
    private static int FindMatching(string text, int openIndex, char open, char close)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            i = SkipNonCode(text, i, out var skipped);
            if (skipped) { i--; continue; }
            if (i >= text.Length) break;

            var c = text[i];
            if (c == open) depth++;
            else if (c == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Starting at <paramref name="start"/> (just past a method's parameter list), skips whitespace and an
    /// optional ": ReturnType" annotation, returning the index of the body's opening brace (or a ';' for
    /// abstract/overload signatures). The skipped annotation text is returned via <paramref name="annotation"/>.
    /// </summary>
    private static int SkipReturnAnnotation(string text, int start, out string annotation)
    {
        annotation = string.Empty;

        var i = start;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;

        if (i >= text.Length || text[i] != ':')
        {
            return i; // No return annotation; 'i' points at the body brace (or other token).
        }

        var annotationStart = ++i; // Skip ':'.
        while (i < text.Length && text[i] != '{' && text[i] != ';')
        {
            i = SkipNonCode(text, i, out var skipped);
            if (skipped) continue;
            i++;
        }

        annotation = text.Substring(annotationStart, Math.Max(0, i - annotationStart));
        return i;
    }

    /// <summary>
    /// If a string literal, template literal, or comment starts at <paramref name="i"/>, advances past it and
    /// sets <paramref name="skipped"/> to true; otherwise leaves <paramref name="i"/> unchanged.
    /// </summary>
    private static int SkipNonCode(string text, int i, out bool skipped)
    {
        skipped = false;
        if (i >= text.Length) return i;

        var c = text[i];

        // Line comment.
        if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
        {
            skipped = true;
            i += 2;
            while (i < text.Length && text[i] != '\n') i++;
            return i;
        }

        // Block comment.
        if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
        {
            skipped = true;
            i += 2;
            while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
            return Math.Min(text.Length, i + 2);
        }

        // String / template literal.
        if (c is '"' or '\'' or '`')
        {
            skipped = true;
            var quote = c;
            i++;
            while (i < text.Length && text[i] != quote)
            {
                if (text[i] == '\\') i++; // Skip escaped character.
                i++;
            }
            return Math.Min(text.Length, i + 1);
        }

        return i;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, string pattern)
    {
        if (!Directory.Exists(root)) yield break;

        foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            // Exclude build outputs.
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            yield return file;
        }
    }

    private static string? LastTwoSegments(string identifier)
    {
        var segments = identifier.Split('.');
        if (segments.Length < 2) return null;

        return $"{segments[^2]}.{segments[^1]}";
    }

    private static string FindBlazorUiRoot([CallerFilePath] string callerFilePath = "")
    {
        // callerFilePath points at this test file; walk up to the BlazorUI source root, which is the
        // directory that contains both the Bit.BlazorUI and Bit.BlazorUI.Extras projects.
        var dir = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Bit.BlazorUI.Extras")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' and " +
            $"'Bit.BlazorUI.Extras') starting from '{callerFilePath}'.");
    }
}
