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
        var general = SourceFiles.Read("Bit.BlazorUI", "Scripts", "general.ts");

        // On the window in the capture phase: the surfaces listen on their own element, on the document and on the
        // window, and the claim has to be on the event before the first of them reads it.
        StringAssert.Contains(general, "window.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.claimEscape(e), true);");
    }

    [TestMethod]
    public void EveryClaimTheMarkupWritesShouldBeOneTheScriptReads()
    {
        var script = GetMethod(SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts"), "claimEscape");

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
        DataRow("setupTooltip"),
        DataRow("ensureTooltipListeners")
    ]
    public void EverySurfaceShouldLeaveAClaimedEscapeAlone(string method)
    {
        var body = GetMethod(SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts"), method);

        // Each keydown listener that answers the key - the ones calling into .NET - reads the claim itself: one of
        // them reading it does not keep another listener of the same method from closing the surface. The listeners
        // that only record what was there to see (an open callout, a composition) for another one are left out.
        var listeners = body.Split("addEventListener(").Skip(1)
                            .Where(listener => listener.StartsWith("'keydown'"))
                            .Where(listener => listener.Contains("invokeMethodAsync"))
                            .ToList();

        Assert.IsTrue(listeners.Count > 0, $"Utils.{method} has no keydown listener that answers the key.");

        foreach (var listener in listeners)
        {
            StringAssert.Contains(listener, "e.defaultPrevented", $"A keydown listener of Utils.{method} answers an Escape a component inside it has claimed.");
        }
    }

    [TestMethod,
        DataRow("setupSurfaceEscape"),
        DataRow("setupEscapeGuard"),
        DataRow("setupEscape")
    ]
    public void EverySurfaceShouldBoundTheClaimsAroundIt(string method)
    {
        var script = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts");

        // A claim is only looked for up to the nearest surface the key was pressed in, so a message or a field around
        // a dialog never takes the key away from the dialog itself.
        StringAssert.Contains(GetMethod(script, "claimEscape"), "Utils.isEscapeSurface(element)");
        StringAssert.Contains(GetMethod(script, method), "Utils._escapeSurfaces.add(element)", $"Utils.{method} does not mark its element as a surface for Utils.claimEscape.");
    }

    [TestMethod]
    public void TheClaimShouldLeaveTheKeyToAnOpenPopupOrAShownTooltipInsideTheClaimer()
    {
        var claim = GetMethod(SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts"), "claimEscape");

        // A component whose popup is open closes it on the key, and a shown tooltip is dismissed by it, without either
        // claiming it: a root claiming every Escape pressed inside it (a navigation drawer, a dialog of the Extras)
        // would otherwise take the key away from them and close itself instead.
        StringAssert.Contains(claim, "Callouts.componentContains(target, element)");
        StringAssert.Contains(claim, "Utils.showsDismissibleTooltip(element)");
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
        var script = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Utils.ts");
        var tooltip = GetMethod(script, "setupTooltip");

        // A press inside the anchor goes on to a component there only when the component said it acts on it - a claim,
        // or a popup of its own that is open - never because the target looks like something that might.
        StringAssert.Contains(tooltip, "Utils.escapeClaimer(e)");
        StringAssert.Contains(tooltip, "Callouts.componentContains(");
        Assert.IsFalse(tooltip.Contains(".closest("), "Utils.setupTooltip decides who owns the key from a selector.");
        Assert.IsFalse(script.Contains("_escapeOwners"), "Utils.ts still keeps a selector of elements guessed to own Escape.");
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

    // From the method's signature to the next member declared at the same depth.
    private static string GetMethod(string script, string name)
    {
        var match = Regex.Match(script, $@"\n        (public|private) static {name}\(.*?(?=\n        (public|private) )", RegexOptions.Singleline);

        Assert.IsTrue(match.Success, $"Utils.{name} was not found in Utils.ts.");

        return match.Value;
    }
}
