using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.SwipeTrap;

/// <summary>
/// Pins what a bUnit render cannot see of the SwipeTrap: its public --bit-SwipeTrap-* variables, which the stylesheet
/// reads and the header comment of that stylesheet documents, and the touch-action each state declares to the browser.
/// </summary>
[TestClass]
public class BitSwipeTrapStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-SwipeTrap-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-SwipeTrap-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-SwipeTrap-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitSwipeTrapShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        Assert.AreSequenceEqual(documented.Order().ToArray(), read.Order().ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The --bit-SwipeTrap-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitSwipeTrapShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every trap below it.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitSwipeTrapShouldFallBackToTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("focus-ring-own(var(--bit-SwipeTrap-focus-color))", stylesheet);
        Assert.Contains("cursor: var(--bit-SwipeTrap-swiping-cursor, grabbing);", stylesheet);
    }

    [TestMethod]
    public void BitSwipeTrapShouldDeclareTheAxesItTakesAsATouchAction()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains("touch-action: pan-y pinch-zoom;", SourceFiles.GetScssDeclarations(stylesheet, "&.bit-stp-hrz {"));
        Assert.Contains("touch-action: pan-x pinch-zoom;", SourceFiles.GetScssDeclarations(stylesheet, "&.bit-stp-vrt {"));

        // A disabled trap takes nothing, so the page scrolls over it - and the rule comes after the locks it overrides.
        var disabled = stylesheet.IndexOf("&.bit-dis {", StringComparison.Ordinal);
        Assert.Contains("touch-action: auto;", SourceFiles.GetScssDeclarations(stylesheet, "&.bit-dis {"));
        Assert.IsGreaterThan(stylesheet.IndexOf("&.bit-stp-lck {", StringComparison.Ordinal), disabled);
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "SwipeTrap", "BitSwipeTrap.scss");
}
