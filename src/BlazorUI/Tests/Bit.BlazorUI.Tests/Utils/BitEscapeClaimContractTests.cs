using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the convention that keeps one Escape from doing two things - a field inside a dialog clearing and the dialog
/// closing on the same press: whoever acts on the key claims it by preventing its default. The components that act on
/// it in .NET say so through data-bit-esc in their markup, which Utils.claimEscape reads as the key goes down, and every
/// surface that closes on Escape leaves a claimed key alone. None of it runs in a bUnit render, which never runs the
/// script; the component tests pin what each component writes into the attribute.
/// </summary>
[TestClass]
public sealed class BitEscapeClaimContractTests
{
    [TestMethod]
    public void TheClaimShouldBeMadeAheadOfEverySurface()
    {
        var escape = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Escape.ts");

        // On the window in the capture phase, as the script loads: the surfaces listen on their own element, on the
        // document and on the window, and the claim has to be on the event before the first of them reads it.
        StringAssert.Matches(escape, new Regex(@"\nwindow\.addEventListener\('keydown', \(e: KeyboardEvent\) => \{\n    BitBlazorUI\.Utils\.claimEscape\(e\);\n[^}]*\n\}, true\);"));

        // Kept by the publish-time trimming for every component that claims a key in its markup - the claim is read off
        // the DOM, so no interop call of theirs names this script.
        StringAssert.Contains(escape, "// bit-trim: markup data-bit-esc");
    }

    [TestMethod]
    public void EveryClaimTheMarkupWritesShouldBeOneTheScriptReads()
    {
        var script = GetMethod(SourceFiles.ReadUtilsScripts(), "claimEscape");

        StringAssert.Contains(script, "e.preventDefault()");
        StringAssert.Contains(script, "claim === 'claim'");
        StringAssert.Contains(script, "claim === 'text'");

        // The core components and the Extras ones alike: both packages' components sit in the surfaces of either.
        string[] components = [SourceFiles.GetDirectory("Bit.BlazorUI", "Components"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components")];

        // A claim is written either straight into the attribute - a literal, or the branches of a conditional - or
        // through one of the components' ...Claim members, whose branches are the values it can take.
        var markup = components.SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories))
                              .SelectMany(file => SourceFiles.ReadFullPath(file).Split('\n')
                                                             .Where(line => line.Contains("data-bit-esc=\""))
                                                             .Select(line => (file, line)))
                              .ToList();

        Assert.IsTrue(markup.Count > 0, "No data-bit-esc claim was found in the components.");

        var written = markup.SelectMany(m => Branches(Regex.Match(m.line, @"data-bit-esc=""(?<value>.*)""").Groups["value"].Value)
                                                 .Select(value => (m.file, value)));

        var properties = components.SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor.cs", SearchOption.AllDirectories))
                                  .SelectMany(file => Regex.Matches(SourceFiles.ReadFullPath(file), @"\w*Claim(\([^)]*\))?\s*=>[^;]*;", RegexOptions.Singleline)
                                                           .SelectMany(m => Branches(m.Value))
                                                           .Select(value => (file, value)));

        // Anything else is a claim nothing reads, which would leave the dialog around the component closing again.
        foreach (var (file, value) in written.Concat(properties))
        {
            Assert.IsTrue(value is "claim" or "text", $"{Path.GetFileName(file)} writes data-bit-esc=\"{value}\", which Utils.claimEscape does not read.");
        }
    }

    [TestMethod,
        DataRow("setupSurfaceEscape"),
        DataRow("setupEscapeGuard"),
        DataRow("watchEscape"),
        DataRow("watchLayerEscape"),
        DataRow("setupEscape"),
        DataRow("setupTooltip")
    ]
    public void EverySurfaceShouldLeaveAClaimedEscapeAlone(string method)
    {
        var body = GetMethod(SourceFiles.ReadUtilsScripts(), method);

        // Each keydown listener that answers the key - the ones calling into .NET - reads the claim itself: one of
        // them reading it does not keep another listener of the same method from closing the surface. The listeners
        // that only record what was there to see (an open callout, a composition) for another one are left out.
        var listeners = body.Split("addEventListener(").Skip(1)
                            .Where(listener => listener.StartsWith("'keydown'"))
                            .Where(listener => listener.Contains("invokeMethodAsync"))
                            .ToList();

        // A verdict handed to Utils.sendEscapeVerdicts answers the key as well when it is what the surface's .NET handler
        // dismisses on (a panel's escape guard) - unless all it says is which surface owns the press (the modal's, which
        // reports every Escape of its own, the claimed ones included).
        listeners.AddRange(body.Split("Utils.sendEscapeVerdicts(").Skip(1)
                               .Select(call => Regex.Split(call, @"\r?\n        \}")[0])
                               .Where(call => Regex.IsMatch(call, @"^[^;]*=>\s*Utils\.claimSurfaceEscape\(e, element\)\);") is false));

        Assert.IsTrue(listeners.Count > 0, $"Utils.{method} has no keydown listener that answers the key.");

        foreach (var listener in listeners)
        {
            StringAssert.Contains(listener, "e.defaultPrevented", $"A keydown listener of Utils.{method} answers an Escape a component inside it has claimed.");
        }
    }

    [TestMethod]
    public void TheCompositionOfAKeyShouldBeReadInOnePlace()
    {
        var check = GetMethod(SourceFiles.ReadUtilsScripts(), "isComposingKey");

        // The engines that report the real key during a composition set isComposing, and the ones that predate it
        // report the legacy 229 key code instead - so a check of one of them alone leaves a dialog closing in the other.
        StringAssert.Contains(check, "e.isComposing");
        StringAssert.Contains(check, "keyCode === 229");

        // Every script of both packages: the components' own (a text field holding back the keys of a composition, a
        // search box, a chart's tooltip) as well as the library's surfaces. The Extras ones call the core helper
        // through its declaration, which is no copy of it.
        string[] packages = [SourceFiles.GetDirectory("Bit.BlazorUI"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras")];

        var scripts = packages.SelectMany(folder => Directory.EnumerateFiles(folder, "*.ts", SearchOption.AllDirectories))
                              .Where(file => file.EndsWith(".d.ts") is false)
                              .Where(file => file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                 .Any(part => part is "node_modules" or "wwwroot" or "bin" or "obj") is false)
                              .ToList();

        Assert.IsTrue(scripts.Count > 1, "No script of the packages was found.");

        var copies = scripts.SelectMany(file => SourceFiles.ReadFullPath(file).Split('\n')
                                                           .Where(line => line.TrimStart().StartsWith("//") is false)
                                                           .Where(line => Regex.IsMatch(line, @"\.isComposing\b|=== 229"))
                                                           .Where(line => check.Contains(line.Trim()) is false)
                                                           .Select(line => $"{Path.GetFileName(file)}: {line.Trim()}"))
                            .ToList();

        Assert.AreEqual(0, copies.Count, $"A script reads the composition of a key outside Utils.isComposingKey: {string.Join(" | ", copies)}");
    }

    [TestMethod,
        DataRow("claimEscape"),
        DataRow("setupSurfaceEscape"),
        DataRow("setupEscapeGuard"),
        DataRow("watchEscape"),
        DataRow("watchLayerEscape"),
        DataRow("setupEscape"),
        DataRow("setupTooltip"),
        DataRow("dismissTooltipsOnEscape"),
        DataRow("stopComposingEscape")
    ]
    public void EverySurfaceShouldLeaveTheEscapeOfAnImeCompositionAlone(string method)
    {
        var body = GetMethod(SourceFiles.ReadUtilsScripts(), method);

        // Escape cancels the candidate an input method editor is composing, so the press is nobody's: a surface that
        // closed on it would take away the text the user was typing along with it.
        StringAssert.Contains(body, "Utils.isComposingKey(e)", $"Utils.{method} does not leave the Escape of an IME composition alone.");
    }

    [TestMethod,
        DataRow("setupSurfaceEscape"),
        DataRow("setupEscape"),
        DataRow("setupTooltip")
    ]
    public void EveryListenerThatAnswersTheKeyShouldReadTheCompositionItself(string method)
    {
        var body = GetMethod(SourceFiles.ReadUtilsScripts(), method);

        // As with a claim, one listener of the method reading it does not keep another one from closing the surface.
        // (watchEscape and watchLayerEscape record it in a listener of their own, which the answering one reads.)
        var listeners = body.Split("addEventListener(").Skip(1)
                            .Where(listener => listener.StartsWith("'keydown'"))
                            .Where(listener => listener.Contains("invokeMethodAsync"))
                            .ToList();

        Assert.IsTrue(listeners.Count > 0, $"Utils.{method} has no keydown listener that answers the key.");

        foreach (var listener in listeners)
        {
            StringAssert.Contains(listener, "Utils.isComposingKey(e)", $"A keydown listener of Utils.{method} answers the Escape of an IME composition.");
        }
    }

    [TestMethod]
    public void TheEscapeOfAnImeCompositionShouldNeverReachDotNet()
    {
        // KeyboardEventArgs cannot tell it from any other Escape (no key code, and no composition state before .NET 9),
        // so it is stopped on the document element in the bubble phase: after every listener of the page's elements,
        // and ahead of Blazor's delegation, which listens on the document.
        var keys = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Keys.ts");

        StringAssert.Contains(keys, "document.documentElement.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.stopComposingEscape(e));");

        // Every .NET handler acting on Escape relies on it, through markup no interop call names: every bundle keeps it.
        StringAssert.Contains(keys, "// bit-trim: always");

        var stop = GetMethod(SourceFiles.ReadUtilsScripts(), "stopComposingEscape");

        StringAssert.Contains(stop, "e.key !== 'Escape'");
        StringAssert.Contains(stop, "e.stopPropagation()");
        Assert.IsFalse(stop.Contains("preventDefault"), "Utils.stopComposingEscape takes the key away from the IME it belongs to.");
    }

    [TestMethod]
    public void TheCalloutShouldLeaveAnEscapeOfAPopupOpenedFromInsideItAlone()
    {
        // A BitCallout dismisses itself in .NET, on its root and on its content, which hear a press a popup opened from
        // inside them closes, too - so the watch on its Escapes asks about the open callouts as a panel's guard does, and
        // is told which callout is asking: that one is the innermost open callout the key closes, never another.
        var utils = SourceFiles.ReadUtilsScripts();

        StringAssert.Contains(GetMethod(utils, "watchNestedEscape"), "Utils.isEscapeOfOpenCallout(e.target, id)");
        StringAssert.Contains(GetMethod(utils, "isEscapeOfOpenCallout"), "current.calloutId === ownCalloutId");
    }

    [TestMethod,
        DataRow("setupSurfaceEscape"),
        DataRow("setupEscapeGuard"),
        DataRow("setupEscape")
    ]
    public void EverySurfaceShouldBoundTheClaimsAroundIt(string method)
    {
        var script = SourceFiles.ReadUtilsScripts();

        // A claim is only looked for up to the nearest surface the key was pressed in, so a message or a field around
        // a dialog never takes the key away from the dialog itself.
        StringAssert.Contains(GetMethod(script, "claimEscape"), "Utils.isEscapeSurface(element)");
        StringAssert.Contains(GetMethod(script, method), "Utils._escapeSurfaces.add(element)", $"Utils.{method} does not mark its element as a surface for Utils.claimEscape.");
    }

    [TestMethod]
    public void TheClaimShouldLeaveTheKeyToAnOpenPopupOrAShownTooltipInsideTheClaimer()
    {
        var claim = GetMethod(SourceFiles.ReadUtilsScripts(), "claimEscape");

        // A component whose popup is open closes it on the key, and a shown tooltip is dismissed by it, without either
        // claiming it: a root claiming every Escape pressed inside it (a navigation drawer, a dialog of the Extras)
        // would otherwise take the key away from them and close itself instead.
        StringAssert.Contains(claim, "Callouts.componentContains(target, element)");
        StringAssert.Contains(claim, "Utils._escapeBoundaries.some(isBoundary => isBoundary(element))");

        // The tooltips' script adds its anchors to the boundaries as it loads, so the claim stops at a shown one in every
        // bundle that has tooltips, and costs nothing in one that has none.
        StringAssert.Contains(SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Tooltips.ts"),
                              "BitBlazorUI.Utils.addEscapeBoundary(element => BitBlazorUI.Utils.showsDismissibleTooltip(element));");
    }

    [TestMethod]
    public void TheVerdictForAClaimingRootShouldBeWrittenOnce()
    {
        // Every root whose .NET handler acts on the key it claims tells its own press from one a part of it claimed
        // first through Utils.watchEscapeClaim - never through a copy of it, which would drift from it.
        var copies = new[] { SourceFiles.GetDirectory("Bit.BlazorUI"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras") }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(folder, "Components"), "*.ts", SearchOption.AllDirectories))
            .Where(file => SourceFiles.ReadFullPath(file).Contains("'OnEscapeVerdict'"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.AreEqual(0, copies.Count, $"These scripts answer OnEscapeVerdict on their own: {string.Join(", ", copies)}.");
    }

    [TestMethod]
    public void TheTooltipShouldNotGuessWhoActsOnTheKey()
    {
        var script = SourceFiles.ReadUtilsScripts();
        var tooltip = GetMethod(script, "setupTooltip");

        // A press inside the anchor goes on to a component there only when the component said it acts on it - a claim,
        // or a popup of its own that is open - never because the target looks like something that might.
        StringAssert.Contains(tooltip, "Utils.escapeClaimer(e)");
        StringAssert.Contains(tooltip, "Callouts.componentContains(");
        Assert.IsFalse(tooltip.Contains(".closest("), "Utils.setupTooltip decides who owns the key from a selector.");
        Assert.IsFalse(script.Contains("_escapeOwners"), "Utils.ts still keeps a selector of elements guessed to own Escape.");
    }

    [TestMethod]
    public void TheTooltipShouldTakeItsEscapeRightAfterTheClaim()
    {
        // Listeners on one node run in the order they were added, so a listener a tooltip added as it registered would
        // run behind whatever had registered before it - a hover-opened menu around it (Utils.setupEscape), an overlay
        // (Utils.watchLayerEscape) - and those would act on the press the tooltip takes. Run on the claim's own listener,
        // added as the page loads on the window in the capture phase, right after the claim, the only listener of the
        // library ahead of it is the claim, which it reads - in whichever order the bundle loads the two scripts.
        var escape = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Escape.ts");
        var tooltips = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Tooltips.ts");

        var keydowns = Regex.Matches(escape + tooltips, @"(window|document)\.addEventListener\('keydown'.*").Select(m => m.Value.Trim()).ToList();

        Assert.HasCount(1, keydowns, "The claim's script was expected to add the one page-level keydown listener of the two, and the tooltips' script none.");
        StringAssert.Contains(escape, "for (const listener of BitBlazorUI.Utils._afterEscapeClaim) listener(e);");
        StringAssert.Contains(tooltips, "BitBlazorUI.Utils.afterEscapeClaim(e => BitBlazorUI.Utils.dismissTooltipsOnEscape(e));");

        // No tooltip adds a page-level keydown listener of its own, whose place among the others would be left to chance.
        var utils = SourceFiles.ReadUtilsScripts();
        foreach (var method in new[] { "setupTooltip", "ensureTooltipListeners" })
        {
            var body = GetMethod(utils, method);
            Assert.IsFalse(Regex.IsMatch(body, @"(window|document)\.addEventListener\('keydown'"), $"Utils.{method} adds a page-level keydown listener.");
        }
    }

    [TestMethod]
    public void TheTooltipShouldLeaveAnEscapeItDoesNotTakeToTheListenersAfterIt()
    {
        var body = GetMethod(SourceFiles.ReadUtilsScripts(), "dismissTooltipsOnEscape");

        // A press a component claimed, or that a gesture in progress puts back (a swipe, whose own listener is added on
        // the window after this one), is not the tooltip's.
        StringAssert.Contains(body, "e.defaultPrevented");
        StringAssert.Contains(body, "Utils._escapeGestures");
        StringAssert.Contains(GetMethod(SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "SwipeTrap", "BitSwipeTrap.ts"), "setup", "SwipeTrap"),
                              "Utils.addEscapeGesture(");

        // A press it does take goes no further than the window, where every other listener still hears it - as taken.
        StringAssert.Contains(body, "e.preventDefault()");
        StringAssert.Contains(body, "e.stopPropagation()");
        Assert.IsFalse(body.Contains("stopImmediatePropagation"), "The tooltip's Escape keeps the press from the other listeners on the window as well.");
    }

    // The strings an attribute value or a property can come out as: the whole of a plain literal, or each branch of a
    // conditional expression.
    private static string[] Branches(string expression)
    {
        if (expression.StartsWith('@') is false && expression.Contains('"') is false) return [expression];

        return Regex.Matches(expression, @"[?:]\s*""(?<value>[^""]*)""")
                    .Select(m => m.Groups["value"].Value)
                    .ToArray();
    }

    // From the member's declaration to the next member declared at the same depth.
    private static string GetMethod(string script, string name, string type = "Utils") => SourceFiles.GetScriptMember(script, name, type);
}
