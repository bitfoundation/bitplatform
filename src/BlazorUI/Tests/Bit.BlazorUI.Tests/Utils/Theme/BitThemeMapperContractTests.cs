using System;
using System.Collections.Generic;
using System.IO;
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
        var scssPath = Path.Combine(AppContext.BaseDirectory, "theme-variables.scss");
        Assert.IsTrue(File.Exists(scssPath), $"Missing {scssPath}; ensure theme-variables.scss is copied to output.");

        var scss = File.ReadAllText(scssPath);
        var expectedKeys = CssVarRef.Matches(scss)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToHashSet(StringComparer.Ordinal);

        var theme = new BitTheme();
        FillAllStringProperties(theme, []);

        var mapped = BitThemeUtilities.ToCssVariables(theme);

        var missing = expectedKeys.Where(k => !mapped.ContainsKey(k)).ToArray();
        CollectionAssert.AreEqual(Array.Empty<string>(), missing, $"Mapper missing keys referenced in theme-variables.scss: {string.Join(", ", missing)}");
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
