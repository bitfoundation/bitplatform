using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Pins the public --bit-FullCalendar-* variables and the rules of the stylesheet a bUnit render cannot see: every
/// variable the header documents is read with a fallback, none is ever declared (so they keep inheriting from :root),
/// nothing is read that the header does not document, and the calendar has a forced-colors and a print treatment.
/// </summary>
[TestClass]
public partial class BitFullCalendarStylesheetTests
{
    [TestMethod]
    public void BitFullCalendarShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(23, documented.Length, "The stylesheet does not document the twenty-three public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitFullCalendarShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitFullCalendarShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-FullCalendar-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitFullCalendarShouldHaveForcedColorsAndPrintTreatments()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, "@media (forced-colors: active) {");
        StringAssert.Contains(stylesheet, "@media print {");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotHardCodeWhiteOrALiteralShadow()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(body.Contains("#fff"), "A literal white is used instead of a theme token.");
        Assert.IsFalse(Regex.IsMatch(body, @"rgba\(\s*0\s*,\s*0\s*,\s*0"), "A literal black shadow is used instead of an elevation token.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    // The header comment is where the variables are documented, so only what is not a comment is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI.Extras", "Components", "FullCalendar", "BitFullCalendar.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-FullCalendar-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-FullCalendar-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-FullCalendar-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
