using System;
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
/// a <c>$xxx-private-properties</c> one an <c>@each</c> resets, so this also fails on a list nothing resets and on a
/// name in it nothing uses any more.
/// </remarks>
[TestClass]
public sealed class BitComponentPrivatePropertyResetTests
{
    // A header that publishes what its parameter asked for: a generated role class, or a size class.
    private static readonly Regex PublishingHeader = new(
        @"^\.(?<prefix>bit-[a-z]+)-(?:#\{\$role\}|sm|md|lg)$",
        RegexOptions.Compiled);

    // A list a stylesheet resets with one @each (BitIcon's $ico-private-properties).
    private static readonly Regex PropertyList = new(
        @"\$(?<list>[a-z-]+-private-properties)\s*:\s*(?<names>[^;]+);",
        RegexOptions.Compiled);

    [TestMethod]
    public void EveryPropertyAParameterClassPublishesIsResetByTheComponent()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            // Only a component whose parameters publish nothing while they are unset resets anything, and that is the
            // one a forgotten reset breaks.
            if (stylesheet.Contains("initial;", StringComparison.Ordinal) is false) continue;

            var publishing = SourceFiles.GetScssRules(stylesheet)
                .Select(rule => (Rule: rule, Match: PublishingHeader.Match(rule.Header)))
                .Where(r => r.Match.Success)
                .ToArray();

            if (publishing.Length == 0) continue;

            // What the publishing classes declare, and - with their blocks cut out - what the rest of the stylesheet
            // declares, which is where the reset lives (the root, a callout root, an item, or a mixin they include).
            var published = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var rest = stylesheet;

            foreach (var (rule, match) in publishing)
            {
                var prefix = match.Groups["prefix"].Value;
                var block = SourceFiles.GetScssBlock(stylesheet[rule.Index..], rule.Header);

                if (published.TryGetValue(prefix, out var names) is false) published[prefix] = names = new(StringComparer.Ordinal);

                foreach (Match declaration in Regex.Matches(block, $@"(--{prefix}-[a-z0-9-]+)\s*:"))
                {
                    names.Add(declaration.Groups[1].Value);
                }

                rest = rest.Replace(block, string.Empty, StringComparison.Ordinal);
            }

            foreach (var (prefix, names) in published)
            {
                var reset = Regex.Matches(rest, $@"(--{prefix}-[a-z0-9-]+)\s*:")
                                 .Select(m => m.Groups[1].Value)
                                 .Concat(PropertyList.Matches(stylesheet)
                                                     .SelectMany(m => m.Groups["names"].Value.Split(','))
                                                     .Select(name => $"--{prefix}-{name.Trim()}"))
                                 .ToHashSet(StringComparer.Ordinal);

                offenders.AddRange(names.Where(name => reset.Contains(name) is false)
                                        .Select(name => $"{Path.GetFileName(file)}: {name}"));
            }
        }

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A role or size class publishes a private property no rule of its component resets, so a nested instance inherits the outer one's choice: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void EveryPrivatePropertyListIsResetByAnEachBlock()
    {
        var offenders = new List<string>();
        var lists = 0;

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            foreach (Match list in PropertyList.Matches(stylesheet))
            {
                lists++;

                var name = list.Groups["list"].Value;
                var each = Regex.Match(stylesheet, $@"@each \$name in \${name} \{{\s*--(?<prefix>bit-[a-z]+)-#\{{\$name\}}: initial;\s*\}}");

                if (each.Success is false)
                {
                    offenders.Add($"{Path.GetFileName(file)}: ${name} resets nothing");
                    continue;
                }

                // A name nothing else mentions is one left behind when its variable was renamed or removed, which the
                // reset would then carry for nothing while the new name goes without it.
                var prefix = each.Groups["prefix"].Value;
                var source = ReadComponentSource(file);
                var rest = stylesheet.Remove(list.Index, list.Length);

                offenders.AddRange(Names(list).Select(n => $"--{prefix}-{n}")
                                              .Where(n => Regex.IsMatch(rest + source, $@"{Regex.Escape(n)}(?![a-z0-9-])") is false)
                                              .Select(n => $"{Path.GetFileName(file)}: {n} is listed but used nowhere"));
            }
        }

        Assert.IsTrue(lists > 50, $"Only {lists} private property lists were found; the pattern no longer matches them.");

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders, string.Join(", ", offenders));
    }

    [TestMethod]
    public void EveryPrivatePropertyAComponentWritesInlineIsInItsList()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            var listed = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match list in PropertyList.Matches(stylesheet))
            {
                var each = Regex.Match(stylesheet, $@"@each \$name in \${list.Groups["list"].Value} \{{\s*--(?<prefix>bit-[a-z]+)-#\{{\$name\}}");

                if (each.Success is false) continue;

                listed.UnionWith(Names(list).Select(n => $"--{each.Groups["prefix"].Value}-{n}"));
            }

            // Only a component that resets its private properties is held to it; the prefixes are the ones it resets.
            if (listed.Count == 0) continue;

            var prefixes = listed.Select(n => Regex.Match(n, "^--bit-[a-z]+-").Value).ToHashSet(StringComparer.Ordinal);

            // A rule that starts a variable out at a default of its own, rather than unset, resets it just as well: the
            // root rule of the component, or one of the rules the lists are reset in.
            foreach (var rule in SourceFiles.GetScssRules(stylesheet).Where(r => r.Ancestors.Count == 0))
            {
                var isRoot = prefixes.Any(p => rule.Header == $".{p.TrimStart('-').TrimEnd('-')}");
                var block = SourceFiles.GetScssBlock(stylesheet[rule.Index..], rule.Header);

                if (isRoot is false && block.Contains("@each $name in $", StringComparison.Ordinal) is false) continue;

                var declarations = SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header);

                listed.UnionWith(Regex.Matches(declarations, @"(--bit-[a-z]+-[a-z0-9-]+)\s*:").Select(m => m.Groups[1].Value));
            }

            // An inline declaration on the root is inherited by everything inside it, a nested instance included, so
            // it has to be started out unset there like any value a class publishes.
            var inline = InlineDeclaration.Matches(ReadComponentSource(file))
                                          .Select(m => m.Groups[1].Value)
                                          .Where(n => prefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal)))
                                          .Distinct(StringComparer.Ordinal);

            offenders.AddRange(inline.Where(n => listed.Contains(n) is false)
                                     .Select(n => $"{Path.GetFileName(file)}: {n}"));
        }

        offenders.Sort(StringComparer.Ordinal);

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A component writes a private property into its inline style that no private property list of its stylesheet resets, so a nested instance inherits it: {string.Join(", ", offenders)}");
    }

    private static IEnumerable<string> Names(Match list)
        => list.Groups["names"].Value.Split([',', ' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);

    // The C# and markup beside a stylesheet: the component it styles, and the ones in its folder that share it.
    private static string ReadComponentSource(string stylesheet)
        => string.Join("\n", Directory.EnumerateFiles(Path.GetDirectoryName(stylesheet)!, "*.*", SearchOption.AllDirectories)
                                      .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".razor", StringComparison.Ordinal))
                                      .Select(SourceFiles.ReadFullPath));

    // A private property given a value in the C# or the markup: an inline style, interpolated or concatenated.
    private static readonly Regex InlineDeclaration = new(
        @"(?<![a-zA-Z0-9-])(--bit-[a-z]+-[a-z0-9-]+)\s*:",
        RegexOptions.Compiled);

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
