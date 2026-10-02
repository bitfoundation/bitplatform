using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AppShell;

/// <summary>
/// Pins the public --bit-AppShell-* variables and the rules of the stylesheet a bUnit render cannot see: every variable
/// the header documents is read with a fallback, none of them is ever declared (so they keep inheriting from :root),
/// nothing is read that the header does not document, the No*Inset flags still win over a safe area a page sets, and
/// the shell prints at the length of its content.
/// </summary>
[TestClass]
public partial class BitAppShellStylesheetTests
{
    [TestMethod]
    public void BitAppShellShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.AreEqual(10, documented.Length, "The stylesheet does not document the ten public variables.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitAppShellShouldNotReadAPublicVariableItDoesNotDocument()
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
    public void BitAppShellShouldNeverDeclareAPublicVariable()
    {
        var body = RulesOf(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-AppShell-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitAppShellShouldReadTheSafeAreasBeforeTheDeviceInsets()
    {
        var stylesheet = ReadStylesheet();

        var root = Block(stylesheet, "\n.bit-ash {");
        StringAssert.Contains(root, "--bit-ash-inset-top: var(--bit-AppShell-safe-area-top, #{$bit-env-inset-top});");
        StringAssert.Contains(root, "--bit-ash-inset-start: var(--bit-AppShell-safe-area-start, #{$bit-env-inset-inline-start});");

        var stable = Block(stylesheet, "\n.bit-ash-sin {");
        StringAssert.Contains(stable, "--bit-ash-inset-bottom: var(--bit-AppShell-safe-area-bottom, #{$bit-env-max-inset-bottom});");
        StringAssert.Contains(stable, "--bit-ash-inset-end: var(--bit-AppShell-safe-area-end, #{$bit-env-max-inset-inline-end});");

        // The flags that take an edge away come after both, so they win over a safe area the page set too.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-ash-sin {", System.StringComparison.Ordinal));
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-ash-nit {", System.StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-ash-nin {", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitAppShellShouldFallEachBarBackToTheBarsAndTheShell()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(Block(stylesheet, "\n.bit-ash {"), "background-color: var(--bit-AppShell-background, #{$clr-bg-pri});");

        foreach (var (rule, edge) in new[] { ("top", "top"), ("bottom", "bottom"), ("left", "start"), ("right", "end") })
        {
            StringAssert.Contains(Block(stylesheet, $"\n.bit-ash-{rule} {{"),
                                  $"background-color: var(--bit-AppShell-inset-{edge}-background, var(--bit-AppShell-inset-background, var(--bit-AppShell-background, #{{$clr-bg-pri}})));");
        }
    }

    [TestMethod]
    public void BitAppShellShouldPrintAtTheLengthOfItsContent()
    {
        var print = Block(ReadStylesheet(), "\n@media print {");

        StringAssert.Contains(Block(print, "\n    .bit-ash {"), "height: auto;");
        StringAssert.Contains(Block(print, "\n    .bit-ash {"), "--bit-ash-inset-top: 0px;");
        StringAssert.Contains(Block(print, "\n    .bit-ash-fsc {"), "position: static;");
        StringAssert.Contains(print, ".bit-ash-main {\n        overflow: visible !important;");
        StringAssert.Contains(print, "display: none;");
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

    // The header comment is where the variables are documented, so only what is not a comment is searched for declarations.
    private static string RulesOf(string stylesheet)
    {
        return string.Join('\n', stylesheet.Split('\n').Where(line => line.TrimStart().StartsWith("//") is false));
    }

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI.Extras", "Components", "AppShell", "BitAppShell.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"^//\s+(--bit-AppShell-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-AppShell-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-AppShell-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
