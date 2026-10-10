using System;
using System.Linq;
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
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            var read = name.EndsWith("-focus-color", StringComparison.Ordinal) ? $"var({name})" : $"var({name}, ";

            StringAssert.Contains(stylesheet, read, $"{name} is documented but never read as it should be.");
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
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Icon-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitIconShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Color publishes the role, which is read before the public variable; the variable only restyles
        // the primary role an unset Color stands for, and so does the shade of an app's own --bit-Icon-color.
        StringAssert.Contains(stylesheet, "$ico-clr: var(--bit-ico-clr, var(--bit-Icon-color, #{$clr-pri}));");
        StringAssert.Contains(stylesheet, "$ico-clr-txt: var(--bit-ico-clr-txt, var(--bit-Icon-contrast-color, #{$clr-pri-text}));");
        StringAssert.Contains(stylesheet, "$ico-clr-hover: var(--bit-ico-clr-hover, var(--bit-Icon-hover-color, var(--bit-ico-pub-hover, #{$clr-pri-hover})));");
        StringAssert.Contains(stylesheet, "$ico-clr-active: var(--bit-ico-clr-active, var(--bit-Icon-active-color, var(--bit-ico-pub-active, #{$clr-pri-active})));");
        StringAssert.Contains(stylesheet, "$ico-clr-focus: var(--bit-ico-clr-focus, var(--bit-Icon-focus-color));");
        StringAssert.Contains(stylesheet, "@include focus-ring-own($ico-clr-focus);");

        // So does an explicit Size, for the size of the glyph.
        StringAssert.Contains(stylesheet, "font-size: var(--bit-ico-size, var(--bit-Icon-size, #{$siz-icon-md}));");

        // The only private values read after a public variable are the shades of that variable itself, which no
        // parameter publishes.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Icon-[a-z-]+, var\(--bit-ico-(?!pub-)"), "A public variable is read before the parameter it restyles the default of.");

        // A disabled icon never reads a public color, so it never looks enabled.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"\$ico-clr-dis[a-z-]*: [^\n]*--bit-Icon-"), "A disabled color reads a public variable.");
    }

    [TestMethod]
    public void BitIconShouldShadeAPublicColorForHoverAndPress()
    {
        var clickable = Block(ReadStylesheet(), ".bit-ico-int");

        // An app that sets --bit-Icon-color alone would otherwise see the same color at rest, under the pointer and
        // pressed. Neither shade has a fallback, so it is unset - and the role's own shade is read - while the public
        // variables are.
        StringAssert.Contains(clickable, "--bit-ico-pub-hover: color-mix(in srgb, var(--bit-Icon-color) 85%, ");
        StringAssert.Contains(clickable, "--bit-ico-pub-active: color-mix(in srgb, var(--bit-Icon-hover-color, var(--bit-ico-pub-hover)) 85%, ");
    }

    [TestMethod]
    public void BitIconShouldPaintEveryVariantThroughTheChains()
    {
        var rules = SourceFiles.StripScssComments(ReadStylesheet());

        // The chains are the only place the private role variables are read, so no rule can skip the public variable
        // an icon given no Color is meant to be painted in.
        var direct = Regex.Matches(rules, @"var\(--bit-ico-clr[a-z-]*\)");

        Assert.AreEqual(0, direct.Count, "A rule reads a private color variable without the public fallback.");
    }

    [TestMethod]
    public void BitIconShouldReadItsBoxAndSizeFromThePublicVariables()
    {
        var root = Block(ReadStylesheet(), ".bit-ico");

        StringAssert.Contains(root, "font-size: var(--bit-ico-size, var(--bit-Icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(root, "padding: var(--bit-Icon-padding, #{spacing(0.5)});");
        StringAssert.Contains(root, "border-radius: var(--bit-Icon-radius, #{$shp-radius-control});");
        StringAssert.Contains(root, "border-width: var(--bit-Icon-border-width, #{$shp-border-width});");
    }

    [TestMethod]
    public void BitIconShouldDrawTheThemedFocusRingOnEveryFocusableIcon()
    {
        // A TabIndex makes an icon without a handler focusable too - the anchor of a tooltip - so the ring is drawn on
        // the root rather than only on the clickable one.
        var root = Block(ReadStylesheet(), ".bit-ico");

        StringAssert.Contains(root, "&:focus-visible {");
        StringAssert.Contains(root, "@include focus-ring-own($ico-clr-focus);");
        StringAssert.DoesNotMatch(Block(ReadStylesheet(), ".bit-ico-int"), new Regex("focus-ring"));
    }

    [TestMethod]
    public void BitIconShouldKeepTheFixedWidthForTheGlyphUnderABorderBoxReset()
    {
        // Under the `* { box-sizing: border-box }` of most resets, the padding and border of a Fill or an Outline
        // icon would otherwise be taken out of the fixed width and the glyph would spill out of its box.
        var fixedWidth = Block(ReadStylesheet(), ".bit-ico-fxw");

        StringAssert.Contains(fixedWidth, "width: var(--bit-Icon-fixed-width, 1.25em);");
        StringAssert.Contains(fixedWidth, "box-sizing: content-box;");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitIconShouldTakeTheDefaultOfItsSizeFromTheTheme(string size)
    {
        // A Size parameter publishes the private size the root reads ahead of --bit-Icon-size, the same as a Color does
        // for the colors - and only that, so the one rule sizing the glyph keeps the weight a Classes class competes with.
        var block = Block(ReadStylesheet(), $".bit-ico-{size}");

        StringAssert.Contains(block, $"--bit-ico-size: #{{$siz-icon-{size}}};");
        Assert.IsFalse(block.Contains("font-size"), $".bit-ico-{size} sets the font size itself rather than the private size the root reads.");
    }

    [TestMethod]
    public void BitIconShouldKeepItsOwnStateAwayFromAnIconNestedInIt()
    {
        var stylesheet = ReadStylesheet();

        // Every private variable a class or an inline style sets would otherwise be inherited by an icon drawn inside
        // this one's ChildContent - its role, its size, its turn and its timing - so each has to be in the list the
        // stylesheet registers as non-inheriting (and resets on the root, for a browser without @property).
        var set = PrivateDeclaration().Matches(SourceFiles.StripScssComments(stylesheet))
                                      .Concat(InlinePrivateDeclaration().Matches(ReadComponentSource()))
                                      .Select(m => m.Groups[1].Value)
                                      .Distinct()
                                      .ToArray();

        Assert.IsTrue(set.Length > 0, "No private variable is set by a class.");
        CollectionAssert.Contains(set, "--bit-ico-anm-itr", "The variables written inline by the component are not found.");

        var registered = PrivateProperties(stylesheet);

        foreach (var name in set)
        {
            CollectionAssert.Contains(registered, name, $"{name} is not in $ico-private-properties, so a nested icon inherits it.");
        }
    }

    [TestMethod]
    public void BitIconShouldRegisterItsPrivateVariablesAsNonInheriting()
    {
        var stylesheet = ReadStylesheet();

        Assert.IsTrue(PrivateProperties(stylesheet).Length > 0, "$ico-private-properties lists nothing.");

        StringAssert.Contains(stylesheet, """
            @each $name in $ico-private-properties {
                @property --bit-ico-#{$name} {
                    syntax: "*";
                    inherits: false;
                }
            }
            """.Replace("\r\n", "\n"));

        StringAssert.Contains(Block(stylesheet, ".bit-ico"), """
                @each $name in $ico-private-properties {
                    --bit-ico-#{$name}: initial;
                }
            """.Replace("\r\n", "\n"));
    }

    private static string[] PrivateProperties(string stylesheet)
    {
        var list = PrivatePropertyList().Match(stylesheet);

        Assert.IsTrue(list.Success, "The stylesheet has no $ico-private-properties list.");

        return list.Groups[1].Value.Split([',', ' ', '\n'], System.StringSplitOptions.RemoveEmptyEntries)
                                   .Select(name => $"--bit-ico-{name}")
                                   .ToArray();
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string Block(string stylesheet, string selector)
    {
        return SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
    }

    private static string ReadComponentSource() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Icon", "BitIcon.razor.cs");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Utilities", "Icon", "BitIcon.scss");

    [GeneratedRegex(@"^//\s+(--bit-Icon-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Icon-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Icon-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    // A private variable given a value other than the reset, anywhere in the rules. The lookahead sits right after
    // the colon, where no whitespace is left for the pattern to backtrack into and slip past it.
    [GeneratedRegex(@"^\s*(--bit-ico-[a-z0-9-]+):(?!\s*initial;)", RegexOptions.Multiline)]
    private static partial Regex PrivateDeclaration();

    // A private variable the component writes into its inline style.
    [GeneratedRegex(@"\$""(--bit-ico-[a-z0-9-]+):")]
    private static partial Regex InlinePrivateDeclaration();

    [GeneratedRegex(@"^\$ico-private-properties:\s*([^;]+);", RegexOptions.Multiline)]
    private static partial Regex PrivatePropertyList();
}
