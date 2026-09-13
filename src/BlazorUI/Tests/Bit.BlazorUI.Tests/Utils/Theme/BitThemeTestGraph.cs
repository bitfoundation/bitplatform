using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Shared reflection walker for the <see cref="BitTheme"/> model graph, so the theme contract
/// tests (<see cref="BitThemeModelCoverageTests"/>, <see cref="BitThemeMapperMergeDriftTests"/>)
/// agree on a single definition of which properties count as string tokens and which as nested
/// model branches.
/// </summary>
internal static class BitThemeTestGraph
{
    /// <summary>
    /// Walks the graph and sets every readable/writable <see cref="string"/> property to a unique
    /// non-empty sentinel (<c>"{prefix}-0"</c>, <c>"{prefix}-1"</c>, ... - non-empty so the
    /// mapper's whitespace guard never skips it), returning the count. Recursion is restricted to
    /// Bit.BlazorUI model branches (see <see cref="IsModelBranch"/>) with reference-equality cycle
    /// protection; a branch that cannot be instantiated is a model-shape bug and throws.
    /// </summary>
    public static int FillStringLeavesWithSentinels(object root, string prefix = "sentinel")
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var counter = 0;
        Walk(root);
        return counter;

        void Walk(object obj)
        {
            if (!visited.Add(obj)) return;

            foreach (var prop in GetModelProperties(obj.GetType()))
            {
                var pt = prop.PropertyType;

                if (pt == typeof(string))
                {
                    prop.SetValue(obj, $"{prefix}-{counter++}");
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
                    Walk(child);
                }
            }
        }
    }

    /// <summary>The walkable surface of a model type: public, readable/writable, non-indexed instance properties.</summary>
    public static IEnumerable<PropertyInfo> GetModelProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

    /// <summary>A nested model object: a non-string reference type declared in the Bit.BlazorUI namespace.</summary>
    public static bool IsModelBranch(Type type)
        => type != typeof(string)
           && !type.IsValueType
           && string.Equals(type.Namespace, "Bit.BlazorUI", StringComparison.Ordinal);
}
