using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bit.BlazorUI.Demo.Client.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the enum tables the demo pages share (<c>DemoSharedEnums</c>) to the enums they document. Written once,
/// a table is shown on every page that names the type, so a member it describes wrongly is wrong on all of them at
/// once - and the MCP server answers from the same tables. The factories are compile-linked into this project, with
/// the half generated out of the library's XML documentation, and called, so every table is checked however its
/// factory happens to be written.
/// <para>
/// The members, values and prose of a table are read off its enum, so what is left to pin is that the enum documents
/// every one of them - at runtime a missing summary only leaves a cell empty - and the anchor id, which no page may
/// give to another type.
/// </para>
/// </summary>
[TestClass]
public partial class DemoSharedEnumsTests
{
    private static readonly MethodInfo[] Factories = [.. typeof(DemoSharedEnums).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                .Where(m => m.ReturnType == typeof(ComponentSubEnum) && m.Name != nameof(DemoSharedEnums.Only))];

    [TestMethod]
    public void EverySharedTableShouldListTheMembersOfTheEnumItIsNamedAfterInTheirDeclaredOrder()
    {
        Assert.IsTrue(Factories.Length > 0, "No shared table was found.");

        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.AreEqual(factory.Name, table.Name, $"The {factory.Name} factory builds a table named after another type.");

            var type = typeof(BitColor).Assembly.GetType($"Bit.BlazorUI.{factory.Name}");

            Assert.IsNotNull(type, $"{factory.Name} is not an enum of Bit.BlazorUI.");
            Assert.IsTrue(type.IsEnum, $"{factory.Name} is not an enum.");

            // GetFields keeps the order the members are declared in; Enum.GetNames sorts them by value.
            CollectionAssert.AreEqual(type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.Name).ToArray(),
                                      table.Items.Select(i => i.Name).ToArray(),
                                      $"The shared {factory.Name} table does not list the members of the enum in the order it declares them.");
        }
    }

    [TestMethod]
    public void EverySharedTableShouldDescribeItselfAndEachOfItsMembers()
    {
        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.IsFalse(string.IsNullOrWhiteSpace(table.Description), $"{factory.Name} has no XML summary for its shared table to show.");

            foreach (var item in table.Items)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.Description), $"{factory.Name}.{item.Name} has no XML summary for the shared {factory.Name} table to show.");
            }
        }
    }

    [TestMethod]
    public void EveryEnumWhoseDocumentationIsGeneratedShouldHaveASharedTableAndViceVersa()
    {
        var documented = DemoSharedEnums.Summaries.Keys.Where(k => k.Contains('.') is false).Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(Factories.Select(f => f.Name).Order(StringComparer.Ordinal).ToArray(), documented,
            "The enums named in DemoSharedEnumDocs.targets and the factories of DemoSharedEnums differ.");
    }

    [TestMethod]
    public void EverySharedTableShouldHaveAnAnchorIdOfItsOwn()
    {
        var ids = Factories.Select(f => Build(f).Id).ToArray();

        foreach (var id in ids)
        {
            Assert.IsTrue(id?.EndsWith("-enum", StringComparison.Ordinal), $"The anchor id {id} does not end in -enum like the others.");
        }

        CollectionAssert.AllItemsAreUnique(ids, "Two shared tables are anchored under the same id.");
    }

    [TestMethod]
    public void NoPageShouldAnchorAnotherTypeUnderTheIdOfASharedTable()
    {
        var shared = Factories.Select(Build).ToDictionary(t => t.Name!, t => t.Id!, StringComparer.Ordinal);
        var sharedIds = shared.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

        foreach (var (page, id, name) in PageOwnTables())
        {
            if (sharedIds.TryGetValue(id, out var owner))
            {
                Assert.AreEqual(owner, name, $"{page} anchors its {name} table under {id}, the id of the shared {owner} table.");
            }

            if (shared.TryGetValue(name, out var sharedId))
            {
                Assert.AreEqual(sharedId, id, $"{page} keeps a {name} table of its own under {id}, rather than under {sharedId} like the shared one.");
            }
        }
    }

    [TestMethod]
    public void EveryTypeAPageKeepsATableOfShouldBeAnchoredUnderOneIdOnEveryPage()
    {
        var tables = PageOwnTables().ToArray();

        Assert.IsTrue(tables.Length > 50, $"Only {tables.Length} tables were read off the demo pages.");

        var offenders = tables.GroupBy(t => t.Name, StringComparer.Ordinal)
                                       .Where(g => g.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() > 1)
                                       .Select(g => $"{g.Key} ({string.Join(", ", g.Select(t => $"{t.Page}: {t.Id}"))})")
                                       .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders, $"Types anchored under more than one id: {string.Join("; ", offenders)}");
    }

    [TestMethod]
    public void PageOwnTableShouldReadATableHoweverItsInitializerIsSpelled()
    {
        string[] spellings =
        [
            """new() { Id = "size-enum", Name = "BitSize", Items = [] }""",
            """new() { Id="size-enum", Name="BitSize" }""",
            """new() { Id= "size-enum",Name ="BitSize" }""",
            """new() { Name = "BitSize", Id = "size-enum" }""",
            "new()\n{\n    Name = \"BitSize\",\n    Description = \"The sizes.\",\n    Id = \"size-enum\",\n    Items = []\n}",
        ];

        foreach (var spelling in spellings)
        {
            var page = $"private readonly List<ComponentSubEnum> componentSubEnums =\n[\n    DemoSharedEnums.BitColor(),\n    {spelling},\n];";

            CollectionAssert.AreEqual(new[] { ("size-enum", "BitSize") }, ReadTables(page).ToArray(), $"Not read: {spelling}");
        }

        // The demo data of a page sets an Id and a Name on its items as well, and is no table.
        Assert.AreEqual(0, ReadTables("""private readonly List<Item> items = [ new() { Id = "edit", Name = "Edit" } ];""").Count());
    }

    [TestMethod]
    public void FlattenShouldNameACrefTheWayAReaderWritesIt()
    {
        var documentation = XDocument.Parse("""
            <doc><members>
                <member name="T:Bit.BlazorUI.BitNav`1"><summary>A nav.</summary><typeparam name="TItem">The item.</typeparam></member>
                <member name="T:Bit.BlazorUI.BitThing">
                    <summary>
                        Works with <see cref="T:Bit.BlazorUI.BitNav`1"/>, <see cref="M:Bit.BlazorUI.BitNav`1.Select(System.String)"/>,
                        <see cref="M:Bit.BlazorUI.BitNav`1.#ctor"/>, <see cref="F:Bit.BlazorUI.BitColor.Primary"/>,
                        <see cref="T:System.Collections.Generic.Dictionary`2"/>, <see cref="M:Bit.BlazorUI.BitThing.Get``1"/>,
                        <see langword="null"/>, <paramref name="value"/>, <c>code</c> and
                        <see href="https://example.com">a link</see>. <see href="https://example.com"/>
                    </summary>
                </member>
            </members></doc>
            """);

        var members = DemoSharedEnumDocs.Members(documentation);

        Assert.AreEqual("Works with BitNav<TItem>, BitNav<TItem>.Select, BitNav<TItem>, BitColor.Primary, Dictionary<T1, T2>, BitThing.Get, null, value, code and a link.",
                        DemoSharedEnumDocs.Flatten(members["T:Bit.BlazorUI.BitThing"].Element("summary")!, members));
    }

    [TestMethod]
    public void EveryFactoryShouldLetAPageReplaceTheLineAboveTheTable()
    {
        foreach (var factory in Factories)
        {
            var parameters = factory.GetParameters();

            Assert.IsTrue(parameters is [{ Name: "description", HasDefaultValue: true }], $"The {factory.Name} factory does not take an optional description.");

            Assert.AreEqual("A page's own line.", ((ComponentSubEnum)factory.Invoke(null, ["A page's own line."])!).Description);
        }
    }

    [TestMethod]
    public void OnlyShouldKeepTheNamedMembersInTheOrderOfTheTable()
    {
        var table = DemoSharedEnums.BitColor().Only("Error", "Primary");

        CollectionAssert.AreEqual(new[] { "Primary", "Error" }, table.Items.Select(i => i.Name).ToArray());
    }

    [TestMethod]
    public void OnlyShouldThrowOnANameThatIsNotAMemberOfTheTable()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DemoSharedEnums.BitColor().Only("Primary", "Secondry"));
    }

    [TestMethod]
    public void OnlyShouldThrowWhenItIsNamedNoMemberAtAll()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DemoSharedEnums.BitColor().Only());
    }

    private static ComponentSubEnum Build(MethodInfo factory)
        => (ComponentSubEnum)factory.Invoke(null, [.. factory.GetParameters().Select(p => p.DefaultValue)])!;

    // Every table a demo page writes out itself, read from the pages where they sit in the source tree.
    private static IEnumerable<(string Page, string Id, string Name)> PageOwnTables()
    {
        var pages = SourceFiles.GetDirectory("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components");
        var files = Directory.GetFiles(pages, "*.razor.cs", SearchOption.AllDirectories);

        Assert.IsTrue(files.Length > 0, "No demo page was found.");

        foreach (var file in files)
        {
            foreach (var (id, name) in ReadTables(SourceFiles.ReadFullPath(file)))
            {
                yield return (Path.GetFileName(file), id, name);
            }
        }
    }

    // Every object initializer of a List<ComponentSubEnum> that sets both an Id and a Name, in either order and
    // however the assignments are spaced.
    private static IEnumerable<(string Id, string Name)> ReadTables(string source)
    {
        foreach (var list in SubEnumLists(source))
        {
            foreach (Match initializer in Initializer().Matches(list))
            {
                var id = IdAssignment().Match(initializer.Value);
                var name = NameAssignment().Match(initializer.Value);

                if (id.Success && name.Success)
                {
                    yield return (id.Groups["id"].Value, name.Groups["name"].Value);
                }
            }
        }
    }

    // The collection expression each List<ComponentSubEnum> is initialized with, up to the bracket closing it -
    // the demo data of a page sets an Id and a Name on its items too. Brackets inside string literals do not count.
    private static IEnumerable<string> SubEnumLists(string source)
    {
        foreach (Match declaration in SubEnumListDeclaration().Matches(source))
        {
            var start = declaration.Index + declaration.Length - 1;
            var depth = 0;
            var inString = false;

            for (var i = start; i < source.Length; i++)
            {
                var c = source[i];

                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                }
                else if (c == '"') inString = true;
                else if (c == '[') depth++;
                else if (c == ']' && --depth == 0)
                {
                    yield return source[start..(i + 1)];
                    break;
                }
            }
        }
    }

    [GeneratedRegex(@"List<ComponentSubEnum>\s+\w+\s*=\s*\[")]
    private static partial Regex SubEnumListDeclaration();

    // The innermost brace pair: a table's own fields sit directly inside it, before its Items list opens one of
    // its own rows.
    [GeneratedRegex(@"\{[^{}]*\}|\{[^{}]*(?=\[)")]
    private static partial Regex Initializer();

    [GeneratedRegex(@"\bId\s*=\s*""(?<id>[\w-]+)""")]
    private static partial Regex IdAssignment();

    [GeneratedRegex(@"\bName\s*=\s*""(?<name>\w+)""")]
    private static partial Regex NameAssignment();
}
