using System;
using System.Linq;
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

        Assert.AreSequenceEqual(documented.Order().ToArray(), read.Order().ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The --bit-Splitter-* variables the stylesheet reads and the ones its header comment documents have drifted apart.");
    }

    [TestMethod]
    public void BitSplitterShouldNeverDeclareItsPublicVariables()
    {
        // Read with a fallback and never declared, so a value set on :root or on an ancestor reaches every splitter
        // below it: a declaration on the splitter would shadow them all.
        var declared = PublicVariableDeclaration.Matches(ReadStylesheet()).Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(Array.Empty<string>(), declared);
    }

    [TestMethod]
    public void BitSplitterShouldStartEveryPanelSizeOverRatherThanInheritIt()
    {
        // A splitter nested in the panel of another would otherwise take the outer one's split, minimums and
        // maximums for its own wherever it declares none.
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spl {");

        foreach (var name in new[] { "fpn-size", "fpn-grow", "fpn-max", "fpn-min", "spn-size", "spn-grow", "spn-max", "spn-min", "col-size" })
        {
            Assert.Contains($"--bit-spl-{name}: initial;", root);
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

        Assert.AreSequenceEqual(Array.Empty<string>(), unprefixed);
    }

    [TestMethod]
    public void BitSplitterGutterShouldLetTheParametersWinOverTheVariables()
    {
        // GutterSize and GutterHitSize write the private variable inline, which beats the class declaring it from
        // the public one.
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spl {");

        Assert.Contains("--bit-spl-gtr-size: var(--bit-Splitter-gutter-size, #{spacing(1.25)});", root);
        Assert.Contains("--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(3)});", root);
    }

    [TestMethod]
    public void BitSplitterCoarsePointerShouldStillReadTheHitSizeVariable()
    {
        var stylesheet = ReadStylesheet();
        var coarse = SourceFiles.GetScssBlock(stylesheet, "@media (pointer: coarse) {");

        Assert.Contains("--bit-spl-hit-size: var(--bit-Splitter-gutter-hit-size, #{spacing(5.5)});", coarse);
    }

    [TestMethod]
    public void BitSplitterCollapseButtonShouldReachTheTargetSize()
    {
        // The button is drawn 1.75 spacing units across the gutter; the part answering a press reaches out to 3.
        var button = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spl-cbt {");

        Assert.Contains("&::before {", button);
        Assert.Contains("calc((spacing(1.75) - spacing(3)) / 2)", button);
    }

    [TestMethod]
    public void BitSplitterGripShouldBeDrawnInAColorWithAContrastFloor()
    {
        // The gutter at rest is the decorative stroke tier, so the grip is what keeps the control at 3:1 (SC 1.4.11):
        // the secondary foreground has that floor over the gutter, the primary stroke does not.
        var grip = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-spl-gti {");

        Assert.Contains("var(--bit-Splitter-gutter-indicator-color, #{$clr-fg-sec})", grip);
    }

    [TestMethod]
    public void BitSplitterShouldNeverReachIntoANestedSplitter()
    {
        // A splitter is made to be nested in the panel of another, so a rule going from the state of the root to one
        // of its parts has to go through a child combinator: a descendant one hands the orientation, the drag or the
        // read-only look of the outer splitter to every splitter inside it - a row nested in a column would get a
        // gutter lying on its side and lose the width limits of its panels.
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        var descendant = new Regex(@"\.bit-spl[a-z-]*(?:[.:][^\s,{>)]+)*\s+\.bit-spl", RegexOptions.Compiled);

        var offending = stylesheet.Split('\n')
                                  .Where(line => line.TrimEnd().EndsWith('{') || line.TrimEnd().EndsWith(','))
                                  .Where(line => descendant.IsMatch(line))
                                  .ToArray();

        Assert.AreSequenceEqual(Array.Empty<string>(), offending);

        // Nesting a part's block inside a state's block compiles to the same descendant selector.
        var parents = new System.Collections.Generic.Stack<string>();
        var nested = new System.Collections.Generic.List<string>();

        foreach (var line in stylesheet.Split('\n').Select(l => l.Trim()))
        {
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

        Assert.AreSequenceEqual(Array.Empty<string>(), nested);
    }

    [TestMethod]
    public void BitSplitterPanelLimitsShouldCostTheSpecificityOfASingleClass()
    {
        // A min or max size set through Classes, in any unit, is one the drag honours, so the root in front of the part
        // adds nothing and an app's class ties with the defaults in both orientations. No specificity at all would hand
        // them to any element rule too - a reset's `div { min-height: auto }` would stop the panels shrinking.
        var stylesheet = ReadStylesheet();

        foreach (var selector in new[] { "\n:where(.bit-spl) > .bit-spl-pnl {", "\n:where(.bit-spl) > .bit-spl-fpn {", "\n:where(.bit-spl) > .bit-spl-spn {",
                                         "\n:where(.bit-spl-vrt) > .bit-spl-pnl {", "\n:where(.bit-spl-vrt) > .bit-spl-fpn {", "\n:where(.bit-spl-vrt) > .bit-spl-spn {" })
        {
            Assert.Contains(selector, stylesheet);
        }

        // The column ties with the row, so it has to come after it to swap the axes.
        Assert.IsGreaterThan(stylesheet.IndexOf("\n:where(.bit-spl) > .bit-spl-spn {", StringComparison.Ordinal), stylesheet.IndexOf("\n:where(.bit-spl-vrt) > .bit-spl-pnl {", StringComparison.Ordinal));

        Assert.IsFalse(stylesheet.Contains(":where(.bit-spl > ", StringComparison.Ordinal), "A panel limit costs no specificity at all again.");
        Assert.IsFalse(stylesheet.Contains(":where(.bit-spl-vrt > ", StringComparison.Ordinal), "A panel limit costs no specificity at all again.");

        foreach (var selector in new[] { "\n.bit-spl-fpn {", "\n.bit-spl-spn {", "\n.bit-spl-pnl {" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, selector);

            Assert.IsFalse(Regex.IsMatch(block, @"\b(min|max)-(width|height)\s*:"), $"{selector.Trim()} sets a panel limit at full specificity.");
        }
    }

    [TestMethod]
    public void BitSplitterHoverShouldOnlyApplyWhereThePointerCanHover()
    {
        // A finger lifting off the gutter would otherwise leave it in its hover color until the next tap elsewhere.
        var rules = SourceFiles.GetScssRules(ReadStylesheet()).Where(r => r.Header.Contains(":hover", StringComparison.Ordinal)).ToArray();

        Assert.IsNotEmpty(rules);

        foreach (var rule in rules)
        {
            Assert.IsTrue(IsInsideHoverQuery(rule), $"'{rule.Header}' is not gated by @media (hover: hover).");
        }
    }

    [TestMethod]
    public void BitSplitterShouldMirrorTheDefaultChevronByTheDirectionItIsLaidOutIn()
    {
        // :dir() reads the direction a splitter inherits from the html element - the same direction the drag and the
        // keys go by - and a stacked splitter is left alone. One whose Dir parameter says so is already turned in the
        // markup, which every browser draws, so it is left out here rather than turned back again.
        var stylesheet = ReadStylesheet();
        var rule = ".bit-spl:not(.bit-spl-vrt):not([dir=\"rtl\"]):dir(rtl) > .bit-spl-gtr > .bit-spl-cbt > .bit-spl-cbd {\n    scale: -1 1;";

        Assert.Contains(rule, stylesheet);

        // A browser without :dir() drops every selector of a list that holds it, so the rule stands on its own.
        var start = stylesheet.IndexOf(rule, StringComparison.Ordinal);
        var lineStart = stylesheet.LastIndexOf('\n', start - 1) + 1;

        Assert.AreEqual(start, lineStart, "The :dir() rule shares its selector list with another one.");
    }

    [TestMethod]
    public void BitSplitterStatesShouldMoveTheVariablesRatherThanPaintOverAnAppsClass()
    {
        // A gutter or a collapse button an app paints through Classes keeps that paint under the pointer and through a
        // drag: the states only move the private variables the one rule at rest paints from. The whole block is read,
        // since paint nested in a state (its own &:hover, an @media) would paint over the app's class all the same.
        var stylesheet = ReadStylesheet();

        foreach (var selector in new[] { "    :where(.bit-spl:not(.bit-spl-rdo, .bit-dis, .bit-spl-col:not(.bit-spl-cpb))) > .bit-spl-gtr:hover {",
                                         "\n.bit-spl-drg > .bit-spl-gtr {", "        &:hover {" })
        {
            var block = SourceFiles.GetScssBlock(stylesheet, selector);

            Assert.IsFalse(Regex.IsMatch(block, @"^\s*(background-color|background|color|border-color)\s*:", RegexOptions.Multiline),
                           $"{selector.Trim()} paints the part directly.");
        }
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

        Assert.IsGreaterThanOrEqualTo(0, hover, "The gutter's hover rule has changed shape.");
        Assert.IsGreaterThan(hover, drag, "The drag rule has to come after the hover rule it ties with.");
    }

    // Whether one of the blocks a rule sits in is the hover query - or the forced-colors one, which only repaints what
    // the hover query already allowed.
    private static bool IsInsideHoverQuery(SourceFiles.ScssRule rule)
    {
        return rule.Ancestors.Any(header => header.Contains("@media (hover: hover)", StringComparison.Ordinal)
                                            || header.Contains("@media (forced-colors: active)", StringComparison.Ordinal));
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Surfaces", "Splitter", "BitSplitter.scss");
}
