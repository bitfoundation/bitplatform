using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

/// <summary>
/// Pins what a bUnit render cannot see of the message: the color its unfilled variants put on the page as text, which
/// only exists in CSS.
/// </summary>
[TestClass]
public class BitMessageStylesheetTests
{
    [TestMethod,
        DataRow("\n.bit-msg-otl {"),
        DataRow("\n.bit-msg-txt {")]
    public void BitMessageUnfilledVariantsShouldWriteTheirTextInTheRoleForeground(string variant)
    {
        var stylesheet = ReadStylesheet();

        // The two variants drop the fill and leave the text on the page's own surface, so it reads the role's fg - the
        // shade meant to be read there, which for a background or border role is its on color (BitColorRoleMapsTests).
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, variant), "--bit-msg-fg: var(--bit-msg-clr-txt);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-msg-#{$role} {"), "--bit-msg-clr-txt: #{role($tokens, fg)};");
    }

    [TestMethod]
    public void BitMessageUnfilledVariantsShouldNeverWriteTheirTextInTheRoleMain()
    {
        var roles = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    .bit-msg-#{$role} {");

        // A role's main is picked to be a fill; a background role's main as text on the page is the page itself.
        Assert.IsFalse(roles.Contains("--bit-msg-clr-txt: #{role($tokens, main)};"), "The unfilled text is the role's main.");
    }

    [TestMethod]
    public void BitMessageTintedVariantShouldWashTheSurfaceWithTheRoleTint()
    {
        var stylesheet = ReadStylesheet();

        // The tint is a faint wash of the role, picked to keep the fg text above it readable.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-msg-tnt {"), "--bit-msg-bg: var(--bit-msg-clr-tint);");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-msg-#{$role} {"), "--bit-msg-clr-tint: #{role($tokens, tint)};");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Notifications", "Message", "BitMessage.scss");
}
