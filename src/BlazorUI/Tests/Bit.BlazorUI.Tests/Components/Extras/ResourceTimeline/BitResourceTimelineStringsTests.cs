using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline.BitResourceTimelineTestInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Drift guards between the strings of the BitResourceTimeline and the scripts. The component hands the engine every
/// string of BitResourceTimelineStrings by its camelCase name, less the ones only its markup uses (MARKUP_STRINGS of
/// BitResourceTimeline.ts), and the engine warns about a string it does not know (DEFAULT_STRINGS of
/// BitResourceTimelineEngine.ts) and falls back to its own default for one it is not given - so a string renamed or
/// reworded on one side only is a warning in the console, or an engine and a component that say different things.
/// </summary>
[TestClass]
public partial class BitResourceTimelineStringsTests
{
    [TestMethod]
    public void BitResourceTimelineStringsShouldBeTheStringsOfTheEngineAndOfTheMarkup()
    {
        var engine = EngineDefaults();
        var markup = MarkupStrings();

        Assert.IsGreaterThanOrEqualTo(20, engine.Count, "DEFAULT_STRINGS was not read off the engine.");
        Assert.IsGreaterThanOrEqualTo(5, markup.Length, "MARKUP_STRINGS was not read off the facade.");

        var overlap = engine.Keys.Intersect(markup).ToList();
        Assert.IsEmpty(overlap, "A string is both one of the engine and one of the markup: " + string.Join(", ", overlap));

        CollectionAssert.AreEquivalent(engine.Keys.Concat(markup).Order(StringComparer.Ordinal).ToArray(),
                                       StringNames().Order(StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void BitResourceTimelineStringsShouldDefaultToWhatTheEngineDefaultsTo()
    {
        var strings = new BitResourceTimelineStrings();
        var engine = EngineDefaults();

        foreach (var property in typeof(BitResourceTimelineStrings).GetProperties())
        {
            if (engine.TryGetValue(Camel(property.Name), out var expected) is false) continue;

            Assert.AreEqual(expected, (string)property.GetValue(strings)!, $"BitResourceTimelineStrings.{property.Name} is not the default of the engine.");
        }
    }

    [TestMethod]
    public void BitResourceTimelineMarkupStringsShouldBeTheOnesItsMarkupReads()
    {
        var folder = new[] { "Bit.BlazorUI.Extras", "Components", "ResourceTimeline" };
        var razor = SourceFiles.Read([.. folder, "BitResourceTimeline.razor"]) + SourceFiles.Read([.. folder, "BitResourceTimeline.razor.cs"]);

        var read = Regex.Matches(razor, @"ActiveStrings\.(\w+)").Select(m => Camel(m.Groups[1].Value)).Distinct().Order(StringComparer.Ordinal).ToArray();

        // A string the markup reads but the facade hands the engine is a warning in the console; one the facade keeps from
        // the engine but nothing reads is a string no app can change anything with.
        CollectionAssert.AreEqual(MarkupStrings().Order(StringComparer.Ordinal).ToArray(), read);
    }

    [TestMethod]
    public void BitResourceTimelineStringsShouldCrossTheInteropUnderTheNamesTheScriptsUse()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new BitResourceTimelineStrings(), Web));

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        CollectionAssert.AreEquivalent(StringNames(), names);
        Assert.IsTrue(document.RootElement.EnumerateObject().All(p => p.Value.ValueKind == JsonValueKind.String));
    }

    [TestMethod]
    public void BitResourceTimelineStringsShouldBeFormatsExactlyWhenTheirNamesSaySo()
    {
        var strings = new BitResourceTimelineStrings();

        foreach (var property in typeof(BitResourceTimelineStrings).GetProperties())
        {
            var value = (string)property.GetValue(strings)!;

            Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{property.Name} has no default.");

            if (property.Name.EndsWith("Format", StringComparison.Ordinal))
            {
                StringAssert.Contains(value, "{0}", $"{property.Name} is a format without a placeholder.");

                // The markup formats the group button labels with string.Format and the name of the group alone, so a
                // placeholder past {0} there throws; the engine formats the rest with the same syntax and up to four.
                object[] arguments = property.Name is nameof(BitResourceTimelineStrings.ExpandGroupFormat)
                                                    or nameof(BitResourceTimelineStrings.CollapseGroupFormat)
                                     ? ["a"]
                                     : ["a", "b", "c", "d"];

                _ = string.Format(CultureInfo.InvariantCulture, value, arguments);
            }
            else
            {
                Assert.IsFalse(value.Contains('{') || value.Contains('}'), $"{property.Name} is not a format, but its default reads like one: {value}");
            }
        }
    }

    // The camelCase names of the strings, as the interop sends them.
    private static string[] StringNames() => typeof(BitResourceTimelineStrings).GetProperties().Select(p => Camel(p.Name)).ToArray();

    private static string Camel(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static Dictionary<string, string> EngineDefaults()
    {
        var engine = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimelineEngine.ts");
        var block = Regex.Match(engine, @"const DEFAULT_STRINGS[^=]*= \{(.*?)\n    \};", RegexOptions.Singleline);

        Assert.IsTrue(block.Success, "DEFAULT_STRINGS was not found in the engine.");

        return EngineString().Matches(block.Groups[1].Value).ToDictionary(m => m.Groups[1].Value, m => Unescape(m.Groups[2].Value));
    }

    private static string[] MarkupStrings()
    {
        var facade = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimeline.ts");
        var list = Regex.Match(facade, @"const MARKUP_STRINGS = \[([^\]]*)\]");

        Assert.IsTrue(list.Success, "MARKUP_STRINGS was not found in the facade.");

        return Regex.Matches(list.Groups[1].Value, @"'(\w+)'").Select(m => m.Groups[1].Value).ToArray();
    }

    // The escapes a single-quoted string of TypeScript may hold.
    private static string Unescape(string literal) => Regex.Replace(literal, @"\\(u\{[0-9a-fA-F]+\}|u[0-9a-fA-F]{4}|x[0-9a-fA-F]{2}|.)", m =>
    {
        var escape = m.Groups[1].Value;

        return escape[0] switch
        {
            'u' when escape[1] == '{' => char.ConvertFromUtf32(int.Parse(escape[2..^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
            'u' => ((char)int.Parse(escape[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString(),
            'x' => ((char)int.Parse(escape[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString(),
            'n' => "\n",
            't' => "\t",
            _ => escape,
        };
    });

    [GeneratedRegex(@"^\s*(\w+):\s*'((?:[^'\\]|\\.)*)'\s*,?\s*$", RegexOptions.Multiline)]
    private static partial Regex EngineString();
}
