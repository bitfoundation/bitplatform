using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Collapse;

/// <summary>
/// Pins the public --bit-Collapse-* variables against the stylesheet that reads them, which a bUnit render cannot
/// see: every variable the header documents is read somewhere with a fallback, none of them is ever declared (so
/// they keep inheriting from :root and the ancestors), nothing is read that the header does not document, and the
/// private tokens the parameters arrive as never leak into a nested collapse.
/// </summary>
[TestClass]
public partial class BitCollapseStylesheetTests
{
    [TestMethod]
    public void BitCollapseShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            // A variable documented as unset by default is read bare on purpose: an unset one invalidates what
            // reads it, which is how the peek fade applies no mask at all rather than one that fades nothing.
            if (Regex.IsMatch(stylesheet, $@"^//\s+{Regex.Escape(name)}\s.*\(default: unset", RegexOptions.Multiline))
            {
                StringAssert.Contains(stylesheet, $"var({name})", $"{name} is documented as unset but never read bare.");
                Assert.IsFalse(stylesheet.Contains($"var({name}, "), $"{name} is documented as unset but read with a fallback.");
                continue;
            }

            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitCollapseShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitCollapseShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Collapse-* variable is declared, which stops it inheriting.");
    }

    [TestMethod,
        DataRow("--bit-col-dur-full"),
        DataRow("--bit-col-del-full"),
        DataRow("--bit-col-eas"),
        DataRow("--bit-col-csz")]
    public void BitCollapseShouldStartTheInlineTokensOutUnsetOnEveryRoot(string token)
    {
        // The parameters arrive inline as private tokens, which inherit: each root has to start them out unset so a
        // collapse nested in one given a Duration or a CollapsedSize does not take them on as its own.
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-col {");

        StringAssert.Contains(block, $"{token}: initial;");
    }

    [TestMethod]
    public void BitCollapseShouldLetTheParametersWinOverThePublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // The inline -full tokens are read ahead of the public variables...
        StringAssert.Contains(stylesheet, "var(--bit-col-dur-full, var(--bit-Collapse-duration, ");
        StringAssert.Contains(stylesheet, "var(--bit-col-eas, var(--bit-Collapse-easing, ");

        // ...and the Background classes paint the theme color outright rather than through the variable.
        foreach (var kind in new[] { "pbg", "sbg", "tbg", "rbg" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-col-{kind} {{");

            Assert.IsFalse(block.Contains("--bit-Collapse-"), $"The {kind} background reads a public variable.");
        }
    }

    [TestMethod]
    public void BitCollapseShouldKeepADisabledCollapseInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {", "The disabled collapse has no rule of its own.");

        StringAssert.Contains(block, "color: $clr-fg-dis;");
        StringAssert.Contains(block, "background-color: $clr-bg-dis;");
        Assert.IsFalse(block.Contains("--bit-Collapse-"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepTheDisabledAndFocusedStatesInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, ".bit-col.bit-dis {\n        color: GrayText;");
        StringAssert.Contains(block, ".bit-col-con:focus-visible {\n        outline-color: Highlight;");
    }

    [TestMethod]
    public void BitCollapseShouldCollapseTheTransitionUnderReducedMotionUnlessAnimationIsForced()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "@media (prefers-reduced-motion: reduce)");
        StringAssert.Contains(stylesheet, "--bit-col-duration: 0.01ms;");
        StringAssert.Contains(stylesheet, ".bit-col.bit-fam,\n.bit-fam .bit-col {");
    }

    [TestMethod]
    public void BitCollapseShouldOpenTheClosedSectionForPrint()
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf("\n@media print {", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "There is no print block.");

        var block = stylesheet[start..];

        StringAssert.Contains(block, ".bit-col-eop {");
        StringAssert.Contains(block, "grid-template-rows: 1fr;");
        StringAssert.Contains(block, "grid-template-columns: 1fr;");
        StringAssert.Contains(block, "visibility: inherit;");
        StringAssert.Contains(block, "content-visibility: visible;");

        // A browser that treats hidden="until-found" as plain hidden takes the content out with display:none.
        StringAssert.Contains(block, "> .bit-col-cco[hidden] {\n            display: block;");
    }

    [TestMethod]
    public void BitCollapseShouldFadeOnlyTheEdgeOfAClosedPeek()
    {
        var stylesheet = ReadStylesheet();

        // No fade unless the public variable asks for one, read once on the root so a nested collapse starts over.
        // There is no fallback, so an unset variable invalidates the mask rather than masking with a 0px fade.
        StringAssert.Contains(stylesheet, "--bit-col-fade: var(--bit-Collapse-peek-fade);");
        Assert.IsFalse(stylesheet.Contains("var(--bit-Collapse-peek-fade,"), "The peek fade must not fall back to a length.");

        // Only a closed peek is masked, along the axis it collapses on, and the sideways one follows the direction.
        StringAssert.Contains(stylesheet, ".bit-col-pek.bit-col-col > .bit-col-con {\n    -webkit-mask-image: linear-gradient(to bottom, #000 calc(100% - var(--bit-col-fade)), transparent);");
        StringAssert.Contains(stylesheet, "linear-gradient(to right, #000 calc(100% - var(--bit-col-fade)), transparent)");
        StringAssert.Contains(stylesheet, "&.bit-col-pek.bit-col-col:dir(rtl) > .bit-col-con {");

        // A section printed open prints its peek in full.
        var print = SourceFiles.GetScssBlock(stylesheet, "\n@media print {");

        StringAssert.Contains(print, "--bit-col-fade: initial;");
    }

    [TestMethod]
    public void BitCollapseShouldHoldBackTheHidingOfASearchableSectionUntilTheCloseHasPlayed()
    {
        var stylesheet = ReadStylesheet();

        // hidden="until-found" is applied as the close starts, so what it hides with - content-visibility, or the
        // display:none of a browser that treats it as a plain hidden - is a discrete transition timed like the track.
        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-col-huf > .bit-col-con {");

        StringAssert.Contains(block, "content-visibility var(--bit-col-duration) linear var(--bit-col-delay) allow-discrete");
        StringAssert.Contains(block, "display var(--bit-col-duration) linear var(--bit-col-delay) allow-discrete");

        // NoAnimation takes the transitions off at the same specificity, so it has to come later to win.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-col-huf > .bit-col-con {", System.StringComparison.Ordinal)
                      < stylesheet.IndexOf("\n.bit-col-nan,", System.StringComparison.Ordinal),
                      "The NoAnimation rule has to follow the searchable transition to override it.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Collapse", "BitCollapse.scss");

    [GeneratedRegex(@"^//\s+(--bit-Collapse-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Collapse-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Collapse-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
