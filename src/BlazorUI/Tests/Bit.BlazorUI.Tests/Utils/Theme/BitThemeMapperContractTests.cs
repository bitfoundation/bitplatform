using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

[TestClass]
public sealed class BitThemeMapperContractTests
{
    private static readonly Regex CssVarRef = new(@"var\((--bit-[a-z0-9-]+)\)", RegexOptions.Compiled);

    [TestMethod]
    public void ThemeVariablesReferencedTokensAreEmittedByMapperWhenSet()
    {
        var scss = SourceFiles.ReadThemeStylesheet("theme-variables.scss");
        var expectedKeys = CssVarRef.Matches(scss)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToHashSet(StringComparer.Ordinal);

        var theme = new BitTheme();
        FillAllStringProperties(theme, []);

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        // A plain motion token is an alias of its unreduced -full token, which the reduced-motion query collapses and
        // ForceAnimation restores, so BitTheme sets it through the -full one it aliases and never itself.
        var missing = expectedKeys.Where(k => !mapped.ContainsKey(k) && !(k.StartsWith("--bit-mot-", StringComparison.Ordinal) && mapped.ContainsKey($"{k}-full"))).ToArray();
        CollectionAssert.AreEqual(Array.Empty<string>(), missing, $"Mapper missing keys referenced in theme-variables.scss: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void ThemeMotionIsWrittenToTheUnreducedTokens()
    {
        // Written inline, a plain duration would outrank the prefers-reduced-motion query that collapses it, and inside
        // a ForceAnimation subtree .bit-fam would restore it to the -full token the theme never set. The mapper writes
        // the -full tokens, as a preset does, and leaves the plain aliases to the stylesheets.
        var theme = new BitTheme();
        theme.Motion.Duration = "250ms";
        theme.Motion.DurationShort = "120ms";
        theme.Motion.DurationLong = "400ms";
        theme.Motion.DurationFactor = "0.5";
        theme.Motion.DurationSpinner = "2s";
        theme.Motion.EasingSpinner = "linear";
        theme.Motion.LoopFactor = "2";

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        Assert.AreEqual("250ms", mapped[BitCss.Var.Motion.DurationFull]);
        Assert.AreEqual("120ms", mapped[BitCss.Var.Motion.DurationShortFull]);
        Assert.AreEqual("400ms", mapped[BitCss.Var.Motion.DurationLongFull]);
        Assert.AreEqual("0.5", mapped[BitCss.Var.Motion.DurationFactorFull]);
        Assert.AreEqual("2s", mapped[BitCss.Var.Motion.DurationSpinnerFull]);
        Assert.AreEqual("linear", mapped[BitCss.Var.Motion.EasingSpinnerFull]);
        Assert.AreEqual("2", mapped[BitCss.Var.Motion.LoopFactorFull]);

        foreach (var plain in new[] { BitCss.Var.Motion.Duration, BitCss.Var.Motion.DurationShort, BitCss.Var.Motion.DurationLong,
                                      BitCss.Var.Motion.DurationFactor,
                                      BitCss.Var.Motion.DurationSpinner, BitCss.Var.Motion.EasingSpinner, BitCss.Var.Motion.LoopFactor })
        {
            Assert.IsFalse(mapped.ContainsKey(plain), $"{plain} is an alias the reduced-motion query collapses; the theme must not set it.");
        }
    }

    [TestMethod]
    public void MapperEmittedTokensAreConsumedByShippedStyles()
    {
        var scss = string.Join("\n", SourceFiles.EnumerateThemeStylesheets().Select(SourceFiles.ReadFullPath));

        var theme = new BitTheme();
        FillAllStringProperties(theme, []);

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        // A token that the mapper emits but no shipped stylesheet declares or consumes is dead:
        // setting it from BitTheme silently changes nothing. The boundary lookahead keeps a token
        // from being satisfied by a longer sibling (e.g. -duration by -duration-short).
        var dead = mapped.Keys
            .Where(key => !Regex.IsMatch(scss, Regex.Escape(key) + "(?![a-z0-9-])"))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), dead, $"Mapper emits dead tokens: {string.Join(", ", dead)}");
    }

    [TestMethod]
    public void BitThemeSerializationRoundtripPreservesPrimaryColor()
    {
        var original = new BitTheme();
        original.Color.Primary.Main = "#ABCDEF";

        var json = BitThemeSerialization.Serialize(original);
        var roundTrip = BitThemeSerialization.Deserialize(json);

        Assert.AreEqual("#ABCDEF", roundTrip.Color.Primary.Main);
    }

    [TestMethod]
    public void BitThemeSerialization_OmitsEmptyNestedObjects()
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = "#ABCDEF";

        var json = BitThemeSerialization.Serialize(theme);

        Assert.IsFalse(json.Contains("\"secondary\"", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("\"main\"", StringComparison.Ordinal));
        Assert.IsTrue(json.Length < 500, "Sparse theme JSON should stay compact.");
    }

    [TestMethod]
    public void BitThemeSerialization_EmptyThemeSerializesToEmptyObject()
    {
        var json = BitThemeSerialization.Serialize(new BitTheme());
        Assert.AreEqual("{}", json.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", ""));
    }

    [TestMethod]
    public void BitThemeSerialization_DeserializeMergeAfterSparseJson_DoesNotThrow()
    {
        var sparse = new BitTheme();
        sparse.Color.Primary.Main = "#112233";
        var json = BitThemeSerialization.Serialize(sparse);
        var roundTrip = BitThemeSerialization.Deserialize(json);
        var merged = BitThemeUtilities.Merge(roundTrip, new BitTheme());
        Assert.AreEqual("#112233", merged.Color.Primary.Main);
    }

    [TestMethod]
    public void MergeChildOverridesParentForSingleProperty()
    {
        var parent = new BitTheme();
        parent.Color.Primary.Main = "#111111";
        parent.Color.Primary.MainHover = "#333333";

        var child = new BitTheme();
        child.Color.Primary.Main = "#222222";

        var merged = BitThemeUtilities.Merge(child, parent);
        Assert.AreEqual("#222222", merged.Color.Primary.Main);
        Assert.AreEqual("#333333", merged.Color.Primary.MainHover);
    }

    [DataTestMethod]
    [DataRow("red; background:url(https://evil.example/x)", DisplayName = "semicolon injects a declaration")]
    [DataRow("red/* comment-out trailing */", DisplayName = "comment marker")]
    [DataRow("red} body{display:none", DisplayName = "block delimiters")]
    [DataRow("red<script", DisplayName = "html metacharacter")]
    [DataRow("\\65 vil", DisplayName = "css escape sequence")]
    [DataRow("red\n;color:blue", DisplayName = "newline")]
    [DataRow("a\"b", DisplayName = "unbalanced double quote swallows siblings")]
    [DataRow("a'b", DisplayName = "unbalanced single quote swallows siblings")]
    public void ToCssVariablesDropsInjectionProneTokenValues(string maliciousValue)
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = maliciousValue;

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        Assert.IsFalse(mapped.ContainsKey("--bit-clr-pri"),
            $"Injection-prone token value '{maliciousValue}' must be dropped, not emitted.");
    }

    [DataTestMethod]
    [DataRow("#ABCDEF", DisplayName = "hex color")]
    [DataRow("rgba(0, 0, 0, .5)", DisplayName = "rgba with commas")]
    [DataRow("hsl(210 50% 40%)", DisplayName = "hsl")]
    public void ToCssVariablesPreservesLegitimateColorValues(string value)
    {
        var theme = new BitTheme();
        theme.Color.Primary.Main = value;

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        Assert.IsTrue(mapped.TryGetValue("--bit-clr-pri", out var emitted) && emitted == value,
            $"Legitimate color value '{value}' must be preserved.");
    }

    [TestMethod]
    public void ToCssVariablesPreservesLegitimateCompositeValues()
    {
        var theme = new BitTheme();
        theme.Typography.FontFamily = "\"Segoe UI\", Arial, sans-serif";
        theme.BoxShadow.Sm = "0 1px 2px rgba(0, 0, 0, .1), 0 2px 4px rgba(0, 0, 0, .2)";
        theme.Motion.EasingStandard = "cubic-bezier(0.4, 0, 0.2, 1)";

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        Assert.AreEqual("\"Segoe UI\", Arial, sans-serif", mapped["--bit-tpg-font-family"]);
        Assert.AreEqual("0 1px 2px rgba(0, 0, 0, .1), 0 2px 4px rgba(0, 0, 0, .2)", mapped["--bit-shd-sm"]);
        Assert.AreEqual("cubic-bezier(0.4, 0, 0.2, 1)", mapped["--bit-mot-easing"]);
    }

    private static void FillAllStringProperties(object? obj, HashSet<object> visited)
    {
        if (obj is null) return;
        if (!visited.Add(obj)) return;

        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var propType = prop.PropertyType;
            if (propType == typeof(string))
            {
                prop.SetValue(obj, "1");
                continue;
            }

            if (propType.IsClass && propType != typeof(string))
            {
                var existing = prop.GetValue(obj);
                if (existing is null)
                {
                    existing = Activator.CreateInstance(propType)
                               ?? throw new InvalidOperationException($"Cannot create {propType.Name}");
                    prop.SetValue(obj, existing);
                }

                FillAllStringProperties(existing, visited);
            }
        }
    }
}
