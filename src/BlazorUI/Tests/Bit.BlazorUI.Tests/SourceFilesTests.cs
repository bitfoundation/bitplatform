using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

/// <summary>
/// Pins how <see cref="SourceFiles"/> picks a stylesheet apart, which every stylesheet test leans on: a misread
/// comment, string or brace there turns into a test that passes or fails against the wrong rule.
/// </summary>
[TestClass]
public class SourceFilesTests
{
    [TestMethod]
    public void StripScssCommentsShouldDropBothKindsOfCommentAndKeepTheLines()
    {
        var stripped = SourceFiles.StripScssComments("/* #fff\n   --bit-X-y: 1; */\n.a { // rgba(0, 0, 0\n    color: red; /* var(--bit-X-y, 0) */\n}");

        Assert.AreEqual("\n\n.a { \n    color: red; \n}", stripped);
    }

    [TestMethod]
    public void StripScssCommentsShouldKeepADoubleSlashInAStringOrAUrl()
    {
        var stylesheet = "a[href^=\"//\"] {\n    background: url(//cdn/x.svg);\n    mask: url(\"//cdn/y.svg\");\n    content: '//';\n}";

        Assert.AreEqual(stylesheet, SourceFiles.StripScssComments(stylesheet));
    }

    [TestMethod]
    public void GetScssBlockShouldSkipAStartMentionedInAComment()
    {
        var stylesheet = "// @media (forced-colors: active) comes last\n.a {\n    color: red;\n}\n@media (forced-colors: active) {\n    .a {\n        color: CanvasText;\n    }\n}";

        Assert.AreEqual("@media (forced-colors: active) {\n    .a {\n        color: CanvasText;\n    }\n}",
                        SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active)"));
    }

    [TestMethod]
    public void GetScssBlockShouldNotCountTheBracesOfAStringOrAComment()
    {
        var stylesheet = ".a {\n    content: \"}\";\n    /* } */\n    // }\n    &::after {\n        content: '{';\n    }\n}\n.b {\n}";

        Assert.AreEqual(stylesheet[..stylesheet.IndexOf("\n.b", System.StringComparison.Ordinal)], SourceFiles.GetScssBlock(stylesheet, ".a {"));
    }

    [TestMethod]
    public void GetScssBlockShouldTakeASelectorHoldingADoubleSlash()
    {
        var stylesheet = "a[href^=\"//\"] {\n    color: red;\n}\n.b {\n}";

        Assert.AreEqual("a[href^=\"//\"] {\n    color: red;\n}", SourceFiles.GetScssBlock(stylesheet, "a[href^="));
    }

    [TestMethod]
    public void GetScssBlockShouldSkipAStartQuotedInAString()
    {
        // A selector quoted in a mixin argument is not the rule of that selector.
        var stylesheet = "@include own('*:not(.a, .b)') {\n    color: red;\n}\n.b {\n    color: blue;\n}";

        Assert.AreEqual(".b {\n    color: blue;\n}", SourceFiles.GetScssBlock(stylesheet, ".b"));
    }

    [TestMethod]
    public void GetScssBlockShouldNotOpenTheBlockAtAnInterpolation()
    {
        var stylesheet = ".a-#{$role} {\n    width: #{$w};\n}\n.b {\n}";

        Assert.AreEqual(".a-#{$role} {\n    width: #{$w};\n}", SourceFiles.GetScssBlock(stylesheet, ".a-"));
    }

    [TestMethod]
    public void GetScssBlockShouldNotCountTheBracesOfAnUnquotedUrl()
    {
        var stylesheet = ".a {\n    background: url(data:image/svg+xml;utf8,<svg>{}}</svg>);\n}\n.b {\n}";

        Assert.AreEqual(stylesheet[..stylesheet.IndexOf("\n.b", System.StringComparison.Ordinal)], SourceFiles.GetScssBlock(stylesheet, ".a {"));
    }

    [TestMethod]
    public void GetScssBlockShouldFailWithTheMessageGiven()
    {
        var failure = Assert.ThrowsExactly<AssertFailedException>(() => SourceFiles.GetScssBlock(".a {\n}", ".b {", "No b."));

        StringAssert.Contains(failure.Message, "No b.");
    }

    [TestMethod]
    public void GetScssDeclarationsShouldLeaveOutANestedRuleWithAnInterpolationInItsHeader()
    {
        var stylesheet = ".a {\n    width: #{$w};\n    &.a-#{$role} {\n        color: red;\n    }\n    height: url(x;y);\n}";

        Assert.AreEqual(".a {\n    width: #{$w};\n    height: url(x;y);\n}", SourceFiles.GetScssDeclarations(stylesheet, ".a {"));
    }

    [TestMethod]
    public void GetScssRulesShouldListEveryRuleWithTheRulesItIsNestedIn()
    {
        var stylesheet = "@media (hover: hover) {\n    /* { */ .a:hover, .b-#{$x} {\n        content: \"{\";\n        &::after {\n        }\n    }\n}\n.c {\n}";

        var rules = SourceFiles.GetScssRules(stylesheet);

        CollectionAssert.AreEqual(new[] { "@media (hover: hover)", ".a:hover, .b-#{$x}", "&::after", ".c" }, rules.Select(r => r.Header).ToArray());
        CollectionAssert.AreEqual(new[] { "@media (hover: hover)", ".a:hover, .b-#{$x}" }, rules[2].Ancestors.ToArray());
        Assert.AreEqual(0, rules[3].Ancestors.Count);
        Assert.AreEqual(stylesheet.IndexOf(".a:hover", System.StringComparison.Ordinal), rules[1].Index);
    }

    [TestMethod]
    public void GetScssDeclarationsShouldLeaveTheNestedRulesOut()
    {
        var stylesheet = ".a {\n    --x: #{$y};\n    // paints on hover\n    &:hover {\n        background-color: red;\n    }\n    @media (forced-colors: active) {\n        color: Highlight;\n    }\n    color: var(--x);\n}";

        Assert.AreEqual(".a {\n    --x: #{$y};\n    color: var(--x);\n}", SourceFiles.GetScssDeclarations(stylesheet, ".a {"));
    }
}
