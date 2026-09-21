using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards the "a C# interop call names a JavaScript function that exists" contract.
///
/// <para>
/// Nothing but a string ties an interop call to the TypeScript it runs: <c>"BitBlazorUI.Utils.setStyle"</c> is
/// resolved at runtime, in the browser, so renaming or removing the JavaScript side still compiles and then
/// silently does nothing - or, on the synchronous <c>FastInvoke</c> path, throws a <c>JSException</c> in the
/// middle of a render. This test reads every <c>BitBlazorUI.*</c> identifier out of the two library projects
/// and fails when one of them has no static TypeScript method behind it.
/// </para>
/// </summary>
[TestClass]
public class JsInteropIdentifierContractTests
{
    // A JS interop identifier as the library writes it: the BitBlazorUI namespace, the class, the method.
    // Deeper paths are allowed (only the last two segments are resolved), a bare "BitBlazorUI.Something" is
    // not - that is a class, not a call.
    private static readonly Regex IdentifierRegex =
        new(@"""(?<identifier>BitBlazorUI(?:\.[A-Za-z_]\w*){2,})""", RegexOptions.Compiled);

    // The one identifier-shaped literal that is not a call: the root object itself is handed to the browser
    // by name in a couple of places, and identifiers built for a component instance are covered by the
    // FastInvoke/Invoke call sites that use them.
    private static readonly HashSet<string> KnownNonMethodIdentifiers = new(StringComparer.Ordinal);

    [TestMethod]
    public void JsInteropIdentifiers_ShouldNameAnExistingJavaScriptFunction()
    {
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            // Same as the other source-scanning contract tests: without the source tree there is nothing to
            // scan, so report inconclusive rather than failing. In the repo/CI the sources are present.
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' " +
                "and 'Bit.BlazorUI.Extras'). The source tree is required to scan interop identifiers.");
            return;
        }

        var projectDirectories = JsInteropSources.ProjectDirectories(blazorUiRoot);

        var jsMethods = CollectJsStaticMethods(projectDirectories);

        Assert.IsTrue(jsMethods.Count > 100,
            $"Expected to find the library's TypeScript methods, but only found {jsMethods.Count}. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

        var identifiers = CollectCSharpIdentifiers(projectDirectories);

        Assert.IsTrue(identifiers.Count > 100,
            $"Expected to find the library's interop identifiers, but only found {identifiers.Count}. " +
            "The C# scanning in this test is likely broken or the source layout changed.");

        var missing = identifiers
            .Where(i => KnownNonMethodIdentifiers.Contains(i.Identifier) is false)
            .Where(i => jsMethods.Contains(LastTwoSegments(i.Identifier)) is false)
            .Select(i => $"  - '{i.Identifier}' named in {Path.GetFileName(i.File)} has no static TypeScript method behind it")
            .Distinct()
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(0, missing.Count,
            "Every BitBlazorUI.* interop identifier must name a static method of one of the library's " +
            "TypeScript classes. A missing one is a call that compiles and then fails, or silently does " +
            "nothing, in the browser:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
    }

    [TestMethod]
    public void JsInteropIdentifiers_ShouldBeSpelledTheSameWayEverywhere()
    {
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            Assert.Inconclusive("Skipped: could not locate the BlazorUI source root.");
            return;
        }

        // The identifiers are case-sensitive in the browser, and the C# and TypeScript sides spell their
        // members differently by convention (PascalCase against camelCase), so a transcription that only
        // differs in case resolves to nothing at runtime while reading as correct.
        var identifiers = CollectCSharpIdentifiers(JsInteropSources.ProjectDirectories(blazorUiRoot));

        var inconsistent = identifiers
            .Select(i => i.Identifier)
            .Distinct(StringComparer.Ordinal)
            .GroupBy(i => i, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"  - {string.Join(" / ", g.OrderBy(i => i, StringComparer.Ordinal))}")
            .ToList();

        Assert.AreEqual(0, inconsistent.Count,
            "The same interop identifier is spelled with different casing in different places; only one of " +
            "the spellings can be the function the browser resolves:" +
            Environment.NewLine + string.Join(Environment.NewLine, inconsistent));
    }

    private static List<(string Identifier, string File)> CollectCSharpIdentifiers(IEnumerable<string> roots)
    {
        var result = new List<(string Identifier, string File)>();

        foreach (var root in roots)
        foreach (var file in JsInteropSources.EnumerateSourceFiles(root, "*.cs"))
        {
            foreach (Match match in IdentifierRegex.Matches(File.ReadAllText(file)))
            {
                result.Add((match.Groups["identifier"].Value, file));
            }
        }

        return result;
    }

    private static HashSet<string> CollectJsStaticMethods(IEnumerable<string> roots)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in roots)
        foreach (var file in JsInteropSources.EnumerateSourceFiles(root, "*.ts"))
        {
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var key in TsPromiseMethodScanner.CollectStaticMethodsFromSource(File.ReadAllText(file)))
            {
                result.Add(key);
            }
        }

        return result;
    }

    // "BitBlazorUI.Utils.getBodyWidth" -> "Utils.getBodyWidth", the key the TypeScript side is read into.
    private static string LastTwoSegments(string identifier)
    {
        var segments = identifier.Split('.');
        return $"{segments[^2]}.{segments[^1]}";
    }
}
