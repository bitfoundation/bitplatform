using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Container;

/// <summary>
/// Pins the contract between the container and the stylesheet that lays it out, which a bUnit render cannot see: the
/// centered column, the parameters winning over the public variables they restyle the default of, the gutter stepping
/// with the breakpoints, and the resets that keep a nested container from inheriting an outer one's choice.
/// </summary>
[TestClass]
public class BitContainerStylesheetTests
{
    [TestMethod]
    public void BitContainerShouldCenterItselfInAColumnOfItsMaximumWidth()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnt {");

        // The maximum width includes the gutters, so two containers of the same width line their content up. The display
        // is set rather than left to the tag, so an inline Element (a span, an a) is still capped and centered.
        StringAssert.Contains(root, "display: block;");
        StringAssert.Contains(root, "width: 100%;");
        StringAssert.Contains(root, "margin-inline: auto;");
        StringAssert.Contains(root, "box-sizing: border-box;");
    }

    [TestMethod]
    public void BitContainerShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnt {");

        // MaxWidth and NoGutter publish these only while they are set, so they are read before the variable, which only
        // restyles the default an unset one stands for.
        StringAssert.Contains(root, "max-width: var(--bit-cnt-max-width, var(--bit-Container-max-width, #{rem2(1280px)}));");
        StringAssert.Contains(root, "padding-inline: var(--bit-cnt-gut, var(--bit-Container-padding-inline, var(--bit-cnt-dgut)));");
        StringAssert.Contains(root, "padding-block: var(--bit-Container-padding-block, 0);");

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-cnt-ngt {"), "--bit-cnt-gut: 0;");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Container-[a-z-]+, var\(--bit-cnt-(gut|max-width)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitContainerShouldPublishItsParametersOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssDeclarations(ReadStylesheet(), "\n.bit-cnt {");

        // A container can sit in the content of another one, which must not inherit the outer container's MaxWidth or
        // NoGutter: each root starts the values its parameters publish out unset, and the class - declared further down
        // at the same weight - and the inline style still win on the root that carries them.
        foreach (var property in new[] { "--bit-cnt-gut", "--bit-cnt-max-width" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitContainerShouldWidenItsDefaultGutterWithTheBreakpoints()
    {
        var stylesheet = ReadStylesheet();

        // The default gutter is declared on every root, so a nested container steps with the window rather than with
        // its ancestor, and each breakpoint moves the variable rather than the padding itself.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-cnt {"), "--bit-cnt-dgut: #{spacing(2)};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n@media (min-width: $mq-sm-min) {"), "--bit-cnt-dgut: #{spacing(3)};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n@media (min-width: $mq-md-min) {"), "--bit-cnt-dgut: #{spacing(4)};");
    }

    [TestMethod]
    public void BitContainerShouldNeverDeclareAPublicVariable()
    {
        // A public variable is read off the root with a fallback and never declared, so it inherits from wherever an
        // app sets it; a declaration here would cut :root and every ancestor off.
        var code = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(code, @"--bit-Container-[a-z-]+\s*:"), "A public --bit-Container-* variable is declared by the stylesheet.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Layouts", "Container", "BitContainer.scss");
}
