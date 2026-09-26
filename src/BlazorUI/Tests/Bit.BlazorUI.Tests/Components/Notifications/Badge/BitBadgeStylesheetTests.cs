using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Badge;

/// <summary>
/// Pins the public --bit-Badge-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read somewhere with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), and nothing is read that the header does not document.
/// </summary>
[TestClass]
public partial class BitBadgeStylesheetTests
{
    [TestMethod]
    public void BitBadgeShouldReadEveryPublicVariableItDocuments()
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
    public void BitBadgeShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitBadgeShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Badge-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitBadgeShouldKeepADisabledBadgeInTheDisabledColors()
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf("\n.bit-bdg.bit-dis .bit-bdg-ctn {", System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "The disabled badge has no rule of its own.");

        var block = stylesheet[start..stylesheet.IndexOf("\n}", start, System.StringComparison.Ordinal)];

        // The disabled colors are read directly, never through the public color variables, so a re-tinted badge that
        // is disabled still reads as disabled.
        StringAssert.Contains(block, "color: var(--bit-bdg-cnt-clr-txt);");
        StringAssert.Contains(block, "background-color: var(--bit-bdg-cnt-clr-bg);");
        Assert.IsFalse(block.Contains("--bit-Badge-"));
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
                                                 "Bit.BlazorUI", "Components", "Notifications", "Badge", "BitBadge.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-Badge-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Badge-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Badge-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
