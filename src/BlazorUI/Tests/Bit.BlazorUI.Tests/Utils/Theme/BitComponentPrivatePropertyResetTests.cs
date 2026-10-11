using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the reset that lets an explicit Color or Size win over a component's public variables without leaking into an
/// instance nested in it.
/// </summary>
/// <remarks>
/// The role classes (<c>.bit-xxx-#{$role}</c>) and the size classes (<c>.bit-xxx-sm/md/lg</c>) publish private
/// properties only while their parameter is set, and every read ranks those before the public variable. A custom
/// property inherits, though, so each one they publish has to be declared again on the component's own rules - unset
/// (<c>initial</c>), or at the default its unset parameter stands for - or an instance nested in another one's content
/// would read the outer one's choice as its own. That reset is a second list beside the classes, and the one that is
/// easy to forget when a slot is added to <c>$bit-color-roles</c> or a size class gains a line; this is what fails
/// on it, for every stylesheet at once. The same holds for what a component writes into an inline style. The list is
/// a <c>$xxx-private-properties</c> one an <c>@each</c> resets, read by <see cref="SourceFiles.GetPrivatePropertyLists"/>,
/// so this also fails on a list nothing resets and on a name in it nothing uses any more.
/// </remarks>
[TestClass]
public sealed class BitComponentPrivatePropertyResetTests
{
    // A header that publishes what its parameter asked for: a generated role class, or a size class.
    private static readonly Regex PublishingHeader = new(
        @"^\.(?<prefix>bit-[a-z]+)-(?:#\{\$role\}|sm|md|lg)$",
        RegexOptions.Compiled);

    // A header naming one element of a component and nothing else: its root (.bit-xxx) or a part of it (.bit-xxx-yyy).
    private static readonly Regex ElementHeader = new(
        @"^\.(?<prefix>bit-[a-z]+)(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    // A private property, wherever it is written: declared, read, or named in a string. The head of a name put
    // together in Sass (--bit-xxx-yyy-#{$name}) is no name of its own.
    private static readonly Regex PrivateProperty = new(
        @"(?<![a-zA-Z0-9-])--bit-[a-z]+-[a-z0-9-]*[a-z0-9](?![a-z0-9-])",
        RegexOptions.Compiled);

    // A private property given a value in the C# or the markup: an inline style, interpolated or concatenated.
    private static readonly Regex InlineDeclaration = new(
        @"(?<![a-zA-Z0-9-])(--bit-[a-z]+-[a-z0-9-]+)\s*:",
        RegexOptions.Compiled);

    // A private property a string starts with, or a declaration after a semicolon starts with - a name handed to the
    // code that writes it ("--bit-xxx-yyy"), rather than one read inside a var().
    private static readonly Regex QuotedName = new(
        @"(?<=[""';]\s*)(--bit-[a-z]+-[a-z0-9-]+)(?![a-z0-9{-])",
        RegexOptions.Compiled);

    // A private property whose name is put together in the C# ($"--bit-xxx-yyy{suffix}"), which can only stand for the
    // names its stylesheet uses.
    private static readonly Regex InterpolatedName = new(
        @"(?<![a-zA-Z0-9-])(--bit-[a-z]+-[a-z0-9-]*(?:\{[^{}""]+\}[a-z0-9-]*)+)",
        RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, string> ComponentSourceCache = new(StringComparer.Ordinal);

    [TestMethod]
    public void EveryPropertyAParameterClassPublishesIsResetByTheComponent()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = ReadCode(file);

            // Only a component whose parameters publish nothing while they are unset resets anything, and that is the
            // one a forgotten reset breaks.
            if (stylesheet.Contains("initial", StringComparison.Ordinal) is false) continue;

            var rules = SourceFiles.GetScssRules(stylesheet);

            var publishing = rules.Select(rule => (Rule: rule, Match: PublishingHeader.Match(rule.Header)))
                                  .Where(r => r.Match.Success)
                                  .ToArray();

            if (publishing.Length == 0) continue;

            // The role classes and the size classes, each with what it publishes.
            var published = new Dictionary<(string Prefix, bool Role), HashSet<string>>();

            foreach (var (rule, match) in publishing)
            {
                var prefix = match.Groups["prefix"].Value;
                var block = SourceFiles.GetScssBlock(stylesheet[rule.Index..], rule.Header);
                var key = (prefix, rule.Header.EndsWith("#{$role}", StringComparison.Ordinal));

                if (published.TryGetValue(key, out var names) is false) published[key] = names = new(StringComparer.Ordinal);

                names.UnionWith(Regex.Matches(block, $@"(--{prefix}-[a-z0-9-]+)\s*:").Select(m => m.Groups[1].Value));
            }

            // The classes are given to one element - the root, as a rule, or each item of a component whose items
            // carry them - and that element is where what they publish has to be reset: by its own rules, the mixins
            // they include, and the lists reset in either. So one element has to reset all of it; a list reset only on
            // another element (a callout beside the root) resets nothing on the root, however many of the same names
            // it holds.
            foreach (var ((prefix, _), names) in published)
            {
                var elements = rules.Where(r => r.Ancestors.Count == 0)
                                    .SelectMany(r => SplitSelectors(r.Header))
                                    .Where(part => ElementHeader.Match(part) is { Success: true } m && m.Groups["prefix"].Value == prefix && PublishingHeader.IsMatch(part) is false)
                                    .Distinct(StringComparer.Ordinal)
                                    .ToArray();

                var resets = elements.Select(element => (Element: element, Reset: GetResets(stylesheet, rules, GetElementScope(stylesheet, rules, element), prefix)))
                                     .ToArray();

                if (resets.Any(r => names.All(r.Reset.Contains))) continue;

                // None does, so the names the root misses are what is reported.
                var root = resets.FirstOrDefault(r => r.Element == $".{prefix}").Reset ?? [];

                offenders.AddRange(names.Where(name => root.Contains(name) is false)
                                        .Select(name => $"{Path.GetFileName(file)}: {name}"));
            }
        }

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A role or size class publishes a private property the root of its component does not reset, so a nested instance inherits the outer one's choice: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void EveryPrivatePropertyListIsResetByAnEachBlock()
    {
        var offenders = new List<string>();
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = ReadCode(file);
            var lists = SourceFiles.GetPrivatePropertyLists(stylesheet);

            if (lists.Count == 0) continue;

            // A name nothing else mentions is one left behind when its variable was renamed or removed, which the
            // reset would then carry for nothing while the new name goes without it. The list names no property, and
            // its reset only one put together in Sass, so neither counts as a use.
            var used = PrivateProperty.Matches($"{stylesheet}\n{ReadComponentSource(file)}")
                                      .Select(m => m.Value)
                                      .ToHashSet(StringComparer.Ordinal);

            foreach (var list in lists)
            {
                found.Add(list.Name);

                if (list.Resets.Count == 0)
                {
                    offenders.Add($"{Path.GetFileName(file)}: ${list.Name} resets nothing");
                    continue;
                }

                offenders.AddRange(list.Resets.Select(r => r.Prefix).Distinct(StringComparer.Ordinal)
                                       .SelectMany(prefix => list.Names.Select(n => $"--{prefix}-{n}"))
                                       .Where(n => used.Contains(n) is false)
                                       .Select(n => $"{Path.GetFileName(file)}: {n} is listed but used nowhere"));
            }
        }

        // Two lists known to be there, so a reading that no longer finds them fails here rather than passing on none.
        foreach (var anchor in new[] { "bdg-private-properties", "ico-private-properties" })
        {
            Assert.IsTrue(found.Contains(anchor), $"${anchor} was not found; the lists are no longer read.");
        }

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders, string.Join(", ", offenders));
    }

    [TestMethod]
    public void EveryPrivatePropertyAComponentWritesInlineIsReset()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = ReadCode(file);
            var rules = SourceFiles.GetScssRules(stylesheet);
            var lists = SourceFiles.GetPrivatePropertyLists(stylesheet);

            // The elements the stylesheet styles, whether or not it has a list: a component that never had one is held
            // to the same reset as one that does.
            var elements = rules.Where(r => r.Ancestors.Count == 0)
                                .SelectMany(r => SplitSelectors(r.Header))
                                .Where(part => ElementHeader.IsMatch(part) && PublishingHeader.IsMatch(part) is false)
                                .ToHashSet(StringComparer.Ordinal);

            // The components whose root the stylesheet styles, or whose lists it resets; a stylesheet styling only the
            // parts of another one's root (a loader of the BitLoading family) leaves that one to its own.
            var prefixes = elements.Select(part => ElementHeader.Match(part).Groups["prefix"].Value)
                                   .Where(prefix => elements.Contains($".{prefix}"))
                                   .Concat(lists.SelectMany(l => l.Resets).Select(r => r.Prefix))
                                   .ToHashSet(StringComparer.Ordinal);

            if (prefixes.Count == 0) continue;

            // Which element an inline style is written on is the C#'s business, so a name counts as reset by a list
            // reset anywhere, or by a default the base rule of any element of the component declares for it.
            var reset = lists.SelectMany(l => l.Resets.Select(r => r.Prefix)
                                                      .Distinct(StringComparer.Ordinal)
                                                      .SelectMany(prefix => l.Names.Select(n => $"--{prefix}-{n}")))
                             .ToHashSet(StringComparer.Ordinal);

            foreach (var rule in rules.Where(r => r.Ancestors.Count == 0 && SplitSelectors(r.Header).Any(elements.Contains)))
            {
                reset.UnionWith(InlineDeclaration.Matches(SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header))
                                                 .Select(m => m.Groups[1].Value));
            }

            var source = ReadComponentSource(file);

            bool IsOwn(string name) => prefixes.Any(p => name.StartsWith($"--{p}-", StringComparison.Ordinal));

            // An inline declaration on an element is inherited by everything inside it, a nested instance included,
            // so it has to be started out unset there like any value a class publishes.
            var written = InlineDeclaration.Matches(source).Select(m => m.Groups[1].Value)
                                           .Concat(QuotedName.Matches(source).Select(m => m.Groups[1].Value))
                                           .Where(n => IsOwn(n))
                                           .ToHashSet(StringComparer.Ordinal);

            // A name put together in the C# stands for every name of the stylesheet it can spell.
            var used = PrivateProperty.Matches(stylesheet).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

            foreach (var interpolated in InterpolatedName.Matches(source).Select(m => m.Groups[1].Value).Where(n => IsOwn(n)).Distinct(StringComparer.Ordinal))
            {
                var pattern = new Regex($"^{Regex.Replace(interpolated, @"\{[^{}]+\}|[^{}]+", m => m.Value[0] == '{' ? "[a-z0-9-]*" : Regex.Escape(m.Value))}$");
                var spelled = used.Where(n => pattern.IsMatch(n)).ToArray();

                if (spelled.Length == 0)
                {
                    offenders.Add($"{Path.GetFileName(file)}: {interpolated} spells no property of the stylesheet");
                }

                written.UnionWith(spelled);
            }

            offenders.AddRange(written.Where(n => reset.Contains(n) is false)
                                      .Select(n => $"{Path.GetFileName(file)}: {n}"));
        }

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A component writes a private property into its inline style that its stylesheet never resets, so a nested instance inherits it: {string.Join(", ", offenders)}");
    }

    // The headers of the rules that style one element of a component: every top-level rule naming its class among its
    // selectors, and the mixins those include, through any number of includes.
    private static HashSet<string> GetElementScope(string stylesheet, IReadOnlyList<SourceFiles.ScssRule> rules, string element)
    {
        var topLevel = rules.Where(r => r.Ancestors.Count == 0).ToArray();
        var scope = topLevel.Where(r => SplitSelectors(r.Header).Contains(element))
                            .Select(r => r.Header)
                            .ToHashSet(StringComparer.Ordinal);

        var pending = new Queue<string>(scope);
        while (pending.TryDequeue(out var header))
        {
            foreach (var rule in topLevel.Where(r => r.Header == header))
            {
                foreach (Match include in Regex.Matches(SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header), @"@include\s+([a-z0-9-]+)"))
                {
                    var mixin = topLevel.FirstOrDefault(r => Regex.IsMatch(r.Header, $@"^@mixin\s+{Regex.Escape(include.Groups[1].Value)}(?![a-z0-9-])"));

                    if (mixin is not null && scope.Add(mixin.Header)) pending.Enqueue(mixin.Header);
                }
            }
        }

        return scope;
    }

    // What the rules of a scope reset: what they declare themselves, and the lists reset in them.
    private static HashSet<string> GetResets(string stylesheet, IReadOnlyList<SourceFiles.ScssRule> rules, HashSet<string> scope, string prefix)
    {
        var reset = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in rules.Where(r => r.Ancestors.Count == 0 && scope.Contains(r.Header)))
        {
            reset.UnionWith(Regex.Matches(SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header), $@"(--{prefix}-[a-z0-9-]+)\s*:")
                                 .Select(m => m.Groups[1].Value));
        }

        foreach (var list in SourceFiles.GetPrivatePropertyLists(stylesheet))
        {
            if (list.Resets.Any(r => r.Prefix == prefix && scope.Contains(r.Rule)))
            {
                reset.UnionWith(list.Names.Select(n => $"--{prefix}-{n}"));
            }
        }

        return reset;
    }

    private static IEnumerable<string> SplitSelectors(string header) => header.Split(',').Select(part => part.Trim());

    private static string ReadCode(string file) => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file));

    // The C# and markup beside a stylesheet: the component it styles, and the ones in its folder that share it. Read
    // once per folder, however many of its stylesheets ask.
    private static string ReadComponentSource(string stylesheet)
        => ComponentSourceCache.GetOrAdd(Path.GetDirectoryName(stylesheet)!, folder =>
            string.Join("\n", Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                                       .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
                                       .Select(SourceFiles.ReadFullPath)));

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
