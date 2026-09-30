using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Overlay;

/// <summary>
/// Pins what a bUnit render cannot see of the Overlay: its public --bit-Overlay-* variables, which the stylesheet reads
/// and the header comment of that stylesheet documents, and the layout rules the parameters stand for.
/// </summary>
[TestClass]
public class BitOverlayStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Overlay-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Overlay-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Overlay-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitOverlayShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Overlay-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitOverlayShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every Overlay below
        // it: a declaration on the Overlay would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitOverlayShouldFallBackToTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-ovl {"), "z-index: var(--bit-Overlay-z-index, #{$zindex-overlay});");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-ovl-mfl {"), "background-color: var(--bit-Overlay-background, #{$clr-bg-overlay});");
        StringAssert.Contains(GetBlock(stylesheet, "\n.bit-ovl {"), "var(--bit-Overlay-transition-duration, #{$mot-duration-short})");
    }

    [TestMethod]
    public void BitOverlayAbsolutePositionShouldTakeTheCornersOfItsContainer()
    {
        var absolute = GetBlock(ReadStylesheet(), "\n.bit-ovl-abs {");

        StringAssert.Contains(absolute, "border-radius: inherit;");
        StringAssert.Contains(absolute, "z-index: unset;");
    }

    [TestMethod]
    public void BitOverlayShouldPlaceItsContentForEveryPosition()
    {
        // Every class the Position parameter maps to has a rule of its own.
        var stylesheet = ReadStylesheet();

        foreach (var cls in new[] { "tlf", "tcr", "trg", "tst", "ten", "clf", "ctr", "crg", "cst", "cen", "blf", "bcr", "brg", "bst", "ben" })
        {
            StringAssert.Contains(stylesheet, $".bit-ovl-{cls}");
        }

        // The physical ones are pinned back to their own side in a right-to-left layout.
        StringAssert.Contains(stylesheet, ".bit-ovl-tlf:dir(rtl)");
        StringAssert.Contains(stylesheet, ".bit-ovl-brg:dir(rtl)");
    }

    [TestMethod]
    public void BitOverlayShouldNeverHardCodeMotion()
    {
        // The fade reads the motion tokens, which is what collapses it under reduced motion.
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\b(ease|ease-in|ease-out|ease-in-out|cubic-bezier)\b"));
        StringAssert.Contains(stylesheet, "$mot-duration-short");
    }

    private static string GetBlock(string stylesheet, string selector, string terminator = "\n}")
    {
        var start = stylesheet.IndexOf(selector, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector.Trim()} was not found in the stylesheet.");

        var end = stylesheet.IndexOf(terminator, start, StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Overlay", "BitOverlay.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
