using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Drift-detection contract for <c>BitThemeMapper.Merge</c>. The merge implementation is hand-mirrored
/// from the model graph; without this test, adding a new property and forgetting to extend
/// <c>Merge</c> would silently drop the value during cascading <see cref="BitThemeProvider"/>
/// composition, while the keyset snapshot test (which only inspects <c>MapToCssVariables</c>)
/// would still pass.
/// </summary>
/// <remarks>
/// <para>
/// We pin the contract via the mapper's emitted CSS-variable surface rather than the underlying
/// property graph. The model exposes a few properties the mapper deliberately doesn't emit
/// (e.g. <c>Typography.H1.FontFamily</c> — typography font-family is a single root-level token);
/// asserting on every model property would fail on those by design. Asserting on emitted vars
/// gives us the contract that actually matters: every CSS variable that survives serialization
/// of one theme must also survive serialization of the merge of that theme over an empty one.
/// </para>
/// <para>
/// If this test fails, find the entry in <c>BitThemeMapper.MapToCssVariables</c> that emits the
/// listed key, and add the corresponding null-coalesce in <c>BitThemeMapper.Merge</c>.
/// </para>
/// </remarks>
[TestClass]
public sealed class BitThemeMapperMergeDriftTests
{
    [TestMethod]
    public void EmittedKeysFromChildSurviveMergeOverEmptyParent()
    {
        var sentinelTheme = BuildSentinelTheme();
        var direct = BitThemeUtilities.ToCssVariables(sentinelTheme);
        var merged = BitThemeUtilities.ToCssVariables(BitThemeUtilities.Merge(sentinelTheme, new BitTheme()));

        AssertNoDrift(direct, merged, "child-over-empty");
    }

    [TestMethod]
    public void EmittedKeysFromParentSurviveMergeWhenChildLeavesAllNull()
    {
        var sentinelTheme = BuildSentinelTheme();
        var direct = BitThemeUtilities.ToCssVariables(sentinelTheme);
        var merged = BitThemeUtilities.ToCssVariables(BitThemeUtilities.Merge(new BitTheme(), sentinelTheme));

        AssertNoDrift(direct, merged, "empty-over-parent");
    }

    private static void AssertNoDrift(IReadOnlyDictionary<string, string> direct, IReadOnlyDictionary<string, string> merged, string scenario)
    {
        var missing = direct
            .Where(kv => !merged.TryGetValue(kv.Key, out var v) || !string.Equals(v, kv.Value, StringComparison.Ordinal))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToArray();

        if (missing.Length > 0)
        {
            var detail = string.Join("\n  ", missing.Select(kv =>
                merged.TryGetValue(kv.Key, out var actual)
                    ? $"{kv.Key} (expected '{kv.Value}', got '{actual}')"
                    : $"{kv.Key} (expected '{kv.Value}', missing)"));

            Assert.Fail(
                $"BitThemeMapper.Merge dropped {missing.Length} CSS variables in the '{scenario}' scenario. " +
                "For each key listed below, locate the entry in BitThemeMapper.MapToCssVariables and add the " +
                "matching null-coalesce in BitThemeMapper.Merge:\n  " + detail);
        }
    }

    /// <summary>
    /// Builds a fully-populated <see cref="BitTheme"/> where each leaf string property is set to a
    /// unique sentinel value. We populate every reachable string slot, not just the ones the mapper
    /// emits — Merge needs to handle the full model graph in case the mapper is later extended.
    /// </summary>
    private static BitTheme BuildSentinelTheme()
    {
        var theme = new BitTheme();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var counter = 0;

        Walk(theme, visited, ref counter);
        return theme;

        static void Walk(object obj, HashSet<object> visited, ref int counter)
        {
            if (!visited.Add(obj)) return;

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;

                var pt = prop.PropertyType;

                if (pt == typeof(string))
                {
                    prop.SetValue(obj, $"sentinel-{counter++}");
                }
                else if (!pt.IsValueType)
                {
                    var val = prop.GetValue(obj);
                    if (val is null)
                    {
                        val = Activator.CreateInstance(pt);
                        if (val is null) continue;
                        prop.SetValue(obj, val);
                    }
                    Walk(val, visited, ref counter);
                }
            }
        }
    }
}
