using System.Collections.Generic;
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.MarkdownViewer;

/// <summary>
/// Covers what the viewer adds around the markup the renderers write: in-page links that survive a
/// <c>&lt;base href&gt;</c>, the heading outline, the names of its regions and controls, the disabled state and the
/// boundary the stylesheet stops at around a template's output.
/// </summary>
[TestClass]
public class BitMarkdownViewerAccessibilityTests : BunitTestContext
{
    private void NavigateTo(string uri) => Services.GetRequiredService<BunitNavigationManager>().NavigateTo(uri);

    [TestMethod]
    public void BitMarkdownViewerShouldWriteInPageLinksAgainstThePage()
    {
        // With <base href="/">, a bare "#id" on any other page resolves to the home page.
        NavigateTo("/docs/guide?tab=2");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Install\n\nSee [install](#install), [elsewhere](/other) and a note[^1].\n\n[^1]: The note.");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseFootnotes().UseAutoIdentifiers(anchorLinks: true).Build());
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual("/docs/guide?tab=2#install", component.Find(".bit-mdv-anchor").GetAttribute("href"));
        Assert.AreEqual("/docs/guide?tab=2#install", links.Single(a => a.TextContent == "install").GetAttribute("href"));
        Assert.AreEqual("/other", links.Single(a => a.TextContent == "elsewhere").GetAttribute("href"));
        StringAssert.StartsWith(component.Find(".footnote-ref a").GetAttribute("href"), "/docs/guide?tab=2#");
        StringAssert.StartsWith(component.Find(".footnote-backref").GetAttribute("href"), "/docs/guide?tab=2#");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepBareFragmentsOnTheBaseAddress()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[top](#top)");
        });

        Assert.AreEqual("#top", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldFollowThePageAddressWhenItChanges()
    {
        NavigateTo("/a");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[top](#top)");
        });

        Assert.AreEqual("/a#top", component.Find(".bit-mdv a").GetAttribute("href"));

        NavigateTo("/b?x=1#somewhere");

        component.WaitForAssertion(() => Assert.AreEqual("/b?x=1#top", component.Find(".bit-mdv a").GetAttribute("href")));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldWriteInPageLinksAgainstThePageWithATemplateToo()
    {
        NavigateTo("/docs");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[top](#top)\n\n```\nx\n```");
            parameters.Add(p => p.CodeBlockTemplate, code => builder => builder.AddContent(0, "CODE"));
        });

        Assert.AreEqual("/docs#top", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldRenderWhereThereIsNoPageAddress()
    {
        // An HtmlRenderer drawing into an email or a static file has a NavigationManager nobody initialized.
        Services.AddSingleton<NavigationManager>(new UninitializedNavigationManager());

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[top](#top) ![x](https://example.com/x.png)");
        });

        Assert.AreEqual("#top", component.Find(".bit-mdv a").GetAttribute("href"));
        // Whether an image is cross-origin cannot be told, so the SameOrigin policy blocks it.
        Assert.IsFalse(component.Find(".bit-mdv img").HasAttribute("src"));
    }

    private sealed class UninitializedNavigationManager : NavigationManager { }

    [DataTestMethod]
    [DataRow(1, "# a\n\n## b", "h2,h3")]
    [DataRow(2, "# a\n\n###### b", "h3,h6")]
    [DataRow(-1, "# a\n\n### b", "h1,h2")]
    [DataRow(0, "## a", "h2")]
    public void BitMarkdownViewerShouldShiftTheHeadingLevels(int offset, string markdown, string expected)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, markdown);
            parameters.Add(p => p.HeadingLevelOffset, offset);
        });

        var headings = component.FindAll(".bit-mdv h1, .bit-mdv h2, .bit-mdv h3, .bit-mdv h4, .bit-mdv h5, .bit-mdv h6");

        Assert.AreEqual(expected, string.Join(",", headings.Select(h => h.LocalName)));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepTheHeadingIdsAndContentWhenShifting()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# The *title* {#custom}");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build());
            parameters.Add(p => p.HeadingLevelOffset, 1);
        });

        var heading = component.Find(".bit-mdv h2");

        Assert.AreEqual("custom", heading.GetAttribute("id"));
        Assert.AreEqual(1, heading.QuerySelectorAll("em").Length);

        // The parsed document is the shifted one, so a table of contents read off it nests the same way.
        var node = BitMarkdownAstHelper.Descendants(component.Instance.Document).OfType<BitMarkdownHeadingNode>().Single();
        Assert.AreEqual(2, node.Level);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReparseWhenTheHeadingOffsetChanges()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# a");
        });

        component.Render(parameters => parameters.Add(p => p.HeadingLevelOffset, 2));

        Assert.AreEqual(1, component.FindAll(".bit-mdv h3").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNameAnInteractiveTaskAfterItsText()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] Write the **tests** for [docs](/d)");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, _ => { });
        });

        Assert.AreEqual("Write the tests for docs", component.Find(".bit-mdv input[type=checkbox]").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepTheTasksReadOnlyWhenDisabled()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] one");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, _ => { });
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Find(".bit-mdv input[type=checkbox]").HasAttribute("disabled"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));

        Assert.IsFalse(component.Find(".bit-mdv input[type=checkbox]").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotParseAgainWhenOnlyTheEnabledStateChanges()
    {
        int parsed = 0;
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- [ ] one");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.OnTaskChanged, _ => { });
            parameters.Add(p => p.OnParsed, _ => parsed++);
        });

        var document = component.Instance.Document;

        component.Render(parameters => parameters.Add(p => p.IsEnabled, false));
        Assert.IsTrue(component.Find(".bit-mdv input[type=checkbox]").HasAttribute("disabled"));

        component.Render(parameters => parameters.Add(p => p.IsEnabled, true));
        Assert.IsFalse(component.Find(".bit-mdv input[type=checkbox]").HasAttribute("disabled"));

        Assert.AreEqual(1, parsed);
        Assert.AreSame(document, component.Instance.Document);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMakeANamedDocumentARegion()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "text");
            parameters.Add(p => p.AriaLabel, "Release notes");
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Release notes", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepARoleOfItsOwn()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMarkdownViewer>(0);
            builder.AddAttribute(1, nameof(BitMarkdownViewer.Markdown), "text");
            builder.AddAttribute(2, nameof(BitMarkdownViewer.AriaLabel), "Comment");
            builder.AddAttribute(3, "role", "article");
            builder.CloseComponent();
        });

        Assert.AreEqual("article", component.Find(".bit-mdv").GetAttribute("role"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotGiveAnUnnamedDocumentARole()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters => parameters.Add(p => p.Markdown, "text"));

        Assert.IsFalse(component.Find(".bit-mdv").HasAttribute("role"));
    }

    [DataTestMethod]
    [DataRow("aria-labelledby", "post-title")]
    [DataRow("aria-label", "Comment")]
    public void BitMarkdownViewerShouldMakeADocumentNamedBySplattedAttributesARegion(string attribute, string value)
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitMarkdownViewer>(0);
            builder.AddAttribute(1, nameof(BitMarkdownViewer.Markdown), "text");
            builder.AddAttribute(2, attribute, value);
            builder.CloseComponent();
        });

        var root = component.Find(".bit-mdv");

        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual(value, root.GetAttribute(attribute));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldGiveEachBlockItsOwnDirectionUnderAuto()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# عنوان\n\nEnglish text.\n\n- یک\n\n> متن");
            parameters.Add(p => p.Dir, BitDir.Auto);
        });

        Assert.AreEqual("auto", component.Find(".bit-mdv").GetAttribute("dir"));
        Assert.AreEqual("auto", component.Find(".bit-mdv h1").GetAttribute("dir"));
        Assert.AreEqual("auto", component.Find(".bit-mdv ul").GetAttribute("dir"));
        Assert.IsTrue(component.FindAll(".bit-mdv p").All(p => p.GetAttribute("dir") == "auto"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldGiveEachListItemAndCellItsOwnDirectionUnderAuto()
    {
        // A tight item's text sits straight in the <li>, so the <li> has to take its own direction, or every item
        // follows the first one.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "- این مورد فارسی است\n- English item\n\n| ستون | Column |\n|---|---|\n| یک | one |\n\n> [!NOTE]\n> x");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.Dir, BitDir.Auto);
        });

        Assert.IsTrue(component.FindAll(".bit-mdv li").All(li => li.GetAttribute("dir") == "auto"));
        Assert.IsTrue(component.FindAll(".bit-mdv th, .bit-mdv td").All(cell => cell.GetAttribute("dir") == "auto"));
        Assert.AreEqual(4, component.FindAll(".bit-mdv th, .bit-mdv td").Count);
        Assert.AreEqual("auto", component.Find(".bit-mdv .markdown-alert-title").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldGiveDefinitionsTheirOwnDirectionUnderAuto()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "واژه\n: تعریف");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseDefinitionLists().Build());
            parameters.Add(p => p.Dir, BitDir.Auto);
        });

        Assert.AreEqual("auto", component.Find(".bit-mdv dt").GetAttribute("dir"));
        Assert.AreEqual("auto", component.Find(".bit-mdv dd").GetAttribute("dir"));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow(BitDir.Rtl)]
    public void BitMarkdownViewerShouldNotGiveBlocksADirectionOtherwise(BitDir? dir)
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# a\n\nb\n\n- c");
            parameters.Add(p => p.Dir, dir);
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv h1[dir], .bit-mdv p[dir], .bit-mdv ul[dir]").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMakeCodeBlocksKeyboardScrollable()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters => parameters.Add(p => p.Markdown, "```\nx\n```"));

        var pre = component.Find(".bit-mdv pre");

        Assert.AreEqual("0", pre.GetAttribute("tabindex"));
        // A tab stop needs a name, and a name needs a role.
        Assert.AreEqual("region", pre.GetAttribute("role"));
        Assert.AreEqual("Code block", pre.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldWrapEachTemplateInABoundaryTheStylesheetStopsAt()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[a](/a) ![b](/b.png)\n\n```\nx\n```");
            parameters.Add(p => p.CodeBlockTemplate, code => builder => builder.AddMarkupContent(0, "<button>copy</button>"));
            parameters.Add(p => p.LinkTemplate, link => builder => builder.AddMarkupContent(0, "<b>link</b>"));
            parameters.Add(p => p.ImageTemplate, image => builder => builder.AddMarkupContent(0, "<i>image</i>"));
        });

        // A div may not sit in a paragraph, so the inline nodes get a span.
        AssertWrapper(component.Find(".bit-mdv button"), "DIV");
        AssertWrapper(component.Find(".bit-mdv b"), "SPAN");
        AssertWrapper(component.Find(".bit-mdv i"), "SPAN");

        static void AssertWrapper(AngleSharp.Dom.IElement content, string tag)
        {
            var wrapper = content.ParentElement!;

            Assert.AreEqual(tag, wrapper.TagName);
            Assert.IsTrue(wrapper.ClassList.Contains("bit-mdv-tpl"));
        }
    }

    [TestMethod]
    public void BitMarkdownViewerShouldTellAScreenReaderALinkOpensInANewTab()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[docs](https://example.com) and [home](/home)");
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual("docs (opens in a new tab)", links[0].TextContent);
        Assert.AreEqual("bit-mdv-new-tab", links[0].LastElementChild!.ClassName);
        Assert.AreEqual("home", links[1].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNoticeANewTabTheLinkOptionsChose()
    {
        var texts = new BitMarkdownTexts { NewTab = "(neuer Tab)" };
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[docs](https://example.com) and [home](/home)");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder()
                                                .UseTexts(texts)
                                                .UseLinkOptions(externalTarget: BitMarkdownLinkTarget.Self, internalTarget: BitMarkdownLinkTarget.Blank)
                                                .Build());
        });

        var links = component.FindAll(".bit-mdv a");

        Assert.AreEqual("docs", links[0].TextContent);
        Assert.AreEqual("home (neuer Tab)", links[1].TextContent);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveTheNewTabNoticeOutWhenItHasNoText()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[docs](https://example.com)");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseTexts(new BitMarkdownTexts { NewTab = "" }).Build());
        });

        Assert.AreEqual(0, component.FindAll(".bit-mdv-new-tab").Count);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldHideItsScreenReaderTextInlineWhereThereIsNoPage()
    {
        // Rendered by an HtmlRenderer into an email or a static file, the output is read without the stylesheet.
        Services.AddSingleton<NavigationManager>(new UninitializedNavigationManager());

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[docs](https://example.com) and a note[^1].\n\n[^1]: Note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        StringAssert.Contains(component.Find(".bit-mdv-new-tab").GetAttribute("style"), "clip-path:inset(50%)");
        StringAssert.Contains(component.Find(".bit-mdv .footnotes .bit-mdv-sr-only").GetAttribute("style"), "clip-path:inset(50%)");
    }

    [TestMethod]
    public void BitMarkdownViewerShouldLeaveHidingItsScreenReaderTextToTheStylesheetOnAPage()
    {
        // A strict Content-Security-Policy refuses inline styles, so none is written where the stylesheet is.
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[docs](https://example.com) and a note[^1].\n\n[^1]: Note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        Assert.IsFalse(component.Find(".bit-mdv-new-tab").HasAttribute("style"));
        Assert.IsFalse(component.Find(".bit-mdv .footnotes .bit-mdv-sr-only").HasAttribute("style"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldHandALinkTemplateTheInPageDestinationWrittenAgainstThePage()
    {
        NavigateTo("/docs/page");

        var urls = new List<string>();
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[see](#install) and [other](/other)");
            parameters.Add(p => p.LinkTemplate, link => builder =>
            {
                urls.Add(link.Url);
                builder.AddContent(0, link.Url);
            });
        });

        CollectionAssert.AreEqual(new[] { "/docs/page#install", "/other" }, urls);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldNotRenderAgainWhenTheAddressChangesUnderADocumentWithNoInPageLink()
    {
        NavigateTo("/a");

        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "[elsewhere](/other) and text");
        });

        var renders = component.RenderCount;

        NavigateTo("/a?sort=new");

        Assert.AreEqual(renders, component.RenderCount);
        Assert.AreEqual("/other", component.Find(".bit-mdv a").GetAttribute("href"));
    }

    [TestMethod]
    public void BitMarkdownViewerShouldPrefixHeadingIdsAndTheLinksToThem()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "## Install\n\n## Café {#café}\n\nSee [install](#install), [café](#caf%C3%A9) and [the form](#signup).");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseAutoIdentifiers(anchorLinks: true).Build());
            parameters.Add(p => p.HeadingIdPrefix, "msg-1-");
        });

        var headings = component.FindAll(".bit-mdv h2");
        var links = component.FindAll(".bit-mdv p a");

        Assert.AreEqual("msg-1-install", headings[0].Id);
        Assert.AreEqual("msg-1-café", headings[1].Id);
        Assert.AreEqual("#msg-1-install", headings[0].QuerySelector(".bit-mdv-anchor")!.GetAttribute("href"));
        Assert.AreEqual("#msg-1-install", links[0].GetAttribute("href"));
        Assert.AreEqual("#msg-1-caf%C3%A9", links[1].GetAttribute("href"));
        // Nothing in the document has that id, so it points at the page and keeps the id it has there.
        Assert.AreEqual("#signup", links[2].GetAttribute("href"));
        Assert.AreEqual("msg-1-install", component.Instance.Document.Children.OfType<BitMarkdownHeadingNode>().First().Id);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldReapplyTheHeadingIdPrefixWhenItChanges()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "# Title");
            parameters.Add(p => p.Pipeline, new BitMarkdownPipelineBuilder().UseAutoIdentifiers().Build());
            parameters.Add(p => p.HeadingIdPrefix, "a-");
        });

        Assert.AreEqual("a-title", component.Find(".bit-mdv h1").Id);

        component.Render(parameters => parameters.Add(p => p.HeadingIdPrefix, "b-"));

        Assert.AreEqual("b-title", component.Find(".bit-mdv h1").Id);

        component.Render(parameters => parameters.Add(p => p.HeadingIdPrefix, (string?)null));

        Assert.AreEqual("title", component.Find(".bit-mdv h1").Id);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldDescribeAFootnoteReferenceByTheSectionsLabelNotItsNotes()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "One[^a] and two[^b].\n\n[^a]: First note.\n[^b]: Second note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var section = component.Find(".bit-mdv .footnotes");
        var label = section.QuerySelector(".bit-mdv-sr-only")!;
        var describedBy = component.Find(".footnote-ref a").GetAttribute("aria-describedby");

        // A description is read out in full: pointing at the section would read every note after every reference.
        Assert.AreEqual(label.Id, describedBy);
        Assert.AreEqual(label.Id, section.GetAttribute("aria-labelledby"));
        Assert.AreEqual("Footnotes", label.TextContent);
        Assert.AreEqual("H2", label.TagName);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldMoveTheFootnotesHeadingWithTheHeadingLevelOffset()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "Text[^1].\n\n[^1]: Note.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
            parameters.Add(p => p.HeadingLevelOffset, 2);
        });

        Assert.AreEqual("H4", component.Find(".bit-mdv .footnotes .bit-mdv-sr-only").TagName);
    }

    [TestMethod]
    public void BitMarkdownViewerShouldKeepTheAlertIconOutOfTheAccessibleName()
    {
        var component = RenderComponent<BitMarkdownViewer>(parameters =>
        {
            parameters.Add(p => p.Markdown, "> [!WARNING]\n> Mind the gap.");
            parameters.Add(p => p.Pipeline, BitMarkdownPipelines.GitHub);
        });

        var icon = component.Find(".bit-mdv .markdown-alert-title svg");

        Assert.IsTrue(icon.ClassList.Contains("bit-mdv-alert-icon"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
        Assert.AreEqual("currentColor", icon.GetAttribute("stroke"));
        Assert.AreEqual("Warning", component.Find(".bit-mdv .markdown-alert-title").TextContent);
    }
}
