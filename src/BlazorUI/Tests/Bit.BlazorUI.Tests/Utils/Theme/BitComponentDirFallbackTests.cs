using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that a right-to-left rule written with <c>:dir()</c> still holds for an explicit <c>Dir</c> on an engine
/// without <c>:dir()</c>.
/// </summary>
/// <remarks>
/// <c>:dir()</c> reads the direction an element is actually laid out in, inherited or not, which is why the components
/// use it - but it arrived late (Chromium 120, Safari 16.4), and the WebViews a Blazor Hybrid app runs in can be older.
/// An engine that does not know a selector drops the whole rule, every other selector of its list included (#13566).
/// So a <c>:dir(rtl)</c> is written inside a forgiving <c>:is()</c> beside the <c>bit-rtl</c> class (or the
/// <c>dir="rtl"</c> attribute) an explicit <c>Dir</c> renders, or else stands in a list of its own with a counterpart
/// reading the class, with the same declarations, in the same at-rules. Either way the class is read on the element
/// the <c>:dir()</c> is on or on an ancestor reached through child combinators only; a class further up, behind a
/// descendant combinator, would also reach into a nested instance reading the other way, so it is read only inside
/// <c>@supports not selector(:dir(rtl))</c>, where nothing better is known. Left out: what is inside
/// <c>@supports selector(:dir(...))</c>, which only an engine knowing <c>:dir()</c> reads. A mixin is checked where
/// it is written, its counterparts looked for in the mixin itself.
/// </remarks>
[TestClass]
public sealed class BitComponentDirFallbackTests
{
    // The :dir() rules whose explicit Dir is taken care of another way.
    private static readonly HashSet<string> CoveredOtherwise = new(StringComparer.Ordinal)
    {
        // The bit-rtl class on the root sets --bit-brc-div-transform, which the divider reads.
        ".bit-brc-div:dir(rtl)",
        // The component turns the chevron itself for an explicit Dir; the rule only serves an inherited direction.
        ".bit-spl:not(.bit-spl-vrt):not([dir=\"rtl\"]):dir(rtl) > .bit-spl-gtr > .bit-spl-cbt > .bit-spl-cbd",
    };

    // The bit-rtl class, the class of its own a component renders for an explicit Dir on a part outside its root (the
    // relocated callout of a dropdown, bit-drp-rtl), or the dir attribute.
    private static readonly Regex RtlHook = new(@"\.bit-(?:[a-z0-9]+-)?rtl(?![a-zA-Z0-9_-])|\[dir=[""']?rtl[""']?\]", RegexOptions.Compiled);

    private static readonly Regex DirRtl = new(@":dir\(\s*rtl\s*\)", RegexOptions.Compiled);

    private static readonly Regex Forgiving = new(@":(?:is|where)\(", RegexOptions.Compiled);

    // Only an engine that knows :dir() reads what is inside.
    private static readonly Regex DirKnownGuard = new(@"^@supports\s+selector\(\s*:dir\(", RegexOptions.Compiled);

    // Only an engine that does not know :dir() reads what is inside.
    private static readonly Regex DirUnknownGuard = new(@"^@supports\s+not\s+selector\(\s*:dir\(", RegexOptions.Compiled);

    [TestMethod]
    public void EveryDirRuleHasAFallbackForAnExplicitDir()
    {
        var offenders = EnumerateStylesheets().SelectMany(file => Check(Path.GetFileName(file), SourceFiles.ReadFullPath(file))).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A :dir(rtl) rule is lost for an explicit Dir on an engine without :dir(), or its fallback reaches into a nested instance; write it as :is(.bit-rtl, :dir(rtl)), or read the class through child combinators only, or inside @supports not selector(:dir(rtl)):{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    // The rules that read the class or the dir attribute an explicit Dir renders, and nothing else, on purpose - each a
    // prefix of the selectors it covers.
    private static readonly string[] ExplicitOnly =
    [
        // The class sets --bit-brc-div-transform for an explicit Dir; .bit-brc-div:dir(rtl) turns the divider for the rest.
        ".bit-brc.bit-rtl",
        // The direction a right-to-left culture implies for a picker whose Dir is unset: the class is the whole of it.
        ".bit-dtp-cal.bit-dtp-rtl",
        ".bit-dtrp-cal.bit-dtrp-rtl",
        ".bit-tpc-cal.bit-tpc-rtl",
        // The calendar writes the direction it resolved (its culture's, or the one read back for Auto) on its root and
        // in bit-bfc-rtl, so the class and the attribute are what it is laid out in.
        ".bit-bfc-rtl ",
        "[dir=\"rtl\"] .bit-bfc-",
        // The safe-area insets of the page, mapped onto the inline sides of whatever declares a direction.
        ":root [dir=\"rtl\"]",
    ];

    // A rule reading the class or the dir attribute an explicit Dir renders sees only that one: a component whose Dir
    // leaves the direction to the page or to its content (Auto) reads right to left without either. So every such rule
    // is the fallback of a :dir(rtl) one with the same declarations, which follows the direction the element is
    // actually laid out in, whatever made it so (#13567).
    [TestMethod]
    public void EveryRtlRuleAlsoFollowsADirectionDirDoesNotName()
    {
        var offenders = EnumerateStylesheets().SelectMany(file => CheckFollowsResolvedDirection(Path.GetFileName(file), SourceFiles.ReadFullPath(file))).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A right-to-left rule reads only the class or the attribute an explicit Dir renders, so it is lost for a component that is right to left by inheriting the page's direction or by its content (Auto); write it as :is(.bit-rtl, :dir(rtl)), or give it a :dir(rtl) twin and keep the class inside @supports not selector(:dir(rtl)):{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    [TestMethod,
        DataRow(".a:is(.bit-rtl, :dir(rtl)) { b: c; }"),
        DataRow(".a:dir(rtl) { b: c; }\n@supports not selector(:dir(rtl)) { .r.bit-rtl .a { b: c; } }"),
        DataRow(".a:dir(rtl) { b: c; }\n.r.bit-rtl > .a { b: c; }"),
        DataRow("@supports selector(:dir(rtl)) { .a:dir(rtl) { b: c; } }\n.bit-rtl.a { b: c; }"),
        DataRow(".a:not(.bit-rtl) { b: c; }")]
    public void AnRtlRuleWithADirTwinPasses(string stylesheet)
    {
        CollectionAssert.AreEqual(Array.Empty<string>(), CheckFollowsResolvedDirection("test.scss", stylesheet).ToArray());
    }

    [TestMethod,
        DataRow(".bit-rtl.a { b: c; }"),
        DataRow(".r.bit-rtl .a { b: c; }"),
        DataRow(".a[dir=\"rtl\"] .b { b: c; }"),
        DataRow(".a.bit-drp-rtl { b: c; }"),
        DataRow(".a:dir(rtl) { b: c; }\n.bit-rtl.a { b: d; }")]
    public void AnRtlRuleWithoutADirTwinFails(string stylesheet)
    {
        Assert.AreEqual(1, CheckFollowsResolvedDirection("test.scss", stylesheet).Count());
    }

    private static IEnumerable<string> CheckFollowsResolvedDirection(string name, string source)
    {
        var stylesheet = SourceFiles.StripScssComments(source).Replace("\r\n", "\n");
        var lineStarts = LineStarts(stylesheet);
        var entries = new List<(string Selector, string[] Declarations, bool Fallback, string Location)>();

        foreach (var rule in SourceFiles.GetScssRules(stylesheet))
        {
            var headers = rule.Ancestors.Append(rule.Header).ToArray();

            if (headers.Any(header => header.StartsWith("@function", StringComparison.Ordinal))) continue;

            var declarations = rule.Declarations.Select(Squash).Order(StringComparer.Ordinal).ToArray();

            if (declarations.Length == 0) continue;

            var fallback = headers.Any(header => DirUnknownGuard.IsMatch(header));

            foreach (var selector in SourceFiles.ResolveScssSelectors(headers.Where(header => header.StartsWith('@') is false)))
            {
                entries.Add((Squash(selector), declarations, fallback, $"{name}:{LineOf(lineStarts, rule.Index)}"));
            }
        }

        var twins = entries.Where(entry => DirRtl.IsMatch(entry.Selector)).ToArray();

        foreach (var entry in entries)
        {
            // Inside @supports not selector(:dir(rtl)) it is the fallback of a :dir() rule, which the other test pins.
            if (entry.Fallback) continue;

            // A :dir() anywhere in it - beside the class in an :is(), or the twin itself - follows the direction already.
            if (entry.Selector.Contains(":dir(", StringComparison.Ordinal)) continue;

            if (FindHook(entry.Selector) is null) continue;

            if (ExplicitOnly.Any(prefix => entry.Selector.StartsWith(prefix, StringComparison.Ordinal))) continue;

            if (twins.Any(twin => twin.Declarations.SequenceEqual(entry.Declarations))) continue;

            yield return $"{entry.Location} reads the direction only off an explicit Dir: {entry.Selector}";
        }
    }

    [TestMethod,
        DataRow(".a:is(.bit-rtl, :dir(rtl)) { b: c; }"),
        DataRow(".a:is(:dir(rtl), .r.bit-rtl > .p > *) { b: c; }"),
        DataRow(".a:dir(rtl) { b: c; }\n.bit-rtl.a { b: c; }"),
        DataRow(".a:dir(rtl) { b: c; }\n.r.bit-rtl > .a { b: c; }"),
        DataRow(".a:is(.bit-drp-rtl, :dir(rtl)) { b: c; }"),
        DataRow(".a:dir(rtl) { b: c; }\n@supports not selector(:dir(rtl)) { .bit-drp-rtl .a { b: c; } }"),
        DataRow(".a:dir(rtl) { b: c; }\n@supports not selector(:dir(rtl)) { .r.bit-rtl .a { b: c; } }"),
        DataRow("@media print { .a:dir(rtl) { b: c; } .bit-rtl.a { b: c; } }"),
        DataRow("@mixin m { &:dir(rtl) { b: c; } &.bit-rtl { b: c; } }"),
        DataRow("@supports selector(:dir(rtl)) { .a:dir(ltr) { b: c; } }")]
    public void AStylesheetWithAFallbackForEveryDirRulePasses(string stylesheet)
    {
        CollectionAssert.AreEqual(Array.Empty<string>(), Check("test.scss", stylesheet).ToArray());
    }

    [TestMethod,
        // The rule is dropped whole, its other selector with it.
        DataRow(".a:dir(rtl), .bit-rtl.a { b: c; }", "shares a selector list"),
        DataRow(".a:dir(rtl) { b: c; }", "no counterpart"),
        DataRow(".a:dir(rtl) { b: c; }\n.bit-rtl.a { b: d; }", "no counterpart"),
        DataRow(".a:dir(rtl) { b: c; }\n@media print { .bit-rtl.a { b: c; } }", "no counterpart"),
        // A selector that rules right to left out is no fallback for it.
        DataRow(".a:dir(rtl) .b { b: c; }\n.a:not(.bit-rtl) .b { b: c; }", "no counterpart"),
        DataRow(".a:dir(rtl) { b: c; }\n.a:not([dir=\"rtl\"]) { b: c; }", "no counterpart"),
        // A class further up also reaches into a nested instance that reads the other way.
        DataRow(".a:dir(rtl) { b: c; }\n.bit-rtl .a { b: c; }", "descendant combinator"),
        DataRow(".a:dir(rtl) { b: c; }\n.r.bit-rtl > .p .a { b: c; }", "descendant combinator"),
        DataRow(".a:is(.bit-rtl .p > *, :dir(rtl)) { b: c; }", "descendant combinator"),
        // Only an engine that knows :dir() reads it, which is the engine that does not need it.
        DataRow(".a:dir(rtl) { b: c; }\n@supports selector(:dir(rtl)) { .bit-rtl.a { b: c; } }", "no counterpart"),
        DataRow(".a:is(:dir(rtl)) { b: c; }", "no .bit-rtl alternative"),
        DataRow("@mixin m { &:dir(rtl) { b: c; } }", "no counterpart"),
        DataRow("@mixin m { &:dir(rtl), &.x { b: c; } }", "shares a selector list"),
        DataRow(".a:dir(ltr) { b: c; }", "other than rtl")]
    public void AStylesheetMissingAFallbackFails(string stylesheet, string reason)
    {
        var offenders = Check("test.scss", stylesheet).ToArray();

        Assert.IsTrue(offenders.Any(offender => offender.Contains(reason, StringComparison.Ordinal)), $"Expected \"{reason}\", got: {string.Join(" / ", offenders)}");
    }

    private static IEnumerable<string> Check(string name, string source)
    {
        var stylesheet = SourceFiles.StripScssComments(source).Replace("\r\n", "\n");
        var lineStarts = LineStarts(stylesheet);
        var offenders = new List<string>();
        var entries = new List<Entry>();

        foreach (var rule in SourceFiles.GetScssRules(stylesheet))
        {
            var headers = rule.Ancestors.Append(rule.Header).ToArray();

            if (headers.Any(header => header.StartsWith("@function", StringComparison.Ordinal))) continue;

            var guard = headers.Any(header => DirKnownGuard.IsMatch(header)) ? Guard.DirKnown
                      : headers.Any(header => DirUnknownGuard.IsMatch(header)) ? Guard.DirUnknown
                      : Guard.None;

            if (guard is Guard.DirKnown) continue;

            var location = $"{name}:{LineOf(lineStarts, rule.Index)}";

            // A list holding a :dir() is dropped whole where :dir() is unknown, taking its other selectors with it.
            if (rule.Header.StartsWith('@') is false)
            {
                var alternatives = SourceFiles.SplitScssSelectorList(rule.Header);

                if (alternatives.Any(UsesDir) && alternatives.Any(alternative => UsesDir(alternative) is false))
                {
                    offenders.Add($"{location} shares a selector list with a :dir() selector: {Squash(rule.Header)}");
                }
            }

            var declarations = rule.Declarations.Select(Squash).Order(StringComparer.Ordinal).ToArray();

            if (declarations.Length == 0) continue;

            // The at-rules a rule is written in, a @mixin among them, so a counterpart is looked for in the same ones.
            var context = string.Join(" | ", headers.Where(header => header.StartsWith('@') && DirUnknownGuard.IsMatch(header) is false).Select(Squash));

            foreach (var selector in SourceFiles.ResolveScssSelectors(headers.Where(header => header.StartsWith('@') is false)))
            {
                entries.Add(new(Squash(selector), declarations, context, guard, location));
            }
        }

        var counterparts = entries.Where(entry => UsesDir(entry.Selector) is false && FindHook(entry.Selector) is not null).ToArray();

        foreach (var entry in entries.Where(entry => entry.Guard is Guard.None))
        {
            CheckForgivingLists(entry, offenders);

            if (UsesDir(entry.Selector) is false || CoveredOtherwise.Contains(entry.Selector)) continue;

            var masked = Mask(entry.Selector, "is", "where", "not");
            var dir = DirRtl.Match(masked);

            if (dir.Success is false)
            {
                offenders.Add($"{entry.Location} uses a :dir() other than rtl outside @supports selector(:dir(...)): {entry.Selector}");
                continue;
            }

            var target = Compounds(entry.Selector.Remove(dir.Index, dir.Length));
            var dirCompound = CompoundAt(Compounds(entry.Selector), dir.Index);
            var misplaced = false;
            var found = false;

            foreach (var candidate in counterparts.Where(candidate => candidate.Context == entry.Context && candidate.Declarations.SequenceEqual(entry.Declarations)))
            {
                var hook = FindHook(candidate.Selector)!;
                var compounds = Compounds(candidate.Selector.Remove(hook.Index, hook.Length));
                var offset = compounds.Count - target.Count;

                if (offset < 0 || EndsWith(compounds, target) is false) continue;

                var hookCompound = CompoundAt(Compounds(candidate.Selector), hook.Index);
                var reached = ReachedThroughChildren(compounds, hookCompound, offset + dirCompound);

                if (candidate.Guard is Guard.DirUnknown || reached)
                {
                    found = true;
                    break;
                }

                misplaced = true;
            }

            if (found) continue;

            offenders.Add(misplaced
                ? $"{entry.Location} reads the bit-rtl class through a descendant combinator outside @supports not selector(:dir(rtl)): {entry.Selector}"
                : $"{entry.Location} has no counterpart reading the bit-rtl class with the same declarations: {entry.Selector}");
        }

        return offenders;
    }

    // A :dir(rtl) in a forgiving list is dropped on its own where :dir() is unknown, which leaves the list nothing for an
    // explicit Dir unless the class is read beside it - on the element the list is on, or through child combinators.
    private static void CheckForgivingLists(Entry entry, List<string> offenders)
    {
        var masked = Mask(entry.Selector, "not");

        foreach (Match match in Forgiving.Matches(masked))
        {
            var open = match.Index + match.Length - 1;
            var close = CloseOf(masked, open);
            var alternatives = SourceFiles.SplitScssSelectorList(entry.Selector[(open + 1)..close]);

            if (alternatives.Any(alternative => DirRtl.IsMatch(Mask(alternative, "not"))) is false) continue;

            var hooked = alternatives.Where(alternative => UsesDir(alternative) is false && FindHook(alternative) is not null).ToArray();

            if (hooked.Length == 0)
            {
                offenders.Add($"{entry.Location} has an :is() reading :dir(rtl) with no .bit-rtl alternative beside it: {entry.Selector}");
            }
            else if (hooked.Any(alternative => ReachedThroughChildren(Compounds(alternative), CompoundAt(Compounds(alternative), FindHook(alternative)!.Index), int.MaxValue)) is false)
            {
                offenders.Add($"{entry.Location} reads the bit-rtl class through a descendant combinator inside an :is(): {entry.Selector}");
            }
        }
    }

    private enum Guard { None, DirKnown, DirUnknown }

    private sealed record Entry(string Selector, string[] Declarations, string Context, Guard Guard, string Location);

    private sealed record Compound(string Combinator, string Text, int Start);

    // Whether a selector leans on :dir() where an engine without it would drop it: anywhere but in a forgiving list.
    private static bool UsesDir(string selector) => Mask(selector, "is", "where").Contains(":dir(", StringComparison.Ordinal);

    // The first .bit-rtl / [dir=rtl] the selector requires: one inside a :not() rules right to left out instead.
    private static Match? FindHook(string selector)
    {
        var match = RtlHook.Match(Mask(selector, "not"));

        return match.Success ? match : null;
    }

    // The selector with the inside of every named functional pseudo-class blanked out, nested parentheses and all, so a
    // match in what is left is one outside them; the length stays, so its index is one into the selector itself.
    private static string Mask(string selector, params string[] names)
    {
        var masked = new StringBuilder(selector);

        foreach (Match match in Regex.Matches(selector, $@":(?:{string.Join('|', names)})\("))
        {
            var open = match.Index + match.Length - 1;
            var close = CloseOf(selector, open);

            for (var i = open + 1; i < close; i++) masked[i] = '\u0001';
        }

        return masked.ToString();
    }

    private static int CloseOf(string selector, int open)
    {
        var depth = 0;

        for (var i = open; i < selector.Length; i++)
        {
            if (selector[i] == '(') depth++;
            else if (selector[i] == ')' && --depth == 0) return i;
        }

        return selector.Length;
    }

    // The compound selectors of a complex one, each with the combinator before it ("" for the first, " " for a
    // descendant) and where it starts; the combinators inside parentheses and attribute selectors are part of their
    // compound. A compound the class was the whole of is kept, empty, so a selector has as many compounds with the
    // class taken out as with it.
    private static List<Compound> Compounds(string selector)
    {
        var compounds = new List<Compound>();
        var text = new StringBuilder();
        var combinator = "";
        var pending = "";
        var start = 0;
        var depth = 0;

        for (var i = 0; i < selector.Length; i++)
        {
            var c = selector[i];

            if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;

            if (depth == 0 && (char.IsWhiteSpace(c) || c is '>' or '+' or '~'))
            {
                if (char.IsWhiteSpace(c) is false || pending.Length == 0) pending = char.IsWhiteSpace(c) ? " " : c.ToString();
                continue;
            }

            if (pending.Length > 0)
            {
                compounds.Add(new(combinator, text.ToString(), start));
                text.Clear();
                combinator = pending;
                pending = "";
                start = i;
            }

            text.Append(c);
        }

        compounds.Add(new(combinator, text.ToString(), start));

        if (pending.Length > 0) compounds.Add(new(pending, "", selector.Length));

        return compounds;
    }

    // Which compound of the selector the character at the index is in.
    private static int CompoundAt(List<Compound> compounds, int index) => compounds.FindLastIndex(compound => compound.Start <= index);

    // Whether the selector ends with the target's compounds, in the same order and joined the same way.
    private static bool EndsWith(List<Compound> selector, List<Compound> target)
    {
        var offset = selector.Count - target.Count;

        return target.Select((compound, i) => compound.Text == selector[offset + i].Text && (i == 0 || compound.Combinator == selector[offset + i].Combinator)).All(match => match);
    }

    // Whether every combinator from the compound reading the class down to the one the :dir() is on (or the last) is a
    // child combinator: the class is then read on an element whose direction the other one inherits, and on no other.
    private static bool ReachedThroughChildren(List<Compound> compounds, int from, int to)
    {
        to = Math.Min(to, compounds.Count - 1);

        return from <= to && compounds.Skip(from + 1).Take(to - from).All(compound => compound.Combinator == ">");
    }

    private static int[] LineStarts(string text) => [0, .. text.Select((c, i) => (c, i)).Where(x => x.c == '\n').Select(x => x.i + 1)];

    private static int LineOf(int[] lineStarts, int index)
    {
        var line = Array.BinarySearch(lineStarts, index);

        return (line >= 0 ? line : ~line - 1) + 1;
    }

    private static string Squash(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    // Every package's stylesheets, Legacy, Assets and Icons included.
    private static IEnumerable<string> EnumerateStylesheets()
        => Directory.EnumerateDirectories(SourceFiles.Root, "Bit.BlazorUI*")
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.scss", SearchOption.AllDirectories))
            .Where(file => file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               .Any(segment => segment is "bin" or "obj" or "node_modules") is false);
}
