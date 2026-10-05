using System;
using System.Linq;
using System.Reflection;
using Bit.BlazorUI.Demo.Client.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the enum tables the demo pages share (<c>DemoSharedEnums</c>) to the enums they document. Written once,
/// a table is shown on every page that names the type, so a member it leaves out or numbers wrongly is wrong on
/// all of them at once - and the MCP server answers from the same tables. The factories are compile-linked into
/// this project and called, so every table is checked however its factory happens to be written.
/// </summary>
[TestClass]
public class DemoSharedEnumsTests
{
    private static readonly MethodInfo[] Factories = [.. typeof(DemoSharedEnums).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                .Where(m => m.ReturnType == typeof(ComponentSubEnum) && m.Name != nameof(DemoSharedEnums.Only))];

    [TestMethod]
    public void EverySharedTableShouldListTheMembersOfItsEnumInOrder()
    {
        Assert.IsTrue(Factories.Length > 0, "No shared table was found.");

        foreach (var factory in Factories)
        {
            var table = Build(factory);

            Assert.AreEqual(factory.Name, table.Name, $"The {factory.Name} factory builds a table named after another type.");

            var type = typeof(BitColor).Assembly.GetType($"Bit.BlazorUI.{factory.Name}");

            Assert.IsNotNull(type, $"{factory.Name} is not an enum of Bit.BlazorUI.");
            Assert.IsTrue(type.IsEnum, $"{factory.Name} is not an enum.");

            var documented = table.Items.Select(i => $"{i.Name}={i.Value}").ToArray();

            var declared = Enum.GetNames(type)
                               .Select(n => $"{n}={Convert.ToInt64(Enum.Parse(type, n))}")
                               .ToArray();

            CollectionAssert.AreEqual(declared, documented, $"The shared {factory.Name} table has drifted from the enum.");
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

    private static ComponentSubEnum Build(MethodInfo factory)
        => (ComponentSubEnum)factory.Invoke(null, [.. factory.GetParameters().Select(p => p.DefaultValue)])!;
}
