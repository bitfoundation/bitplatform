using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MarkdownView;

/// <summary>
/// The view's extension points and policies: custom code languages, mathematics, footnotes, raw HTML, headings, links
/// and the image policy.
/// </summary>
[TestClass]
public class BitAiMarkdownViewHooksTests : BitAiTestBase
{
    private IRenderedComponent<BitAiMarkdownView> RenderMarkdown(string source, BitAiMarkdownOptions? options = null, BitAiUiOptions? ui = null) =>
        RenderComponent<BitAiMarkdownView>(parameters =>
        {
            parameters.Add(p => p.Document, BitAiMarkdownParser.Parse(source, options));
            if (ui is not null) parameters.Add(p => p.CascadedOptions, ui);
        });



    [TestMethod]
    public void ARegisteredCodeLanguageRendersThroughItsOwnRenderer()
    {
        Renderers.RegisterCodeLanguage("mermaid", context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "diagram");
            builder.AddAttribute(2, "data-streaming", context.IsStreaming);
            builder.AddContent(3, context.Code);
            builder.CloseElement();
        });

        var component = RenderMarkdown("```mermaid\ngraph TD; A-->B\n```");

        Assert.AreEqual("graph TD; A-->B", component.Find(".bit-aimdv > div.diagram").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aicbv"));
    }

    [TestMethod]
    public void CodeLanguagesAreMatchedWithoutRegardToCaseAndOtherLanguagesStillUseTheCodeView()
    {
        Renderers.RegisterCodeLanguage("Vega", _ => builder => builder.AddMarkupContent(0, "<div class=\"chart\"></div>"));

        Assert.IsNotNull(RenderMarkdown("```vega\n{}\n```").Find("div.chart"));
        Assert.IsNotNull(RenderMarkdown("```json\n{}\n```").Find(".bit-aicbv"));
    }

    [TestMethod]
    public void MathFallsBackToMonospaceWithTheSourceInADataAttribute()
    {
        var component = RenderMarkdown("Inline $x^2$ and\n\n$$\nE = mc^2\n$$");

        var inline = component.Find(".bit-aimdv-par > .bit-aimdv-mth");
        Assert.AreEqual("x^2", inline.GetAttribute("data-math"));
        Assert.IsFalse(inline.ClassList.Contains("bit-aimdv-mtb"));

        var block = component.Find(".bit-aimdv-mtb");
        Assert.IsTrue(block.ClassList.Contains("bit-aimdv-mth"));
        Assert.AreEqual("E = mc^2", block.GetAttribute("data-math"));
    }

    [TestMethod]
    public void ARegisteredMathRendererReplacesTheFallback()
    {
        Renderers.MathRenderer = context => builder =>
        {
            builder.OpenElement(0, context.IsBlock ? "div" : "span");
            builder.AddAttribute(1, "class", "katex");
            builder.AddContent(2, context.Expression);
            builder.CloseElement();
        };

        var component = RenderMarkdown("Inline $x^2$ and\n\n$$\nE\n$$");

        Assert.AreEqual("x^2", component.Find("span.katex").TextContent);
        Assert.AreEqual("E", component.Find("div.katex").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aimdv-mth"));
    }

    [TestMethod]
    public void ACodeLanguageRendererThatThrowsCostsItsBlockAlone()
    {
        Renderers.RegisterCodeLanguage("mermaid", _ => _ => throw new InvalidOperationException("unexpected shape"));

        var component = RenderMarkdown("before\n\n```mermaid\ngraph TD; A-->\n```\n\nafter");

        var paragraphs = component.FindAll(".bit-aimdv > p.bit-aimdv-par").Select(p => p.TextContent).ToArray();
        CollectionAssert.AreEqual(new[] { "before", "after" }, paragraphs);

        var notice = component.Find(".bit-aimdv > .bit-airbd");
        StringAssert.Contains(notice.TextContent, "The renderer for 'mermaid' failed");
        Assert.AreEqual("unexpected shape", component.FindComponent<BitTag>().Instance.Title);
    }

    [TestMethod]
    public void ACodeLanguageRendererThatThrowsWhenAskedForItsMarkupIsContainedToo()
    {
        Renderers.RegisterCodeLanguage("vega", _ => throw new InvalidOperationException("no spec"));

        var component = RenderMarkdown("```vega\n{}\n```\n\nafter");

        StringAssert.Contains(component.Find(".bit-airbd").TextContent, "The renderer for 'vega' failed");
        Assert.AreEqual("after", component.Find("p.bit-aimdv-par").TextContent);
    }

    [TestMethod]
    public void AMathRendererThatThrowsLeavesTheRestOfTheDocumentAndANoticeInPlaceOfEachExpression()
    {
        Renderers.MathRenderer = _ => _ => throw new InvalidOperationException("bad TeX");

        var component = RenderMarkdown("Inline $x^2$ here\n\n$$\nE = mc^2\n$$\n\nafter");

        var notices = component.FindAll(".bit-airbd");
        Assert.HasCount(2, notices);
        Assert.IsTrue(notices.All(n => n.TextContent.Contains("The renderer for 'math' failed")));

        // The notice of an inline expression stays in its line, inside the paragraph, between the words around it.
        var inline = component.Find(".bit-aimdv-par > .bit-airbd");
        Assert.IsTrue(inline.ClassList.Contains("bit-aimdv-rbi"));
        StringAssert.StartsWith(inline.ParentElement!.TextContent, "Inline");
        StringAssert.EndsWith(inline.ParentElement!.TextContent, "here");

        // The block expression's notice stands in a block of its own.
        var block = component.Find(".bit-aimdv > .bit-airbd");
        Assert.IsFalse(block.ClassList.Contains("bit-aimdv-rbi"));

        Assert.AreEqual("after", component.FindAll(".bit-aimdv > p.bit-aimdv-par").Last().TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aimdv-mth"));
    }

    [TestMethod]
    public void ARegisteredCitationRendererReplacesTheChip()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        BitAiCitationContext? handed = null;
        Renderers.CitationRenderer = context => builder =>
        {
            handed = context;
            builder.OpenElement(0, "sup");
            builder.AddAttribute(1, "class", "my-chip");
            builder.AddContent(2, context.Citation.Source.Title);
            builder.CloseElement();
        };

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here, unlike [7]"))
            .Add(p => p.Citations, new[] { citation }));

        Assert.IsEmpty(component.FindComponents<BitAiCitationMarker>());
        Assert.AreEqual("The Docs", component.Find(".bit-aimdv-par > sup.my-chip").TextContent);
        Assert.AreSame(citation, handed!.Citation);

        // A number no citation answers to stays the text the model wrote, renderer or not.
        StringAssert.Contains(component.Find(".bit-aimdv-par").TextContent, "[7]");
    }

    [TestMethod]
    public void ACitationRendererIsHandedTheOnCitationClickOfTheViewForItsChipToRaise()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        BitAiCitation? clicked = null;
        BitAiCitationContext? handed = null;
        Renderers.CitationRenderer = context => builder =>
        {
            handed = context;
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "my-chip");
            builder.AddAttribute(2, "onclick", (Func<Task>)context.ActivateAsync);
            builder.AddContent(3, context.Citation.Number);
            builder.CloseElement();
        };

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here"))
            .Add(p => p.Citations, new[] { citation })
            .Add(p => p.OnCitationClick, c => clicked = c));

        Assert.IsTrue(handed!.OnActivate.HasDelegate);
        Assert.IsNull(handed.Message);

        component.Find("button.my-chip").Click();

        // The same callback the built-in chip raises, with the citation the chip was drawn for.
        Assert.AreSame(citation, clicked);
    }

    [TestMethod]
    public async Task WithoutOnCitationClickActivatingTheChipOfACitationRendererDoesNothing()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        BitAiCitationContext? handed = null;
        Renderers.CitationRenderer = context => builder =>
        {
            handed = context;
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "my-chip");
            builder.AddAttribute(2, "onclick", (Func<Task>)context.ActivateAsync);
            builder.CloseElement();
        };

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here"))
            .Add(p => p.Citations, new[] { citation }));

        Assert.IsFalse(handed!.OnActivate.HasDelegate);

        await handed.ActivateAsync();
        component.Find("button.my-chip").Click();

        Assert.AreSame(citation, handed.Citation);
    }

    [TestMethod]
    public async Task TheContextOfACitationActivatesItThroughTheCallbackItWasHanded()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs"));
        var activated = new List<BitAiCitation>();

        await new BitAiCitationContext(citation).ActivateAsync();
        await new BitAiCitationContext(citation, OnActivate: new EventCallback<BitAiCitation>(null, (Action<BitAiCitation>)activated.Add)).ActivateAsync();

        Assert.AreSame(citation, Assert.ContainsSingle(activated));
    }

    [TestMethod]
    public void ACitationRendererIsNotAskedWhileInlineCitationsAreOff()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        Renderers.CitationRenderer = _ => builder => builder.AddMarkupContent(0, "<sup class=\"my-chip\"></sup>");

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here"))
            .Add(p => p.Citations, new[] { citation })
            .Add(p => p.CascadedOptions, BitAiUiOptions.Default with { InlineCitations = false }));

        Assert.IsEmpty(component.FindAll(".my-chip"));
        StringAssert.Contains(component.Find(".bit-aimdv-par").TextContent, "[1]");
    }

    [TestMethod]
    public void ACitationRendererThatThrowsLeavesANoticeInTheLine()
    {
        var citation = new BitAiCitation("c1", new BitAiCitationSource("s1", "The Docs")) { Number = 1 };
        Renderers.CitationRenderer = _ => _ => throw new InvalidOperationException("no chip");

        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("as stated [1] here\n\nafter"))
            .Add(p => p.Citations, new[] { citation }));

        var notice = component.Find(".bit-aimdv-par > .bit-airbd");
        Assert.IsTrue(notice.ClassList.Contains("bit-aimdv-rbi"));
        StringAssert.Contains(notice.TextContent, "The renderer for 'citation' failed");
        StringAssert.StartsWith(notice.ParentElement!.TextContent, "as stated");
        Assert.AreEqual("after", component.FindAll(".bit-aimdv > p.bit-aimdv-par").Last().TextContent);
    }

    [TestMethod]
    public void FootnotesAreListedAfterTheTextWithBackLinks()
    {
        var component = RenderMarkdown("Claim[^1] and again[^1].\n\n[^1]: The note.");

        var references = component.FindAll("sup.bit-aimdv-fnr");
        Assert.HasCount(2, references);
        Assert.AreEqual("1", references[0].TextContent);

        var section = component.Find("section.bit-aimdv-fns");
        Assert.AreEqual("doc-endnotes", section.GetAttribute("role"));
        StringAssert.Contains(section.TextContent, "The note.");

        var backLinks = section.QuerySelectorAll("a.bit-aimdv-fnb");
        Assert.HasCount(2, backLinks);
        Assert.AreEqual("doc-backlink", backLinks[0].GetAttribute("role"));

        // A note cited twice has a back-link per citation, each named apart from the other.
        Assert.AreEqual("Back to reference 1", backLinks[0].GetAttribute("aria-label"));
        Assert.AreEqual("Back to reference 1-2", backLinks[1].GetAttribute("aria-label"));

        // Each reference links to the note, and each back-link to its reference.
        var noteId = section.QuerySelector("li.bit-aimdv-fni")!.Id;
        Assert.AreEqual("#" + noteId, references[0].QuerySelector("a.bit-aimdv-fra")!.GetAttribute("href"));
        Assert.AreEqual(references[0].Id, backLinks[0].GetAttribute("href")![1..]);
        Assert.AreEqual(references[1].Id, backLinks[1].GetAttribute("href")![1..]);
    }

    [TestMethod]
    public void TheFootnotesOfTwoViewsDoNotShareAnchors()
    {
        var first = RenderMarkdown("Claim[^1].\n\n[^1]: One.");
        var second = RenderMarkdown("Claim[^1].\n\n[^1]: Two.");

        Assert.AreNotEqual(first.Find("li.bit-aimdv-fni").Id, second.Find("li.bit-aimdv-fni").Id);
        Assert.AreNotEqual(first.Find("sup.bit-aimdv-fnr").Id, second.Find("sup.bit-aimdv-fnr").Id);
    }

    [TestMethod]
    public void AFootnoteReferenceIsFollowedOnThePageRatherThanByTheBrowser()
    {
        var component = RenderMarkdown("Claim[^1] and again[^2].\n\n[^1]: One.\n[^2]: Two.");

        var notes = component.FindAll("li.bit-aimdv-fni").Select(n => n.Id).ToArray();
        var reference = component.FindAll("a.bit-aimdv-fra")[1];

        // The href still names the note - what a copy of the link and a page without script have - but the click is the
        // view's: "#..." would be resolved against the base address of the app rather than this page.
        Assert.AreEqual("#" + notes[1], reference.GetAttribute("href"));
        Assert.IsTrue(PreventsTheClickDefault(reference));

        reference.Click();

        var follow = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMarkdownView.follow");
        Assert.AreEqual(notes[1], follow.Arguments[0]);
    }

    [TestMethod]
    public void EachBackLinkIsFollowedOnThePageToItsOwnReference()
    {
        var component = RenderMarkdown("Claim[^1] and again[^1].\n\n[^1]: The note.");

        var references = component.FindAll("sup.bit-aimdv-fnr").Select(r => r.Id).ToArray();
        var backLinks = component.FindAll("a.bit-aimdv-fnb");

        Assert.IsTrue(backLinks.All(PreventsTheClickDefault));

        backLinks[1].Click();

        var follow = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiMarkdownView.follow");
        Assert.AreEqual(references[1], follow.Arguments[0]);

        component.FindAll("a.bit-aimdv-fnb")[0].Click();

        Assert.AreEqual(references[0], Context.JSInterop.Invocations["BitBlazorUI.AiMarkdownView.follow"][^1].Arguments[0]);
    }

    [TestMethod]
    public void AnOrdinaryLinkIsStillTheBrowsers()
    {
        var component = RenderMarkdown("[docs](https://bitplatform.dev)");

        var link = component.Find("a.bit-aimdv-lnk");

        Assert.IsFalse(PreventsTheClickDefault(link));
        Assert.IsFalse(link.Attributes.Any(a => a.Name.Equals("blazor:onclick", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool PreventsTheClickDefault(AngleSharp.Dom.IElement element) =>
        element.Attributes.Any(a => a.Name.Equals("blazor:onclick:preventDefault", StringComparison.OrdinalIgnoreCase));

    [TestMethod]
    public void ADocumentWithoutFootnotesHasNoFootnoteSection() =>
        Assert.IsEmpty(RenderMarkdown("plain").FindAll(".bit-aimdv-fns"));

    [TestMethod]
    public void AnHtmlBlockIsShownAsTextWhenTheRendererDoesNotAllowRawHtml()
    {
        var component = RenderMarkdown("<div onclick=\"x()\">hi</div>", new BitAiMarkdownOptions { AllowRawHtml = true });

        Assert.IsFalse(component.Markup.Contains("<div onclick"));
        Assert.IsEmpty(component.FindAll("pre"));
        StringAssert.StartsWith(component.Find("p.bit-aimdv-par").TextContent, "<div onclick=");
    }

    [TestMethod]
    public void AnHtmlBlockIsRenderedWhenTheRendererAllowsRawHtml()
    {
        var options = new BitAiMarkdownOptions { AllowRawHtml = true };
        var component = RenderMarkdown("<div class=\"raw\">hi</div>", options, BitAiUiOptions.Default with { Markdown = options });

        Assert.AreEqual("hi", component.Find("div.raw").TextContent);
    }

    [TestMethod]
    public void HeadingIdentifiersArePrefixedInTheMarkup()
    {
        var component = RenderMarkdown("## Setup");

        Assert.AreEqual($"{component.Find(".bit-aimdv").Id}-bit-ai-h-setup", component.Find(".bit-aimdv-hdg").Id);
    }

    [TestMethod]
    public void TheHeadingsOfTwoViewsDoNotShareIds()
    {
        var first = RenderMarkdown("## Setup");
        var second = RenderMarkdown("## Setup");

        Assert.AreNotEqual(first.Find(".bit-aimdv-hdg").Id, second.Find(".bit-aimdv-hdg").Id);
    }

    [TestMethod]
    public void AHeadingIsNamedAfterTheIdOfItsView()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("## Setup\n\n## Setup"))
            .Add(p => p.Id, "answer"));

        CollectionAssert.AreEqual(new[] { "answer-bit-ai-h-setup", "answer-bit-ai-h-setup-2" },
                                  component.FindAll(".bit-aimdv-hdg").Select(h => h.Id).ToArray());
    }

    [TestMethod]
    public void LinkTitlesAreRendered() =>
        Assert.AreEqual("The docs", RenderMarkdown("[docs](https://x.dev \"The docs\")").Find("a.bit-aimdv-lnk").GetAttribute("title"));

    [TestMethod]
    public void EntitiesRenderAsTheirCharacters() =>
        Assert.AreEqual("AT&T ©", RenderMarkdown("AT&amp;T &copy;").Find("p.bit-aimdv-par").TextContent);

    [TestMethod]
    public void IndentedCodeRendersAsACodeBlock()
    {
        var component = RenderMarkdown("para\n\n    var x = 1;");

        StringAssert.Contains(component.Find(".bit-aicbv-cod").TextContent, "var x = 1;");
    }

    [TestMethod]
    public void ReferenceLinksRenderAsLinks()
    {
        var component = RenderMarkdown("See [the docs][d].\n\n[d]: https://x.dev/docs");

        Assert.AreEqual("https://x.dev/docs", component.Find("a.bit-aimdv-lnk").GetAttribute("href"));
        Assert.IsFalse(component.Markup.Contains("[d]"));
    }

    [TestMethod]
    public void PowerShellCodeIsHighlightedWithItsOwnTokens()
    {
        var component = RenderMarkdown("```powershell\nGet-ChildItem -Path $env:HOME\n```");

        Assert.IsNotEmpty(component.FindAll(".bit-aicbv-fnc"));
        Assert.IsNotEmpty(component.FindAll(".bit-aicbv-vrb"));
    }

    [TestMethod]
    public void ImagesAreShownByDefault()
    {
        var image = RenderMarkdown("![cat](https://img.x.dev/cat.png \"A cat\")").Find("img.bit-aimdv-img");

        Assert.AreEqual("https://img.x.dev/cat.png", image.GetAttribute("src"));
        Assert.AreEqual("cat", image.GetAttribute("alt"));
        Assert.AreEqual("A cat", image.GetAttribute("title"));
        Assert.AreEqual("lazy", image.GetAttribute("loading"));
    }

    [TestMethod]
    public void AnImageIsToldToLoadLazilyBeforeItIsGivenItsSource()
    {
        var image = RenderMarkdown("![cat](https://img.x.dev/cat.png)").Find("img.bit-aimdv-img");
        var names = image.Attributes.Select(a => a.Name).ToList();

        // Blazor sets the attributes in the order they are written; an engine handed the source first fetches it then.
        Assert.IsLessThan(names.IndexOf("src"), names.IndexOf("loading"));
        Assert.IsGreaterThanOrEqualTo(0, names.IndexOf("loading"));
    }

    [TestMethod]
    public void TheBlockPolicyReplacesImagesWithTheirAlternativeText()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };
        var component = RenderMarkdown("![a cat](https://img.x.dev/cat.png) ![](data:image/png;base64,AAA)", ui: ui);

        Assert.IsEmpty(component.FindAll("img"));
        var blocked = component.FindAll(".bit-aimdv-imb");
        Assert.HasCount(1, blocked);
        Assert.AreEqual("a cat", blocked[0].TextContent);
    }

    [TestMethod]
    public void TheClickToLoadPolicyShowsAPlaceholderUntilActivated()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
        var component = RenderMarkdown("![a cat](https://img.x.dev/cat.png)", ui: ui);

        Assert.IsEmpty(component.FindAll("img"));
        var placeholder = component.Find("button.bit-acb");
        Assert.AreEqual("Load image: a cat", placeholder.TextContent.Trim());
        Assert.AreEqual("https://img.x.dev/cat.png", placeholder.GetAttribute("title"));

        placeholder.Click();

        Assert.AreEqual("https://img.x.dev/cat.png", component.Find("img.bit-aimdv-img").GetAttribute("src"));
        Assert.IsEmpty(component.FindAll("button.bit-acb"));
    }

    // The button pressed is gone once the image is shown, and the focus would drop to the body of the document with it.
    [TestMethod]
    public void TheImageTheUserLoadedTakesTheFocusOffThePlaceholder()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
        var component = RenderMarkdown("![a cat](https://img.x.dev/cat.png) and ![a dog](https://img.x.dev/dog.png)", ui: ui);

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        component.FindAll("button.bit-acb")[1].Click();

        // The image of the button pressed - the other one is still a button - takes the focus, and can hold it.
        var dog = component.Find("img.bit-aimdv-img");
        Assert.AreEqual("https://img.x.dev/dog.png", dog.GetAttribute("src"));
        Assert.AreEqual("-1", dog.GetAttribute("tabindex"));

        var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
        Assert.HasCount(1, focus);
        Assert.AreEqual(dog.GetAttribute("blazor:elementreference"), ((ElementReference)focus[0].Arguments[0]!).Id);

        // Rendered again, the image keeps the focus where it is.
        component.Render();

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void AnImageThePolicyShowsAtOnceTakesNoFocus()
    {
        var image = RenderMarkdown("![a cat](https://img.x.dev/cat.png)").Find("img.bit-aimdv-img");

        Assert.IsFalse(image.HasAttribute("tabindex"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    [TestMethod]
    public void APlaceholderWithoutAlternativeTextIsNamedAfterTheHost()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
        var component = RenderMarkdown("![](https://img.x.dev/cat.png)", ui: ui);

        Assert.AreEqual("Load image: img.x.dev", component.Find("button.bit-acb").TextContent.Trim());
    }

    [TestMethod]
    public void ThePlaceholderTakesTheClassesOfItsPart()
    {
        var component = RenderComponent<BitAiMarkdownView>(parameters => parameters
            .Add(p => p.Document, BitAiMarkdownParser.Parse("![a cat](https://img.x.dev/cat.png)"))
            .Add(p => p.CascadedOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad })
            .Add(p => p.Classes, new() { ImagePlaceholder = new() { Root = "c-placeholder", Icon = "c-icon" } }));

        Assert.IsTrue(component.Find("button.bit-acb").ClassList.Contains("c-placeholder"));

        // The glyph is drawn from the package's own icon subset, beside the classes the app gave the icon.
        var icon = component.Find("button.bit-acb .bit-acb-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Photo2"));
        Assert.IsTrue(icon.ClassList.Contains("c-icon"));
    }

    [TestMethod]
    public void ClickToLoadStillShowsInlineDataImages()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
        var component = RenderMarkdown("![x](data:image/png;base64,AAA)", ui: ui);

        Assert.AreEqual("data:image/png;base64,AAA", component.Find("img").GetAttribute("src"));
    }

    [TestMethod]
    public void TheHostListPolicyShowsListedHostsAndAsksForTheRest()
    {
        var ui = BitAiUiOptions.Default with { Images = BitAiImagePolicy.AllowListedHosts("cdn.x.dev", "*.trusted.org") };
        var component = RenderMarkdown(
            "![a](https://cdn.x.dev/a.png) ![b](https://img.trusted.org/b.png) ![c](https://evil.example/c.png)",
            ui: ui);

        var sources = component.FindAll("img").Select(i => i.GetAttribute("src")).ToArray();

        CollectionAssert.AreEqual(new[] { "https://cdn.x.dev/a.png", "https://img.trusted.org/b.png" }, sources);
        Assert.HasCount(1, component.FindAll("button.bit-acb"));
    }

    [TestMethod]
    public void TheHostListPolicyAsksAboutARelativeImageAsTheAddressTheBrowserFetches()
    {
        // bUnit's base address is http://localhost/, which is where the browser would fetch these from.
        var own = RenderMarkdown("![a](/img/a.png) ![b](img/b.png)", ui: BitAiUiOptions.Default with { Images = BitAiImagePolicy.AllowListedHosts("localhost") });
        var cdn = RenderMarkdown("![a](/img/a.png)", ui: BitAiUiOptions.Default with { Images = BitAiImagePolicy.AllowListedHosts("cdn.x.dev") });

        // Shown as written: the browser resolves it.
        CollectionAssert.AreEqual(new[] { "/img/a.png", "img/b.png" }, own.FindAll("img.bit-aimdv-img").Select(i => i.GetAttribute("src")).ToArray());
        Assert.IsEmpty(own.FindAll("button.bit-acb"));

        Assert.IsEmpty(cdn.FindAll("img"));
        Assert.AreEqual("/img/a.png", cdn.Find("button.bit-acb").GetAttribute("title"));
    }

    [TestMethod]
    public void APlaceholderOfARelativeImageWithoutAlternativeTextIsNamedAfterTheHostOfTheApp()
    {
        var component = RenderMarkdown("![](/img/a.png)", ui: BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad });

        Assert.AreEqual("Load image: localhost", component.Find("button.bit-acb").TextContent.Trim());
    }

    [TestMethod]
    public void ScriptImageUrlsAreNeutralisedUnderEveryPolicy()
    {
        foreach (var policy in new[] { BitAiImagePolicy.Allow, BitAiImagePolicy.ClickToLoad, BitAiImagePolicy.Block, BitAiImagePolicy.AllowListedHosts("x.dev") })
        {
            var markup = RenderMarkdown("![x](javascript:alert(1))", ui: BitAiUiOptions.Default with { Images = policy }).Markup;

            Assert.IsFalse(markup.Contains("javascript:", StringComparison.OrdinalIgnoreCase), policy.Mode.ToString());
        }
    }
}
