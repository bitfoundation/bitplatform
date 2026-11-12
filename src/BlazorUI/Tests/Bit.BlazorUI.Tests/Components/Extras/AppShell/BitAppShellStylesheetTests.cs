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

        Assert.HasCount(11, documented, "The stylesheet does not document the eleven public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
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
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
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
        Assert.Contains("--bit-ash-inset-top: var(--bit-AppShell-safe-area-top, #{$bit-env-inset-top});", root);
        Assert.Contains("--bit-ash-inset-start: var(--bit-AppShell-safe-area-start, #{$bit-env-inset-inline-start});", root);

        var stable = SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash-sin {");
        Assert.Contains("--bit-ash-inset-bottom: var(--bit-AppShell-safe-area-bottom, #{$bit-env-max-inset-bottom});", stable);
        Assert.Contains("--bit-ash-inset-end: var(--bit-AppShell-safe-area-end, #{$bit-env-max-inset-inline-end});", stable);

        // The flags that take an edge away come after both, so they win over a safe area the page set too.
        Assert.IsGreaterThan(stylesheet.IndexOf("\n.bit-ash-sin {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal));
        Assert.IsGreaterThan(stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal), stylesheet.IndexOf("\n.bit-ash-nit {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitAppShellShouldFallEachBarBackToTheBarsAndTheShell()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("background-color: var(--bit-AppShell-background, #{$clr-bg-pri});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash {"));

        // The foreground comes with the background, so the text is never left to a page color that does not read on it.
        Assert.Contains("color: var(--bit-AppShell-color, #{$clr-fg-pri});", SourceFiles.GetScssBlock(stylesheet, "\n.bit-ash {"));

        foreach (var (rule, edge) in new[] { ("top", "top"), ("bottom", "bottom"), ("left", "start"), ("right", "end") })
        {
            Assert.Contains($"background-color: var(--bit-AppShell-inset-{edge}-background, var(--bit-AppShell-inset-background, var(--bit-AppShell-background, #{{$clr-bg-pri}})));", SourceFiles.GetScssBlock(stylesheet, $"\n.bit-ash-{rule} {{"));
        }
    }

    [TestMethod]
    public void BitAppShellShouldPrintAtTheLengthOfItsContent()
    {
        var print = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media print {");

        Assert.Contains("height: auto;", SourceFiles.GetScssBlock(print, "\n    .bit-ash {"));
        Assert.Contains("--bit-ash-inset-top: 0px;", SourceFiles.GetScssBlock(print, "\n    .bit-ash {"));
        Assert.Contains("position: static;", SourceFiles.GetScssBlock(print, "\n    .bit-ash-fsc {"));
        Assert.Contains(".bit-ash-main {\n        overflow: visible !important;", print);
        Assert.Contains("display: none;", print);

        // The theme's colors go back to the page on paper: a dark theme's foreground is near-white, and a printout
        // leaves the background it was chosen against out by default.
        Assert.Contains("color: inherit;", SourceFiles.GetScssBlock(print, "\n    .bit-ash {"));
        Assert.Contains("background-color: transparent;", SourceFiles.GetScssBlock(print, "\n    .bit-ash {"));
    }

    [TestMethod]
    public void BitAppShellShouldStopTheLandscapeTextInflationWithoutBlockingTheReadersTextSize()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-ash {");

        // 100%, never none: none also takes away the reader's own text size and zoom in some engines.
        Assert.Contains("-webkit-text-size-adjust: 100%;", root);
        Assert.Contains("\n    text-size-adjust: 100%;", root);
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
