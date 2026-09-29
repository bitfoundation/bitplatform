using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Icon;

/// <summary>
/// Pins the public --bit-Icon-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read somewhere with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), and nothing is read that the header does not document.
/// </summary>
[TestClass]
public partial class BitIconStylesheetTests
{
    [TestMethod]
    public void BitIconShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitIconShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitIconShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Icon-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitIconShouldLetAColorParameterWinOverThePublicColors()
    {
        var stylesheet = ReadStylesheet();

        // The role a Color parameter sets comes first, then the public variable, then the primary role an icon given no
        // Color is painted in.
        StringAssert.Contains(stylesheet, "$ico-clr: var(--bit-ico-clr, var(--bit-Icon-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "$ico-clr-txt: var(--bit-ico-clr-txt, var(--bit-Icon-contrast-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "$ico-clr-hover: var(--bit-ico-clr-hover, var(--bit-Icon-hover-color, var(--bit-Icon-color, ");
        StringAssert.Contains(stylesheet, "$ico-clr-active: var(--bit-ico-clr-active, var(--bit-Icon-active-color, var(--bit-Icon-hover-color, var(--bit-Icon-color, ");

        // The ring has to be seen against the page rather than match the icon, so the variable wins over the role.
        StringAssert.Contains(stylesheet, "$ico-clr-focus: var(--bit-Icon-focus-color, var(--bit-ico-clr-focus, ");
        StringAssert.Contains(stylesheet, "@include focus-ring($ico-clr-focus);");

        // A disabled icon never reads a public color, so it never looks enabled.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\$ico-clr-dis[a-z-]*: [^\n]*--bit-Icon-"), "A disabled color reads a public variable.");
    }

    [TestMethod]
    public void BitIconShouldPaintEveryVariantThroughTheChains()
    {
        var rules = RulesOf(ReadStylesheet());

        // The chains are the only place the private role variables are read, so no rule can skip the public variable
        // an icon given no Color is meant to be painted in.
        var direct = Regex.Matches(rules, @"var\(--bit-ico-clr[a-z-]*\)");

        Assert.AreEqual(0, direct.Count, "A rule reads a private color variable without the public fallback.");
    }

    [TestMethod]
    public void BitIconShouldReadItsBoxAndSizeFromThePublicVariables()
    {
        var root = Block(ReadStylesheet(), ".bit-ico");

        StringAssert.Contains(root, "font-size: var(--bit-Icon-size, #{$siz-icon-md});");
        StringAssert.Contains(root, "padding: var(--bit-Icon-padding, #{spacing(0.5)});");
        StringAssert.Contains(root, "border-radius: var(--bit-Icon-radius, #{$shp-radius-control});");
        StringAssert.Contains(root, "border-width: var(--bit-Icon-border-width, #{$shp-border-width});");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitIconShouldTakeAnExplicitSizeFromTheTheme(string size)
    {
        // A Size parameter is the more specific ask, so its class wins over --bit-Icon-size by being written later.
        StringAssert.Contains(Block(ReadStylesheet(), $".bit-ico-{size}"), $"font-size: $siz-icon-{size};");
    }

    [TestMethod]
    public void BitIconShouldKeepItsOwnStateAwayFromAnIconNestedInIt()
    {
        var stylesheet = ReadStylesheet();

        var root = Block(stylesheet, ".bit-ico");

        // Every private variable a class or an inline style sets would otherwise be inherited by an icon drawn inside
        // this one's ChildContent - its role, its turn and its timing.
        var set = PrivateDeclaration().Matches(RulesOf(stylesheet))
                                      .Select(m => m.Groups[1].Value)
                                      .Concat(["--bit-ico-anm-dur", "--bit-ico-anm-dly"]) // written inline by the component
                                      .Distinct()
                                      .ToArray();

        Assert.IsTrue(set.Length > 0, "No private variable is set by a class.");

        foreach (var name in set)
        {
            StringAssert.Contains(root, $"{name}: initial;", $"{name} is not reset on the root, so a nested icon inherits it.");
        }
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string Block(string stylesheet, string selector)
    {
        var start = stylesheet.IndexOf($"\n{selector} {{", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{selector} has no rule of its own.");

        return stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];
    }

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Icon", "BitIcon.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Icon-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Icon-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Icon-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    // A private variable given a value other than the reset, anywhere in the rules.
    [GeneratedRegex(@"^\s*(--bit-ico-[a-z0-9-]+):\s*(?!initial;)", RegexOptions.Multiline)]
    private static partial Regex PrivateDeclaration();
}
