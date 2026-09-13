using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Coverage contracts for the hand-mirrored <see cref="BitTheme"/> model plumbing:
/// <c>BitThemeMapper.MapToCssVariables</c>, <c>BitThemeMapper.Merge</c>, and
/// <c>BitThemeSerialization.EnsureNestedObjects</c>.
/// </summary>
/// <remarks>
/// <para>
/// These three methods are written out by hand, one line per token / branch. Without a guard,
/// adding a property to the model and forgetting to extend one of them fails silently: a token
/// that isn't mapped never reaches CSS, a branch that isn't re-inflated throws a
/// <see cref="NullReferenceException"/> the first time a sparse deserialized theme is walked, and a
/// merge gap drops the value during cascading <see cref="BitThemeProvider"/> composition.
/// </para>
/// <para>
/// Runtime reflection is intentionally NOT used in the production mapper (it breaks under
/// trimming / AOT - see <c>BitThemeSerialization.EnsureNestedObjects</c>), but the test assembly
/// has no such constraint, so we use reflection here to pin the contract at build/test time.
/// </para>
/// <para>
/// Companion: <see cref="BitThemeMapperMergeDriftTests"/> pins that every <em>emitted</em> variable
/// survives <c>Merge</c>. Combined with <see cref="EveryLeafStringTokenIsEmittedByTheMapper"/>
/// (every leaf token is emitted), the two together guarantee every model token survives a merge.
/// </para>
/// </remarks>
[TestClass]
public sealed class BitThemeModelCoverageTests
{
    [TestMethod]
    public void EveryLeafStringTokenIsEmittedByTheMapper()
    {
        // Set every settable string property in the graph to a unique non-empty sentinel, then
        // assert the mapper emits exactly one CSS variable per token. A shortfall means either a
        // token was added to the model without a matching addCssVar(...) in
        // BitThemeMapper.MapToCssVariables, or two tokens were mapped to the same CSS variable
        // name (a copy/paste bug - which additionally surfaces as a duplicate-key throw below).
        var theme = new BitTheme();
        var leafCount = BitThemeTestGraph.FillStringLeavesWithSentinels(theme);

        var emitted = BitThemeUtilities.ToCssVariables(theme);

        Assert.AreEqual(leafCount, emitted.Count,
            $"The BitTheme model exposes {leafCount} settable string tokens but " +
            $"BitThemeMapper.MapToCssVariables emitted {emitted.Count} CSS variables. Every token must map " +
            "to exactly one unique CSS variable. If you added a token, add the matching addCssVar(...) entry " +
            "(and the null-coalesce in BitThemeMapper.Merge). A shortfall also occurs when two tokens map to " +
            "the same CSS variable name.");

        // Each sentinel is unique ("sentinel-0" ... "sentinel-{n-1}") and passes through the mapper
        // verbatim, so the emitted value SET must equal the assigned sentinel set. Comparing the sets
        // (not just the counts) additionally catches a token routed to the wrong CSS variable or a
        // hard-coded/constant emit that a count-only check would let slip through.
        var expectedValues = Enumerable.Range(0, leafCount).Select(i => $"sentinel-{i}").ToHashSet(StringComparer.Ordinal);
        var emittedValues = emitted.Values.ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(expectedValues.SetEquals(emittedValues),
            "The CSS-variable values emitted by BitThemeMapper.MapToCssVariables do not match the unique " +
            "per-token sentinels; a token is mapped to the wrong CSS variable, emits a constant, or is dropped:\n" +
            "  missing (assigned but not emitted): " +
            string.Join(", ", expectedValues.Except(emittedValues).OrderBy(v => v, StringComparer.Ordinal)) + "\n" +
            "  unexpected (emitted but not assigned): " +
            string.Join(", ", emittedValues.Except(expectedValues).OrderBy(v => v, StringComparer.Ordinal)));
    }

    [TestMethod]
    public void DeserializeEmptyJsonReInflatesEveryBranch()
    {
        // "{}" has no nested objects at all; EnsureNestedObjects must rebuild the whole graph so
        // downstream walkers (mapper, merge, derivation) never hit a null branch.
        var theme = BitThemeSerialization.Deserialize("{}");

        AssertNoNullBranches(theme, "Deserialize(\"{}\")");
    }

    [TestMethod]
    public void DeserializeSparseRoundTripReInflatesEveryBranch()
    {
        // Serialize prunes empty nested objects, so a theme with a single token round-trips through
        // JSON with most branches absent. EnsureNestedObjects must restore them all.
        var sparse = new BitTheme();
        sparse.Color.Primary.Main = "#123456";
        sparse.Typography.Inherit.FontFamily = "Inter";

        var json = BitThemeSerialization.Serialize(sparse);
        var theme = BitThemeSerialization.Deserialize(json);

        AssertNoNullBranches(theme, "round-tripped sparse theme");
        Assert.AreEqual("#123456", theme.Color.Primary.Main);
        Assert.AreEqual("Inter", theme.Typography.Inherit.FontFamily);
    }

    [TestMethod]
    public void FreshThemeHasNoNullBranches()
    {
        // Sanity check that the model's property initializers themselves leave no null branch;
        // this is the shape EnsureNestedObjects is expected to reproduce.
        AssertNoNullBranches(new BitTheme(), "new BitTheme()");
    }

    private static void AssertNoNullBranches(object root, string scenario)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var nullBranches = new List<string>();
        Walk(root, root.GetType().Name, visited, nullBranches);

        Assert.AreEqual(0, nullBranches.Count,
            $"Null branches found after {scenario}; extend BitThemeSerialization.EnsureNestedObjects to " +
            $"initialize them:\n  {string.Join("\n  ", nullBranches)}");

        static void Walk(object obj, string path, HashSet<object> visited, List<string> nullBranches)
        {
            if (!visited.Add(obj)) return;

            foreach (var prop in BitThemeTestGraph.GetModelProperties(obj.GetType()))
            {
                if (!BitThemeTestGraph.IsModelBranch(prop.PropertyType)) continue;

                var child = prop.GetValue(obj);
                var childPath = $"{path}.{prop.Name}";
                if (child is null)
                {
                    nullBranches.Add(childPath);
                    continue;
                }
                Walk(child, childPath, visited, nullBranches);
            }
        }
    }
}
