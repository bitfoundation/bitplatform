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
/// synchronously. If the target JavaScript function is asynchronous (declared <c>async</c> / returns a
/// Promise), the call becomes fire-and-forget: callers proceed before the work completes and any error is
/// lost. Since the interop is keyed by string identifiers, this test statically links every
/// <c>FastInvoke</c> call site to its TypeScript definition and fails if any target is declared <c>async</c>.
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

    // Matches a static async method declaration, e.g. "public static async parseAsync(".
    private static readonly Regex TsAsyncMethodRegex =
        new(@"\bstatic\s+async\s+(?<method>\w+)\s*(?:<[^>]+>)?\s*\(", RegexOptions.Compiled);

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

        // 2. Collect every async static JS method as "Class.method" across all TypeScript sources.
        var asyncJsMethods = CollectAsyncJsMethods(csharpDirs);

        Assert.IsTrue(asyncJsMethods.Count > 0,
            "Expected to find async TypeScript methods (e.g. BitBlazorUI.PdfReader.renderPage), but none were found. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

        // 3. A FastInvoke target declared async on the JS side violates the synchronous-only contract.
        var violations = fastInvokeTargets
            .Where(t => asyncJsMethods.Contains(t.ClassMethod))
            .Select(t => $"  - '{t.Identifier}' (async JS) invoked via FastInvoke in {Path.GetFileName(t.File)}")
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

            foreach (Match method in TsAsyncMethodRegex.Matches(text))
            {
                var owningClass = classMatches.LastOrDefault(c => c.Index < method.Index);
                if (owningClass.Name is null) continue;

                result.Add($"{owningClass.Name}.{method.Groups["method"].Value}");
            }
        }

        return result;
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
