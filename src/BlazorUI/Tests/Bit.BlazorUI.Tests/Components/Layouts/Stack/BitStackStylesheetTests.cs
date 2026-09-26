using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Stack;

/// <summary>
/// Pins the contract between the default gap the stack writes and the stylesheet that resolves it, which a bUnit render
/// cannot see: the component points the gap at a private custom property, and the stylesheet declares that property
/// on every stack from the public --bit-Stack-gap with the spacing scale of the theme as its fallback.
/// </summary>
[TestClass]
public class BitStackStylesheetTests : BunitTestContext
{
    [TestMethod]
    public void BitStackShouldPointTheDefaultGapAtTheVariableTheStylesheetDeclares()
    {
        var component = RenderComponent<BitStack>();

        StringAssert.Contains(component.Find(".bit-stc").GetAttribute("style")!, "gap:var(--bit-stc-dgap)");

        // Declared on the root class of every stack, so a nested stack resolves the public variable against its own
        // ancestors rather than inheriting the value its parent resolved to.
        var root = GetBlock(ReadStylesheet(), "\n.bit-stc {");

        StringAssert.Contains(root, "--bit-stc-dgap: var(--bit-Stack-gap, #{spacing(2)});");
    }

    [TestMethod]
    public void BitStackShouldNotUseTheDefaultGapWhenOneIsGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Gap, "2rem");
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        StringAssert.Contains(style, "gap:2rem");
        Assert.IsFalse(style.Contains("--bit-stc-dgap"));
    }

    [TestMethod]
    public void BitStackShouldNotUseTheDefaultGapWhenASizeIsGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        StringAssert.Contains(style, "gap:var(--bit-stc-size)");
        Assert.IsFalse(style.Contains("--bit-stc-dgap"));
    }

    [TestMethod]
    public void BitStackShouldNotLetAResponsiveStackInheritTheAlignmentOfTheOneAroundIt()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.HorizontalMd, true);
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<BitStack>(0);
                builder.AddAttribute(1, nameof(BitStack.HorizontalMd), (bool?)true);
                builder.CloseComponent();
            });
        });

        var stacks = component.FindAll(".bit-stc-rsp");

        // The outer stack hands its base alignment over inline, the inner one declares none - so it would read the
        // outer one's through inheritance unless the class empties the base on every responsive stack.
        StringAssert.Contains(stacks[0].GetAttribute("style")!, "--bit-stc-ai:center");
        Assert.IsFalse(stacks[1].GetAttribute("style")!.Contains("--bit-stc-ai"));

        var rsp = GetBlock(ReadStylesheet(), "\n.bit-stc-rsp {");

        StringAssert.Contains(rsp, "--bit-stc-ai: initial;");
        StringAssert.Contains(rsp, "--bit-stc-jc: initial;");
    }

    [TestMethod]
    public void BitStackShouldLetTheHiddenAttributeWinOverTheInlineDisplay()
    {
        // Rendered through a fragment because the stack gathers attributes it has no parameter for in SetParametersAsync
        // rather than through CaptureUnmatchedValues, which is what the AddUnmatched of bUnit requires.
        var component = Context!.Render(builder =>
        {
            builder.OpenComponent<BitStack>(0);
            builder.AddAttribute(1, "hidden", true);
            builder.CloseComponent();
        });

        var stack = component.Find(".bit-stc");

        // The inline display outranks the display:none of the user agent, so only an important rule can hide it.
        Assert.IsTrue(stack.HasAttribute("hidden"));
        StringAssert.Contains(stack.GetAttribute("style")!, "display:flex");

        // Matched on what the rule does rather than how it is laid out, so it holds nested or written out in full and
        // with any whitespace or comments inside. hidden="until-found" is left to the browser, which reveals it on a
        // find-in-page match.
        var rule = new Regex(@"(?:&|\.bit-stc)\[hidden\]:not\(\[hidden=""until-found""\s+i\]\)\s*\{[^}]*display:\s*none\s*!important");

        Assert.IsTrue(rule.IsMatch(ReadStylesheet()), "A [hidden] stack is not hidden by an important display:none.");
    }

    private static string GetBlock(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf(selector, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Layouts", "Stack", "BitStack.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
