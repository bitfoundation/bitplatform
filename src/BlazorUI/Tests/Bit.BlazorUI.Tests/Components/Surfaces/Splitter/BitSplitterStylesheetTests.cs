using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Splitter;

/// <summary>
/// Pins what a bUnit render cannot see of the splitter: its public --bit-Splitter-* variables, which the stylesheet
/// reads and the header comment of that stylesheet documents, and the private size variables every splitter starts
/// over from rather than inheriting from the one it is nested in.
/// </summary>
[TestClass]
public class BitSplitterStylesheetTests
{
    private static readonly Regex PublicVariableRead = new(@"var\(\s*(--bit-Splitter-[a-zA-Z0-9-]+)", RegexOptions.Compiled);
    private static readonly Regex PublicVariableDeclaration = new(@"^\s*(--bit-Splitter-[a-zA-Z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex DocumentedVariable = new(@"^//\s+(--bit-Splitter-[a-zA-Z0-9-]+)\s", RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void BitSplitterShouldDocumentEveryPublicVariableItReads()
    {
        var stylesheet = ReadStylesheet();

        var read = PublicVariableRead.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var documented = DocumentedVariable.Matches(stylesheet).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(documented.Order().ToArray(), read.Order().ToArray(),
            "The --bit-Splitter-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitSplitterShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every splitter
        // below it: a declaration on the splitter would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitSplitterShouldStartEveryPanelSizeOverRatherThanInheritIt()
    {
        // A splitter nested in the panel of another would otherwise take the outer one's split, minimums and
        // maximums for its own wherever it declares none.
        var root = GetBlock(ReadStylesheet(), "\n.bit-spl {");

        foreach (var name in new[] { "fpn-size", "fpn-grow", "fpn-max", "fpn-min", "spn-size", "spn-grow", "spn-max", "spn-min", "col-size" })
        {
            StringAssert.Contains(root, $"--bit-spl-{name}: initial;");
        }
    }

    [TestMethod]
    public void BitSplitterShouldReadOnlyPrefixedPrivateVariables()
    {
        // Unprefixed custom properties on the root would be inherited by, and collide with, whatever the panels hold.
        var unprefixed = Regex.Matches(ReadStylesheet(), @"var\(\s*(--(?!bit-)[a-zA-Z0-9-]+)")
                              .Select(m => m.Groups[1].Value)
                              .Distinct()
                              .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), unprefixed);
    }

    [TestMethod]
    public void BitSplitterGutterShouldLetTheParametersWinOverTheVariables()
    {
        // GutterSize and GutterHitSize write the private variable inline, which beats the class declaring it from
        // the public one.
        var root = GetBlock(ReadStylesheet(), "\n.bit-spl {");

        StringAssert.Contains(root, "--bit-spl-gtr-size: var(--bit-Splitter-gutter-size, #{spacing(1.25)});");
        StringAssert.Contains(root, "--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(3)});");
    }

    [TestMethod]
    public void BitSplitterCoarsePointerShouldStillReadTheHitSizeVariable()
    {
        var stylesheet = ReadStylesheet();
        var coarse = stylesheet[stylesheet.IndexOf("@media (pointer: coarse)", StringComparison.Ordinal)..];

        StringAssert.Contains(coarse, "--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(5.5)});");
    }

    [TestMethod]
    public void BitSplitterCollapseButtonShouldReachTheTargetSize()
    {
        // The button is drawn 1.75 spacing units across the gutter; the part answering a press reaches out to 3.
        var button = GetBlock(ReadStylesheet(), "\n.bit-spl-cbt {");

        StringAssert.Contains(button, "&::before {");
        StringAssert.Contains(button, "calc((spacing(1.75) - spacing(3)) / 2)");
    }

    [TestMethod]
    public void BitSplitterGripShouldBeDrawnInAColorWithAContrastFloor()
    {
        // The gutter at rest is the decorative stroke tier, so the grip is what keeps the control at 3:1 (SC 1.4.11):
        // the secondary foreground has that floor over the gutter, the primary stroke does not.
        var grip = GetBlock(ReadStylesheet(), "\n.bit-spl-gti {");

        StringAssert.Contains(grip, "var(--bit-Splitter-gutter-indicator-color, #{$clr-fg-sec})");
    }

    [TestMethod]
    public void BitSplitterShouldNeverReachIntoANestedSplitter()
    {
        // A splitter is made to be nested in the panel of another, so a rule going from the state of the root to one
        // of its parts has to go through a child combinator: a descendant one hands the orientation, the drag or the
        // read-only look of the outer splitter to every splitter inside it - a row nested in a column would get a
        // gutter lying on its side and lose the width limits of its panels.
        var stylesheet = ReadStylesheet();

        var descendant = new Regex(@"\.bit-spl[a-z-]*(?:[.:][^\s,{>)]+)*\s+\.bit-spl", RegexOptions.Compiled);

        var offending = stylesheet.Split('\n')
                                  .Where(line => line.TrimEnd().EndsWith('{') || line.TrimEnd().EndsWith(','))
                                  .Where(line => line.TrimStart().StartsWith("//", StringComparison.Ordinal) is false)
                                  .Where(line => descendant.IsMatch(line))
                                  .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offending);

        // Nesting a part's block inside a state's block compiles to the same descendant selector.
        var parents = new System.Collections.Generic.Stack<string>();
        var nested = new System.Collections.Generic.List<string>();

        foreach (var line in stylesheet.Split('\n').Select(l => l.Trim()))
        {
            if (line.StartsWith("//", StringComparison.Ordinal)) continue;

            if (line.EndsWith('{'))
            {
                if (line.StartsWith(".bit-spl", StringComparison.Ordinal) && parents.Any(p => p.StartsWith(".bit-spl", StringComparison.Ordinal)))
                {
                    nested.Add(line);
                }

                parents.Push(line);
            }
            else if (line.StartsWith('}') && parents.Count > 0)
            {
                parents.Pop();
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), nested);
    }

    [TestMethod]
    public void BitSplitterPanelLimitsShouldCostNoSpecificity()
    {
        // A min or max size set through Classes, in any unit, is one the drag honours, so it has to beat the defaults
        // in both orientations wherever the app's stylesheet is loaded.
        var stylesheet = ReadStylesheet();

        foreach (var selector in new[] { ":where(.bit-spl > .bit-spl-fpn) {", ":where(.bit-spl > .bit-spl-spn) {",
                                         ":where(.bit-spl-vrt > .bit-spl-fpn) {", ":where(.bit-spl-vrt > .bit-spl-spn) {" })
        {
            StringAssert.Contains(stylesheet, selector);
        }

        foreach (var selector in new[] { "\n.bit-spl-fpn {", "\n.bit-spl-spn {", "\n.bit-spl-pnl {" })
        {
            var block = GetBlock(stylesheet, selector);

            Assert.IsFalse(Regex.IsMatch(block, @"\b(min|max)-(width|height)\s*:"), $"{selector.Trim()} sets a panel limit at full specificity.");
        }
    }

    [TestMethod]
    public void BitSplitterHoverShouldOnlyApplyWhereThePointerCanHover()
    {
        // A finger lifting off the gutter would otherwise leave it in its hover color until the next tap elsewhere.
        var stylesheet = ReadStylesheet();

        var rules = Regex.Matches(stylesheet, @"^[^/\n]*:hover[^\n]*\{", RegexOptions.Multiline);

        Assert.IsTrue(rules.Count > 0);

        foreach (Match match in rules)
        {
            Assert.IsTrue(IsInsideHoverQuery(stylesheet[..match.Index]), $"'{match.Value.Trim()}' is not gated by @media (hover: hover).");
        }
    }

    [TestMethod]
    public void BitSplitterShouldMirrorTheDefaultChevronByTheDirectionItIsLaidOutIn()
    {
        // :dir() reads the direction from the html element as much as from a Dir parameter - the same direction the
        // drag and the keys go by - and a stacked splitter is left alone.
        StringAssert.Contains(ReadStylesheet(), ".bit-spl:not(.bit-spl-vrt):dir(rtl) > .bit-spl-gtr > .bit-spl-cbt > .bit-spl-cbd {\n    scale: -1 1;");
    }

    [TestMethod]
    public void BitSplitterDragShouldWinOverHover()
    {
        // The pointer is over the gutter for much of a drag, so the hover rule must not outrank the active one: the
        // conditions it is gated on are wrapped in :where() to leave both at the specificity of a class and a state,
        // and the drag rule comes later.
        var stylesheet = ReadStylesheet();

        var hover = stylesheet.IndexOf("    :where(.bit-spl:not(.bit-spl-rdo, .bit-dis, .bit-spl-col:not(.bit-spl-cpb))) > .bit-spl-gtr:hover {", StringComparison.Ordinal);
        var drag = stylesheet.IndexOf("\n.bit-spl-drg > .bit-spl-gtr {", StringComparison.Ordinal);

        Assert.IsTrue(hover >= 0, "The gutter's hover rule has changed shape.");
        Assert.IsTrue(drag > hover, "The drag rule has to come after the hover rule it ties with.");
    }

    // Walks the braces back out from a rule to see whether one of the blocks it sits in is the hover query - or the
    // forced-colors one, which only repaints what the hover query already allowed.
    private static bool IsInsideHoverQuery(string before)
    {
        var depth = 0;

        for (var i = before.Length - 1; i >= 0; i--)
        {
            if (before[i] == '}') depth++;
            else if (before[i] == '{')
            {
                if (depth == 0)
                {
                    var lineStart = before.LastIndexOf('\n', i) + 1;
                    var header = before[lineStart..i];

                    if (header.Contains("@media (hover: hover)", StringComparison.Ordinal)
                        || header.Contains("@media (forced-colors: active)", StringComparison.Ordinal)) return true;
                }
                else
                {
                    depth--;
                }
            }
        }

        return false;
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
                                                 "Bit.BlazorUI", "Components", "Surfaces", "Splitter", "BitSplitter.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
