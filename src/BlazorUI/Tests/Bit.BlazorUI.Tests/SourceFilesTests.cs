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
    public void GetScssDeclarationsShouldLeaveTheNestedRulesOut()
    {
        var stylesheet = ".a {\n    --x: #{$y};\n    // paints on hover\n    &:hover {\n        background-color: red;\n    }\n    @media (forced-colors: active) {\n        color: Highlight;\n    }\n    color: var(--x);\n}";

        Assert.AreEqual(".a {\n    --x: #{$y};\n    color: var(--x);\n}", SourceFiles.GetScssDeclarations(stylesheet, ".a {"));
    }
}
