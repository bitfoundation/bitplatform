using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// BitThemeFluentPalettes is a C# transcription of the packaged stylesheets, and the seed factory
/// treats it as the source of truth for what "the Fluent palette" is. That makes drift between the
/// two silent and expensive: the generated theme would quietly stop matching the CSS it is supposed
/// to reproduce. This re-parses the stylesheets and fails on any difference.
/// </summary>
[TestClass]
public sealed class BitThemeFluentPaletteContractTests
{
    private static readonly string[] RoleSuffixes =
    [
        "", "-hover", "-active", "-dark", "-dark-hover", "-dark-active",
        "-light", "-light-hover", "-light-active", "-text", "-dis", "-dis-text",
    ];

    private static readonly string[] TierSuffixes =
    [
        "", "-hover", "-active", "-dark", "-dark-hover", "-dark-active",
        "-light", "-light-hover", "-light-active", "-dis", "-dis-text",
    ];

    private static Dictionary<string, string> Tokens(string file)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", "Fluent", file);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library Styles folder is copied to output.");

        return Regex.Matches(File.ReadAllText(path), @"--bit-clr-([a-z0-9-]+):\s*([^;]+);")
                    .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.Trim().ToUpperInvariant());
    }

    private static void AssertFamily(
        IDictionary<string, string> tokens, string prefix, string[] suffixes, string[] transcribed, string name, List<string> failures)
    {
        Assert.AreEqual(suffixes.Length, transcribed.Length, $"{name} has the wrong number of slots");

        for (var i = 0; i < suffixes.Length; i++)
        {
            var token = prefix + suffixes[i];
            if (tokens.TryGetValue(token, out var expected) is false)
            {
                failures.Add($"--bit-clr-{token} is missing from the stylesheet");
                continue;
            }

            if (transcribed[i] != expected)
            {
                failures.Add($"--bit-clr-{token}: stylesheet {expected}, {name}[{i}] {transcribed[i]}");
            }
        }
    }

    [DataTestMethod]
    [DataRow(false, "colors.fluent-light.scss")]
    [DataRow(true, "colors.fluent-dark.scss")]
    public void TranscribedPaletteMatchesTheStylesheet(bool isDark, string paletteFile)
    {
        var tokens = Tokens(paletteFile);
        var failures = new List<string>();

        var roles = new (string Prefix, string[] Values)[]
        {
            ("pri", isDark ? BitThemeFluentPalettes.DarkPri : BitThemeFluentPalettes.LightPri),
            ("sec", isDark ? BitThemeFluentPalettes.DarkSec : BitThemeFluentPalettes.LightSec),
            ("ter", isDark ? BitThemeFluentPalettes.DarkTer : BitThemeFluentPalettes.LightTer),
            ("inf", isDark ? BitThemeFluentPalettes.DarkInf : BitThemeFluentPalettes.LightInf),
            ("suc", isDark ? BitThemeFluentPalettes.DarkSuc : BitThemeFluentPalettes.LightSuc),
            ("wrn", isDark ? BitThemeFluentPalettes.DarkWrn : BitThemeFluentPalettes.LightWrn),
            ("swr", isDark ? BitThemeFluentPalettes.DarkSwr : BitThemeFluentPalettes.LightSwr),
            ("err", isDark ? BitThemeFluentPalettes.DarkErr : BitThemeFluentPalettes.LightErr),
        };

        foreach (var (prefix, values) in roles)
        {
            AssertFamily(tokens, prefix, RoleSuffixes, values, prefix, failures);
        }

        var tiers = new (string Prefix, string[] Values)[]
        {
            ("fg-pri", isDark ? BitThemeFluentPalettes.DarkFgPri : BitThemeFluentPalettes.LightFgPri),
            ("fg-sec", isDark ? BitThemeFluentPalettes.DarkFgSec : BitThemeFluentPalettes.LightFgSec),
            ("fg-ter", isDark ? BitThemeFluentPalettes.DarkFgTer : BitThemeFluentPalettes.LightFgTer),
            ("bg-pri", isDark ? BitThemeFluentPalettes.DarkBgPri : BitThemeFluentPalettes.LightBgPri),
            ("bg-sec", isDark ? BitThemeFluentPalettes.DarkBgSec : BitThemeFluentPalettes.LightBgSec),
            ("bg-ter", isDark ? BitThemeFluentPalettes.DarkBgTer : BitThemeFluentPalettes.LightBgTer),
            ("brd-pri", isDark ? BitThemeFluentPalettes.DarkBrdPri : BitThemeFluentPalettes.LightBrdPri),
            ("brd-sec", isDark ? BitThemeFluentPalettes.DarkBrdSec : BitThemeFluentPalettes.LightBrdSec),
            ("brd-ter", isDark ? BitThemeFluentPalettes.DarkBrdTer : BitThemeFluentPalettes.LightBrdTer),
        };

        foreach (var (prefix, values) in tiers)
        {
            AssertFamily(tokens, prefix, TierSuffixes, values, prefix, failures);
        }

        foreach (var (token, transcribed) in new[]
        {
            ("fg-dis", isDark ? BitThemeFluentPalettes.DarkFgDisabled : BitThemeFluentPalettes.LightFgDisabled),
            ("bg-dis", isDark ? BitThemeFluentPalettes.DarkBgDisabled : BitThemeFluentPalettes.LightBgDisabled),
            ("brd-dis", isDark ? BitThemeFluentPalettes.DarkBrdDisabled : BitThemeFluentPalettes.LightBrdDisabled),
            ("req", isDark ? BitThemeFluentPalettes.DarkRequired : BitThemeFluentPalettes.LightRequired),
        })
        {
            if (tokens[token] != transcribed) failures.Add($"--bit-clr-{token}: stylesheet {tokens[token]}, transcribed {transcribed}");
        }

        var overlay = isDark ? BitThemeFluentPalettes.DarkOverlay : BitThemeFluentPalettes.LightOverlay;
        if (tokens["bg-overlay"] != overlay.ToUpperInvariant())
        {
            failures.Add($"--bit-clr-bg-overlay: stylesheet {tokens["bg-overlay"]}, transcribed {overlay}");
        }

        Assert.AreEqual(0, failures.Count, $"{paletteFile} has drifted from the transcription: {string.Join("; ", failures.Take(12))}");
    }

    [TestMethod]
    public void TranscribedNeutralRampMatchesTheStylesheet()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", "Fluent", "neutrals.fluent.scss");
        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        var tokens = Regex.Matches(File.ReadAllText(path), @"--bit-clr-ntr-(\w+):\s*(#[0-9A-Fa-f]{6})")
                          .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value.ToUpperInvariant());

        string[] names = ["white", "black", .. Enumerable.Range(1, 22).Select(i => $"gray{i * 10}")];

        Assert.AreEqual(names.Length, BitThemeFluentPalettes.Neutrals.Length, "ramp length");

        var failures = new List<string>();
        for (var i = 0; i < names.Length; i++)
        {
            if (tokens.TryGetValue(names[i], out var expected) is false)
            {
                failures.Add($"--bit-clr-ntr-{names[i]} is missing from the stylesheet");
                continue;
            }

            if (BitThemeFluentPalettes.Neutrals[i] != expected)
            {
                failures.Add($"--bit-clr-ntr-{names[i]}: stylesheet {expected}, transcribed {BitThemeFluentPalettes.Neutrals[i]}");
            }
        }

        Assert.AreEqual(0, failures.Count, $"the neutral ramp has drifted: {string.Join("; ", failures.Take(12))}");
    }
}
