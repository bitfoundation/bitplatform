using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Element;

/// <summary>
/// Pins the contract of the public --bit-Element-* CSS variables, which a bUnit render cannot see: they are read
/// with a fallback and never declared (so they inherit from :root or arrive on the Style of an instance), and the
/// demo page documents every one of them.
/// </summary>
[TestClass]
public class BitElementStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Element-disabled-opacity",
    ];

    [TestMethod]
    public void BitElementShouldReadEveryPublicVariableWithAFallbackAndNeverDeclareIt()
    {
        var stylesheet = StripComments(ReadStylesheet());

        foreach (var variable in PublicVariables)
        {
            Assert.IsTrue(stylesheet.Contains($"var({variable}, "), $"{variable} is never read with a fallback.");
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"(^|[\s;{{]){Regex.Escape(variable)}\s*:", RegexOptions.Multiline), $"{variable} is declared, which stops it from inheriting.");
        }

        var read = Regex.Matches(stylesheet, @"var\((--bit-Element-[A-Za-z0-9_-]+)").Select(m => m.Groups[1].Value).Distinct();

        CollectionAssert.IsSubsetOf(read.ToArray(), PublicVariables);
    }

    [TestMethod]
    public void BitElementShouldDocumentEveryPublicVariableOnTheDemoPage()
    {
        // The demo page's CSS variables table is the only source of these names the site and the MCP server have.
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Utilities", "Element", "BitElementDemo.razor.cs");

        var documented = Regex.Matches(demo, @"Name = ""(--bit-Element-[A-Za-z0-9_-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, documented);
    }

    [TestMethod]
    public void BitElementShouldDimADisabledElementWithTheThemeToken()
    {
        var stylesheet = ReadStylesheet();

        // The fallback is the global disabled opacity, so every preset re-skins the element with no variable set.
        StringAssert.Contains(stylesheet, "opacity: var(--bit-Element-disabled-opacity, #{$opa-dis});");
        StringAssert.Contains(stylesheet, "pointer-events: none;");
    }

    [TestMethod]
    public void BitElementShouldPaintADisabledElementInGrayTextUnderForcedColors()
    {
        var stylesheet = StripComments(ReadStylesheet());

        // Forced colors paint only the form elements the browser disables itself in GrayText.
        StringAssert.Matches(stylesheet, new Regex(@"@media \(forced-colors: active\) \{\s*\.bit-elm\.bit-dis \{\s*color: GrayText;"));
    }

    private static string StripComments(string stylesheet)
    {
        return Regex.Replace(stylesheet, @"//[^\n]*", string.Empty);
    }

    private static string ReadStylesheet()
    {
        return ReadFile("Bit.BlazorUI", "Components", "Utilities", "Element", "BitElement.scss");
    }

    // The path is relative to the BlazorUI folder this test file sits five levels under.
    private static string ReadFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(GetThisFile())!, "..", "..", "..", "..", "..", .. segments]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string GetThisFile([CallerFilePath] string thisFile = "") => thisFile;
}
