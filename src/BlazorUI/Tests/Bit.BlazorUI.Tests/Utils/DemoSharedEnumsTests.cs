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
/// once - and the MCP server answers from the same tables. The factories are compile-linked into this project and
/// called, so every table is checked however its factory happens to be written.
/// <para>
/// The members and values of a table are read off its enum, so what is left to pin is the prose, which is a copy of
/// the enum's XML documentation, and the anchor id, which no page may give to another type.
/// </para>
/// </summary>
[TestClass]
public partial class DemoSharedEnumsTests
{
    private static readonly MethodInfo[] Factories = [.. typeof(DemoSharedEnums).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                .Where(m => m.ReturnType == typeof(ComponentSubEnum) && m.Name != nameof(DemoSharedEnums.Only))];

    [TestMethod]
    public void EverySharedTableShouldListTheMembersOfTheEnumItIsNamedAfter()
    {
        Assert.IsTrue(Factories.Length > 0, "No shared table was found.");

        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.AreEqual(factory.Name, table.Name, $"The {factory.Name} factory builds a table named after another type.");

            var type = typeof(BitColor).Assembly.GetType($"Bit.BlazorUI.{factory.Name}");

            Assert.IsNotNull(type, $"{factory.Name} is not an enum of Bit.BlazorUI.");
            Assert.IsTrue(type.IsEnum, $"{factory.Name} is not an enum.");

            CollectionAssert.AreEqual(Enum.GetNames(type), table.Items.Select(i => i.Name).ToArray(), $"The shared {factory.Name} table does not list the members of the enum.");
        }
    }

    [TestMethod]
    public void EverySharedTableShouldDescribeItselfAndEachOfItsMembers()
    {
        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.IsFalse(string.IsNullOrWhiteSpace(table.Description), $"The shared {factory.Name} table has no description.");

            foreach (var item in table.Items)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(item.Description), $"The {item.Name} member of the shared {factory.Name} table has no description.");
            }
        }
    }

    [TestMethod]
    public void EverySharedTableShouldSayWhatTheXmlDocumentationOfItsEnumSays()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Bit.BlazorUI.xml");

        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library's XML documentation is copied to output.");

        var summaries = XDocument.Load(path)
                                 .Descendants("member")
                                 .Where(m => m.Element("summary") is not null)
                                 .ToDictionary(m => m.Attribute("name")!.Value, m => Flatten(m.Element("summary")!), StringComparer.Ordinal);

        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.AreEqual(summaries.GetValueOrDefault($"T:Bit.BlazorUI.{table.Name}"), table.Description,
                $"The shared {table.Name} table and the summary of the enum describe it differently.");

            foreach (var item in table.Items)
            {
                Assert.AreEqual(summaries.GetValueOrDefault($"F:Bit.BlazorUI.{table.Name}.{item.Name}"), item.Description,
                    $"The shared {table.Name} table and the summary of {table.Name}.{item.Name} describe the member differently.");
            }
        }
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

        var pages = Path.Combine(AppContext.BaseDirectory, "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components");

        Assert.IsTrue(Directory.Exists(pages), $"Missing {pages}; ensure the demo pages are copied to output.");

        var files = Directory.GetFiles(pages, "*.razor.cs", SearchOption.AllDirectories);

        Assert.IsTrue(files.Length > 0, "No demo page was found.");

        foreach (var file in files)
        {
            foreach (Match table in PageOwnTable().Matches(File.ReadAllText(file)))
            {
                var (id, name) = (table.Groups["id"].Value, table.Groups["name"].Value);
                var page = Path.GetFileName(file);

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

    // A summary as the plain text a table shows: a link becomes its url, a cref the name it points at, and
    // every run of whitespace a single space.
    private static string Flatten(XElement summary)
    {
        return string.Join(' ', string.Concat(summary.Nodes().Select(Text)).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        static string Text(XNode node) => node switch
        {
            XText text => text.Value,
            XElement { Name.LocalName: "see" or "seealso" } see => see.Attribute("href")?.Value
                                                                   ?? see.Attribute("langword")?.Value
                                                                   ?? see.Attribute("cref")?.Value.Split('.')[^1]
                                                                   ?? string.Concat(see.Nodes().Select(Text)),
            XElement element => string.Concat(element.Nodes().Select(Text)),
            _ => string.Empty,
        };
    }

    [GeneratedRegex(@"Id = ""(?<id>[\w-]+)"",\s*Name = ""(?<name>\w+)""")]
    private static partial Regex PageOwnTable();
}
