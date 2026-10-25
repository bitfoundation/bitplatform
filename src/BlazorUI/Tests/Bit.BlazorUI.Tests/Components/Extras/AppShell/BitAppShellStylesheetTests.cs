using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AppShell;

/// <summary>
/// Pins the public --bit-AppShell-* variables and the rules of the stylesheet a bUnit render cannot see: every variable
/// the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from :root),
/// nothing is read that the header does not document, the No*Inset flags still win over a safe area a page sets, and
/// the shell prints at the length of its content.
/// </summary>
[TestClass]
public partial class BitAppShellStylesheetTests
{
    [TestMethod]
    public void BitAppShellShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(11, documented.Length, "The stylesheet does not document the eleven public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitAppShellShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitAppShellShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-AppShell-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitAppShellShouldReadTheSafeAreasBeforeTheDeviceInsets()
    {
        var stylesheet = ReadStylesheet();

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash {");
        StringAssert.Contains(root, "--bit-ash-inset-top: var(--bit-AppShell-safe-area-top, #{$bit-env-inset-top});");
        StringAssert.Contains(root, "--bit-ash-inset-start: var(--bit-AppShell-safe-area-start, #{$bit-env-inset-inline-start});");

        var stable = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash-sin {");
        StringAssert.Contains(stable, "--bit-ash-inset-bottom: var(--bit-AppShell-safe-area-bottom, #{$bit-env-max-inset-bottom});");
        StringAssert.Contains(stable, "--bit-ash-inset-end: var(--bit-AppShell-safe-area-end, #{$bit-env-max-inset-inline-end});");

        // The flags that take an edge away come after both, so they win over a safe area the page set too.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-ash-sin {", System.StringComparison.Ordinal));
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-ash-nit {", System.StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitAppShellShouldFallEachBarBackToTheBarsAndTheShell()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash {"), "background-color: var(--bit-AppShell-background, #{$clr-bg-pri});");

        // The foreground comes with the background, so the text is never left to a page color that does not read on it.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash {"), "color: var(--bit-AppShell-color, #{$clr-fg-pri});");

        foreach (var (rule, edge) in new[] { ("top", "top"), ("bottom", "bottom"), ("left", "start"), ("right", "end") })
        {
            StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, $"\n.bit-ash-{rule} {{"),
                                  $"background-color: var(--bit-AppShell-inset-{edge}-background, var(--bit-AppShell-inset-background, var(--bit-AppShell-background, #{{$clr-bg-pri}})));");
        }
    }

    [TestMethod]
    public void BitAppShellShouldPrintAtTheLengthOfItsContent()
    {
        var print = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media print {");

        StringAssert.Contains(SourceFiles.GetScssBlock(print, "\n    .bit-ash {"), "height: auto;");
        StringAssert.Contains(SourceFiles.GetScssBlock(print, "\n    .bit-ash {"), "--bit-ash-inset-top: 0px;");
        StringAssert.Contains(SourceFiles.GetScssBlock(print, "\n    .bit-ash-fsc {"), "position: static;");
        StringAssert.Contains(print, ".bit-ash-main {\n        overflow: visible !important;");
        StringAssert.Contains(print, "display: none;");

        // The theme's colors go back to the page on paper: a dark theme's foreground is near-white, and a printout
        // leaves the background it was chosen against out by default.
        StringAssert.Contains(SourceFiles.GetScssBlock(print, "\n    .bit-ash {"), "color: inherit;");
        StringAssert.Contains(SourceFiles.GetScssBlock(print, "\n    .bit-ash {"), "background-color: transparent;");
    }

    [TestMethod]
    public void BitAppShellShouldStopTheLandscapeTextInflationWithoutBlockingTheReadersTextSize()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-ash {");

        // 100%, never none: none also takes away the reader's own text size and zoom in some engines.
        StringAssert.Contains(root, "-webkit-text-size-adjust: 100%;");
        StringAssert.Contains(root, "\n    text-size-adjust: 100%;");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "AppShell", "BitAppShell.scss");

    [GeneratedRegex(@"^//\s+(--bit-AppShell-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-AppShell-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-AppShell-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
