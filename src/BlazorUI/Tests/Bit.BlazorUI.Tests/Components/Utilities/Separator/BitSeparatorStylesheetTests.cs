using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Separator;

/// <summary>
/// Pins the public --bit-Separator-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from
/// :root and the ancestors), nothing is read that the header does not document, a parameter wins over the variable
/// standing for the same thing, nested separators do not inherit each other's line, and the disabled state holds in a
/// forced-colors mode.
/// </summary>
[TestClass]
public partial class BitSeparatorStylesheetTests
{
    [TestMethod]
    public void BitSeparatorShouldReadEveryPublicVariableItDocuments()
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
    public void BitSeparatorShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitSeparatorShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Separator-* variable is declared, which stops it inheriting.");
    }

    [TestMethod,
        DataRow("--bit-spr-brd"),
        DataRow("--bit-spr-siz"),
        DataRow("--bit-spr-stl"),
        DataRow("--bit-spr-bg"),
        DataRow("--bit-spr-ofs"),
        DataRow("--bit-spr-ins"),
        DataRow("--bit-spr-dis-brd"),
        DataRow("--bit-spr-dis-fg")]
    public void BitSeparatorShouldStartEveryPrivateVariableUnset(string name)
    {
        // A separator nested in the content of another would otherwise inherit its line.
        StringAssert.Contains(Block(ReadStylesheet(), "\n.bit-spr {"), $"{name}: initial;");
    }

    [TestMethod,
        DataRow("var(--bit-spr-stl, var(--bit-Separator-line-style, solid))"),
        DataRow("var(--bit-spr-brd, var(--bit-Separator-color, #{$clr-brd-sec}))"),
        DataRow("var(--bit-spr-siz, var(--bit-Separator-thickness, #{$siz-divider}))"),
        DataRow("var(--bit-spr-ins, var(--bit-Separator-inset, 0))"),
        DataRow("var(--bit-spr-ofs, var(--bit-Separator-content-offset, 0))"),
        DataRow("var(--bit-spr-bg, var(--bit-Separator-content-background, transparent))")]
    public void BitSeparatorShouldLetTheParameterWinOverItsVariable(string chain)
    {
        // The parameter classes and the inline lengths set the private variable, which is read ahead of the public one.
        StringAssert.Contains(ReadStylesheet(), chain);
    }

    [TestMethod]
    public void BitSeparatorShouldGiveSolidAClassOfItsOwn()
    {
        // An explicit Solid has to beat a dashed line-style variable set on an ancestor, so it is not the unset default.
        StringAssert.Contains(ReadStylesheet(), ".bit-spr-sld {\n    --bit-spr-stl: solid;");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepADisabledSeparatorInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var block = Block(stylesheet, "\n    &.bit-dis {");
        StringAssert.Contains(block, "--bit-spr-dis-brd: #{$clr-brd-dis};");
        StringAssert.Contains(block, "--bit-spr-dis-fg: #{$clr-fg-dis};");

        // The disabled colors come first in each chain, ahead of the parameters and the public variables.
        StringAssert.Contains(stylesheet, "var(--bit-spr-dis-brd, var(--bit-spr-brd, ");
        StringAssert.Contains(stylesheet, "color: var(--bit-spr-dis-fg, var(--bit-Separator-content-color, ");
    }

    [TestMethod]
    public void BitSeparatorShouldKeepTheDisabledStateInForcedColors()
    {
        var block = Block(ReadStylesheet(), "\n@media (forced-colors: active) {");

        StringAssert.Contains(block, "border-color: GrayText;");
        StringAssert.Contains(block, "color: GrayText;");
    }

    [TestMethod]
    public void BitSeparatorShouldInsetTheLineLogically()
    {
        var stylesheet = ReadStylesheet();

        // Logical padding takes one length for both ends or two for the start and the end, in the reading direction.
        StringAssert.Contains(Block(stylesheet, "\n.bit-spr-hrz {"), "padding-inline: var(--bit-spr-ins, ");
        StringAssert.Contains(Block(stylesheet, "\n.bit-spr-vrt {"), "padding-block: var(--bit-spr-ins, ");
    }

    [TestMethod]
    public void BitSeparatorShouldLetTheContentShrinkAndWrap()
    {
        var block = Block(ReadStylesheet(), "\n.bit-spr-cnt {");

        StringAssert.Contains(block, "min-width: 0;");
        StringAssert.Contains(block, "overflow-wrap: break-word;");
    }

    private static string Block(string stylesheet, string opening)
    {
        var start = stylesheet.IndexOf(opening, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"No rule opens with {opening.Trim()}.");

        var indent = opening[1..].Length - opening[1..].TrimStart().Length;
        var end = stylesheet.IndexOf("\n" + new string(' ', indent) + "}", start + opening.Length, System.StringComparison.Ordinal);

        return stylesheet[start..end];
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The header comment is where the variables are documented, so only what follows it is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Utilities", "Separator", "BitSeparator.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Separator-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Separator-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Separator-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
