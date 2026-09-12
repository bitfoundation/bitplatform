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
/// synchronously. If the target JavaScript function returns a Promise, the call becomes fire-and-forget.
/// This test links every <c>FastInvoke</c> call site to its TypeScript definition via
/// <see cref="TsPromiseMethodScanner"/> and fails on any match.
///
/// <para>
/// Detection is header-only: a TypeScript method counts as promise-returning when it is declared <c>async</c>
/// or annotated <c>: Promise&lt;...&gt;</c>, so every Promise-returning interop method must say so in its
/// header. See <see cref="TsPromiseMethodScanner"/>.
/// </para>
/// </summary>
[TestClass]
public class FastInvokeSyncContractTests
{
    // Matches FastInvoke(...) / FastInvokeVoid(...) / FastInvoke<T>(...) - T may nest one level, as in
    // FastInvoke<Dictionary<string, string>>(...) - and captures how the JS identifier is passed: a quoted
    // literal ('id') or a bare identifier ('var') naming a local const string. The optional 'receiver' is the
    // bare identifier before the first comma: in the static form IJSRuntimeFastExtensions.FastInvoke<T>(js, "id")
    // that is the runtime and 'id'/'var' still land on the identifier; in the extension form
    // js.FastInvoke<T>(identifier, arg) it is the identifier itself and 'var' the next argument, which is why
    // resolution tries 'var' first and falls back to 'receiver'.
    private static readonly Regex FastInvokeCallRegex =
        new(@"FastInvoke(?:Void)?\s*(?:<(?:[^<>]|<[^<>]*>)*>)?\s*\(\s*(?:(?<receiver>[A-Za-z_]\w*)\s*,\s*)?(?:""(?<id>[^""]+)""|(?<var>[A-Za-z_]\w*))",
            RegexOptions.Compiled);

    // The convention for an identifier that is used more than once in an extension method (invoked, then
    // reported on) is one `const string identifier = "...";` local per extension method. A bare identifier at
    // a call site resolves to the nearest such declaration of that name that precedes it in the file: a file
    // holds several extension methods, each with its own local, so the name alone is not unique per file.
    private static readonly Regex ConstStringRegex =
        new(@"const\s+string\s+(?<name>\w+)\s*=\s*""(?<value>[^""]+)""\s*;", RegexOptions.Compiled);

    // The FastInvoke definitions themselves (whose first parameter is `this IJSRuntime jsRuntime`) are the
    // one place a call-shaped FastInvoke( is not a call site to resolve.
    private static readonly string FastExtensionsDefinitionFile =
        Path.Combine("Extensions", "JsInterop", "IJSRuntimeFastExtensions.cs");

    [TestMethod]
    public void FastInvoke_CallSites_ShouldNotTargetAsyncJavaScriptFunctions()
    {
        var blazorUiRoot = TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            // This test reads the .cs/.ts sources from disk. When the tests run from packaged binaries
            // without the source tree present, there is nothing to scan, so report inconclusive rather
            // than failing. In the repo/CI the sources are present and the contract is fully enforced.
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' " +
                "and 'Bit.BlazorUI.Extras'). The source tree is required to scan FastInvoke call sites.");
            return;
        }

        var csharpDirs = new[]
        {
            Path.Combine(blazorUiRoot, "Bit.BlazorUI"),
            Path.Combine(blazorUiRoot, "Bit.BlazorUI.Extras"),
        };

        var fastInvokeTargets = new List<(string ClassMethod, string Identifier, string File)>();
        var unresolved = new List<string>();

        foreach (var dir in csharpDirs)
        {
            foreach (var file in EnumerateSourceFiles(dir, "*.cs"))
            {
                if (file.EndsWith(FastExtensionsDefinitionFile, StringComparison.OrdinalIgnoreCase)) continue;

                var text = File.ReadAllText(file);

                var constStrings = ConstStringRegex.Matches(text)
                    .Select(m => (m.Index, Name: m.Groups["name"].Value, Value: m.Groups["value"].Value))
                    .ToList();

                foreach (Match match in FastInvokeCallRegex.Matches(text))
                {
                    string? Resolve(Group group) => group.Success
                        ? constStrings.LastOrDefault(c => c.Name == group.Value && c.Index < match.Index).Value
                        : null;

                    var identifier = match.Groups["id"].Success
                        ? match.Groups["id"].Value
                        : Resolve(match.Groups["var"]) ?? Resolve(match.Groups["receiver"]);

                    // Reduce "BitBlazorUI.Utils.getBodyWidth" to "Utils.getBodyWidth" for TS class.method lookup.
                    // This assumes TS class names are unique across the scanned sources: two classes with the
                    // same name in different namespaces would collapse to the same key and could produce a false
                    // positive. That's acceptable for the current single-project layout (one class per file,
                    // distinct class names), so the simpler last-two-segments match is preferred over tracking
                    // full namespaces. Revisit if the TypeScript sources ever introduce duplicate class names.
                    var classMethod = identifier is null ? null : LastTwoSegments(identifier);
                    if (classMethod is null)
                    {
                        var line = text.AsSpan(0, match.Index).Count('\n') + 1;
                        unresolved.Add($"  - {file}({line}): {match.Value}");
                        continue;
                    }

                    fastInvokeTargets.Add((classMethod, identifier!, file));
                }
            }
        }

        Assert.AreEqual(0, unresolved.Count,
            "Every FastInvoke/FastInvokeVoid call site must pass its JS identifier as a \"Class.method\" string " +
            "literal, or as a local `const string` declared earlier in the same file, so this test can link it " +
            "to its TypeScript definition. The following call sites could not be resolved:" +
            Environment.NewLine + string.Join(Environment.NewLine, unresolved));

        Assert.IsTrue(fastInvokeTargets.Count > 0,
            "Expected to find FastInvoke call sites to validate, but none were found. " +
            "The scanning logic in this test is likely broken or the source layout changed.");

        var asyncJsMethods = CollectAsyncJsMethods(csharpDirs);

        Assert.IsTrue(asyncJsMethods.Count > 0,
            "Expected to find async/Promise-returning TypeScript methods (e.g. BitBlazorUI.PdfReader.renderPage), but none were found. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

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
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var key in TsPromiseMethodScanner.CollectFromSource(File.ReadAllText(file)))
            {
                result.Add(key);
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

    private static string? TryFindBlazorUiRoot([CallerFilePath] string callerFilePath = "")
    {
        // callerFilePath points at this test file; walk up to the BlazorUI source root, which is the
        // directory that contains both the Bit.BlazorUI and Bit.BlazorUI.Extras projects. Returns null
        // when the source tree isn't present (e.g. running from packaged binaries) so the caller can
        // report the test as inconclusive instead of throwing.
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
}
