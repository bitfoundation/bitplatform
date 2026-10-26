using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MarkdownView;

/// <summary>
/// How the view renders a parsed document: the elements each block becomes, the safety of links, images and raw HTML,
/// code blocks, the caret and the citation markers - and the bit conventions of its root and its parts.
/// </summary>
[TestClass]
public class BitAiMarkdownViewTests : BitAiTestBase
{
    private IRenderedComponent<BitAiMarkdownView> RenderMarkdown(string source, bool complete = true) =>
        RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse(source, isComplete: complete)));



    [TestMethod]
    public void BitAiMarkdownViewShouldRenderItsRootClass()
    {
        var component = RenderMarkdown("Hello");

        var root = component.Find(".bit-aimdv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
        Assert.AreEqual("Hello", root.QuerySelector("p.bit-aimdv-par")!.TextContent);
    }

    [TestMethod]
    public void BitAiMarkdownViewShouldRenderNothingWithoutADocument()
    {
        var component = RenderComponent<BitAiMarkdownView>();

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void BitAiMarkdownViewShouldRespectClassStyleAndId()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("text"))
            .Add(p => p.Class, "custom-class")
            .Add(p => p.Style, "color: red")
            .Add(p => p.Id, "answer"));

        var root = component.Find(".bit-aimdv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.AreEqual("answer", root.GetAttribute("id"));
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Ltr, "ltr"),
        DataRow(BitDir.Rtl, "rtl"),
        DataRow(BitDir.Auto, "auto")]
    public void BitAiMarkdownViewShouldRespectDir(BitDir? dir, string? expected)
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("text"))
            .Add(p => p.Dir, dir));

        var root = component.Find(".bit-aimdv");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(dir == BitDir.Rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiMarkdownViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("text"))
            .Add(p => p.Visibility, visibility));

        var style = component.Find(".bit-aimdv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitAiMarkdownViewShouldApplyTheClassesAndStylesOfItsParts()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("# Title\n\nSome `code` and a [link](https://x.dev).\n\n- one\n\n> quote\n\n---"))
            .Add(p => p.Classes, new()
            {
                Root = "c-root",
                Paragraph = "c-par",
                Heading = "c-hdg",
                InlineCode = "c-cod",
                Link = "c-lnk",
                List = "c-lst",
                ListItem = "c-itm",
                Quote = "c-quo",
                Separator = "c-sep",
            })
            .Add(p => p.Styles, new() { Root = "margin: 1px", Paragraph = "margin: 2px", Heading = "margin: 3px" }));

        var root = component.Find(".bit-aimdv");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");

        Assert.IsTrue(component.Find(".bit-aimdv-par").ClassList.Contains("c-par"));
        Assert.AreEqual("margin: 2px", component.Find(".bit-aimdv-par").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aimdv-hdg").ClassList.Contains("c-hdg"));
        Assert.AreEqual("margin: 3px", component.Find(".bit-aimdv-hdg").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aimdv-cod").ClassList.Contains("c-cod"));
        Assert.IsTrue(component.Find(".bit-aimdv-lnk").ClassList.Contains("c-lnk"));
        Assert.IsTrue(component.Find(".bit-aimdv-lst").ClassList.Contains("c-lst"));
        Assert.IsTrue(component.Find(".bit-aimdv-itm").ClassList.Contains("c-itm"));
        Assert.IsTrue(component.Find(".bit-aimdv-quo").ClassList.Contains("c-quo"));
        Assert.IsTrue(component.Find(".bit-aimdv-sep").ClassList.Contains("c-sep"));
    }

    [TestMethod]
    public void HeadingsRenderOneLevelDownSoTheyDoNotCompeteWithThePageTitle()
    {
        var component = RenderMarkdown("# Top level");

        var heading = component.Find(".bit-aimdv-hdg");

        Assert.AreEqual("H2", heading.TagName);
        Assert.AreEqual("Top level", heading.TextContent);
    }

    [TestMethod]
    public void EmphasisBecomesSemanticElements()
    {
        var markup = RenderMarkdown("**bold** and *italic* and ~~gone~~").Markup;

        StringAssert.Contains(markup, "<strong>bold</strong>");
        StringAssert.Contains(markup, "<em>italic</em>");
        StringAssert.Contains(markup, "<del>gone</del>");
    }

    [TestMethod]
    public void ListsRenderAsListsAndTaskItemsGetCheckboxes()
    {
        Assert.AreEqual("UL", RenderMarkdown("- a\n- b").Find(".bit-aimdv-lst").TagName);
        Assert.AreEqual("OL", RenderMarkdown("1. a\n2. b").Find(".bit-aimdv-lst").TagName);
        Assert.AreEqual("3", RenderMarkdown("3. a\n4. b").Find(".bit-aimdv-lst").GetAttribute("start"));

        var tasks = RenderMarkdown("- [x] done\n- [ ] todo");
        Assert.IsTrue(tasks.Find(".bit-aimdv-lst").ClassList.Contains("bit-aimdv-tsk"));

        var boxes = tasks.FindAll("input.bit-aimdv-chk");
        Assert.HasCount(2, boxes);
        Assert.IsTrue(boxes.All(b => b.GetAttribute("type") == "checkbox"));
        Assert.IsTrue(boxes.All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
        Assert.IsTrue(boxes[0].HasAttribute("checked"));
        Assert.IsFalse(boxes[1].HasAttribute("checked"));
        Assert.AreEqual("done", tasks.FindAll(".bit-aimdv-tkb")[0].TextContent);
    }

    [TestMethod]
    public void EachTaskCheckboxIsNamedByTheTextOfItsItem()
    {
        var tasks = RenderMarkdown("- [x] done\n- [ ] todo\n\nthen\n\n- [ ] later");

        var boxes = tasks.FindAll("input.bit-aimdv-chk");
        Assert.HasCount(3, boxes);

        var ids = boxes.Select(b => b.GetAttribute("aria-labelledby")).ToList();
        Assert.IsTrue(ids.All(id => string.IsNullOrEmpty(id) is false));
        Assert.HasCount(3, ids.Distinct());

        Assert.AreEqual("done", tasks.Find($"[id='{ids[0]}']").TextContent);
        Assert.AreEqual("todo", tasks.Find($"[id='{ids[1]}']").TextContent);
        Assert.AreEqual("later", tasks.Find($"[id='{ids[2]}']").TextContent);

        // The ids belong to the view, so two views on one page never name each other's boxes.
        var other = RenderMarkdown("- [x] done");
        Assert.AreNotEqual(ids[0], other.Find("input.bit-aimdv-chk").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void ALooseListIsMarkedSo()
    {
        Assert.IsTrue(RenderMarkdown("- a\n\n- b").Find(".bit-aimdv-lst").ClassList.Contains("bit-aimdv-lse"));
        Assert.IsFalse(RenderMarkdown("- a\n- b").Find(".bit-aimdv-lst").ClassList.Contains("bit-aimdv-lse"));
    }

    [TestMethod]
    public void TablesScrollInsideTheirOwnContainer()
    {
        var component = RenderMarkdown("| a | b | c |\n| --- | ---: | :---: |\n| 1 | 2 | 3 |");

        var table = component.Find(".bit-aimdv-tbw > table.bit-aimdv-tbl");
        var headers = table.QuerySelectorAll("th.bit-aimdv-thc");

        Assert.HasCount(3, headers);
        Assert.IsTrue(headers.All(h => h.GetAttribute("scope") == "col"));
        Assert.IsFalse(headers[0].ClassList.Contains("bit-aimdv-alr"));
        Assert.IsTrue(headers[1].ClassList.Contains("bit-aimdv-alr"));
        Assert.IsTrue(headers[2].ClassList.Contains("bit-aimdv-alc"));
        Assert.HasCount(3, table.QuerySelectorAll("tbody > tr.bit-aimdv-row > td.bit-aimdv-tdc"));

        // The container takes the focus while the table is too wide for it (the page script BitAiScrollStops.ts reads the
        // mark), so a keyboard can scroll it in an engine that leaves a scrolling box out of the tab order - as a named
        // group, since a tab stop is announced by its name.
        Assert.IsTrue(component.Find(".bit-aimdv-tbw").HasAttribute("data-bit-ai-scroll"));
        Assert.IsNull(component.Find(".bit-aimdv-tbw").GetAttribute("tabindex"));
        Assert.AreEqual("group", component.Find(".bit-aimdv-tbw").GetAttribute("role"));
        Assert.AreEqual("Table", component.Find(".bit-aimdv-tbw").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void LinksOpenSafelyInANewTab()
    {
        var link = RenderMarkdown("[docs](https://example.com)").Find("a.bit-aimdv-lnk");

        Assert.AreEqual("https://example.com", link.GetAttribute("href"));
        Assert.AreEqual("noopener noreferrer nofollow", link.GetAttribute("rel"));
        Assert.AreEqual("_blank", link.GetAttribute("target"));
    }

    // An href of "#" would still be followed - to the home page of the app, in a new tab - so a link whose address was
    // neutralised keeps its text and no address at all.
    [TestMethod]
    public void ScriptUrlsInLinksAreNeutralised()
    {
        var component = RenderMarkdown("[click](javascript:alert(1))");

        Assert.IsFalse(component.Markup.Contains("javascript:", System.StringComparison.OrdinalIgnoreCase));

        var link = component.Find("a.bit-aimdv-lnk");
        Assert.AreEqual("click", link.TextContent);
        Assert.IsFalse(link.HasAttribute("href"));
        Assert.IsFalse(link.HasAttribute("target"));
        Assert.IsFalse(link.HasAttribute("rel"));
    }

    [TestMethod]
    public void ScriptUrlsInImagesAreNeutralisedButDataImagesSurvive()
    {
        Assert.IsFalse(RenderMarkdown("![x](javascript:alert(1))").Markup.Contains("javascript:", System.StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual("data:image/png;base64,AAA", RenderMarkdown("![x](data:image/png;base64,AAA)").Find("img.bit-aimdv-img").GetAttribute("src"));
    }

    [TestMethod]
    public void RawHtmlIsEscapedByDefault()
    {
        var markup = RenderMarkdown("<img src=x onerror=alert(1)>").Markup;

        Assert.IsFalse(markup.Contains("onerror=alert(1)>"));
        StringAssert.Contains(markup, "&lt;img");
    }

    [TestMethod]
    public void CodeBlocksShowTheirLanguageAndAreHighlightedOnceClosed()
    {
        var component = RenderMarkdown("```csharp\npublic int Count = 1;\n```");

        Assert.AreEqual("csharp", component.Find(".bit-aicbv .bit-aicbv-lng").TextContent);
        Assert.IsNotEmpty(component.FindAll(".bit-aicbv-kwd"));
    }

    [TestMethod]
    public void AnOpenCodeBlockIsNotHighlightedWhileItStreams()
    {
        var component = RenderMarkdown("```csharp\npublic int Count = 1;", complete: false);

        Assert.IsNotNull(component.Find(".bit-aicbv"));
        Assert.IsEmpty(component.FindAll(".bit-aicbv-kwd"));
        StringAssert.Contains(component.Find(".bit-aicbv-cod").TextContent, "public int Count = 1;");
    }

    [TestMethod]
    public void TheCaretAppearsOnlyOnTheOpenTrailingParagraph()
    {
        var streaming = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("writing", isComplete: false))
            .Add(p => p.ShowCaret, true));

        var caret = streaming.Find("p.bit-aimdv-par > .bit-aimdv-crt");
        Assert.AreEqual("true", caret.GetAttribute("aria-hidden"));

        var finished = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("written"))
            .Add(p => p.ShowCaret, true));

        Assert.IsEmpty(finished.FindAll(".bit-aimdv-crt"));
    }

    // The caret trails the text while the document is being written, whatever the source ends in: inside the line of text
    // being written, or after the blocks where the next one will start.
    [TestMethod]
    public void TheCaretFollowsAHeadingBeingWrittenAndAListItemJustStarted()
    {
        var heading = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("## Render mo", isComplete: false))
            .Add(p => p.ShowCaret, true));

        Assert.HasCount(1, heading.FindAll(".bit-aimdv-crt"));
        Assert.IsNotNull(heading.Find("h3.bit-aimdv-hdg > .bit-aimdv-crt"));

        var item = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("- one\n- ", isComplete: false))
            .Add(p => p.ShowCaret, true));

        var items = item.FindAll("li.bit-aimdv-itm");
        Assert.HasCount(1, item.FindAll(".bit-aimdv-crt"));
        Assert.HasCount(2, items);
        Assert.IsNull(items[0].QuerySelector(".bit-aimdv-crt"));
        Assert.AreEqual("LI", item.Find(".bit-aimdv-crt").ParentElement!.TagName);
    }

    [TestMethod,
        DataRow("```csharp\nvar total = items.Sum("),
        DataRow("| Mode | Runs |\n| --- | --- |\n| Server | The ser"),
        DataRow("Done.\n\n"),
        DataRow("> Quoted.\n\n")]
    public void AfterAnythingElseTheCaretFollowsTheBlocks(string source)
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse(source, isComplete: false))
            .Add(p => p.ShowCaret, true));

        Assert.HasCount(1, component.FindAll(".bit-aimdv-crt"));
        Assert.IsTrue(component.Find(".bit-aimdv-crt").ParentElement!.ClassList.Contains("bit-aimdv"));

        var finished = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse(source))
            .Add(p => p.ShowCaret, true));

        Assert.IsEmpty(finished.FindAll(".bit-aimdv-crt"));
    }

    // That a reply has nothing to show yet is its host's to say (a chat message shows a thinking indicator), so a lone
    // caret is not drawn beside it.
    [TestMethod]
    public void ADocumentWithNoBlockYetDrawsNoCaret()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse(string.Empty, isComplete: false))
            .Add(p => p.ShowCaret, true));

        Assert.IsEmpty(component.FindAll(".bit-aimdv-crt"));
    }

    [TestMethod]
    public void TheStreamingCaretOptionTurnsTheCaretOff()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("writing", isComplete: false))
            .Add(p => p.ShowCaret, true)
            .Add(p => p.CascadedOptions, BitAiUiOptions.Default with { ShowStreamingCaret = false }));

        Assert.IsEmpty(component.FindAll(".bit-aimdv-crt"));
    }

    [TestMethod]
    public void CitationMarkersRenderAsChipsWhenTheSourceIsKnown()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        BitAiCitation? clicked = null;

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here"))
            .Add(p => p.Citations, new[] { citation })
            .Add(p => p.OnCitationClick, c => clicked = c));

        var marker = component.FindComponent<BitAiCitationMarker>();

        Assert.AreSame(citation, marker.Instance.Citation);
        Assert.IsFalse(component.Find(".bit-aimdv-par").TextContent.Contains("[1]"));

        marker.Find("button").Click();

        Assert.AreSame(citation, clicked);
    }

    [TestMethod]
    public void AnUnknownCitationNumberStaysLiteralText()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [7] here")));

        Assert.IsEmpty(component.FindComponents<BitAiCitationMarker>());
        StringAssert.Contains(component.Find(".bit-aimdv-par").TextContent, "[7]");
    }

    [TestMethod]
    public void InlineCitationsCanBeTurnedOff()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here"))
            .Add(p => p.Citations, new[] { citation })
            .Add(p => p.CascadedOptions, BitAiUiOptions.Default with { InlineCitations = false }));

        Assert.IsEmpty(component.FindComponents<BitAiCitationMarker>());
        StringAssert.Contains(component.Find(".bit-aimdv-par").TextContent, "[1]");
    }
}
