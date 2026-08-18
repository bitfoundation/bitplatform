using Bit.Brouter.Demo.Server.Services;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The flattening that turns a rendered documentation page into what an MCP client reads.
/// <para>
/// The pages are written for people: syntax highlighting wraps every keyword in a span, cards and
/// grids add layers of divs, and none of it means anything to a model - handing that over would
/// spend most of a context window on markup. What matters here is that the content survives the
/// trip: a code sample must not lose its line breaks, a table must stay a table, and the parts of a
/// page that only exist for a browser must not arrive at all.
/// </para>
/// </summary>
[TestClass]
public class HtmlToMarkdownServiceTests
{
    [TestMethod]
    public void Headings_keep_their_level()
    {
        Assert.AreEqual("# Title", "<h1>Title</h1>".ToMarkdown());
        Assert.AreEqual("### Third", "<h3>Third</h3>".ToMarkdown());
    }

    [TestMethod]
    public void Paragraphs_are_separated_by_a_blank_line()
    {
        Assert.AreEqual("One\n\nTwo", "<p>One</p><p>Two</p>".ToMarkdown());
    }

    [TestMethod]
    public void Emphasis_and_inline_code_survive_as_markdown()
    {
        Assert.AreEqual("**bold** and _italic_ and `code`",
            "<p><strong>bold</strong> and <em>italic</em> and <code>code</code></p>".ToMarkdown());
    }

    [TestMethod]
    public void A_code_block_keeps_its_line_breaks_and_indentation()
    {
        var markdown = "<pre><code>if (x)\n{\n    y();\n}</code></pre>".ToMarkdown();

        Assert.AreEqual("```\nif (x)\n{\n    y();\n}\n```", markdown);
    }

    [TestMethod]
    public void A_code_blocks_highlighting_spans_are_dropped_and_its_entities_decoded()
    {
        // The spans carry no information - their text content already is the code.
        var markdown = "<pre><code><span class=\"k\">var</span> x = <span class=\"s\">&quot;a&amp;b&quot;</span>;</code></pre>".ToMarkdown();

        Assert.AreEqual("```\nvar x = \"a&b\";\n```", markdown);
    }

    [TestMethod]
    public void A_code_block_that_shows_markdown_is_fenced_so_it_cannot_end_itself()
    {
        // The docs do show Markdown; a three-backtick fence would end the block in the middle of its
        // own content and the rest of the page would render as prose.
        var markdown = "<pre><code>```csharp\nvar x = 1;\n```</code></pre>".ToMarkdown();

        StringAssert.StartsWith(markdown, "````\n");
        StringAssert.EndsWith(markdown, "\n````");
        StringAssert.Contains(markdown, "```csharp");
    }

    [TestMethod]
    public void Lists_are_flattened_with_their_nesting_intact()
    {
        var markdown = "<ul><li>one<ul><li>nested</li></ul></li><li>two</li></ul>".ToMarkdown();

        Assert.AreEqual("- one\n  - nested\n- two", markdown);

        Assert.AreEqual("1. first\n2. second", "<ol><li>first</li><li>second</li></ol>".ToMarkdown());
    }

    [TestMethod]
    public void A_table_stays_a_table()
    {
        var markdown = "<table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table>".ToMarkdown();

        StringAssert.Contains(markdown, "| A | B |");
        StringAssert.Contains(markdown, "| --- | --- |");
        StringAssert.Contains(markdown, "| 1 | 2 |");
    }

    [TestMethod]
    public void A_link_keeps_its_target_unless_the_target_is_all_it_says()
    {
        Assert.AreEqual("[the docs](/docs)", "<a href=\"/docs\">the docs</a>".ToMarkdown());

        // The docs link to their own routes by URL; repeating it as a Markdown link says it twice.
        Assert.AreEqual("/docs/guards", "<a href=\"/docs/guards\">/docs/guards</a>".ToMarkdown());

        // An in-page anchor is meaningless once the page is text.
        Assert.AreEqual("jump", "<a href=\"#section\">jump</a>".ToMarkdown());
    }

    [TestMethod]
    public void What_only_exists_for_a_browser_does_not_reach_the_client()
    {
        var markdown = """
            <div><script>alert(1)</script><style>.a{}</style>
            <svg><path d="M0 0"/></svg>
            <button>Copy</button>
            <p>content</p></div>
            """.ToMarkdown();

        Assert.AreEqual("content", markdown);
    }

    [TestMethod]
    public void An_image_is_reduced_to_what_it_was_meant_to_say()
    {
        Assert.AreEqual("[image: a diagram]", "<img src=\"x.png\" alt=\"a diagram\">".ToMarkdown());
        Assert.AreEqual(string.Empty, "<img src=\"decorative.png\" alt=\"\">".ToMarkdown());
    }

    [TestMethod]
    public void Empty_input_is_answered_with_empty_output_rather_than_with_a_failure()
    {
        Assert.AreEqual(string.Empty, string.Empty.ToMarkdown());
        Assert.AreEqual(string.Empty, "   ".ToMarkdown());
    }

    [TestMethod]
    public void A_real_page_shrinks_to_a_fraction_of_its_html()
    {
        // The reason this exists at all: the HTML of a docs page is mostly markup, and a client pays
        // for every character of it.
        var page = new string(' ', 0) + """
            <div class="card"><div class="card-body"><h2 class="title">Guards</h2>
            <p class="lead">Decide whether a navigation may <em>happen</em>.</p>
            <pre class="code"><code><span class="k">var</span> x = 1;</code></pre></div></div>
            """;

        var markdown = page.ToMarkdown();

        Assert.IsTrue(markdown.Length < page.Length / 2, $"The Markdown ({markdown.Length}) is not much smaller than the HTML ({page.Length}).");
        StringAssert.Contains(markdown, "## Guards");
        Assert.IsFalse(markdown.Contains("class=", StringComparison.Ordinal));
    }
}
