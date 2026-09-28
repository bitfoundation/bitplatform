using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        var body = RulesOf(ReadStylesheet());

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

        var start = stylesheet.IndexOf("\n.bit-col {", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "The root has no rule of its own.");

        var block = stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];

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
            var start = stylesheet.IndexOf($"\n.bit-col-{kind} {{", System.StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, $"The {kind} background has no rule of its own.");

            var block = stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];

            Assert.IsFalse(block.Contains("--bit-Collapse-"), $"The {kind} background reads a public variable.");
        }
    }

    [TestMethod]
    public void BitCollapseShouldKeepADisabledCollapseInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf("\n    &.bit-dis {", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "The disabled collapse has no rule of its own.");

        var block = stylesheet[start..stylesheet.IndexOf("\n    }", start, System.StringComparison.Ordinal)];

        StringAssert.Contains(block, "color: $clr-fg-dis;");
        StringAssert.Contains(block, "background-color: $clr-bg-dis;");
        Assert.IsFalse(block.Contains("--bit-Collapse-"));
    }

    [TestMethod]
    public void BitCollapseShouldKeepTheDisabledAndFocusedStatesInForcedColors()
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf("\n@media (forced-colors: active) {", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "There is no forced-colors block.");

        var block = stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];

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
    }

    [TestMethod]
    public void BitCollapseShouldFadeOnlyTheEdgeOfAClosedPeek()
    {
        var stylesheet = ReadStylesheet();

        // No fade unless the public variable asks for one, read once on the root so a nested collapse starts over.
        StringAssert.Contains(stylesheet, "--bit-col-fade: var(--bit-Collapse-peek-fade, 0px);");

        // Only a closed peek is masked, along the axis it collapses on, and the sideways one follows the direction.
        StringAssert.Contains(stylesheet, ".bit-col-pek.bit-col-col > .bit-col-con {\n    -webkit-mask-image: linear-gradient(to bottom, #000 calc(100% - var(--bit-col-fade)), transparent);");
        StringAssert.Contains(stylesheet, "linear-gradient(to right, #000 calc(100% - var(--bit-col-fade)), transparent)");
        StringAssert.Contains(stylesheet, "&.bit-col-pek.bit-col-col:dir(rtl) > .bit-col-con {");

        // A section printed open prints its peek in full.
        var print = stylesheet[stylesheet.IndexOf("\n@media print {", System.StringComparison.Ordinal)..];

        StringAssert.Contains(print, "--bit-col-fade: 0px;");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Collapse", "BitCollapse.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Collapse-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Collapse-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Collapse-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
