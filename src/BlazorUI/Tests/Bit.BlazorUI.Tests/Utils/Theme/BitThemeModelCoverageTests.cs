using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        var leafCount = FillStringLeavesWithSentinels(theme);

        var emitted = BitThemeUtilities.ToCssVariables(theme);

        Assert.AreEqual(leafCount, emitted.Count,
            $"The BitTheme model exposes {leafCount} settable string tokens but " +
            $"BitThemeMapper.MapToCssVariables emitted {emitted.Count} CSS variables. Every token must map " +
            "to exactly one unique CSS variable. If you added a token, add the matching addCssVar(...) entry " +
            "(and the null-coalesce in BitThemeMapper.Merge). A shortfall also occurs when two tokens map to " +
            "the same CSS variable name.");
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

    /// <summary>
    /// Walks the graph and sets every readable/writable <see cref="string"/> property to a unique
    /// non-empty sentinel (so the mapper's whitespace guard never skips it), returning the count.
    /// </summary>
    private static int FillStringLeavesWithSentinels(object root)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var counter = 0;
        Walk(root, visited, ref counter);
        return counter;

        static void Walk(object obj, HashSet<object> visited, ref int counter)
        {
            if (!visited.Add(obj)) return;

            foreach (var prop in GetModelProperties(obj.GetType()))
            {
                var pt = prop.PropertyType;

                if (pt == typeof(string))
                {
                    prop.SetValue(obj, $"sentinel-{counter++}");
                }
                else if (IsModelBranch(pt))
                {
                    var child = prop.GetValue(obj);
                    if (child is null)
                    {
                        child = Activator.CreateInstance(pt);
                        if (child is null)
                        {
                            throw new InvalidOperationException(
                                $"Theme branch type '{pt.FullName}' (property '{prop.DeclaringType?.Name}.{prop.Name}') " +
                                "could not be instantiated via its parameterless constructor. " +
                                "Ensure every theme branch model exposes a public parameterless constructor.");
                        }
                        prop.SetValue(obj, child);
                    }
                    Walk(child, visited, ref counter);
                }
            }
        }
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

            foreach (var prop in GetModelProperties(obj.GetType()))
            {
                if (!IsModelBranch(prop.PropertyType)) continue;

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

    private static IEnumerable<PropertyInfo> GetModelProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

    /// <summary>A nested model object: a non-string reference type declared in the Bit.BlazorUI namespace.</summary>
    private static bool IsModelBranch(Type type)
        => type != typeof(string)
           && !type.IsValueType
           && string.Equals(type.Namespace, "Bit.BlazorUI", StringComparison.Ordinal);
}
