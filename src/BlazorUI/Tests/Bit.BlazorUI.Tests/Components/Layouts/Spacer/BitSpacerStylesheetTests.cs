using System.Text.RegularExpressions;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Spacer;

/// <summary>
/// Pins the contract between a sized spacer and the stylesheet that resolves it, which a bUnit render cannot see: the
/// component writes the fixed space of a Size as a private custom property on the axis it targets, and the stylesheet
/// declares that property per size class from the public --bit-Spacer-*-size variable of that step, with the spacing
/// scale of the theme as its fallback - read with a fallback and never declared, so a value set on :root, an ancestor or
/// the spacer's own Style re-sizes the step.
/// </summary>
[TestClass]
public class BitSpacerStylesheetTests : BunitTestContext
{
    [TestMethod,
        DataRow(BitSize.Small, "bit-spc-sm", "--bit-Spacer-small-size", "spacing(1)"),
        DataRow(BitSize.Medium, "bit-spc-md", "--bit-Spacer-medium-size", "spacing(2)"),
        DataRow(BitSize.Large, "bit-spc-lg", "--bit-Spacer-large-size", "spacing(4)")]
    public void BitSpacerShouldPointASizedSpacerAtTheVariableTheStylesheetDeclares(BitSize size, string className, string variable, string fallback)
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        var root = component.Find(".bit-spc");

        // The component names no length of its own: the size class is what picks the step, and the margin reads it back.
        Assert.IsTrue(root.ClassList.Contains(className));
        Assert.AreEqual("margin-inline-start:var(--bit-spc-size)", root.GetAttribute("style"));

        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n.{className} {{", $"No rule for .{className}.");

        StringAssert.Contains(block, $"--bit-spc-size: var({variable}, #{{{fallback}}});");
    }

    [TestMethod]
    public void BitSpacerShouldReadTheSizeOnTheBlockAxisOfAVerticalSpacer()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Medium);
            parameters.Add(p => p.Vertical, true);
        });

        var root = component.Find(".bit-spc");

        Assert.IsTrue(root.ClassList.Contains("bit-spc-md"));
        Assert.AreEqual("margin-block-start:var(--bit-spc-size)", root.GetAttribute("style"));
    }

    [TestMethod]
    public void BitSpacerShouldNotReadTheSizeWhenAMoreSpecificLengthIsGiven()
    {
        var component = RenderComponent<BitSpacer>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Large);
            parameters.Add(p => p.Gap, "2rem");
        });

        var style = component.Find(".bit-spc").GetAttribute("style")!;

        // Gap is the more specific of the two, so the step the class resolves is left unread on that axis.
        StringAssert.Contains(style, "margin-inline-start:2rem");
        Assert.IsFalse(style.Contains("--bit-spc-size"));
    }

    [TestMethod]
    public void BitSpacerShouldNeverDeclareAPublicVariable()
    {
        // A public variable is read off the root with a fallback and never declared, so it inherits from wherever an
        // app sets it; a declaration here would cut :root and every ancestor off.
        var code = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(code, @"--bit-Spacer-[a-z-]+\s*:"), "A public --bit-Spacer-* variable is declared by the stylesheet.");

        // And the one private property is only ever declared from a public one.
        foreach (Match declaration in Regex.Matches(code, @"--bit-spc-size\s*:\s*(?<value>[^;]+);"))
        {
            StringAssert.Matches(declaration.Groups["value"].Value, new Regex(@"^var\(--bit-Spacer-[a-z]+-size, #\{spacing\([0-9.]+\)\}\)$"));
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Spacer", "BitSpacer.scss");
}
