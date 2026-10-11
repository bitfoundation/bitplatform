using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// So each <c>:dir(rtl)</c> rule stands in a list of its own and has a counterpart reading the <c>bit-rtl</c> class (or
/// the <c>dir="rtl"</c> attribute) an explicit <c>Dir</c> renders, with the same declarations, in the same at-rules:
/// written beside it when the class is on the element the rule styles or on its parent, and inside
/// <c>@supports not selector(:dir(rtl))</c> when it is further up, since a descendant selector would also reach into a
/// nested instance reading the other way. Left out: a <c>:dir()</c> inside <c>:is()</c> or <c>:where()</c>, whose lists
/// are forgiving, and one inside <c>@supports selector(:dir(...))</c>, which only an engine knowing it reads.
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

    private static readonly Regex RtlHook = new(@"\.bit-rtl(?![a-zA-Z0-9_-])|\[dir=[""']?rtl[""']?\]", RegexOptions.Compiled);

    private static readonly Regex DirRtl = new(@":dir\(\s*rtl\s*\)", RegexOptions.Compiled);

    private static readonly Regex Forgiving = new(@":(?:is|where)\(", RegexOptions.Compiled);

    [TestMethod]
    public void EveryDirRuleHasAFallbackForAnExplicitDir()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");
            var entries = new List<Entry>();

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                var headers = rule.Ancestors.Append(rule.Header).ToArray();

                // A mixin's selectors are resolved where it is included, not where it is written.
                if (headers.Any(header => header.StartsWith("@mixin", StringComparison.Ordinal) || header.StartsWith("@function", StringComparison.Ordinal))) continue;

                var guarded = headers.Any(IsDirGuard);
                var line = stylesheet.AsSpan(0, rule.Index).Count('\n') + 1;
                var location = $"{Path.GetFileName(file)}:{line}";

                // A list holding a :dir() is dropped whole where :dir() is unknown, taking its other selectors with it.
                if (guarded is false && rule.Header.StartsWith('@') is false)
                {
                    var alternatives = SourceFiles.SplitScssSelectorList(rule.Header);

                    if (alternatives.Any(UsesDir) && alternatives.Any(alternative => UsesDir(alternative) is false))
                    {
                        offenders.Add($"{location} shares a selector list with a :dir() selector: {Squash(rule.Header)}");
                    }
                }

                var declarations = OwnDeclarations(SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header));

                if (declarations.Length == 0) continue;

                var context = string.Join(" | ", headers.Where(header => header.StartsWith('@') && IsDirGuard(header) is false).Select(Squash));

                foreach (var selector in SourceFiles.ResolveScssSelectors(headers.Where(header => header.StartsWith('@') is false)))
                {
                    entries.Add(new(Squash(selector), declarations, context, guarded, location));
                }
            }

            foreach (var entry in entries.Where(entry => entry.Guarded is false && UsesDir(entry.Selector)))
            {
                if (CoveredOtherwise.Contains(entry.Selector)) continue;

                if (DirRtl.IsMatch(WithoutForgiving(entry.Selector)) is false)
                {
                    offenders.Add($"{entry.Location} uses a :dir() other than rtl outside @supports selector(:dir(...)): {entry.Selector}");
                    continue;
                }

                var target = Normalize(entry.Selector);

                var hasFallback = entries.Any(candidate =>
                    UsesDir(candidate.Selector) is false &&
                    RtlHook.IsMatch(candidate.Selector) &&
                    candidate.Context == entry.Context &&
                    EndsWithSelector(Normalize(candidate.Selector), target) &&
                    candidate.Declarations.SequenceEqual(entry.Declarations));

                if (hasFallback is false)
                {
                    offenders.Add($"{entry.Location} has no .bit-rtl counterpart with the same declarations: {entry.Selector}");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A :dir(rtl) rule is lost for an explicit Dir on an engine without :dir(); keep it in a list of its own and add a rule reading the bit-rtl class beside it:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    private sealed record Entry(string Selector, string[] Declarations, string Context, bool Guarded, string Location);

    // An @supports that tests :dir() itself, either way round: inside it, whether :dir() is known is already settled.
    private static bool IsDirGuard(string header) => header.StartsWith("@supports", StringComparison.Ordinal) && header.Contains(":dir(", StringComparison.Ordinal);

    // Whether a selector leans on :dir() where an engine without it would drop it: anywhere but in a forgiving list.
    private static bool UsesDir(string selector) => WithoutForgiving(selector).Contains(":dir(", StringComparison.Ordinal);

    // The selector less every :is(...) and :where(...) in it, nested parentheses and all.
    private static string WithoutForgiving(string selector)
    {
        var match = Forgiving.Match(selector);

        while (match.Success)
        {
            var depth = 0;
            var end = match.Index + match.Length - 1;

            for (; end < selector.Length; end++)
            {
                if (selector[end] == '(') depth++;
                else if (selector[end] == ')' && --depth == 0) break;
            }

            selector = selector.Remove(match.Index, Math.Min(end + 1, selector.Length) - match.Index);
            match = Forgiving.Match(selector);
        }

        return selector;
    }

    // The selector with both ways of saying right to left taken out, so a :dir(rtl) rule and its counterpart read alike.
    private static string Normalize(string selector)
    {
        var plain = RtlHook.Replace(DirRtl.Replace(selector, ""), "");
        plain = Regex.Replace(plain, @"\s*([>+~])\s*", " $1 ");

        return Regex.Replace(Squash(plain), @"^[>+~]\s*", "");
    }

    // The counterpart may hang the class on an ancestor the :dir() rule does not name.
    private static bool EndsWithSelector(string candidate, string target) => candidate == target || candidate.EndsWith(" " + target, StringComparison.Ordinal);

    // The declarations a rule makes itself, in a stable order.
    private static string[] OwnDeclarations(string rule)
    {
        var open = 0;
        var interpolation = 0;

        for (; open < rule.Length; open++)
        {
            if (rule[open] == '{' && open > 0 && rule[open - 1] == '#') interpolation++;
            else if (rule[open] == '}' && interpolation > 0) interpolation--;
            else if (rule[open] == '{' && interpolation == 0) break;
        }

        return rule[(open + 1)..^1].Split(';')
                                   .Select(Squash)
                                   .Where(declaration => declaration.Length > 0)
                                   .Order(StringComparer.Ordinal)
                                   .ToArray();
    }

    private static string Squash(string text) => Regex.Replace(text, @"\s+", " ").Trim();

    private static IEnumerable<string> EnumerateStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => new[] { "Components", "Styles" }.Select(folder => SourceFiles.GetDirectory(project, folder)))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.scss", SearchOption.AllDirectories));
}
