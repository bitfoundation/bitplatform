using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

/// <summary>
/// Pins the parts of the error boundary that live in its stylesheet, which a bUnit render cannot see: the public CSS
/// variables (each read with a fallback and never declared, listed in the header of the stylesheet and in the table
/// of the demo page) and the focus ring of the exception block.
/// </summary>
[TestClass]
public class BitErrorBoundaryStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-ErrorBoundary-background",
        "--bit-ErrorBoundary-border",
        "--bit-ErrorBoundary-radius",
        "--bit-ErrorBoundary-padding",
        "--bit-ErrorBoundary-gap",
        "--bit-ErrorBoundary-align",
        "--bit-ErrorBoundary-icon-color",
        "--bit-ErrorBoundary-icon-size",
        "--bit-ErrorBoundary-title-color",
        "--bit-ErrorBoundary-message-color",
        "--bit-ErrorBoundary-message-max-width",
        "--bit-ErrorBoundary-exception-background",
        "--bit-ErrorBoundary-exception-color",
        "--bit-ErrorBoundary-exception-font-family",
        "--bit-ErrorBoundary-exception-max-height",
    ];

    [TestMethod]
    public void BitErrorBoundaryShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(stylesheet, @"var\((--bit-ErrorBoundary-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({variable}, [^)]"), $"{variable} is read without a fallback.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitErrorBoundaryShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "ErrorBoundary", "BitErrorBoundaryDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-ErrorBoundary-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    // The exception block takes the focus to stay reachable with a keyboard (WCAG 2.1.1), so it is drawn with the
    // ring every control uses while it holds it (WCAG 2.4.7).
    [TestMethod]
    public void BitErrorBoundaryShouldDrawTheSharedFocusRingOnTheExceptionBlock()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-erb-exp {");

        StringAssert.Contains(block, "&:focus-visible {");
        StringAssert.Contains(block, "@include focus-ring;");
    }

    // The root AutoFocus parks the focus on is an anchor, as a dialog's root is: nothing is operated there, and a ring
    // around a boundary standing in for a whole page would trace the edge of the viewport.
    [TestMethod]
    public void BitErrorBoundaryShouldDrawNoRingAroundTheRootAutoFocusParksTheFocusOn()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-erb {");

        StringAssert.Contains(block, "&.bit-erb-anc {");
        StringAssert.Contains(block, "@include focus-anchor;");
    }

    // One class each, so that a Classes.Title or Classes.Message rule of the app's wins over them, and the color handed
    // to BitText through its own variable rather than fought over with the color BitText sets.
    [TestMethod,
        DataRow("bit-erb-ttl", "--bit-ErrorBoundary-title-color"),
        DataRow("bit-erb-msg", "--bit-ErrorBoundary-message-color")]
    public void BitErrorBoundaryShouldStyleTheTitleAndTheMessageWithOneClassEach(string part, string variable)
    {
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(Regex.IsMatch(stylesheet, $@"\S[ \t]+\.{part}\b"), $".{part} is styled through a descendant selector.");

        var block = SourceFiles.GetScssBlock(stylesheet, $"\n.{part} {{");

        StringAssert.Contains(block, $"--bit-Text-color: var({variable}, ");
        Assert.IsFalse(Regex.IsMatch(block, @"^\s*color\s*:", RegexOptions.Multiline), $".{part} sets the color BitText sets.");
    }

    // A design-system decision is read off the theme, never typed into the component.
    [TestMethod]
    public void BitErrorBoundaryShouldNotHardCodeAFontFamilyOrAColor()
    {
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"font-family:\s*[a-z-]+,", RegexOptions.IgnoreCase), "A literal font stack is used instead of $tg-font-family-mono.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"#[0-9a-f]{3,8}\b", RegexOptions.IgnoreCase), "A literal color is used instead of a theme token.");
    }



    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ErrorBoundary", "BitErrorBoundary.scss");
}
