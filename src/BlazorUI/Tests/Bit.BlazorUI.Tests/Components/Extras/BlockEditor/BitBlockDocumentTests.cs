using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.BlockEditor;

/// <summary>
/// Covers the block document model on its own: the blocks, the built-in types, and the JSON, HTML and Markdown forms
/// it reads and writes without a component.
/// </summary>
[TestClass]
public class BitBlockDocumentTests
{
    private static BitBlock Block(string type, string text = "", params (string Name, string Value)[] props)
        => new()
        {
            Type = type,
            Text = text,
            Props = props.ToDictionary(p => p.Name, p => p.Value)
        };

    private static BitBlockDocument Doc(params BitBlock[] blocks) => new(blocks);

    // The same document with every id blanked, for comparing a form that does not keep ids (Markdown, HTML).
    private static BitBlockDocument WithoutIds(BitBlockDocument document) => new(WithoutIds(document.Blocks));

    private static List<BitBlock> WithoutIds(IReadOnlyList<BitBlock> blocks)
        => [.. blocks.Select(b => b with { Id = "", Children = WithoutIds(b.Children) })];

    private static void AssertSameBlocks(BitBlockDocument expected, BitBlockDocument actual)
        => Assert.AreEqual(WithoutIds(expected).ToJson(), WithoutIds(actual).ToJson());

    private static IEnumerable<BitBlock> All(IEnumerable<BitBlock> blocks)
        => blocks.SelectMany(b => new[] { b }.Concat(All(b.Children)));



    [TestMethod]
    public void BitBlockShouldGetAShortUniqueIdWhenNoneIsGiven()
    {
        var ids = Enumerable.Range(0, 200).Select(_ => new BitBlock { Type = "paragraph" }.Id).ToList();

        Assert.IsTrue(ids.All(id => id.Length == 8 && id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c))));
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }

    [TestMethod]
    public void BitBlockShouldCompareByValue()
    {
        var a = new BitBlock
        {
            Id = "a",
            Type = "listItem",
            Text = "one",
            Props = new Dictionary<string, string> { ["style"] = "task", ["checked"] = "true" },
            Children = [new BitBlock { Id = "b", Type = "listItem", Text = "two" }]
        };
        var b = new BitBlock
        {
            Id = "a",
            Type = "listItem",
            Text = "one",
            Props = new Dictionary<string, string> { ["checked"] = "true", ["style"] = "task" },
            Children = [new BitBlock { Id = "b", Type = "listItem", Text = "two" }]
        };

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreEqual(Doc(a), Doc(b));
        Assert.AreNotEqual(a, b with { Text = "other" });
        Assert.AreNotEqual(a, b.WithProp("checked", null));
        Assert.AreNotEqual(a, b with { Children = [] });
    }

    [TestMethod]
    public void BitBlockWithPropShouldSetAndRemoveAPropOnACopy()
    {
        var block = Block("heading", "Title");

        var set = block.WithProp("level", "3");
        var removed = set.WithProp("level", null);

        Assert.AreEqual(0, block.Props.Count);
        Assert.AreEqual("3", set.GetProp("level"));
        Assert.IsNull(removed.GetProp("level"));
        Assert.AreSame(block, block.WithProp("missing", null));
    }

    [TestMethod]
    public void BitBlockTypesShouldListTheBuiltInsAndLetAnAppReplaceOne()
    {
        CollectionAssert.AreEqual(
            new[] { "paragraph", "heading", "listItem", "quote", "code", "image", "divider" },
            BitBlockTypes.All.Select(t => t.Name).ToArray());

        var custom = new BitBlockType { Name = "heading", Kind = BitBlockKind.Text };
        var registry = BitBlockTypes.Resolve([custom, new BitBlockType { Name = "callout", Kind = BitBlockKind.Text }]);

        Assert.AreSame(custom, registry["heading"]);
        Assert.IsTrue(registry.ContainsKey("callout"));
        Assert.AreSame(BitBlockTypes.Paragraph, registry["paragraph"]);
    }

    [TestMethod]
    [DataRow("1", 1)]
    [DataRow("6", 6)]
    [DataRow("0", 2)]
    [DataRow("7", 2)]
    [DataRow("two", 2)]
    [DataRow(null, 2)]
    public void BitBlockTypesShouldReadAHeadingLevelLeniently(string? level, int expected)
    {
        var block = level is null ? Block("heading") : Block("heading", "", ("level", level));

        Assert.AreEqual(expected, BitBlockTypes.HeadingLevel(block));
    }



    [TestMethod]
    [DataRow("plain", "plain")]
    [DataRow("<strong>b</strong> <em>i</em> <strike>s</strike> <u>u</u> <del>d</del>", "<b>b</b> <i>i</i> <s>s</s> <ins>u</ins> <s>d</s>")]
    [DataRow("<ins>u</ins> <mark>m</mark> <sub>2</sub> <sup>3</sup>", "<ins>u</ins> <mark>m</mark> <sub>2</sub> <sup>3</sup>")]
    [DataRow("<span style=\"color:red\">red</span> <p>para</p>", "red para")]
    [DataRow("a<script>alert(1)</script>b", "ab")]
    [DataRow("<a href=\"javascript:alert(1)\">x</a>", "x")]
    [DataRow("<a href=\"https://bitplatform.dev\" target=\"_blank\">x</a>", "<a href=\"https://bitplatform.dev\">x</a>")]
    [DataRow("<a>no address</a>", "no address")]
    [DataRow("<a href=\" /a b \">x</a>", "<a href=\"/a%20b\">x</a>")]
    [DataRow("<b><b>twice</b></b>", "<b>twice</b>")]
    [DataRow("<b>a</b><b>b</b>", "<b>ab</b>")]
    [DataRow("<b> edge </b>x", "<b>edge</b> x")]
    [DataRow("x<b></b>y", "xy")]
    [DataRow("<code><b>c</b>ode</code>", "<code>code</code>")]
    [DataRow("  a \n\t b  ", "a b")]
    [DataRow("line<br>next<br><br>", "line<br>next")]
    [DataRow("a <br> b", "a<br>b")]
    [DataRow("1 &lt; 2 &amp; 3", "1 &lt; 2 &amp; 3")]
    [DataRow("a&nbsp;b", "a&nbsp;b")]
    public void BitBlockInlineShouldPutTextIntoTheVocabulary(string html, string expected)
    {
        var normalized = BitBlockInline.Normalize(html);

        Assert.AreEqual(expected, normalized);
        Assert.AreEqual(normalized, BitBlockInline.Normalize(normalized), "Normalizing twice must change nothing.");
    }



    [TestMethod]
    public void BitBlockDocumentShouldWriteTheJsonForm()
    {
        var document = Doc(
            new BitBlock { Id = "h", Type = "heading", Text = "Title", Props = new Dictionary<string, string> { ["level"] = "1" } },
            new BitBlock { Id = "d", Type = "divider" });

        Assert.AreEqual(
            """{"v":1,"blocks":[{"id":"h","type":"heading","text":"Title","props":{"level":"1"}},{"id":"d","type":"divider"}]}""",
            document.ToJson());
    }

    [TestMethod]
    public void BitBlockDocumentShouldRoundTripJsonExactly()
    {
        var document = Doc(
            new BitBlock { Id = "p", Type = "paragraph", Text = "<b>bold</b> &amp; <a href=\"https://bitplatform.dev\">link</a>" },
            new BitBlock
            {
                Id = "l",
                Type = "listItem",
                Text = "parent",
                Props = new Dictionary<string, string> { ["style"] = "task", ["checked"] = "true" },
                Children = [new BitBlock { Id = "c", Type = "listItem", Text = "child" }]
            },
            new BitBlock { Id = "k", Type = "code", Text = "if (a < b)\n    return;", Props = new Dictionary<string, string> { ["language"] = "csharp" } },
            new BitBlock { Id = "u", Type = "embed", Text = "<raw anything>", Props = new Dictionary<string, string> { ["url"] = "x" } });

        var read = BitBlockDocument.FromJson(document.ToJson());

        Assert.AreEqual(document, read);
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadJsonLeniently()
    {
        var read = BitBlockDocument.FromJson("""
            {
              "v": 2,
              "future": { "anything": [1, 2] },
              "blocks": [
                { "type": "heading", "props": { "level": 3, "flag": true, "gone": null }, "text": "No id", "extra": 1 },
                { "id": "same", "type": "paragraph", "text": "one" },
                { "id": "same", "type": "paragraph", "text": "two" },
                { "id": "t", "text": "no type" }
              ]
            }
            """);

        Assert.AreEqual(4, read.Blocks.Count);
        Assert.AreEqual(8, read.Blocks[0].Id.Length);
        Assert.AreEqual("3", read.Blocks[0].GetProp("level"));
        Assert.AreEqual("true", read.Blocks[0].GetProp("flag"));
        Assert.IsNull(read.Blocks[0].GetProp("gone"));
        Assert.AreEqual("same", read.Blocks[1].Id);
        Assert.AreNotEqual("same", read.Blocks[2].Id, "A repeated id is replaced, so every id stays unique.");
        Assert.AreEqual("paragraph", read.Blocks[3].Type);
    }

    [TestMethod]
    public void BitBlockDocumentShouldSanitizeTheTextOfFormattedTextBlocksFromJson()
    {
        var json = """
            {"v":1,"blocks":[
              {"id":"p","type":"paragraph","text":"ok<script>alert(1)</script><img src=x onerror=alert(1)>"},
              {"id":"k","type":"code","text":"<script>kept as code</script>"},
              {"id":"u","type":"unknown","text":"<b>kept as it is</b><script>"},
              {"id":"c","type":"callout","text":"<i>app</i><iframe src=x></iframe>"}
            ]}
            """;

        var read = BitBlockDocument.FromJson(json, [new BitBlockType { Name = "callout", Kind = BitBlockKind.Text }]);

        Assert.AreEqual("ok", read.Blocks[0].Text);
        Assert.AreEqual("<script>kept as code</script>", read.Blocks[1].Text);
        Assert.AreEqual("<b>kept as it is</b><script>", read.Blocks[2].Text);
        Assert.AreEqual("<i>app</i>", read.Blocks[3].Text);
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadEmptyJsonAsAnEmptyDocumentAndRefuseMalformedJson()
    {
        Assert.AreEqual(0, BitBlockDocument.FromJson(null).Blocks.Count);
        Assert.AreEqual(0, BitBlockDocument.FromJson("  ").Blocks.Count);
        Assert.AreEqual(0, BitBlockDocument.FromJson("{}").Blocks.Count);
        Assert.ThrowsExactly<JsonException>(() => BitBlockDocument.FromJson("{ not json"));
    }

    [TestMethod]
    public void BitBlockDocumentShouldRoundTripDeeplyNestedJson()
    {
        var block = new BitBlock { Id = "n100", Type = "listItem", Text = "deepest" };
        for (var i = 99; i >= 0; i--)
        {
            block = new BitBlock { Id = $"n{i}", Type = "listItem", Text = $"level {i}", Children = [block] };
        }
        var document = Doc(block);

        Assert.AreEqual(document, BitBlockDocument.FromJson(document.ToJson()));
    }



    [TestMethod]
    public void BitBlockDocumentShouldWriteTheHtmlTheRichTextEditorStores()
    {
        var document = Doc(
            Block("heading", "Title", ("level", "2")),
            Block("paragraph", "Some <b>bold</b>, <i>italic</i>, <s>struck</s>, <ins>underlined</ins> and <code>code</code>."),
            Block("paragraph"),
            Block("listItem", "one"),
            Block("listItem", "two"),
            Block("listItem", "first", ("style", "ordered")),
            Block("listItem", "done", ("style", "task"), ("checked", "true")),
            Block("listItem", "todo", ("style", "task")),
            Block("quote", "Quoted"),
            Block("code", "a < b\nc", ("language", "csharp")),
            Block("image", "", ("src", "https://bitplatform.dev/a.png"), ("alt", "An image"), ("width", "320"), ("caption", "Caption")),
            Block("divider"));

        Assert.AreEqual(
            "<h2>Title</h2>"
            + "<p>Some <b>bold</b>, <i>italic</i>, <s>struck</s>, <u>underlined</u> and <code>code</code>.</p>"
            + "<p><br></p>"
            + "<ul><li>one</li><li>two</li></ul>"
            + "<ol><li>first</li></ol>"
            + "<ul><li data-checked=\"true\">done</li><li data-checked=\"false\">todo</li></ul>"
            + "<blockquote><p>Quoted</p></blockquote>"
            + "<pre><code class=\"language-csharp\">a &lt; b\nc</code></pre>"
            + "<p><img src=\"https://bitplatform.dev/a.png\" alt=\"An image\" width=\"320\" title=\"Caption\"></p>"
            + "<hr>",
            document.ToHtml());
    }

    [TestMethod]
    public void BitBlockDocumentShouldNestListsInTheirItemsInHtml()
    {
        var document = Doc(new BitBlock
        {
            Type = "listItem",
            Text = "parent",
            Children = [Block("listItem", "a", ("style", "ordered")), Block("listItem", "b", ("style", "ordered")), Block("paragraph", "note")]
        });

        Assert.AreEqual("<ul><li>parent<ol><li>a</li><li>b</li></ol><p>note</p></li></ul>", document.ToHtml());
    }

    [TestMethod]
    public void BitBlockDocumentShouldSanitizeEveryBlockOfTheHtmlItWrites()
    {
        var custom = new BitBlockType
        {
            Name = "callout",
            Kind = BitBlockKind.Text,
            ToHtml = (block, children) => $"<div class=\"callout\" onclick=\"alert(1)\">{block.Text}<script>alert(1)</script>{children}</div>"
        };
        var document = Doc(
            Block("callout", "Note"),
            Block("image", "", ("src", "javascript:alert(1)"), ("alt", "x")),
            Block("paragraph", "<img src=x onerror=alert(1)>text"));

        var html = document.ToHtml([custom]);

        Assert.AreEqual("<div class=\"callout\">Note</div><p><img alt=\"x\"></p><p>text</p>", html);
    }

    [TestMethod]
    public void BitBlockDocumentShouldExportAnAppTypeWithoutAnExportOfItsOwnAsItsTextOrAsTheBuiltInItReplaces()
    {
        var replacement = new BitBlockType { Name = "heading", Kind = BitBlockKind.Text };
        var document = Doc(
            Block("heading", "Title", ("level", "3")),
            new BitBlock { Type = "toggle", Text = "Summary", Children = [Block("paragraph", "inside")] },
            Block("unknown", "<b>kept</b>"),
            Block("embed"));

        var types = new[] { replacement, new BitBlockType { Name = "toggle", Kind = BitBlockKind.Text, Nests = true }, new BitBlockType { Name = "embed", Kind = BitBlockKind.Atom } };

        Assert.AreEqual("<h3>Title</h3><p>Summary</p><p>inside</p><p><b>kept</b></p>", document.ToHtml(types));
        Assert.AreEqual("### Title\n\nSummary\n\ninside\n\n**kept**", document.ToMarkdown(types));
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadTheHtmlTheRichTextEditorStores()
    {
        var html = "<h3>Title</h3>"
            + "<p>Some <strong>bold</strong> and <span style=\"color:red\">red</span> <em>text</em><br></p>"
            + "<ul><li>one<ul><li>nested</li></ul></li><li><p>two</p></li></ul>"
            + "<ol><li>first</li></ol>"
            + "<ul><li data-checked=\"true\">done</li><li data-checked=\"false\">todo</li></ul>"
            + "<blockquote><p>q1</p><p>q2</p></blockquote>"
            + "<pre><code class=\"language-js\">let a = 1;\n</code></pre>"
            + "<p>before <img src=\"https://bitplatform.dev/a.png\" alt=\"pic\" width=\"40\"> after</p>"
            + "<hr>"
            + "<p><br></p>";

        var document = BitBlockDocument.FromHtml(html);

        var expected = Doc(
            Block("heading", "Title", ("level", "3")),
            Block("paragraph", "Some <b>bold</b> and red <i>text</i>"),
            new BitBlock { Type = "listItem", Text = "one", Children = [Block("listItem", "nested")] },
            Block("listItem", "two"),
            Block("listItem", "first", ("style", "ordered")),
            Block("listItem", "done", ("style", "task"), ("checked", "true")),
            Block("listItem", "todo", ("style", "task")),
            Block("quote", "q1"),
            Block("quote", "q2"),
            Block("code", "let a = 1;", ("language", "js")),
            Block("paragraph", "before"),
            Block("image", "", ("src", "https://bitplatform.dev/a.png"), ("alt", "pic"), ("width", "40")),
            Block("paragraph", "after"),
            Block("divider"),
            Block("paragraph"));

        AssertSameBlocks(expected, document);
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadWhatHasNoBlockAsParagraphs()
    {
        var html = "loose text"
            + "<div><p>in a div</p>more loose</div>"
            + "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>"
            + "<video src=\"https://bitplatform.dev/v.mp4\"></video>"
            + "<script>alert(1)</script>"
            + "<iframe src=\"https://evil.example/\"></iframe>";

        var document = BitBlockDocument.FromHtml(html);

        AssertSameBlocks(
            Doc(
                Block("paragraph", "loose text"),
                Block("paragraph", "in a div"),
                Block("paragraph", "more loose"),
                Block("paragraph", "A | B"),
                Block("paragraph", "1 | 2"),
                Block("paragraph", "<a href=\"https://bitplatform.dev/v.mp4\">https://bitplatform.dev/v.mp4</a>")),
            document);
    }

    [TestMethod]
    public void BitBlockDocumentShouldRoundTripHtml()
    {
        var document = Sample();

        var read = BitBlockDocument.FromHtml(document.ToHtml());

        AssertSameBlocks(document, read);
    }



    [TestMethod]
    public void BitBlockDocumentShouldWriteMarkdown()
    {
        var document = Doc(
            Block("heading", "Title", ("level", "2")),
            Block("paragraph", "<b>b</b> <i>i</i> <s>s</s> <code>c</code> <a href=\"https://bitplatform.dev\">l</a> H<sub>2</sub>O x<sup>2</sup> <mark>m</mark> <ins>u</ins>"),
            Block("paragraph", "line<br>next"),
            Block("listItem", "one"),
            Block("listItem", "done", ("style", "task"), ("checked", "true")),
            Block("listItem", "todo", ("style", "task")),
            Block("listItem", "first", ("style", "ordered")),
            Block("listItem", "second", ("style", "ordered")),
            Block("quote", "q<br>r"),
            Block("code", "let a = 1;", ("language", "js")),
            Block("image", "", ("src", "https://bitplatform.dev/a.png"), ("alt", "pic"), ("caption", "A \"caption\"")),
            Block("divider"));

        Assert.AreEqual(
            """
            ## Title

            **b** *i* ~~s~~ `c` [l](https://bitplatform.dev) H~2~O x^2^ ==m== ++u++

            line\
            next

            - one
            - [x] done
            - [ ] todo

            1. first
            2. second

            > q\
            > r

            ```js
            let a = 1;
            ```

            ![pic](https://bitplatform.dev/a.png "A \"caption\"")

            ---
            """.Replace("\r\n", "\n"),
            document.ToMarkdown());
    }

    [TestMethod]
    public void BitBlockDocumentShouldIndentNestedBlocksUnderTheirListItem()
    {
        var document = Doc(
            new BitBlock
            {
                Type = "listItem",
                Text = "parent",
                Props = Ordered,
                Children =
                [
                    Block("listItem", "child"),
                    new BitBlock { Type = "listItem", Text = "grandparent", Children = [Block("listItem", "grandchild", ("style", "task"))] }
                ]
            },
            new BitBlock { Type = "listItem", Text = "with a paragraph", Props = Ordered, Children = [Block("paragraph", "more<br>lines"), Block("code", "x\n\ny")] });

        Assert.AreEqual(
            """
            1. parent
               - child
               - grandparent
                 - [ ] grandchild
            2. with a paragraph

               more\
               lines

               ```
               x

               y
               ```
            """.Replace("\r\n", "\n"),
            document.ToMarkdown());

        AssertSameBlocks(document, BitBlockDocument.FromMarkdown(document.ToMarkdown()));
    }

    private static readonly Dictionary<string, string> Ordered = new() { ["style"] = "ordered" };

    [TestMethod]
    [DataRow("1. not a list", "1\\. not a list")]
    [DataRow("# not a heading", "\\# not a heading")]
    [DataRow("- not an item", "\\- not an item")]
    [DataRow("+ not an item", "\\+ not an item")]
    [DataRow("*stars* and _underscores_", "\\*stars\\* and \\_underscores\\_")]
    [DataRow("[not](a link) &amp; &lt;tag&gt;", "\\[not\\](a link) \\& \\<tag\\>")]
    [DataRow("a ++ b == c, 1+2=3", "a \\+\\+ b \\=\\= c, 1+2=3")]
    [DataRow("~sub~ ^sup^ `tick` back\\slash", "\\~sub\\~ \\^sup\\^ \\`tick\\` back\\\\slash")]
    public void BitBlockDocumentShouldEscapeTextThatWouldReadAsMarkdown(string text, string expected)
    {
        var html = BitBlockInline.Encode(text.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"));
        var document = Doc(Block("paragraph", html));

        Assert.AreEqual(expected, document.ToMarkdown());
        AssertSameBlocks(document, BitBlockDocument.FromMarkdown(document.ToMarkdown()));
    }

    [TestMethod]
    public void BitBlockDocumentShouldEscapeTheClosingHashesOfAHeading()
    {
        var document = Doc(Block("heading", "C# and F# #", ("level", "1")));

        Assert.AreEqual("# C\\# and F\\# \\#", document.ToMarkdown());
        AssertSameBlocks(document, BitBlockDocument.FromMarkdown(document.ToMarkdown()));
    }

    [TestMethod]
    public void BitBlockDocumentShouldFenceCodeWithMoreBackticksThanItHolds()
    {
        var document = Doc(Block("code", "```\ninner\n```"), Block("paragraph", "a <code>``x` y</code> b <code>`edge`</code>"));

        Assert.AreEqual("````\n```\ninner\n```\n````\n\na ``` ``x` y ``` b `` `edge` ``", document.ToMarkdown());
        AssertSameBlocks(document, BitBlockDocument.FromMarkdown(document.ToMarkdown()));
    }

    [TestMethod]
    public void BitBlockDocumentShouldWriteItalicAgainstBoldWithTheOtherDelimiter()
    {
        var document = Doc(Block("paragraph", "<b><i>both</i></b> and <i><b>both</b> again</i> and <b>x <i>y</i></b>"));

        Assert.AreEqual("**_both_** and _**both** again_ and **x _y_**", document.ToMarkdown());
        AssertSameBlocks(document, BitBlockDocument.FromMarkdown(document.ToMarkdown()));
    }

    [TestMethod]
    public void BitBlockDocumentShouldLeaveOutAnImageWhoseAddressIsUnsafe()
    {
        var document = Doc(Block("image", "", ("src", "javascript:alert(1)")), Block("paragraph", "after"));

        Assert.AreEqual("after", document.ToMarkdown());
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadMarkdown()
    {
        var markdown = """
            # Title

            Text with **bold**, *italic*, ~~struck~~, `code`, [a link](https://bitplatform.dev), H~2~O, x^2^, ==marked== and ++inserted++.
            A soft break.

            - one
              - nested
            - [x] done
            - [ ] todo

            3. third

            > quoted
            >
            > twice

            ```csharp extra
            var a = 1;
            ```

                indented code

            ![pic](https://bitplatform.dev/a.png "Caption")

            ***

            | a | b |
            |---|---|
            | 1 | 2 |

            www.bitplatform.dev and [undefined][ref]
            """;

        var document = BitBlockDocument.FromMarkdown(markdown);

        AssertSameBlocks(
            Doc(
                Block("heading", "Title", ("level", "1")),
                Block("paragraph", "Text with <b>bold</b>, <i>italic</i>, <s>struck</s>, <code>code</code>, <a href=\"https://bitplatform.dev\">a link</a>, H<sub>2</sub>O, x<sup>2</sup>, <mark>marked</mark> and <ins>inserted</ins>. A soft break."),
                new BitBlock { Type = "listItem", Text = "one", Children = [Block("listItem", "nested")] },
                Block("listItem", "done", ("style", "task"), ("checked", "true")),
                Block("listItem", "todo", ("style", "task")),
                Block("listItem", "third", ("style", "ordered")),
                Block("quote", "quoted"),
                Block("quote", "twice"),
                Block("code", "var a = 1;", ("language", "csharp")),
                Block("code", "indented code"),
                Block("image", "", ("src", "https://bitplatform.dev/a.png"), ("alt", "pic"), ("caption", "Caption")),
                Block("divider"),
                Block("paragraph", "| a | b | |---|---| | 1 | 2 |"),
                Block("paragraph", "www.bitplatform.dev and [undefined][ref]")),
            document);
    }

    [TestMethod]
    public void BitBlockDocumentShouldReadEmptyMarkdownAsAnEmptyDocument()
    {
        Assert.AreEqual(0, BitBlockDocument.FromMarkdown(null).Blocks.Count);
        Assert.AreEqual(0, BitBlockDocument.FromMarkdown("").Blocks.Count);
        Assert.AreEqual(0, BitBlockDocument.FromHtml(null).Blocks.Count);
    }

    [TestMethod]
    public void BitBlockDocumentShouldGiveEveryBlockItReadsAUniqueId()
    {
        var document = BitBlockDocument.FromMarkdown("- a\n  - b\n- c\n\npara");

        var ids = All(document.Blocks).Select(b => b.Id).ToList();

        Assert.AreEqual(4, ids.Count);
        Assert.AreEqual(4, ids.Distinct().Count());
    }

    [TestMethod]
    public void BitBlockDocumentShouldWriteAnAppTypesMarkdownThroughItsOwnExport()
    {
        var callout = new BitBlockType
        {
            Name = "callout",
            Kind = BitBlockKind.Text,
            Nests = true,
            ToMarkdown = (block, children) => $"> [!NOTE]\n> {block.Text}" + (children.Length > 0 ? "\n>\n> " + children : "")
        };
        var document = Doc(new BitBlock { Type = "callout", Text = "Heads up", Children = [Block("paragraph", "inner")] });

        Assert.AreEqual("> [!NOTE]\n> Heads up\n>\n> inner", document.ToMarkdown([callout]));
    }



    [TestMethod]
    [DataRow("paragraph", "plain text")]
    [DataRow("paragraph", "<b>bold</b>")]
    [DataRow("paragraph", "<i>italic</i>")]
    [DataRow("paragraph", "<s>struck</s>")]
    [DataRow("paragraph", "<code>code</code>")]
    [DataRow("paragraph", "<a href=\"https://bitplatform.dev/a?b=c&amp;d=e\">link</a>")]
    [DataRow("paragraph", "<a href=\"https://bitplatform.dev/a_(b)\">parenthesized</a>")]
    [DataRow("paragraph", "H<sub>2</sub>O")]
    [DataRow("paragraph", "x<sup>2</sup>")]
    [DataRow("paragraph", "<mark>marked</mark>")]
    [DataRow("paragraph", "<ins>inserted</ins>")]
    [DataRow("paragraph", "line<br>break")]
    [DataRow("paragraph", "<b>bold <i>and italic</i></b> then <a href=\"https://bitplatform.dev\"><b>bold link</b></a>")]
    [DataRow("paragraph", "a&nbsp;b &amp; c &lt; d")]
    [DataRow("heading", "Heading <i>one</i>")]
    [DataRow("quote", "A <b>quote</b><br>on two lines")]
    [DataRow("listItem", "An <s>item</s>")]
    public void BitBlockDocumentShouldRoundTripMarkdownForEveryTypeAndMark(string type, string text)
    {
        var document = Doc(type == "heading" ? Block(type, text, ("level", "4")) : Block(type, text));

        var markdown = document.ToMarkdown();
        var read = BitBlockDocument.FromMarkdown(markdown);

        AssertSameBlocks(document, read);
    }

    [TestMethod]
    public void BitBlockDocumentShouldRoundTripMarkdownForTheWholeSample()
    {
        var document = Sample(withWidth: false);

        var read = BitBlockDocument.FromMarkdown(document.ToMarkdown());

        AssertSameBlocks(document, read);
        Assert.AreEqual(document.ToMarkdown(), read.ToMarkdown());
    }

    // A document of every built-in type, every mark, nested and adjacent lists of each style.
    private static BitBlockDocument Sample(bool withWidth = true)
    {
        var image = Block("image", "", ("src", "https://bitplatform.dev/a.png"), ("alt", "An image"), ("caption", "Its caption"));
        if (withWidth) image = image.WithProp("width", "120");

        return Doc(
            Block("heading", "The <i>title</i>", ("level", "1")),
            Block("paragraph", "Every mark: <b>b</b>, <i>i</i>, <s>s</s>, <code>c</code>, <a href=\"https://bitplatform.dev\">a</a>, <sub>sub</sub>, <sup>sup</sup>, <mark>mark</mark>, <ins>ins</ins>."),
            Block("heading", "Lists", ("level", "2")),
            new BitBlock { Type = "listItem", Text = "bullet", Children = [Block("listItem", "nested", ("style", "ordered")), Block("listItem", "nested two", ("style", "ordered"))] },
            Block("listItem", "bullet two"),
            Block("listItem", "task", ("style", "task"), ("checked", "true")),
            Block("listItem", "open task", ("style", "task")),
            Block("paragraph", "Between lists."),
            Block("listItem", "ordered", ("style", "ordered")),
            Block("listItem", "ordered two", ("style", "ordered")),
            Block("quote", "Quote one"),
            Block("quote", "Quote <b>two</b>"),
            Block("code", "for (var i = 0; i < 3; i++)\n{\n    Console.WriteLine(\"*not* markdown\");\n}", ("language", "csharp")),
            image,
            Block("divider"),
            Block("paragraph", "The end."));
    }
}
