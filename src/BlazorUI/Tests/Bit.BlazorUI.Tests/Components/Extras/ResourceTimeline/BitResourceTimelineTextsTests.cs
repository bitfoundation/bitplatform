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
/// Drift guards between the texts of the BitResourceTimeline and the scripts. The component hands the engine every
/// text of BitResourceTimelineTexts by its camelCase name, less the ones only its markup uses (MARKUP_TEXTS of
/// BitResourceTimeline.ts), and the engine warns about a text it does not know (DEFAULT_TEXTS of
/// BitResourceTimelineEngine.ts) and falls back to its own default for one it is not given - so a text renamed or
/// reworded on one side only is a warning in the console, or an engine and a component that say different things.
/// </summary>
[TestClass]
public partial class BitResourceTimelineTextsTests
{
    [TestMethod]
    public void BitResourceTimelineTextsShouldBeTheTextsOfTheEngineAndOfTheMarkup()
    {
        var engine = EngineDefaults();
        var markup = MarkupTexts();

        Assert.IsGreaterThanOrEqualTo(20, engine.Count, "DEFAULT_TEXTS was not read off the engine.");
        Assert.IsGreaterThanOrEqualTo(5, markup.Length, "MARKUP_TEXTS was not read off the facade.");

        var overlap = engine.Keys.Intersect(markup).ToList();
        Assert.IsEmpty(overlap, "A text is both one of the engine and one of the markup: " + string.Join(", ", overlap));

        CollectionAssert.AreEquivalent(engine.Keys.Concat(markup).Order(StringComparer.Ordinal).ToArray(),
                                       TextNames().Order(StringComparer.Ordinal).ToArray());
    }

    [TestMethod]
    public void BitResourceTimelineTextsShouldDefaultToWhatTheEngineDefaultsTo()
    {
        var texts = new BitResourceTimelineTexts();
        var engine = EngineDefaults();

        foreach (var property in typeof(BitResourceTimelineTexts).GetProperties())
        {
            if (engine.TryGetValue(Camel(property.Name), out var expected) is false) continue;

            Assert.AreEqual(expected, (string)property.GetValue(texts)!, $"BitResourceTimelineTexts.{property.Name} is not the default of the engine.");
        }
    }

    [TestMethod]
    public void BitResourceTimelineMarkupTextsShouldBeTheOnesItsMarkupReads()
    {
        var folder = new[] { "Bit.BlazorUI.Extras", "Components", "ResourceTimeline" };
        var razor = SourceFiles.Read([.. folder, "BitResourceTimeline.razor"]) + SourceFiles.Read([.. folder, "BitResourceTimeline.razor.cs"]);

        var read = Regex.Matches(razor, @"ActiveTexts\.(\w+)").Select(m => Camel(m.Groups[1].Value)).Distinct().Order(StringComparer.Ordinal).ToArray();

        // A text the markup reads but the facade hands the engine is a warning in the console; one the facade keeps from
        // the engine but nothing reads is a text no app can change anything with.
        CollectionAssert.AreEqual(MarkupTexts().Order(StringComparer.Ordinal).ToArray(), read);
    }

    [TestMethod]
    public void BitResourceTimelineTextsShouldCrossTheInteropUnderTheNamesTheScriptsUse()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new BitResourceTimelineTexts(), Web));

        var names = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        CollectionAssert.AreEquivalent(TextNames(), names);
        Assert.IsTrue(document.RootElement.EnumerateObject().All(p => p.Value.ValueKind == JsonValueKind.String));
    }

    [TestMethod]
    public void BitResourceTimelineTextsShouldBeFormatsExactlyWhenTheirNamesSaySo()
    {
        var texts = new BitResourceTimelineTexts();

        foreach (var property in typeof(BitResourceTimelineTexts).GetProperties())
        {
            var value = (string)property.GetValue(texts)!;

            Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"{property.Name} has no default.");

            if (property.Name.EndsWith("Format", StringComparison.Ordinal))
            {
                StringAssert.Contains(value, "{0}", $"{property.Name} is a format without a placeholder.");

                // The markup formats the group button labels with string.Format and the name of the group alone, so a
                // placeholder past {0} there throws; the engine formats the rest with the same syntax and up to four.
                object[] arguments = property.Name is nameof(BitResourceTimelineTexts.ExpandGroupFormat)
                                                    or nameof(BitResourceTimelineTexts.CollapseGroupFormat)
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

    // The camelCase names of the texts, as the interop sends them.
    private static string[] TextNames() => typeof(BitResourceTimelineTexts).GetProperties().Select(p => Camel(p.Name)).ToArray();

    private static string Camel(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static Dictionary<string, string> EngineDefaults()
    {
        var engine = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimelineEngine.ts");
        var block = Regex.Match(engine, @"const DEFAULT_TEXTS[^=]*= \{(.*?)\n    \};", RegexOptions.Singleline);

        Assert.IsTrue(block.Success, "DEFAULT_TEXTS was not found in the engine.");

        return EngineText().Matches(block.Groups[1].Value).ToDictionary(m => m.Groups[1].Value, m => Unescape(m.Groups[2].Value));
    }

    private static string[] MarkupTexts()
    {
        var facade = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimeline.ts");
        var list = Regex.Match(facade, @"const MARKUP_TEXTS = \[([^\]]*)\]");

        Assert.IsTrue(list.Success, "MARKUP_TEXTS was not found in the facade.");

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
    private static partial Regex EngineText();
}
