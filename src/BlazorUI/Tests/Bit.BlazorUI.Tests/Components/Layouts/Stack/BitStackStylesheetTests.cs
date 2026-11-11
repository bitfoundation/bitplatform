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

        Assert.Contains("gap:var(--bit-stc-dgap)", component.Find(".bit-stc").GetAttribute("style")!);

        // Declared on the root class of every stack, so a nested stack resolves the public variable against its own
        // ancestors rather than inheriting the value its parent resolved to.
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-stc {");

        Assert.Contains("--bit-stc-dgap: var(--bit-Stack-gap, #{spacing(2)});", root);
    }

    [TestMethod]
    public void BitStackShouldNotUseTheDefaultGapWhenOneIsGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Gap, "2rem");
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        Assert.Contains("gap:2rem", style);
        Assert.DoesNotContain("--bit-stc-dgap", style);
    }

    [TestMethod]
    public void BitStackShouldNotUseTheDefaultGapWhenASizeIsGiven()
    {
        var component = RenderComponent<BitStack>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
        });

        var style = component.Find(".bit-stc").GetAttribute("style")!;

        Assert.Contains("gap:var(--bit-stc-size)", style);
        Assert.DoesNotContain("--bit-stc-dgap", style);
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
        Assert.Contains("--bit-stc-ai:center", stacks[0].GetAttribute("style")!);
        Assert.DoesNotContain("--bit-stc-ai", stacks[1].GetAttribute("style")!);

        var rsp = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-stc-rsp {");

        Assert.Contains("--bit-stc-ai: initial;", rsp);
        Assert.Contains("--bit-stc-jc: initial;", rsp);
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
        Assert.Contains("display:flex", stack.GetAttribute("style")!);

        // Matched on what the rule does rather than how it is laid out, so it holds nested or written out in full and
        // with any whitespace or comments inside. hidden="until-found" is left to the browser, which reveals it on a
        // find-in-page match.
        var rule = new Regex(@"(?:&|\.bit-stc)\[hidden\]:not\(\[hidden=""until-found""\s+i\]\)\s*\{[^}]*display:\s*none\s*!important");

        Assert.IsTrue(rule.IsMatch(ReadStylesheet()), "A [hidden] stack is not hidden by an important display:none.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Stack", "BitStack.scss");
}
