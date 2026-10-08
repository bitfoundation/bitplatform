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
/// on it, for every stylesheet at once.
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
        @"\$[a-z-]+-private-properties\s*:\s*(?<names>[^;]+);",
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

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
