using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

/// <summary>
/// Pins the public --bit-Persona-* custom properties, which a bUnit render cannot see: they are read off the
/// stylesheet with a fallback and never declared there, so they inherit from :root, an ancestor or the Style of an
/// instance, and the list at the head of the stylesheet names exactly the ones it reads.
/// </summary>
[TestClass]
public class BitPersonaStylesheetTests
{
    private static readonly string[] _publicVariables =
    [
        "--bit-Persona-gap",
        "--bit-Persona-primary-color",
        "--bit-Persona-primary-font-weight",
        "--bit-Persona-secondary-color",
        "--bit-Persona-disabled-color",
        "--bit-Persona-coin-background",
        "--bit-Persona-coin-color",
        "--bit-Persona-coin-radius",
        "--bit-Persona-overlay-background",
        "--bit-Persona-overlay-color",
        "--bit-Persona-focus-color",
        "--bit-Persona-presence-border-color",
        "--bit-Persona-ring-color",
        "--bit-Persona-ring-width",
        "--bit-Persona-ring-gap",
        "--bit-Persona-ring-gap-color",
        "--bit-Persona-active-shadow",
        "--bit-Persona-inactive-opacity",
        "--bit-Persona-inactive-scale",
    ];

    [TestMethod]
    public void BitPersonaStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitPersonaStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Persona-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPersonaStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Persona-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Persona-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read, documented);
    }

    [TestMethod]
    public void BitPersonaSecondaryRowsShouldReadQuieterThanTheName()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "color: var(--bit-Persona-secondary-color, #{$clr-fg-sec});");
        StringAssert.Contains(rules, "color: var(--bit-Persona-primary-color, #{$clr-fg-pri});");
    }

    [TestMethod]
    public void BitPersonaVerticalLayoutShouldStackTheCoinOverTheDetails()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, ".bit-prs-vrt {\n    --bit-prs-flex-direction: column;");
        StringAssert.Contains(rules, "--bit-prs-flex-direction: column-reverse;");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-prs {")..];

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-prs {")];

    private static string ReadStylesheet([CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..",
                                                 "Bit.BlazorUI", "Components", "Notifications", "Persona", "BitPersona.scss"));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
