using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.RichTextEditor;

/// <summary>
/// The editor's C# half and its JavaScript bridge are wired together by nothing but strings: the
/// setup option names the bridge reads off the payload, the JSInvokable callback names it calls
/// back with, and the BitBlazorUI.RichTextEditor.* function names the interop extensions invoke.
/// A rename on either side still compiles and then silently does nothing at runtime - a paste that
/// is no longer sanitized, a callback that never fires - which no rendering test can see. These
/// tests read the TypeScript source itself and fail on the drift.
/// </summary>
[TestClass]
public sealed class BitRichTextEditorBridgeContractTests
{
    // public static initialize(...)   /   public static async setFullScreen(...)
    private static readonly Regex ExportedFunction =
        new(@"public\s+static\s+(?:async\s+)?(?<name>\w+)\s*\(", RegexOptions.Compiled);

    // editor._dotNetRef.invokeMethodAsync('OnContentChanged', ...)
    private static readonly Regex InvokedCallback =
        new(@"invokeMethodAsync\(\s*'(?<name>\w+)'", RegexOptions.Compiled);

    // options.plainTextPaste === true
    private static readonly Regex ReadOption =
        new(@"options\.(?<name>\w+)", RegexOptions.Compiled);

    // jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setHtml", ...)
    private static readonly Regex InvokedFunction =
        new(@"""BitBlazorUI\.RichTextEditor\.(?<name>\w+)""", RegexOptions.Compiled);

    // public string[] AllowedTags { get; set; }
    private static readonly Regex PublicProperty =
        new(@"public\s+[\w\.\?<>,\[\]\s]+?\s+(?<name>\w+)\s*\{\s*get;", RegexOptions.Compiled);

    private static string ReadTypeScript()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ts-sources", "BitRichTextEditor.ts");
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure BitRichTextEditor.ts is copied to output by the test csproj.");

        return File.ReadAllText(path);
    }

    private static string ReadCSharp(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "cs-sources", fileName);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure {fileName} is copied to output by the test csproj.");

        return File.ReadAllText(path);
    }

    /// <summary>The body of the bridge's updateOptions, which is where every option is read.</summary>
    private static string ReadUpdateOptionsBody()
    {
        var source = ReadTypeScript();

        var start = source.IndexOf("public static updateOptions", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "updateOptions is gone from BitRichTextEditor.ts; this contract needs to follow it.");

        var end = source.IndexOf("public static dispose", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "dispose no longer follows updateOptions; the slice below would read the wrong code.");

        return source[start..end];
    }

    private static string CamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    // A [JSInvokable] without an identifier is invoked by the method's own name.
    private static string? JsInvokableIdentifier(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<JSInvokableAttribute>();
        return attribute is null ? null : attribute.Identifier ?? method.Name;
    }



    [TestMethod]
    public void EverySetupOptionIsReadByTheBridge()
    {
        var read = ReadOption.Matches(ReadUpdateOptionsBody())
                             .Select(m => m.Groups["name"].Value)
                             .ToHashSet(StringComparer.Ordinal);

        var declared = PublicProperty.Matches(ReadCSharp("BitRichTextEditorSetupOptions.cs"))
                                     .Select(m => CamelCase(m.Groups["name"].Value))
                                     .ToArray();

        Assert.IsTrue(declared.Length > 0, "No setup options were found; the property regex no longer matches the C# source.");

        foreach (var option in declared)
        {
            Assert.IsTrue(read.Contains(option),
                $"BitRichTextEditorSetupOptions declares '{option}', but updateOptions in BitRichTextEditor.ts never reads it, " +
                "so the parameter behind it does nothing.");
        }
    }

    [TestMethod]
    public void TheBridgeReadsNoSetupOptionThatIsNotSent()
    {
        var declared = PublicProperty.Matches(ReadCSharp("BitRichTextEditorSetupOptions.cs"))
                                     .Select(m => CamelCase(m.Groups["name"].Value))
                                     .ToHashSet(StringComparer.Ordinal);

        foreach (Match match in ReadOption.Matches(ReadUpdateOptionsBody()))
        {
            var option = match.Groups["name"].Value;
            Assert.IsTrue(declared.Contains(option),
                $"BitRichTextEditor.ts reads options.{option}, which BitRichTextEditorSetupOptions never sends, " +
                "so it silently falls back to its default forever.");
        }
    }

    [TestMethod]
    public void EveryPolicyFieldTheBridgeEnforcesIsSent()
    {
        var source = ReadTypeScript();

        var declared = PublicProperty.Matches(ReadCSharp("BitRichTextEditorPolicyPayload.cs"))
                                     .Select(m => CamelCase(m.Groups["name"].Value))
                                     .ToArray();

        Assert.IsTrue(declared.Length > 0, "No policy fields were found; the property regex no longer matches the C# source.");

        foreach (var field in declared)
        {
            // A field the sanitizer never looks at is an allowlist the host thinks it configured.
            Assert.IsTrue(source.Contains($"policy.{field}", StringComparison.Ordinal)
                       || source.Contains($"policy && policy.{field}", StringComparison.Ordinal),
                $"BitRichTextEditorPolicyPayload sends '{field}', but the bridge's sanitizer never reads it.");
        }
    }

    [TestMethod]
    public void EveryJsInvokableCallbackIsCalledByTheBridge()
    {
        var called = InvokedCallback.Matches(ReadTypeScript())
                                    .Select(m => m.Groups["name"].Value)
                                    .ToHashSet(StringComparer.Ordinal);

        var declared = typeof(BitRichTextEditor)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(JsInvokableIdentifier)
            .Where(id => string.IsNullOrEmpty(id) is false)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(declared.Length > 0, "BitRichTextEditor exposes no JSInvokable callbacks; the reflection above is wrong.");

        foreach (var callback in declared)
        {
            Assert.IsTrue(called.Contains(callback!),
                $"BitRichTextEditor exposes the JSInvokable '{callback}', but nothing in BitRichTextEditor.ts calls it.");
        }
    }

    [TestMethod]
    public void TheBridgeCallsNoCallbackTheComponentDoesNotExpose()
    {
        var declared = typeof(BitRichTextEditor)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(JsInvokableIdentifier)
            .Where(id => string.IsNullOrEmpty(id) is false)
            .ToHashSet(StringComparer.Ordinal)!;

        foreach (Match match in InvokedCallback.Matches(ReadTypeScript()))
        {
            var callback = match.Groups["name"].Value;
            Assert.IsTrue(declared.Contains(callback),
                $"BitRichTextEditor.ts calls back into '{callback}', which BitRichTextEditor does not expose as a " +
                "JSInvokable - the call throws at runtime and the feature behind it never reports anything.");
        }
    }

    [TestMethod]
    public void EveryBridgeFunctionTheComponentCallsExists()
    {
        var exported = ExportedFunction.Matches(ReadTypeScript())
                                       .Select(m => m.Groups["name"].Value)
                                       .ToHashSet(StringComparer.Ordinal);

        var called = InvokedFunction.Matches(ReadCSharp("BitRichTextEditorJsRuntimeExtensions.cs"))
                                    .Select(m => m.Groups["name"].Value)
                                    .Distinct(StringComparer.Ordinal)
                                    .ToArray();

        Assert.IsTrue(called.Length > 0, "No bridge calls were found; the invocation regex no longer matches the C# source.");

        foreach (var function in called)
        {
            Assert.IsTrue(exported.Contains(function),
                $"The interop extensions call BitBlazorUI.RichTextEditor.{function}, which the bridge does not export.");
        }
    }

    [TestMethod]
    public void EveryBridgeFunctionIsReachableFromTheComponent()
    {
        var called = InvokedFunction.Matches(ReadCSharp("BitRichTextEditorJsRuntimeExtensions.cs"))
                                    .Select(m => m.Groups["name"].Value)
                                    .ToHashSet(StringComparer.Ordinal);

        foreach (Match match in ExportedFunction.Matches(ReadTypeScript()))
        {
            var function = match.Groups["name"].Value;
            Assert.IsTrue(called.Contains(function),
                $"The bridge exports {function}(), which nothing in C# calls - either it is dead code that still ships " +
                "to every visitor, or the call site was renamed and the feature is gone.");
        }
    }
}
