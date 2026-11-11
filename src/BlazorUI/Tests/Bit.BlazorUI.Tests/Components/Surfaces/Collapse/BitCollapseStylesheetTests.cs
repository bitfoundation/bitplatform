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

        Assert.IsNotEmpty(documented, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            // A variable documented as unset by default is read bare on purpose: an unset one invalidates what
            // reads it, which is how the peek fade applies no mask at all rather than one that fades nothing.
            if (Regex.IsMatch(stylesheet, $@"^//\s+{Regex.Escape(name)}\s.*\(default: unset", RegexOptions.Multiline))
            {
                Assert.Contains($"var({name})", stylesheet, $"{name} is documented as unset but never read bare.");
                Assert.DoesNotContain($"var({name}, ", stylesheet, $"{name} is documented as unset but read with a fallback.");
                continue;
            }

            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
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

        Assert.Contains($"{token}: initial;", block);
    }

    [TestMethod]
    public void BitCollapseShouldLetTheParametersWinOverThePublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // The inline -full tokens are read ahead of the public variables...
        Assert.Contains("var(--bit-col-dur-full, var(--bit-Collapse-duration, ", stylesheet);
        Assert.Contains("var(--bit-col-eas, var(--bit-Collapse-easing, ", stylesheet);

        // ...and the Background classes paint the theme color outright rather than through the variable.
        foreach (var kind in new[] { "pbg", "sbg", "tbg", "rbg" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-col-{kind} {{");

            Assert.DoesNotContain("--bit-Collapse-", block, $"The {kind} background reads a public variable.");
        }
    }

    [TestMethod]
    public void BitCollapseShouldKeepADisabledCollapseInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n    &.bit-dis {", "The disabled collapse has no rule of its own.");

        Assert.Contains("color: $clr-fg-dis;", block);
        Assert.Contains("background-color: $clr-bg-dis;", block);
        Assert.DoesNotContain("--bit-Collapse-", block);
    }

    [TestMethod]
    public void BitCollapseShouldKeepTheDisabledAndFocusedStatesInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n@media (forced-colors: active) {");

        Assert.Contains(".bit-col.bit-dis {\n        color: GrayText;", block);
        Assert.Contains(".bit-col-con:focus-visible {\n        outline-color: Highlight;", block);
    }

    [TestMethod]
    public void BitCollapseShouldCollapseTheTransitionUnderReducedMotionUnlessAnimationIsForced()
    {
        // The pace and the delay - whether a parameter, the public variable or the theme decided them - are scaled
        // by the shared motion factor, which is what collapses them under reduced motion and what ForceAnimation and
        // an app restoring the motion tokens bring back; the collapse keeps no query or restore of its own for them.
        var stylesheet = ReadStylesheet();

        Assert.Contains("--bit-col-duration: #{mot-scaled-duration(var(--bit-col-dur-full, var(--bit-Collapse-duration, #{$mot-duration-long})))};", stylesheet);
        Assert.Contains("--bit-col-delay: #{mot-scaled-delay(var(--bit-col-del-full, 0ms))};", stylesheet);
        Assert.DoesNotContain("prefers-reduced-motion", stylesheet, "The collapse collapses its own motion.");
        Assert.DoesNotContain("bit-fam", stylesheet, "The collapse restores its own motion.");
    }

    [TestMethod]
    public void BitCollapseShouldOpenTheClosedSectionForPrint()
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf("\n@media print {", System.StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, "There is no print block.");

        var block = stylesheet[start..];

        Assert.Contains(".bit-col-eop {", block);
        Assert.Contains("grid-template-rows: 1fr;", block);
        Assert.Contains("grid-template-columns: 1fr;", block);
        Assert.Contains("visibility: inherit;", block);
        Assert.Contains("content-visibility: visible;", block);

        // A browser that treats hidden="until-found" as plain hidden takes the content out with display:none.
        Assert.Contains("> .bit-col-cco[hidden] {\n            display: block;", block);
    }

    [TestMethod]
    public void BitCollapseShouldFadeOnlyTheEdgeOfAClosedPeek()
    {
        var stylesheet = ReadStylesheet();

        // No fade unless the public variable asks for one, read once on the root so a nested collapse starts over.
        // There is no fallback, so an unset variable invalidates the mask rather than masking with a 0px fade.
        Assert.Contains("--bit-col-fade: var(--bit-Collapse-peek-fade);", stylesheet);
        Assert.DoesNotContain("var(--bit-Collapse-peek-fade,", stylesheet, "The peek fade must not fall back to a length.");

        // Only a closed peek is masked, along the axis it collapses on, and the sideways one follows the direction.
        Assert.Contains(".bit-col-pek.bit-col-col > .bit-col-con {\n    -webkit-mask-image: linear-gradient(to bottom, #000 calc(100% - var(--bit-col-fade)), transparent);", stylesheet);
        Assert.Contains("linear-gradient(to right, #000 calc(100% - var(--bit-col-fade)), transparent)", stylesheet);
        Assert.Contains("&.bit-col-pek.bit-col-col:is(.bit-rtl, :dir(rtl)) > .bit-col-con {", stylesheet);

        // A section printed open prints its peek in full.
        var print = SourceFiles.GetScssBlock(stylesheet, "\n@media print {");

        Assert.Contains("--bit-col-fade: initial;", print);
    }

    [TestMethod]
    public void BitCollapseShouldNotHoldBackTheHidingOfASearchableSectionWithADiscreteTransition()
    {
        var code = ReadStylesheet().Split('\n').Where(line => line.TrimStart().StartsWith("//", System.StringComparison.Ordinal) is false);

        // WebKit lays out content mid-way through a discrete content-visibility transition as already hidden, which
        // snaps the track shut; the component applies hidden="until-found" at the end of the close instead.
        Assert.DoesNotContain(line => line.Contains("allow-discrete"), code, "A discrete transition snaps a searchable close shut in WebKit.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Surfaces", "Collapse", "BitCollapse.scss");

    [GeneratedRegex(@"^//\s+(--bit-Collapse-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Collapse-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Collapse-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
