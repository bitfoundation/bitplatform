using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards the "a C# interop call passes what its JavaScript function takes" contract.
///
/// <para>
/// The two halves of every interop call are wired together by nothing but a string and a positional argument
/// list, which neither compiler checks. Drop a parameter on the TypeScript side and the C# keeps passing it;
/// add one and every call silently receives <c>undefined</c> for it. Both compile, ship, and then misbehave
/// in the browser - and on the synchronous <c>FastInvoke</c> path there is not even a promise rejection to
/// notice. This test reads the argument count of every <c>BitBlazorUI.*</c> call site and checks it against
/// the parameter list of the TypeScript method it names.
/// </para>
///
/// <para>
/// Only call sites whose identifier is a string literal are checked; the parameter list is read from the
/// method header, so an optional (<c>id?: string</c>) or defaulted (<c>gap = 0</c>) parameter may be left
/// out and a rest parameter takes any number.
/// </para>
/// </summary>
[TestClass]
public class JsInteropArgumentCountContractTests
{
    // An interop call whose identifier is a literal: the framework's own InvokeAsync/InvokeVoidAsync, the
    // library's Invoke/InvokeVoid guards, and the FastInvoke pair.
    private static readonly Regex InteropCallRegex =
        new(@"\b(?:Fast)?Invoke(?:Void)?(?:Async)?\s*(?:<(?:[^<>]|<[^<>]*>)*>)?\s*\(\s*""(?<identifier>BitBlazorUI(?:\.\w+)+)""",
            RegexOptions.Compiled);

    // A leading argument that is the overload's timeout or cancellation token rather than something passed on
    // to JavaScript. Written out as the handful of shapes the library actually uses, so that a new shape fails
    // here - loudly and with the call site named - instead of being silently mistaken for a JS argument.
    private static readonly Regex TimeoutOrTokenArgumentRegex =
        new(@"^(CancellationToken\.None|default|_?cancellationToken|[\w.]+\.Token|TimeSpan\.[^,]*|_?timeout)$",
            RegexOptions.Compiled);

    [TestMethod]
    public void JsInteropCallSites_ShouldPassWhatTheirJavaScriptFunctionTakes()
    {
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' " +
                "and 'Bit.BlazorUI.Extras'). The source tree is required to scan interop call sites.");
            return;
        }

        var projectDirectories = JsInteropSources.ProjectDirectories(blazorUiRoot);

        var signatures = CollectJsSignatures(projectDirectories);

        Assert.IsTrue(signatures.Count > 100,
            $"Expected to find the library's TypeScript methods, but only found {signatures.Count}. " +
            "The TypeScript parsing in this test is likely broken or the source layout changed.");

        var checkedCalls = 0;
        var mismatches = new List<string>();

        foreach (var directory in projectDirectories)
        foreach (var file in JsInteropSources.EnumerateSourceFiles(directory, "*.cs"))
        {
            var text = File.ReadAllText(file);
            var masked = TsPromiseMethodScanner.MaskNonCode(text);

            foreach (Match call in InteropCallRegex.Matches(text))
            {
                var identifier = call.Groups["identifier"].Value;

                var openParen = text.IndexOf('(', call.Index);
                if (openParen < 0) continue;

                var closeParen = TsPromiseMethodScanner.FindCloseBracket(masked, openParen, '(', ')');
                if (closeParen < 0) continue;

                var arguments = TsPromiseMethodScanner.SplitTopLevel(masked, openParen, closeParen)
                    .Select(range => text[range.Start..range.End].Trim())
                    .Where(argument => argument.Length > 0)
                    .Skip(1) // the identifier itself
                    .ToList();

                // The timeout and cancellation-token overloads take theirs before the JS arguments.
                if (arguments.Count > 0 && TimeoutOrTokenArgumentRegex.IsMatch(arguments[0]))
                {
                    arguments.RemoveAt(0);
                }

                var key = LastTwoSegments(identifier);
                if (signatures.TryGetValue(key, out var signature) is false) continue; // the identifier test owns this

                checkedCalls++;

                if (signature.HasRestParameter) continue;

                if (arguments.Count < signature.RequiredParameters ||
                    arguments.Count > signature.RequiredParameters + signature.OptionalParameters)
                {
                    var line = text.AsSpan(0, call.Index).Count('\n') + 1;
                    var takes = signature.OptionalParameters == 0
                        ? $"{signature.RequiredParameters}"
                        : $"{signature.RequiredParameters} to {signature.RequiredParameters + signature.OptionalParameters}";

                    mismatches.Add(
                        $"  - {Path.GetFileName(file)}({line}): '{identifier}' is passed {arguments.Count} argument(s), " +
                        $"but the JavaScript function takes {takes}");
                }
            }
        }

        Assert.IsTrue(checkedCalls > 100,
            $"Expected to check the library's interop call sites, but only found {checkedCalls}. " +
            "The C# scanning in this test is likely broken or the source layout changed.");

        Assert.AreEqual(0, mismatches.Count,
            "Every interop call must pass the arguments its JavaScript function declares. A call that passes " +
            "too few leaves the rest undefined in the browser; one that passes too many is carrying a " +
            "parameter that no longer exists:" + Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    private static Dictionary<string, TsPromiseMethodScanner.TsMethodSignature> CollectJsSignatures(IEnumerable<string> roots)
    {
        var result = new Dictionary<string, TsPromiseMethodScanner.TsMethodSignature>(StringComparer.Ordinal);

        foreach (var root in roots)
        foreach (var file in JsInteropSources.EnumerateSourceFiles(root, "*.ts"))
        {
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var (key, signature) in TsPromiseMethodScanner.CollectSignaturesFromSource(File.ReadAllText(file)))
            {
                // First declaration wins, the same way a lookup by Class.method could not tell two apart.
                if (result.ContainsKey(key)) continue;

                result[key] = signature;
            }
        }

        return result;
    }

    private static string LastTwoSegments(string identifier)
    {
        var segments = identifier.Split('.');
        return $"{segments[^2]}.{segments[^1]}";
    }
}
