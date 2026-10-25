using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// The item-based components read an app's item type by the name of a property (NameSelectors, a DataGrid column's
/// Field), which the trimmer cannot follow. What keeps those properties in a trimmed app is the
/// [DynamicallyAccessedMembers(PublicProperties)] on the component's TItem: an app that hands its type to the
/// component then keeps the type's public properties. A component that starts reading items by name without it works
/// in every test and breaks only in a trimmed app, so the annotation is pinned here for every one of them.
/// </summary>
[TestClass]
public sealed partial class BitItemPropertyReadersContractTests
{
    private static readonly Assembly[] Packages = [typeof(BitButton).Assembly, typeof(BitDataGrid<>).Assembly];

    [TestMethod]
    public void EveryComponentThatReadsItemsByPropertyNameShouldKeepThePropertiesOfItsItemType()
    {
        var readers = FindReaders().ToArray();

        Assert.IsGreaterThanOrEqualTo(10, readers.Length, "Found fewer readers than the components known to read items by name; the scan of the sources is broken.");

        var missing = readers.Where(r => KeepsPublicProperties(r.Type, "TItem") is false)
                             .Select(r => $"{r.Type.Name} ({r.File})")
                             .ToArray();

        Assert.IsEmpty(missing, "These components read their items by property name, so their TItem needs " +
                                "[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] for a trimmed app " +
                                "to keep the properties: " + string.Join(", ", missing));
    }

    [TestMethod]
    [DataRow(typeof(BitDropdown<,>))]
    [DataRow(typeof(BitNav<>))]
    [DataRow(typeof(BitTimeline<>))]
    [DataRow(typeof(BitDataGridColumn<>))]
    public void TheScanShouldFindTheKnownReaders(Type component)
    {
        Assert.IsTrue(FindReaders().Any(r => r.Type == component), $"{component.Name} reads its items by name but the scan did not find it.");
    }

    // Every generic type declared (in any of its partial files) in a source file that reads an item's property by name.
    private static IEnumerable<(Type Type, string File)> FindReaders()
    {
        var roots = new[] { SourceFiles.GetDirectory("Bit.BlazorUI"), SourceFiles.GetDirectory("Bit.BlazorUI.Extras") };
        var types = Packages.SelectMany(a => a.GetTypes()).Where(t => t.IsGenericTypeDefinition).ToArray();
        var seen = new HashSet<Type>();

        foreach (var file in roots.SelectMany(r => Directory.EnumerateFiles(r, "*.cs", SearchOption.AllDirectories)))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.EndsWith("ObjectExtensions.cs")) continue;

            var source = File.ReadAllText(file);
            if (ReadsByName().IsMatch(source) is false) continue;

            foreach (Match declaration in GenericDeclaration().Matches(source))
            {
                var name = declaration.Groups["name"].Value;
                var arity = declaration.Groups["params"].Value.Split(',').Length;
                var type = types.FirstOrDefault(t => t.Name == $"{name}`{arity}");

                if (type is null || type.GetGenericArguments().All(a => a.Name != "TItem")) continue;
                if (seen.Add(type)) yield return (type, Path.GetFileName(file));
            }
        }
    }

    private static bool KeepsPublicProperties(Type genericTypeDefinition, string parameterName)
    {
        var parameter = genericTypeDefinition.GetGenericArguments().Single(a => a.Name == parameterName);
        var annotation = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();

        return annotation is not null && annotation.MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties);
    }

    [GeneratedRegex(@"\b(GetValueFromProperty|SetValueToProperty)\b|BitDataGridPropertyAccessor<TItem>\.For\(")]
    private static partial Regex ReadsByName();

    // "class Name<A, B>" with any attributes on the parameters; the names are what is counted.
    [GeneratedRegex(@"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)<(?<params>(?:\[[^\]]*\]\s*)?\w+(?:\s*,\s*(?:\[[^\]]*\]\s*)?\w+)*)>")]
    private static partial Regex GenericDeclaration();
}
