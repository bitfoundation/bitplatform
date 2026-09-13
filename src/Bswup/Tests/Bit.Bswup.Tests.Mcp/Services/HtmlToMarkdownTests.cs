using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The flattening that turns a documentation page written for humans into the Markdown an MCP
/// client puts in front of a model. Two things have to hold: nothing an agent needs (a code
/// sample, a table of defaults, a link target) may be lost, and nothing it cannot use (highlighting
/// spans, grid wrappers, copy buttons) may survive - the page is served under a context budget.
/// </summary>
[TestClass]
public class HtmlToMarkdownTests
{
    private static string Markdown(string html) => html.ToMarkdown();

    // -- Structure -------------------------------------------------------------

    [TestMethod]
    public void ToMarkdown_WritesHeadingsAtTheirLevel()
    {
        var markdown = Markdown("<h1>Title</h1><h2>Section</h2><h3>Sub</h3><h6>Deep</h6>");

        StringAssert.Contains(markdown, "# Title");
        StringAssert.Contains(markdown, "## Section");
        StringAssert.Contains(markdown, "### Sub");
        StringAssert.Contains(markdown, "###### Deep");
    }

    [TestMethod]
    public void ToMarkdown_WritesParagraphsAsBlocks()
    {
        var markdown = Markdown("<p>First.</p><p>Second.</p>");

        Assert.AreEqual("First.\n\nSecond.", markdown);
    }

    [TestMethod]
    public void ToMarkdown_WritesInlineEmphasisAndCode()
    {
        var markdown = Markdown("<p><strong>bold</strong> and <em>italic</em> and <code>self.isPassive</code></p>");

        Assert.AreEqual("**bold** and _italic_ and `self.isPassive`", markdown);
    }

    [TestMethod]
    public void ToMarkdown_WritesAHorizontalRule()
    {
        StringAssert.Contains(Markdown("<p>a</p><hr /><p>b</p>"), "---");
    }

    [TestMethod]
    public void ToMarkdown_KeepsAnExplicitLineBreak()
    {
        StringAssert.Contains(Markdown("<p>one<br />two</p>"), "one\ntwo");
    }

    // -- Code samples ----------------------------------------------------------

    [TestMethod]
    public void ToMarkdown_FencesACodeBlockAndDropsTheHighlightingSpans()
    {
        var markdown = Markdown("<pre><code><span class=\"kw\">self</span>.<span class=\"id\">isPassive</span> = false;</code></pre>");

        StringAssert.Contains(markdown, "```\nself.isPassive = false;\n```");
        Assert.IsFalse(markdown.Contains("span", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ToMarkdown_FencesACodeBlockThatContainsBackticksWithALongerFence()
    {
        // The docs do show Markdown; a three-backtick fence would end inside its own sample.
        var markdown = Markdown("<pre><code>```js\nconst a = 1;\n```</code></pre>");

        StringAssert.Contains(markdown, "````");
        StringAssert.Contains(markdown, "const a = 1;");
    }

    [TestMethod]
    public void ToMarkdown_DecodesEntitiesInsideCode()
    {
        var markdown = Markdown("<pre><code>&lt;script src=&quot;a.js&quot;&gt;&lt;/script&gt;</code></pre>");

        StringAssert.Contains(markdown, "<script src=\"a.js\"></script>");
    }

    [TestMethod]
    public void ToMarkdown_SkipsAnEmptyCodeBlock()
    {
        Assert.AreEqual("text", Markdown("<pre><code>   </code></pre><p>text</p>"));
    }

    // -- Lists and tables ------------------------------------------------------

    [TestMethod]
    public void ToMarkdown_WritesUnorderedAndOrderedLists()
    {
        StringAssert.Contains(Markdown("<ul><li>one</li><li>two</li></ul>"), "- one\n- two");
        StringAssert.Contains(Markdown("<ol><li>one</li><li>two</li></ol>"), "1. one\n2. two");
    }

    [TestMethod]
    public void ToMarkdown_IndentsANestedListUnderItsItem()
    {
        var markdown = Markdown("<ul><li>outer<ul><li>inner</li></ul></li></ul>");

        StringAssert.Contains(markdown, "- outer");
        StringAssert.Contains(markdown, "  - inner");
        Assert.IsFalse(markdown.Contains("\n\n  -", StringComparison.Ordinal), "a nested list must stay attached to its item");
    }

    [TestMethod]
    public void ToMarkdown_WritesATableWithTheDelimiterRowMarkdownNeeds()
    {
        var markdown = Markdown("<table><tr><th>Name</th><th>Default</th></tr><tr><td>isPassive</td><td>false</td></tr></table>");

        var lines = markdown.Split('\n');

        StringAssert.Contains(lines[0], "| Name | Default |");
        StringAssert.Contains(lines[1], "| --- | --- |");
        StringAssert.Contains(lines[2], "| isPassive | false |");
    }

    [TestMethod]
    public void ToMarkdown_EscapesAPipeInsideACell()
    {
        var markdown = Markdown("<table><tr><td>'strict' | 'lax'</td></tr></table>");

        StringAssert.Contains(markdown, @"'strict' \| 'lax'", "an unescaped pipe splits the row into extra columns");
    }

    [TestMethod]
    public void ToMarkdown_PadsARowThatIsShorterThanTheHeader()
    {
        var markdown = Markdown("<table><tr><th>A</th><th>B</th></tr><tr><td>only</td></tr></table>");
        var lines = markdown.Split('\n');

        Assert.AreEqual(3, lines[2].Count(c => c == '|'), "every row needs the same number of separators");
    }

    [TestMethod]
    public void ToMarkdown_DoesNotRenderANestedTablesRowsTwice()
    {
        var markdown = Markdown("<table><tr><td><table><tr><td>inner</td></tr></table></td></tr></table>");

        Assert.AreEqual(1, markdown.Split("inner").Length - 1);
    }

    [TestMethod]
    public void ToMarkdown_WritesADefinitionListAsTermAndValue()
    {
        var markdown = Markdown("<dl><dt>scope</dt><dd>the service-worker scope</dd></dl>");

        StringAssert.Contains(markdown, "- **scope**: the service-worker scope");
    }

    // -- Links and images ------------------------------------------------------

    [TestMethod]
    public void ToMarkdown_WritesALinkWithItsTarget()
    {
        Assert.AreEqual("[the docs](https://bitplatform.dev)", Markdown("<a href=\"https://bitplatform.dev\">the docs</a>"));
    }

    [TestMethod]
    public void ToMarkdown_FlattensALinkWhoseTextAlreadyIsItsTarget()
    {
        // The docs link to their own routes that way; repeating the URL reads worse than the text.
        Assert.AreEqual("/service-worker", Markdown("<a href=\"/service-worker\">/service-worker</a>"));
    }

    [TestMethod]
    public void ToMarkdown_FlattensAnAnchorOnlyLink()
    {
        Assert.AreEqual("Back to top", Markdown("<a href=\"#top\">Back to top</a>"));
    }

    [TestMethod]
    public void ToMarkdown_KeepsAnImagesAltTextAndDropsTheImage()
    {
        Assert.AreEqual("[image: the install splash]", Markdown("<img src=\"a.png\" alt=\"the install splash\" />"));
        Assert.AreEqual(string.Empty, Markdown("<img src=\"a.png\" />"));
    }

    // -- What must not survive -------------------------------------------------

    [TestMethod]
    public void ToMarkdown_DropsScriptStyleAndSvgContent()
    {
        var markdown = Markdown("<div><script>var a = 1;</script><style>.a{color:red}</style><svg><path d=\"M0\"/></svg><p>kept</p></div>");

        Assert.AreEqual("kept", markdown);
    }

    [TestMethod]
    public void ToMarkdown_DropsInteractiveAffordances()
    {
        var markdown = Markdown("<div><button>Copy</button><p>kept</p></div>");

        Assert.AreEqual("kept", markdown);
    }

    [TestMethod]
    public void ToMarkdown_DropsCommentsAndPresentationalWrappers()
    {
        var markdown = Markdown("<!-- note --><div class=\"card grid\"><div class=\"inner\"><p>kept</p></div></div>");

        Assert.AreEqual("kept", markdown);
    }

    [TestMethod]
    public void ToMarkdown_CollapsesTheWhitespaceThatOnlyIndentedTheSource()
    {
        var markdown = Markdown("<p>\n    one    two\n</p>\n\n\n<p>three</p>");

        Assert.AreEqual("one two\n\nthree", markdown);
    }

    [TestMethod]
    public void ToMarkdown_NeverLeavesMoreThanOneBlankLine()
    {
        var markdown = Markdown("<div><div><div><p>a</p></div></div></div><div><div><p>b</p></div></div>");

        Assert.IsFalse(markdown.Contains("\n\n\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ToMarkdown_KeepsASeparatingSpaceBetweenInlineElements()
    {
        Assert.AreEqual("**a** _b_", Markdown("<p><strong>a</strong> <em>b</em></p>"));
    }

    [TestMethod]
    public void ToMarkdown_DecodesEntitiesInText()
    {
        Assert.AreEqual("a & b < c", Markdown("<p>a &amp; b &lt; c</p>"));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void ToMarkdown_HandlesEmptyInput(string? html)
    {
        Assert.AreEqual(string.Empty, html!.ToMarkdown());
    }

    [TestMethod]
    public void ToMarkdown_IsMuchSmallerThanTheHtmlItCameFrom()
    {
        // The whole reason the pages are flattened: markup would otherwise eat the context budget.
        var html = string.Concat(Enumerable.Repeat(
            "<div class=\"docs-card\"><div class=\"docs-card-body\"><p><span class=\"tok\">self</span>.<span class=\"tok\">isPassive</span></p></div></div>", 50));

        var markdown = html.ToMarkdown();

        Assert.IsTrue(markdown.Length < html.Length / 3, $"{markdown.Length} vs {html.Length}");
    }
}
