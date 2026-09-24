using System.IO;
using System.Runtime.CompilerServices;
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
