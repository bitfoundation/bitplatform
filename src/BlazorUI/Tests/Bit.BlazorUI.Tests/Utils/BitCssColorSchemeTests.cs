using System;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the color-scheme utilities (BitCss.Class.ColorScheme, bit-css.scss) and the scheme classes of BitImage's
/// DarkSrc, which share one condition (dark-scheme() in functions.scss): read off the compiled core bundle and
/// matched against pages whose document, scoped regions and the element itself carry bit-theme in every way the
/// condition tells apart, so the element of each class is hidden exactly where the scheme of its nearest bit-theme
/// is the other one.
/// </summary>
[TestClass]
public partial class BitCssColorSchemeTests
{
    [TestMethod]
    public void BitCssShouldNameTheColorSchemeClassesOfTheStylesheet()
    {
        Assert.AreEqual("bit-css-dark-only", BitCss.Class.ColorScheme.DarkOnly);
        Assert.AreEqual("bit-css-light-only", BitCss.Class.ColorScheme.LightOnly);

        var bundle = ReadBundle();

        StringAssert.Contains(bundle, $".{BitCss.Class.ColorScheme.DarkOnly}:not(");
        StringAssert.Contains(bundle, $".{BitCss.Class.ColorScheme.LightOnly}:is(");
    }

    [TestMethod,
        // The document alone.
        DataRow(null, null, false),
        DataRow("light", null, false),
        DataRow("fluent", null, false),
        DataRow("dark", null, true),
        DataRow("fluent-dark", null, true),
        DataRow("acme-dark", null, true),
        // A scoped region, which wins over the document.
        DataRow(null, "dark", true),
        DataRow("light", "material-dark", true),
        DataRow("dark", "light", false),
        DataRow("dark", "fluent2-light", false),
        DataRow("dark", "dark light dark", true),
        DataRow("light", "dark light", false),
        DataRow("light", "dark light dark", true),
        DataRow(null, "dark light dark", true),
        // The document's own theme counts as a level of the nesting on a dark page.
        DataRow("dark", "light dark light", false),
        DataRow("dark", "light dark", true)
    ]
    public void TheColorSchemeClassesShouldFollowTheNearestTheme(string? document, string? regions, bool dark)
    {
        var bundle = ReadBundle();

        var rootTheme = document is null ? string.Empty : $@" bit-theme=""{document}""";
        var open = string.Concat((regions?.Split(' ') ?? []).Select(r => $@"<section bit-theme=""{r}"">"));
        var close = string.Concat((regions?.Split(' ') ?? []).Select(_ => "</section>"));

        var html = $@"<!DOCTYPE html><html{rootTheme}><body>{open}
                      <div id=""css-dark"" class=""bit-css-dark-only""></div>
                      <div id=""css-light"" class=""bit-css-light-only""></div>
                      <img id=""img-dark"" class=""bit-img-img bit-img-dsc"" />
                      <img id=""img-light"" class=""bit-img-img bit-img-lsc"" />
                      {close}</body></html>";

        var page = new HtmlParser().ParseDocument(html);

        AssertHidden(page, bundle, "css-dark", ".bit-css-dark-only", hidden: dark is false);
        AssertHidden(page, bundle, "css-light", ".bit-css-light-only", hidden: dark);
        AssertHidden(page, bundle, "img-dark", ".bit-img-dsc", hidden: dark is false);
        AssertHidden(page, bundle, "img-light", ".bit-img-lsc", hidden: dark);
    }

    [TestMethod]
    public void TheColorSchemeClassesShouldReadTheThemeOfTheElementItself()
    {
        var bundle = ReadBundle();

        var page = new HtmlParser().ParseDocument(@"<!DOCTYPE html><html bit-theme=""light""><body>
                                                    <div id=""css-light"" class=""bit-css-light-only"" bit-theme=""dark""></div>
                                                    <div id=""css-dark"" class=""bit-css-dark-only"" bit-theme=""dark""></div>
                                                    </body></html>");

        AssertHidden(page, bundle, "css-light", ".bit-css-light-only", hidden: true);
        AssertHidden(page, bundle, "css-dark", ".bit-css-dark-only", hidden: false);
    }

    private static void AssertHidden(AngleSharp.Html.Dom.IHtmlDocument page, string bundle, string id, string className, bool hidden)
    {
        var selector = HidingSelector(bundle, className);
        var element = page.GetElementById(id)!;

        Assert.IsTrue(element.ClassList.Contains(className[1..]));

        // The selector engine of the tests reads no complex selector inside :is() or :not(), so the condition is
        // walked here, its plain descendant chains handed to the engine: the class, then either the condition
        // itself (hidden while dark) or :not() of it (hidden unless dark).
        var condition = selector[className.Length..];
        var matched = condition.StartsWith(":not(", StringComparison.Ordinal)
                        ? Matches(page, element, condition[5..^1]) is false
                        : Matches(page, element, condition);

        Assert.AreEqual(hidden, matched, $"#{id} should be {(hidden ? "hidden" : "shown")} by {selector}");
    }

    // A condition of dark-scheme(): ":is(X, X *)", optionally followed by ":not(<condition>)" - the element is X or
    // inside an X, and not under the condition that follows.
    private static bool Matches(AngleSharp.Html.Dom.IHtmlDocument page, AngleSharp.Dom.IElement element, string condition)
    {
        Assert.IsTrue(condition.StartsWith(":is(", StringComparison.Ordinal), condition);

        var end = ClosingParenthesis(condition, 3);
        var alternatives = condition[4..end].Split(',').Select(a => a.Trim()).ToArray();

        Assert.AreEqual(2, alternatives.Length, condition);
        Assert.AreEqual($"{alternatives[0]} *", alternatives[1], condition);

        var inside = page.QuerySelectorAll(alternatives[0]).Any(region => region.Contains(element));
        var rest = condition[(end + 1)..];

        if (rest.Length == 0) return inside;

        Assert.IsTrue(rest.StartsWith(":not(", StringComparison.Ordinal) && rest.EndsWith(')'), rest);

        return inside && Matches(page, element, rest[5..^1]) is false;
    }

    private static int ClosingParenthesis(string text, int open)
    {
        var depth = 0;

        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')' && --depth == 0) return i;
        }

        Assert.Fail($"Unbalanced: {text}");
        return -1;
    }

    // The selector of the one rule that takes an element of the class out of the layout.
    private static string HidingSelector(string bundle, string className)
    {
        var rules = HidingRule().Matches(bundle)
                                .Select(m => m.Groups["selector"].Value.Trim())
                                .Where(s => s.StartsWith(className + ":", StringComparison.Ordinal))
                                .ToList();

        Assert.AreEqual(1, rules.Count, $"{className} should be hidden by exactly one rule.");

        return rules[0];
    }

    private static string ReadBundle() => SourceFiles.Read("Bit.BlazorUI", "wwwroot", "styles", "bit.blazorui.css");

    // !important, so an inline display (a Styles part, a style attribute) cannot bring the other scheme's copy back.
    [GeneratedRegex(@"(?<selector>[^{}]+)\{\s*display:\s*none\s*!important;?\s*\}")]
    private static partial Regex HidingRule();
}
