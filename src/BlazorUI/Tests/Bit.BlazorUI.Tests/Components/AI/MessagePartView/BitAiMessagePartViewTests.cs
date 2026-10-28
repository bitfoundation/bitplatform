using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessagePartView;

/// <summary>
/// The dispatch of the part view, and the artifact tests of the original suite that render an artifact part through it.
/// The artifact panel itself is BitAiArtifactView's: these read it through what does not depend on its class names - the
/// component it is, its accessible names, its sandbox and the elements it renders the content as.
/// </summary>
[TestClass]
public class BitAiMessagePartViewTests : BitAiTestBase
{
    private static readonly BitAiCitationSource DocsSource = new("s1", "Docs") { Url = "https://learn.microsoft.com/aspnet" };

    private static (BitAiConversation Conversation, BitAiMessage Message, BitAiArtifactPart Part) WithArtifact(
        BitAiArtifactKind kind, string title, string content)
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", kind, title);
        artifact.SetContent(content);
        artifact.Complete();

        var part = new BitAiArtifactPart("a1") { ArtifactVersion = artifact.Version };
        var message = conversation.AddMessage(Assistant(part));
        return (conversation, message, part);
    }

    private IRenderedComponent<BitAiMessagePartView> RenderPart((BitAiConversation Conversation, BitAiMessage Message, BitAiArtifactPart Part) setup) =>
        RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, setup.Part)
            .Add(c => c.Message, setup.Message)
            .Add(c => c.Conversation, setup.Conversation));

    private static Delegate? Subscribers(BitAiMessagePart part) =>
        (Delegate?)typeof(BitAiMessagePart)
            .GetField(nameof(BitAiMessagePart.Changed), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(part);



    [TestMethod]
    public void AnArtifactPartRendersTheArtifactPanelRatherThanAnUnknownElement()
    {
        var component = RenderPart(WithArtifact(BitAiArtifactKind.Document, "Project README", "# Hello\n\nWorld"));

        Assert.IsFalse(component.Markup.Contains("artifactpartview", StringComparison.OrdinalIgnoreCase));
        Assert.HasCount(1, component.FindComponents<BitAiArtifactView>());
        StringAssert.Contains(component.Markup, "Project README");
        Assert.IsNotNull(component.Find("button[aria-label='Download']"));
        Assert.IsNotNull(component.Find("button[aria-label='Expand']"));
    }

    [TestMethod]
    public void AnArtifactThatCannotBeResolvedRendersAPlaceholder()
    {
        var setup = WithArtifact(BitAiArtifactKind.Code, "x", "y");
        var component = RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, new BitAiArtifactPart("missing"))
            .Add(c => c.Message, setup.Message)
            .Add(c => c.Conversation, setup.Conversation));

        Assert.IsEmpty(component.FindComponents<BitAiArtifactView>());
        StringAssert.Contains(component.Markup, "Artifact");
    }

    [TestMethod]
    public void HtmlArtifactsRenderInAFrameWithoutSameOriginAccess()
    {
        var component = RenderPart(WithArtifact(BitAiArtifactKind.Html, "Page", "<p>hi</p><script>parent.x=1</script>"));

        var frame = component.Find("iframe");
        Assert.AreEqual("allow-scripts allow-forms allow-popups", frame.GetAttribute("sandbox"));
        Assert.IsFalse(frame.GetAttribute("sandbox")!.Contains("allow-same-origin", StringComparison.Ordinal));
        Assert.IsEmpty(component.FindAll("script"));
    }

    [TestMethod]
    public void SvgArtifactsRenderAsAnImageSoEmbeddedScriptNeverRuns()
    {
        var component = RenderPart(WithArtifact(BitAiArtifactKind.Svg, "Logo", "<svg onload=\"alert(1)\"><script>alert(2)</script></svg>"));

        var image = component.Find("img[src^='data:image/svg+xml;base64,']");
        Assert.IsNotNull(image);
        Assert.IsEmpty(component.FindAll("svg[onload]"));
        Assert.IsEmpty(component.FindAll("script"));
    }

    [TestMethod]
    public void CsvTableArtifactsRenderAsATable()
    {
        var component = RenderPart(WithArtifact(BitAiArtifactKind.Table, "Prices", "name,price\n\"Widget, large\",3\nGadget,4\n"));

        CollectionAssert.AreEqual(new[] { "name", "price" }, component.FindAll("th").Select(th => th.TextContent).ToArray());
        Assert.AreEqual("Widget, large", component.FindAll("td")[0].TextContent);
        Assert.HasCount(2, component.FindAll("tbody tr"));
    }

    [TestMethod]
    public void ARegisteredArtifactRendererReplacesTheBuiltInBody()
    {
        Renderers.RegisterArtifact(BitAiArtifactKind.Chart, context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "my-chart");
            builder.AddContent(2, context.Artifact.Content);
            builder.CloseElement();
        });

        var component = RenderPart(WithArtifact(BitAiArtifactKind.Chart, "Sales", "{\"type\":\"bar\"}"));

        Assert.AreEqual("{\"type\":\"bar\"}", component.Find(".my-chart").TextContent);
    }

    [TestMethod]
    public void EachKindOfPartIsDispatchedToItsView()
    {
        var call = new BitAiToolCallPart("c1", "search");
        var result = new BitAiToolResultPart("c2", BitAiToolOutcome.FromText("x"));
        var error = new BitAiErrorPart(new BitAiError("boom"));
        var message = Assistant(call, result, error);

        Assert.HasCount(1, RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, call).Add(c => c.Message, message)).FindComponents<BitAiToolCallView>());
        Assert.HasCount(1, RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, result).Add(c => c.Message, message)).FindComponents<BitAiToolResultView>());
        Assert.HasCount(1, RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, error).Add(c => c.Message, message)).FindComponents<BitAiErrorView>());
    }

    [TestMethod]
    public void ARegisteredPartRendererReplacesTheBuiltInViewInsideABoundary()
    {
        Renderers.RegisterPart(BitAiMessagePartKinds.ToolCall, context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "my-part");
            builder.AddContent(2, context.Part.Kind);
            builder.CloseElement();
        });

        var call = new BitAiToolCallPart("c1", "search");
        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, call).Add(c => c.Message, Assistant(call)));

        Assert.IsNotNull(component.Find(".bit-aimpv .my-part"));
        Assert.HasCount(1, component.FindComponents<BitAiRendererBoundary>());
        Assert.IsEmpty(component.FindComponents<BitAiToolCallView>());
    }

    [TestMethod]
    public void ACitationPartIsTheCardOfItsSourceWithItsQuoteAndLocatorUnderIt()
    {
        var citation = new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Quote = "Every component runs somewhere.", Locator = "Section 2" });

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, citation).Add(c => c.Message, Assistant(citation)));

        var container = component.Find(".bit-aimpv > .bit-aimpv-cit");
        var children = container.Children.ToArray();

        Assert.HasCount(3, children);
        Assert.IsTrue(children[0].ClassList.Contains("bit-aisrc"));
        Assert.AreEqual("BLOCKQUOTE", children[1].TagName);
        Assert.IsTrue(children[1].ClassList.Contains("bit-aimpv-cqt"));
        Assert.AreEqual("Every component runs somewhere.", children[1].TextContent);
        Assert.AreEqual("DIV", children[2].TagName);
        Assert.IsTrue(children[2].ClassList.Contains("bit-aimpv-clc"));
        Assert.AreEqual("Section 2", children[2].TextContent);

        var card = component.FindComponent<BitAiSourceCard>();
        Assert.AreSame(DocsSource, card.Instance.Source);
        Assert.AreEqual("Docs", component.Find(".bit-aisrc-ttl").TextContent);
    }

    [TestMethod,
        DataRow(null, null),
        DataRow("", "")]
    public void ACitationWithoutAQuoteOrALocatorShowsTheCardAlone(string? quote, string? locator)
    {
        var citation = new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Quote = quote, Locator = locator });

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, citation).Add(c => c.Message, Assistant(citation)));

        Assert.HasCount(1, component.Find(".bit-aimpv-cit").Children);
        Assert.IsEmpty(component.FindAll(".bit-aimpv-cqt"));
        Assert.IsEmpty(component.FindAll(".bit-aimpv-clc"));
        Assert.IsEmpty(component.FindAll("blockquote"));
    }

    [TestMethod]
    public void TheCardOfACitationIsNumberedAsTheMessageNumbersThatCitation()
    {
        // Two other sources come first, so the message numbers the citation 3, whatever number the part carries.
        var message = Assistant();
        message.AddCitation(new BitAiCitation("c-a", new BitAiCitationSource("s-a", "A")));
        message.AddCitation(new BitAiCitation("c-b", new BitAiCitationSource("s-b", "B")));
        message.AddCitation(new BitAiCitation("c1", DocsSource));

        var citation = message.AddPart(new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Number = 9 }));

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, citation).Add(c => c.Message, message));

        Assert.AreEqual(3, component.FindComponent<BitAiSourceCard>().Instance.Number);
        Assert.AreEqual("3", component.Find(".bit-aisrc-idx").TextContent);
    }

    [TestMethod]
    public void TheCardOfACitationTheMessageDoesNotHoldTakesTheCitationsOwnNumber()
    {
        var numbered = new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Number = 9 });
        var unnumbered = new BitAiCitationPart(new BitAiCitation("c2", DocsSource));

        // The message numbers a citation of another id only.
        var message = Assistant(numbered, unnumbered);
        message.AddCitation(new BitAiCitation("other", DocsSource));

        var withNumber = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, numbered).Add(c => c.Message, message));
        var withoutNumber = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, unnumbered).Add(c => c.Message, message));

        Assert.AreEqual("9", withNumber.Find(".bit-aisrc-idx").TextContent);
        Assert.IsEmpty(withoutNumber.FindAll(".bit-aisrc-idx"));
    }

    [TestMethod]
    public void WithOnCitationClickTheCardIsAButtonThatHandsOverTheCitation()
    {
        var citation = new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Quote = "quoted" });
        BitAiCitation? clicked = null;

        var component = RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, citation)
            .Add(c => c.Message, Assistant(citation))
            .Add(c => c.OnCitationClick, c => clicked = c));

        Assert.IsEmpty(component.FindAll(".bit-aimpv-cit a"));
        Assert.IsEmpty(component.FindAll("[href]"));
        Assert.IsNotNull(component.Find(".bit-aimpv-cit button.bit-crd-lnk"));

        component.Find(".bit-aimpv-cit .bit-crd").Click();

        Assert.AreSame(citation.Citation, clicked);
    }

    [TestMethod]
    public void WithoutOnCitationClickTheCardIsALinkToTheSource()
    {
        var citation = new BitAiCitationPart(new BitAiCitation("c1", DocsSource));

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, citation).Add(c => c.Message, Assistant(citation)));

        var link = component.Find(".bit-aimpv-cit a.bit-crd-lnk");
        Assert.AreEqual("https://learn.microsoft.com/aspnet", link.GetAttribute("href"));
        Assert.AreEqual("_blank", link.GetAttribute("target"));
        Assert.IsEmpty(component.FindAll(".bit-aimpv-cit button"));
    }

    [TestMethod]
    public void TheClassesAndStylesOfACitationPartReachItsParts()
    {
        var citation = new BitAiCitationPart(new BitAiCitation("c1", DocsSource) { Quote = "quoted", Locator = "p. 4" });

        var component = RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, citation)
            .Add(c => c.Message, Assistant(citation))
            .Add(c => c.Classes, new()
            {
                Citation = "c-citation",
                CitationCard = new() { Root = "c-card", Title = "c-title" },
                CitationQuote = "c-quote",
                CitationLocator = "c-locator",
            })
            .Add(c => c.Styles, new()
            {
                Citation = "margin: 1px",
                CitationCard = new() { Root = "margin: 2px" },
                CitationQuote = "margin: 3px",
                CitationLocator = "margin: 4px",
            }));

        var container = component.Find(".bit-aimpv-cit");
        Assert.IsTrue(container.ClassList.Contains("c-citation"));
        Assert.AreEqual("margin: 1px", container.GetAttribute("style"));

        var card = component.Find(".bit-aimpv-cit > .bit-aisrc");
        Assert.IsTrue(card.ClassList.Contains("c-card"));
        StringAssert.Contains(card.GetAttribute("style"), "margin: 2px");
        Assert.IsTrue(component.Find(".bit-aisrc-ttl").ClassList.Contains("c-title"));

        Assert.IsTrue(component.Find(".bit-aimpv-cqt").ClassList.Contains("c-quote"));
        Assert.AreEqual("margin: 3px", component.Find(".bit-aimpv-cqt").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aimpv-clc").ClassList.Contains("c-locator"));
        Assert.AreEqual("margin: 4px", component.Find(".bit-aimpv-clc").GetAttribute("style"));
    }

    [TestMethod]
    public void APartOfAnUnknownKindWithoutTextRendersNothingAtAll()
    {
        var part = new SilentPart();

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, Assistant(part)));

        Assert.AreEqual(string.Empty, component.Markup.Trim());
    }

    [TestMethod]
    public void APartOfAnUnknownKindIsShownAsItsText()
    {
        var part = new GrowingPart("a poll about lunch");

        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, Assistant(part)));

        Assert.AreEqual("a poll about lunch", component.Find(".bit-aimpv > .bit-aimpv-fbk").TextContent);
    }

    [TestMethod]
    public void APartOfAnUnknownKindAppearsOnceItsTextArrives()
    {
        var part = new GrowingPart(null);
        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, Assistant(part)));
        Assert.IsEmpty(component.FindAll(".bit-aimpv"));

        part.SetText("arrived");

        component.WaitForAssertion(() => Assert.AreEqual("arrived", component.Find(".bit-aimpv > .bit-aimpv-fbk").TextContent), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void ARendererRegisteredForCitationsStillDrawsACitationPart()
    {
        Renderers.RegisterPart(BitAiMessagePartKinds.Citation, context => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "my-citation");
            builder.AddContent(2, ((BitAiCitationPart)context.Part).Citation.Source.Title);
            builder.CloseElement();
        });

        var citation = new BitAiCitationPart(new BitAiCitation("c1", new BitAiCitationSource("s1", "Docs")));
        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, citation).Add(c => c.Message, Assistant(citation)));

        Assert.AreEqual("Docs", component.Find(".bit-aimpv .my-citation").TextContent);
        Assert.HasCount(1, component.FindComponents<BitAiRendererBoundary>());
    }

    [TestMethod]
    public void AChangeOfThePartReRendersTheView()
    {
        var call = new BitAiToolCallPart("c1", "search");
        var component = RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, call).Add(c => c.Message, Assistant(call)));

        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        component.WaitForAssertion(() => StringAssert.Contains(component.Markup, "Allow search to run?"), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task APartViewStopsListeningToItsPartWhenRemoved()
    {
        var call = new BitAiToolCallPart("c1", "search");
        RenderComponent<BitAiMessagePartView>(p => p.Add(c => c.Part, call).Add(c => c.Message, Assistant(call)));
        Assert.IsNotNull(Subscribers(call));

        await Context.DisposeComponentsAsync();

        Assert.IsNull(Subscribers(call));
    }

    [TestMethod]
    public void TheRootShouldRenderTheExpectedClassesAndAttributes()
    {
        var call = new BitAiToolCallPart("c1", "search");

        var component = RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, call)
            .Add(c => c.Message, Assistant(call))
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Id, "part")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "root-class" })
            .Add(c => c.Styles, new() { Root = "padding:2px" }));

        var root = component.Find(".bit-aimpv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("part", root.Id);
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        StringAssert.Contains(root.GetAttribute("style"), "padding:2px");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void TheRootShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var call = new BitAiToolCallPart("c1", "search");

        var component = RenderComponent<BitAiMessagePartView>(p => p
            .Add(c => c.Part, call)
            .Add(c => c.Message, Assistant(call))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aimpv").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }



    // A kind of the app's own: no view of the library knows it, and it says nothing in text.
    private sealed class SilentPart : BitAiMessagePart
    {
        public override string Kind => "acme:silent";
    }

    // A kind of the app's own whose text arrives after the part has been added.
    private sealed class GrowingPart(string? text) : BitAiMessagePart, IBitAiTextualPart
    {
        public override string Kind => "acme:poll";

        public string? Text { get; private set; } = text;

        public void SetText(string value)
        {
            Text = value;
            NotifyChanged();
        }
    }
}
