using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TagsInput;

/// <summary>
/// The tags input's C# half and its script side are wired together by nothing but strings: the two
/// BitBlazorUI.TagsInput.* function names the interop extensions invoke, the data attributes the
/// script reads its settings off, and the class name it tells a chip by. Every one of those calls is
/// made inside a catch that swallows a missing function - the keys still work then, only with their
/// browser defaults - so a rename on either side compiles, renders, passes every rendering test, and
/// then silently gives back a field whose Enter submits the form, whose separators are typed into
/// the input and whose chips scroll the page under the arrow keys. These tests read the TypeScript
/// source itself and fail on the drift.
/// </summary>
[TestClass]
public sealed class BitTagsInputBridgeContractTests
{
    // public static setup(input: HTMLInputElement, isEdit: boolean = false)
    private static readonly Regex ExportedFunction =
        new(@"public\s+static\s+(?:async\s+)?(?<name>\w+)\s*\(", RegexOptions.Compiled);

    // jsRuntime.InvokeVoid("BitBlazorUI.TagsInput.setup", ...)
    private static readonly Regex InvokedFunction =
        new(@"""BitBlazorUI\.TagsInput\.(?<name>\w+)""", RegexOptions.Compiled);

    // input.dataset.noAddOnTab / root.dataset.bitTgiTagsSetup
    private static readonly Regex ReadDataset =
        new(@"\.dataset\.(?<name>\w+)", RegexOptions.Compiled);

    // classList.contains('bit-tgi-tag') / closest('.bit-tgi-tag')
    private static readonly Regex TouchedClass =
        new(@"['""]\.?(?<name>bit-tgi-[\w-]+)['""]", RegexOptions.Compiled);

    // The dataset entries the script writes on itself to remember that an element is already wired,
    // which no markup declares because nothing but the script ever reads them back.
    private static readonly string[] SetupMarkers = ["bitTgiSetup", "bitTgiTagsSetup"];

    private static string ReadTypeScript()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ts-sources", "BitTagsInput.ts");
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure BitTagsInput.ts is copied to output by the test csproj.");

        return File.ReadAllText(path);
    }

    private static string ReadCSharp(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "cs-sources", fileName);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure {fileName} is copied to output by the test csproj.");

        return File.ReadAllText(path);
    }

    /// <summary>
    /// A data attribute as the markup spells it - data-no-add-on-tab - from the camel cased name the
    /// script reads it back by, which is what the DOMStringMap does with it.
    /// </summary>
    private static string ToAttributeName(string datasetKey)
    {
        return "data-" + Regex.Replace(datasetKey, "([A-Z])", m => "-" + m.Value.ToLowerInvariant());
    }

    [TestMethod]
    public void BitTagsInputEveryInvokedFunctionExistsOnTheScriptSide()
    {
        var invoked = InvokedFunction.Matches(ReadCSharp("BitTagsInputJsRuntimeExtensions.cs"))
                                     .Select(m => m.Groups["name"].Value)
                                     .Distinct()
                                     .ToList();

        Assert.IsTrue(invoked.Count > 0, "No BitBlazorUI.TagsInput.* call was found; the extensions file is no longer the bridge's C# half.");

        var exported = ExportedFunction.Matches(ReadTypeScript())
                                       .Select(m => m.Groups["name"].Value)
                                       .ToHashSet(StringComparer.Ordinal);

        foreach (var name in invoked)
        {
            Assert.IsTrue(exported.Contains(name),
                $"The component invokes BitBlazorUI.TagsInput.{name}, which the script does not export. " +
                "The call is made inside a catch, so this fails silently at runtime instead of throwing.");
        }
    }

    [TestMethod]
    public void BitTagsInputEveryScriptFunctionIsInvokedByTheComponent()
    {
        var extensions = ReadCSharp("BitTagsInputJsRuntimeExtensions.cs");

        var exported = ExportedFunction.Matches(ReadTypeScript())
                                       .Select(m => m.Groups["name"].Value)
                                       .Distinct()
                                       .ToList();

        foreach (var name in exported)
        {
            StringAssert.Contains(extensions, $"BitBlazorUI.TagsInput.{name}",
                $"The script exports {name}, which nothing on the C# side calls. Either the component stopped " +
                "needing it, in which case it is dead code shipped to every visitor, or the call was renamed.");
        }
    }

    [TestMethod]
    public void BitTagsInputEveryDataAttributeTheScriptReadsIsWrittenByTheMarkup()
    {
        var markup = ReadCSharp("BitTagsInput.razor");

        var keys = ReadDataset.Matches(ReadTypeScript())
                              .Select(m => m.Groups["name"].Value)
                              .Distinct()
                              .Where(k => SetupMarkers.Contains(k) is false)
                              .ToList();

        Assert.IsTrue(keys.Count > 0, "No dataset read was found; the script no longer takes its settings off the element.");

        foreach (var key in keys)
        {
            var attribute = ToAttributeName(key);

            StringAssert.Contains(markup, attribute,
                $"The script reads dataset.{key}, so the markup has to write {attribute}. Nothing declares that " +
                "pairing but these two spellings, and a settings attribute the script cannot find reads as the " +
                "feature being turned off rather than as an error.");
        }
    }

    [TestMethod]
    public void BitTagsInputEveryClassTheScriptMatchesIsRenderedByTheMarkup()
    {
        var markup = ReadCSharp("BitTagsInput.razor") + ReadCSharp("BitTagsInput.razor.cs");

        var classes = TouchedClass.Matches(ReadTypeScript())
                                  .Select(m => m.Groups["name"].Value)
                                  .Distinct()
                                  .ToList();

        Assert.IsTrue(classes.Count > 0, "No bit-tgi-* class was found; the script no longer tells a chip from anything else.");

        foreach (var name in classes)
        {
            StringAssert.Contains(markup, name,
                $"The script looks for .{name}, which the component no longer renders. The keys the chips answer " +
                "to would keep their browser defaults - Home scrolling the page, Alt with an arrow leaving it.");
        }
    }

    [TestMethod]
    public void BitTagsInputTheScriptHoldsBackOnlyTheKeysTheComponentActsOn()
    {
        // The list of keys whose browser default the script takes away from a focused chip. Holding back
        // a key the component does nothing with takes a scroll away from the user and gives nothing back,
        // and failing to hold back one it does act on lets the browser act first - so the two lists are
        // the same list, and this is what says so.
        var script = ReadTypeScript();

        var listed = Regex.Match(script, @"tagKeys\s*=\s*\[(?<keys>[^\]]*)\]");

        Assert.IsTrue(listed.Success, "The script no longer declares the keys it holds back for a chip.");

        var keys = Regex.Matches(listed.Groups["keys"].Value, @"'(?<key>[^']+)'")
                        .Select(m => m.Groups["key"].Value)
                        .ToHashSet(StringComparer.Ordinal);

        var handled = ReadCSharp("BitTagsInput.razor.cs");

        // The handler of a chip's keydown, which is where every one of them is answered.
        var body = Substring(handled, "private async Task HandleOnTagKeyDown(", "private void FocusTag(");

        foreach (var key in keys)
        {
            StringAssert.Contains(body, $"\"{key}\"",
                $"The script takes the browser's default for {key} away from a focused chip, and the component " +
                "does nothing with it: the key is simply dead there.");
        }

        // The arrow keys are answered through the RTL aware previousKey / nextKey pair rather than by name.
        foreach (var key in new[] { "ArrowLeft", "ArrowRight" })
        {
            Assert.IsTrue(keys.Contains(key), $"The component walks the chips with {key}, which the browser acts on first unless the script holds it back.");
        }
    }

    private static string Substring(string source, string from, string to)
    {
        var start = source.IndexOf(from, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"'{from}' was not found; the contract test no longer reads what it was written to read.");

        var end = source.IndexOf(to, start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, $"'{to}' was not found after '{from}'; the contract test no longer reads what it was written to read.");

        return source[start..end];
    }
}
