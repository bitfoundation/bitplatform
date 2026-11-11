using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bit.BlazorUI.Build;
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
        Assert.MatchesRegex(new Regex(@"\nwindow\.addEventListener\('keydown', \(e: KeyboardEvent\) => \{\n    BitBlazorUI\.Utils\.claimEscape\(e\);\n[^}]*\n\}, true\);"), escape);

        // Kept by the publish-time trimming for every component that claims a key in its markup - the claim is read off
        // the DOM, so no interop call of theirs names this script.
        Assert.Contains("// bit-trim: markup data-bit-esc", escape);
    }

    [TestMethod]
    public void TheClaimShouldBeTheFirstListenerOfTheKeyOnTheWindow()
    {
        // The window in the capture phase is the first place a key is dispatched to, and the listeners of one node run in
        // the order they were added - so the claim is on the event ahead of every surface only while no script the bundle
        // loads before Utils.Escape adds a capturing keydown listener to the window as it loads, at its top level or in a
        // function it calls there. That used to be one file's order (general.ts); now every script adds its own page-level
        // listeners, so it is the bundle's: the manifest lists the scripts in the order they load and, for each, the
        // listeners it adds as it does ("window keydown capture"), as build-assets.mjs works them out.
        var core = PackageAssets.Load(SourceFiles.GetPath("Bit.BlazorUI", "obj", "bit-assets"));

        var windowKeydowns = core.ScriptModules.Where(m => m.Listeners.Contains("window keydown capture")).Select(m => m.Name).ToList();

        Assert.IsNotEmpty(windowKeydowns, "No script of the core bundle adds a capturing keydown listener to the window as it loads: the claim is not added where it has to be.");
        Assert.AreEqual("Utils.Escape", windowKeydowns[0], $"A script loading ahead of Utils.Escape adds a capturing keydown listener to the window, which runs before the claim: {string.Join(", ", windowKeydowns)}.");
        Assert.AreEqual("window keydown capture", core.ScriptModules.Single(m => m.Name == "Utils.Escape").Listeners.First(l => l.StartsWith("window keydown", System.StringComparison.Ordinal)),
                        "Utils.Escape adds a keydown listener to the window ahead of the claim's own.");
    }

    [TestMethod]
    public void EveryClaimTheMarkupWritesShouldBeOneTheScriptReads()
    {
        var script = GetMethod(SourceFiles.ReadUtilsScripts(), "claimEscape");

        Assert.Contains("e.preventDefault()", script);
        Assert.Contains("claim === 'claim'", script);
        Assert.Contains("claim === 'text'", script);

        // The core components and the Extras ones alike: both packages' components sit in the surfaces of either.
        string[] components = [SourceFiles.GetDirectory("Bit.BlazorUI", "Components"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components"), SourceFiles.GetDirectory("Bit.BlazorUI.AI", "Components")];

        // A claim is written either straight into the attribute - a literal, or the branches of a conditional - or
        // through one of the components' ...Claim members, whose branches are the values it can take.
        var markup = components.SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories))
                              .SelectMany(file => SourceFiles.ReadFullPath(file).Split('\n')
                                                             .Where(line => line.Contains("data-bit-esc=\""))
                                                             .Select(line => (file, line)))
                              .ToList();

        Assert.IsNotEmpty(markup, "No data-bit-esc claim was found in the components.");

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

        Assert.IsNotEmpty(listeners, $"Utils.{method} has no keydown listener that answers the key.");

        foreach (var listener in listeners)
        {
            Assert.Contains("e.defaultPrevented", listener, $"A keydown listener of Utils.{method} answers an Escape a component inside it has claimed.");
        }
    }

    [TestMethod]
    public void TheCompositionOfAKeyShouldBeReadInOnePlace()
    {
        var check = GetMethod(SourceFiles.ReadUtilsScripts(), "isComposingKey");

        // The engines that report the real key during a composition set isComposing, and the ones that predate it
        // report the legacy 229 key code instead - so a check of one of them alone leaves a dialog closing in the other.
        Assert.Contains("e.isComposing", check);
        Assert.Contains("keyCode === 229", check);

        // Every script of both packages: the components' own (a text field holding back the keys of a composition, a
        // search box, a chart's tooltip) as well as the library's surfaces. The Extras ones call the core helper
        // through its declaration, which is no copy of it.
        string[] packages = [SourceFiles.GetDirectory("Bit.BlazorUI"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras"), SourceFiles.GetDirectory("Bit.BlazorUI.AI")];

        var scripts = packages.SelectMany(folder => Directory.EnumerateFiles(folder, "*.ts", SearchOption.AllDirectories))
                              .Where(file => file.EndsWith(".d.ts") is false)
                              .Where(file => file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                 .Any(part => part is "node_modules" or "wwwroot" or "bin" or "obj") is false)
                              .ToList();

        Assert.IsGreaterThan(1, scripts.Count, "No script of the packages was found.");

        var copies = scripts.SelectMany(file => SourceFiles.ReadFullPath(file).Split('\n')
                                                           .Where(line => line.TrimStart().StartsWith("//") is false)
                                                           .Where(line => Regex.IsMatch(line, @"\.isComposing\b|=== 229"))
                                                           .Where(line => check.Contains(line.Trim()) is false)
                                                           .Select(line => $"{Path.GetFileName(file)}: {line.Trim()}"))
                            .ToList();

        Assert.IsEmpty(copies, $"A script reads the composition of a key outside Utils.isComposingKey: {string.Join(" | ", copies)}");
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
        Assert.Contains("Utils.isComposingKey(e)", body, $"Utils.{method} does not leave the Escape of an IME composition alone.");
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

        Assert.IsNotEmpty(listeners, $"Utils.{method} has no keydown listener that answers the key.");

        foreach (var listener in listeners)
        {
            Assert.Contains("Utils.isComposingKey(e)", listener, $"A keydown listener of Utils.{method} answers the Escape of an IME composition.");
        }
    }

    [TestMethod]
    public void TheEscapeOfAnImeCompositionShouldNeverReachDotNet()
    {
        // KeyboardEventArgs cannot tell it from any other Escape (no key code, which is all that marks it in WebKit once
        // the composition has ended), so it is stopped on the document element in the bubble phase: after every listener of the page's elements,
        // and ahead of Blazor's delegation, which listens on the document.
        var keys = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Keys.ts");

        Assert.Contains("document.documentElement.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.stopComposingEscape(e));", keys);

        // Every .NET handler acting on Escape relies on it, through markup no interop call names: every bundle keeps it.
        Assert.Contains("// bit-trim: always", keys);

        var stop = GetMethod(SourceFiles.ReadUtilsScripts(), "stopComposingEscape");

        Assert.Contains("e.key !== 'Escape'", stop);
        Assert.Contains("e.stopPropagation()", stop);
        Assert.DoesNotContain("preventDefault", stop, "Utils.stopComposingEscape takes the key away from the IME it belongs to.");
    }

    [TestMethod]
    public void TheCalloutShouldLeaveAnEscapeOfAPopupOpenedFromInsideItAlone()
    {
        // A BitCallout dismisses itself in .NET, on its root and on its content, which hear a press a popup opened from
        // inside them closes, too - so the watch on its Escapes asks about the open callouts as a panel's guard does, and
        // is told which callout is asking: that one is the innermost open callout the key closes, never another.
        var utils = SourceFiles.ReadUtilsScripts();

        Assert.Contains("Utils.isEscapeOfOpenCallout(e.target, id)", GetMethod(utils, "watchNestedEscape"));
        Assert.Contains("current.calloutId === ownCalloutId", GetMethod(utils, "isEscapeOfOpenCallout"));
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
        Assert.Contains("Utils.isEscapeSurface(element)", GetMethod(script, "claimEscape"));
        Assert.Contains("Utils._escapeSurfaces.add(element)", GetMethod(script, method), $"Utils.{method} does not mark its element as a surface for Utils.claimEscape.");
    }

    [TestMethod]
    public void TheClaimShouldLeaveTheKeyToAnOpenPopupOrAShownTooltipInsideTheClaimer()
    {
        var claim = GetMethod(SourceFiles.ReadUtilsScripts(), "claimEscape");

        // A component whose popup is open closes it on the key, and a shown tooltip is dismissed by it, without either
        // claiming it: a root claiming every Escape pressed inside it (a navigation drawer, a dialog of the Extras)
        // would otherwise take the key away from them and close itself instead.
        Assert.Contains("Callouts.componentContains(target, element)", claim);
        Assert.Contains("Utils._escapeBoundaries.some(isBoundary => isBoundary(element))", claim);

        // The tooltips' script adds its anchors to the boundaries as it loads, so the claim stops at a shown one in every
        // bundle that has tooltips, and costs nothing in one that has none.
        Assert.Contains("BitBlazorUI.Utils.addEscapeBoundary(element => BitBlazorUI.Utils.showsDismissibleTooltip(element));", SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.Tooltips.ts"));
    }

    [TestMethod]
    public void TheVerdictForAClaimingRootShouldBeWrittenOnce()
    {
        // Every root whose .NET handler acts on the key it claims tells its own press from one a part of it claimed
        // first through Utils.watchEscapeClaim - never through a copy of it, which would drift from it.
        var copies = new[] { SourceFiles.GetDirectory("Bit.BlazorUI"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras"), SourceFiles.GetDirectory("Bit.BlazorUI.AI") }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(folder, "Components"), "*.ts", SearchOption.AllDirectories))
            .Where(file => SourceFiles.ReadFullPath(file).Contains("'OnEscapeVerdict'"))
            .Select(Path.GetFileName)
            .ToList();

        Assert.IsEmpty(copies, $"These scripts answer OnEscapeVerdict on their own: {string.Join(", ", copies)}.");
    }

    [TestMethod]
    public void TheTooltipShouldNotGuessWhoActsOnTheKey()
    {
        var script = SourceFiles.ReadUtilsScripts();
        var tooltip = GetMethod(script, "setupTooltip");

        // A press inside the anchor goes on to a component there only when the component said it acts on it - a claim,
        // or a popup of its own that is open - never because the target looks like something that might.
        Assert.Contains("Utils.escapeClaimer(e)", tooltip);
        Assert.Contains("Callouts.componentContains(", tooltip);
        Assert.DoesNotContain(".closest(", tooltip, "Utils.setupTooltip decides who owns the key from a selector.");
        Assert.DoesNotContain("_escapeOwners", script, "Utils.ts still keeps a selector of elements guessed to own Escape.");
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

        Assert.ContainsSingle(keydowns, "The claim's script was expected to add the one page-level keydown listener of the two, and the tooltips' script none.");
        Assert.Contains("for (const listener of BitBlazorUI.Utils._afterEscapeClaim) listener(e);", escape);
        Assert.Contains("BitBlazorUI.Utils.afterEscapeClaim(e => BitBlazorUI.Utils.dismissTooltipsOnEscape(e));", tooltips);

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
        Assert.Contains("e.defaultPrevented", body);
        Assert.Contains("Utils._escapeGestures", body);
        Assert.Contains("Utils.addEscapeGesture(", GetMethod(SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "SwipeTrap", "BitSwipeTrap.ts"), "setup", "SwipeTrap"));

        // A press it does take goes no further than the window, where every other listener still hears it - as taken.
        Assert.Contains("e.preventDefault()", body);
        Assert.Contains("e.stopPropagation()", body);
        Assert.DoesNotContain("stopImmediatePropagation", body, "The tooltip's Escape keeps the press from the other listeners on the window as well.");
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
