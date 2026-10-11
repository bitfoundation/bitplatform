using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Callout;

/// <summary>
/// Pins what ties Callouts.ts - which relocates every open popup into a chain of display: contents copies of the
/// ancestors it leaves - to the C# and the stylesheets around it. None of it is visible to a bUnit render, which
/// never runs the script: the arguments of the toggle are matched by position alone, the chain is styled by the
/// markers the script writes on it, and a callout opened inside another one sits under that one in the chain, so
/// it inherits every variable the outer one is given inline unless its own rule resets it.
/// </summary>
[TestClass]
public class BitCalloutScriptContractTests
{
    [TestMethod]
    public void CalloutsToggleShouldTakeItsArgumentsInTheOrderTheBridgeSendsThem()
    {
        var script = StripComments(ReadFile("Bit.BlazorUI", "Scripts", "Callouts.ts"));
        var bridge = StripComments(ReadFile("Bit.BlazorUI", "Extensions", "JsInterop", "CalloutsJsRuntimeExtensions.cs"));

        var signature = Regex.Match(script, @"public static toggle\((?<params>.*?)\)\s*\{", RegexOptions.Singleline);
        Assert.IsTrue(signature.Success, "Callouts.toggle was not found in Callouts.ts.");

        var parameters = Regex.Matches(signature.Groups["params"].Value, @"^\s*(\w+)\s*:", RegexOptions.Multiline)
                              .Select(m => m.Groups[1].Value)
                              .ToArray();

        var call = Regex.Match(bridge, @"""BitBlazorUI\.Callouts\.toggle"",(?<args>.*?)\);", RegexOptions.Singleline);
        Assert.IsTrue(call.Success, "The call of BitBlazorUI.Callouts.toggle was not found in CalloutsJsRuntimeExtensions.cs.");

        var arguments = call.Groups["args"].Value.Split(',').Select(a => a.Trim()).ToArray();

        // A JS function binds its arguments by position: one added on a side alone, or two swapped, shifts every
        // value after it onto the wrong parameter - the rootId the chain copies the root by among them.
        CollectionAssert.AreEqual(parameters, arguments);
    }

    [TestMethod]
    public void CalloutsChainShouldBeMarkedTheWayTheStylesheetSelectsIt()
    {
        var script = ReadFile("Bit.BlazorUI", "Scripts", "Callouts.ts");
        var general = ReadFile("Bit.BlazorUI", "Styles", "general.scss");

        // Every link of the chain copies the classes of what it stands for, so it is kept from drawing their
        // ::before and ::after loose in the body; the element the parts sit in pins their fixed positioning
        // against a descendant rule of the page that now matches them.
        StringAssert.Contains(script, "wanted.set('data-bit-callout-link', '')");
        StringAssert.Contains(script, "holder.setAttribute('data-bit-callout-holder', '')");

        StringAssert.Contains(general, "[data-bit-callout-link]::before,\n[data-bit-callout-link]::after {\n    content: none !important;");
        StringAssert.Contains(general, "[data-bit-callout-holder] > * {\n    position: fixed !important;");
    }

    [TestMethod]
    public void CalloutsOverlayClickShouldBeHeardAheadOfBlazor()
    {
        var script = StripComments(ReadFile("Bit.BlazorUI", "Scripts", "Callouts.ts"));
        var general = StripComments(ReadFile("Bit.BlazorUI", "Scripts", "general.ts"));

        StringAssert.Contains(script, "public static dismissOnOverlayClick(");

        // The overlay's own Blazor handler dismisses the innermost callout on the same click, so the listener that
        // dismisses the ones under it too has to run first and keep the click from reaching that handler: on the
        // window, in the capture phase.
        var listener = Regex.Match(general, @"window\.addEventListener\('click',(?<body>.*?)\},\s*true\);", RegexOptions.Singleline);
        Assert.IsTrue(listener.Success, "The capture-phase click listener on the window was not found in general.ts.");
        StringAssert.Contains(listener.Groups["body"].Value, "Callouts.dismissOnOverlayClick(");
        StringAssert.Contains(listener.Groups["body"].Value, "stopImmediatePropagation()");
    }

    [TestMethod]
    public void BitCalloutShouldResetEveryPrivateVariableItWritesInline()
    {
        var component = ReadFile("Bit.BlazorUI", "Components", "Surfaces", "Callout", "BitCallout.razor.cs");
        var stylesheet = SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Surfaces", "Callout", "BitCallout.scss");

        var written = Regex.Matches(component, @"\$""(--bit-clo-[a-z-]+):")
                           .Select(m => m.Groups[1].Value)
                           .Distinct()
                           .ToArray();

        Assert.IsTrue(written.Length > 0, "BitCallout no longer writes a private variable inline; this test has nothing left to pin.");

        // A callout opened inside another one is its descendant - in the page, and in the chain it is relocated
        // into - so without a reset in its own rule it would be sized by the outer one's parameters.
        foreach (var variable in written)
        {
            StringAssert.Contains(stylesheet, $"{variable}: initial;", $"{variable} is written inline but never reset, so it inherits into a nested callout.");
        }
    }

    private static string StripComments(string source)
    {
        return Regex.Replace(source, @"//[^\n]*", string.Empty);
    }

    // The path is relative to the BlazorUI folder this test file sits five levels under.
    private static string ReadFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(GetThisFile())!, "..", "..", "..", "..", "..", .. segments]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string GetThisFile([CallerFilePath] string thisFile = "") => thisFile;
}
