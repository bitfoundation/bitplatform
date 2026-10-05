using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the enum tables the demo pages share (<c>DemoSharedEnums</c>) to the enums they document. Written once,
/// a table is shown on every page that names the type, so a member it leaves out or numbers wrongly is wrong on
/// all of them at once - and the MCP server answers from the same tables.
/// </summary>
[TestClass]
public class DemoSharedEnumsTests
{
    [TestMethod]
    public void EverySharedTableShouldListTheMembersOfItsEnumInOrder()
    {
        var source = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Models", "DemoSharedEnums.cs");

        var tables = Regex.Matches(source, @"public static ComponentSubEnum (\w+)\([^)]*\) => new\(\)(?:(?!public static).)*", RegexOptions.Singleline);

        Assert.IsTrue(tables.Count > 0, "No shared table was found.");

        foreach (Match table in tables)
        {
            var name = table.Groups[1].Value;

            StringAssert.Contains(table.Value, $"Name = \"{name}\",", $"The {name} factory builds a table named after another type.");

            var type = typeof(BitColor).Assembly.GetType($"Bit.BlazorUI.{name}");

            Assert.IsNotNull(type, $"{name} is not an enum of Bit.BlazorUI.");
            Assert.IsTrue(type.IsEnum, $"{name} is not an enum.");

            var documented = Regex.Matches(table.Value, @"new\(\) \{ Name = ""(\w+)"", Value = ""(\d+)""")
                                  .Select(m => $"{m.Groups[1].Value}={m.Groups[2].Value}")
                                  .ToArray();

            var declared = Enum.GetNames(type)
                               .Select(n => $"{n}={Convert.ToInt64(Enum.Parse(type, n))}")
                               .ToArray();

            CollectionAssert.AreEqual(declared, documented, $"The shared {name} table has drifted from the enum.");
        }
    }

    private static string ReadFile(params string[] segments) => ReadFileFrom(segments);

    private static string ReadFileFrom(string[] segments, [CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(thisFile)!, "..", "..", "..", .. segments]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
