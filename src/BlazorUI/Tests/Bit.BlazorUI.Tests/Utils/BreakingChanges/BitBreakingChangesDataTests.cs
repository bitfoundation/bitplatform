using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bit.BlazorUI.Analyzers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.BreakingChanges;

/// <summary>
/// Pins Bit.BlazorUI.Analyzers/BreakingChanges.json - the list of every name removed or renamed since 10.5.0, which the
/// analyzer fails an app's build on and the MCP server hands an agent upgrading an app - to the packages as compiled.
/// </summary>
/// <remarks>
/// An entry is only worth what its names are: a type in <c>on</c> that does not exist matches nothing, an old name that
/// is still there would fail the build of an app using it correctly, and a replacement that does not exist sends the app
/// nowhere. So every entry is read against the assemblies of every package: the types it names exist, the name it says
/// is gone is gone, and what it says to use instead is there.
/// </remarks>
[TestClass]
public sealed class BitBreakingChangesDataTests
{
    private static readonly string[] Versions = ["10.6.0", "10.6.1", "10.6.2", "10.7.0", "11.0.0"];

    // Names that are gone in the sense the entry means while a member of that name exists again: BitMediaQuery's
    // Matched was the content shown while the query matches, a template written as <Matched>, and is now the bool state.
    // The analyzer reports the child content tag only, which no template takes any more.
    private static readonly HashSet<string> NamesTakenAgain = new(StringComparer.Ordinal) { "BitMediaQuery.Matched" };

    private static readonly Lazy<IReadOnlyList<Type>> PackageTypes = new(() =>
        new[]
        {
            typeof(BitButton).Assembly,
            typeof(BitDataGrid<>).Assembly,
            typeof(BitAiChat).Assembly,
            typeof(BitAiMessage).Assembly,
            Assembly.Load("Bit.BlazorUI.Legacy"),
            Assembly.Load("Bit.BlazorUI.Assets"),
        }
        .Distinct()
        .SelectMany(LoadableTypes.Of)
        .ToArray());

    private static BitBreakingChanges Changes => BitBreakingChanges.Default;

    [TestMethod]
    public void TheEmbeddedListIsReadAndNotEmpty()
    {
        Assert.IsTrue(Changes.All.Length > 200, $"Only {Changes.All.Length} changes were read out of the embedded BreakingChanges.json.");
    }

    [TestMethod]
    public void EveryEntryIsComplete()
    {
        var problems = new List<string>();

        foreach (var change in Changes.All)
        {
            var id = Id(change);
            if (string.IsNullOrWhiteSpace(change.Name)) problems.Add($"{id}: no name");
            if (Versions.Contains(change.Since) is false) problems.Add($"{id}: since '{change.Since}' is none of {string.Join(", ", Versions)}");
            if (change.Hint.Trim().EndsWith('.') is false) problems.Add($"{id}: the hint is not a sentence ending with a period");
            if (change.IsMember && change.On.IsEmpty) problems.Add($"{id}: a member names the types it was on");
            if (change.IsType && change.On.IsEmpty is false) problems.Add($"{id}: a type is on no type");
            if (change.Component && change.IsType is false) problems.Add($"{id}: only a type is a component");
        }

        var duplicates = Changes.All.SelectMany(c => c.IsMember ? c.On.Select(on => $"{on}.{c.Name}") : [c.Name])
                                    .GroupBy(key => key, StringComparer.Ordinal)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => $"{g.Key}: listed {g.Count()} times");

        problems.AddRange(duplicates);

        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    [TestMethod]
    public void EveryMemberIsGoneFromTypesThatExistAndItsReplacementIsThere()
    {
        var problems = new List<string>();

        foreach (var change in Changes.All.Where(c => c.IsMember))
        {
            foreach (var on in change.On)
            {
                var type = FindType(on);
                if (type is null)
                {
                    problems.Add($"{on}.{change.Name}: no type {on} in the packages");
                    continue;
                }

                if (NamesTakenAgain.Contains($"{on}.{change.Name}") is false && HasMember(type, change.Name))
                    problems.Add($"{on}.{change.Name}: {on} still has {change.Name}");

                if (change.Use.Length > 0 && HasMember(type, change.Use) is false)
                    problems.Add($"{on}.{change.Name}: {on} has no {change.Use} to use instead");
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    [TestMethod]
    public void EveryTypeIsGoneAndItsReplacementIsThere()
    {
        var problems = new List<string>();

        foreach (var change in Changes.All.Where(c => c.IsType))
        {
            var current = FindType(change.Name);
            if (current is not null && current.IsPublic)
                problems.Add($"{change.Name}: the packages still have a public {current.FullName}");

            if (change.Use.Length > 0 && FindType(change.Use) is not { IsPublic: true })
                problems.Add($"{change.Name}: the packages have no public {change.Use} to use instead");
        }

        Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
    }

    private static Type? FindType(string simpleName)
        => PackageTypes.Value.FirstOrDefault(t => t.IsNested is false && SimpleName(t) == simpleName);

    private static string SimpleName(Type type)
    {
        var tick = type.Name.IndexOf('`');
        return tick < 0 ? type.Name : type.Name[..tick];
    }

    private static bool HasMember(Type type, string name)
        => type.GetMember(name, MemberTypes.Property | MemberTypes.Field | MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy).Length > 0;

    private static string Id(BitBreakingChange change)
        => change.IsMember ? $"{string.Join("/", change.On)}.{change.Name}" : change.Name;
}
