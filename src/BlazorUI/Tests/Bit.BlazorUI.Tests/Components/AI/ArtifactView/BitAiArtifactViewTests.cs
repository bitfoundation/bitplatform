using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ArtifactView;

[TestClass]
public class BitAiArtifactViewTests : BitAiTestBase
{
    private static BitAiArtifact Completed(BitAiArtifactKind kind, string title, string content, string? language = null)
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", kind, title);
        artifact.Language = language;
        artifact.SetContent(content);
        artifact.Complete();
        return artifact;
    }

    private IRenderedComponent<BitAiArtifactView> RenderArtifact(BitAiArtifact artifact) =>
        RenderComponent<BitAiArtifactView>(p => p.Add(c => c.Artifact, artifact));



    [TestMethod]
    public void BitAiArtifactViewShouldRenderTheTitleTheActionsAndTheBody()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Document, "Project README", "# Hello\n\nWorld"));

        var panel = view.Find(".bit-aiatv > section.bit-aiatv-pnl");
        Assert.AreEqual("Project README", panel.GetAttribute("aria-label"));
        Assert.AreEqual("a1", panel.GetAttribute("data-artifact-id"));
        Assert.AreEqual("Project README", view.Find(".bit-aiatv-ttl").TextContent);
        Assert.HasCount(1, view.FindComponents<BitAiStreamingMarkdown>());
        Assert.HasCount(1, view.FindComponents<BitAiCopyButton>());
        Assert.IsNotNull(view.Find("button[aria-label='Download']"));
        Assert.IsNotNull(view.Find("button[aria-label='Expand']"));
    }

    [TestMethod]
    public void HtmlArtifactsRenderInAFrameWithoutSameOriginAccess()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Html, "Page", "<p>hi</p><script>parent.x=1</script>"));

        var frame = view.Find("iframe.bit-aiatv-frm");
        Assert.AreEqual("allow-scripts allow-forms allow-popups", frame.GetAttribute("sandbox"));
        Assert.IsFalse(frame.GetAttribute("sandbox")!.Contains("allow-same-origin", System.StringComparison.Ordinal));
        Assert.AreEqual("Page", frame.GetAttribute("title"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-bdy script"));
    }

    [TestMethod]
    public void AStreamingHtmlArtifactIsShownAsCodeUntilItIsComplete()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Html, "Page");
        artifact.Append("<p>hi");

        var view = RenderArtifact(artifact);

        Assert.IsEmpty(view.FindAll("iframe"));
        Assert.HasCount(1, view.FindComponents<BitAiCodeBlockView>());
    }

    [TestMethod]
    public void SvgArtifactsRenderAsAnImageSoEmbeddedScriptNeverRuns()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Svg, "Logo", "<svg onload=\"alert(1)\"><script>alert(2)</script></svg>"));

        var image = view.Find(".bit-aiatv-bdy img.bit-aiatv-img");
        Assert.IsTrue(image.GetAttribute("src")!.StartsWith("data:image/svg+xml;base64,", System.StringComparison.Ordinal));
        Assert.AreEqual("Logo", image.GetAttribute("alt"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-bdy svg"));
    }

    [TestMethod]
    public void CsvTableArtifactsRenderAsATable()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "Prices", "name,price\n\"Widget, large\",3\nGadget,4\n"));

        CollectionAssert.AreEqual(new[] { "name", "price" }, view.FindAll("th.bit-aiatv-thc").Select(th => th.TextContent).ToArray());
        Assert.AreEqual("Widget, large", view.FindAll("td.bit-aiatv-tdc")[0].TextContent);
        Assert.AreEqual(2, view.FindAll("tbody tr").Count);
    }

    [TestMethod]
    public void AJsonTableArtifactRendersAsADataTable()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "People", """[{"name":"Ada"}]"""));

        Assert.HasCount(1, view.FindComponents<BitAiDataTable>());
    }

    // A panel rendered again - on every tick while the reply streams on - parses its content anew only once it has
    // changed: the table is handed the same rows meanwhile, which is what tells it it has nothing to build again.
    [TestMethod]
    public void AJsonTableIsParsedAgainOnlyForNewContent()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Table, "People");
        artifact.Append("""[{"name":"Ada"}""");
        var view = RenderArtifact(artifact);
        JsonElement Rows() => view.FindComponent<BitAiDataTable>().Instance.Rows;
        var first = Rows();

        view.Render(p => p.Add(c => c.Artifact, artifact));
        Assert.IsTrue(first.Equals(Rows()));

        artifact.Append(""",{"name":"Grace"}]""");
        view.Render(p => p.Add(c => c.Artifact, artifact));
        Assert.IsFalse(first.Equals(Rows()));
        Assert.HasCount(2, view.FindAll(".bit-aidtb tbody tr"));
    }

    [TestMethod]
    public void AJsonArtifactRendersAsATree()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Json, "Data", """{"a":1}"""));

        Assert.HasCount(1, view.FindComponents<BitAiJsonView>());
        Assert.IsNotNull(view.Find(".bit-aiatv-jsn .bit-aijsv"));
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

        var view = RenderArtifact(Completed(BitAiArtifactKind.Chart, "Sales", "{\"type\":\"bar\"}"));

        Assert.AreEqual("{\"type\":\"bar\"}", view.Find(".bit-aiatv-bdy .my-chart").TextContent);
        Assert.HasCount(1, view.FindComponents<BitAiRendererBoundary>());
    }

    [TestMethod]
    public void StreamedContentAppearsAsItArrives()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Code, "Program");
        artifact.Language = "csharp";
        var view = RenderArtifact(artifact);

        Assert.AreEqual("true", view.Find(".bit-aiatv-bdy").GetAttribute("aria-busy"));
        Assert.HasCount(1, view.FindComponents<BitEllipsisLoading>());

        artifact.Append("var answer = 42;");

        view.WaitForAssertion(() =>
            StringAssert.Contains(view.Find(".bit-aiatv-bdy code").TextContent, "answer"));

        artifact.Complete();

        view.WaitForAssertion(() => Assert.IsFalse(view.Find(".bit-aiatv-bdy").HasAttribute("aria-busy")));
        Assert.IsEmpty(view.FindComponents<BitEllipsisLoading>());
    }

    [TestMethod]
    public void AnEarlierTurnKeepsShowingTheVersionItProducedAndOffersAVersionSelector()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Code, "Program");
        artifact.SetContent("first draft");
        artifact.Complete();
        var firstVersion = artifact.Version;

        artifact.BeginRevision();
        artifact.SetContent("second draft");
        artifact.Complete();

        var older = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, artifact)
            .Add(c => c.InitialVersion, firstVersion));
        var latest = RenderArtifact(artifact);

        StringAssert.Contains(older.Find(".bit-aiatv-bdy").TextContent, "first draft");
        StringAssert.Contains(latest.Find(".bit-aiatv-bdy").TextContent, "second draft");

        var dropdown = older.FindComponent<BitDropdown<BitDropdownItem<int>, int>>();
        Assert.AreEqual("Version", dropdown.Instance.AriaLabel);
        Assert.AreEqual(firstVersion, dropdown.Instance.Value);

        var options = older.FindAll(".bit-aiatv-hdr [role=option]");
        Assert.AreEqual(2, options.Count);

        older.FindAll(".bit-aiatv-hdr [role=option]")[1].Click();

        StringAssert.Contains(older.Find(".bit-aiatv-bdy").TextContent, "second draft");
    }

    [TestMethod]
    public void ARegisteredArtifactRendererIsHandedTheVersionOnScreen()
    {
        Renderers.RegisterArtifact(BitAiArtifactKind.Chart, context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "my-chart");
            builder.AddContent(2, $"{context.Version}:{context.Content}:{context.IsLatest}");
            builder.CloseElement();
        });

        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Chart, "Sales");
        artifact.SetContent("first");
        artifact.Complete();
        var firstVersion = artifact.Version;

        artifact.BeginRevision();
        artifact.SetContent("second");
        artifact.Complete();

        var view = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, artifact)
            .Add(c => c.InitialVersion, firstVersion));

        Assert.AreEqual($"{firstVersion}:first:False", view.Find(".bit-aiatv-bdy .my-chart").TextContent);

        view.FindAll(".bit-aiatv-hdr [role=option]")[1].Click();

        Assert.AreEqual($"{artifact.Version}:second:True", view.Find(".bit-aiatv-bdy .my-chart").TextContent);
    }

    [TestMethod]
    public void ASingleVersionOffersNoVersionSelector()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        Assert.IsEmpty(view.FindComponents<BitDropdown<BitDropdownItem<int>, int>>());
    }

    [TestMethod]
    public void TheFullScreenToggleExpandsAndEscapeCollapses()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        var root = view.Find(".bit-aiatv");
        Assert.IsFalse(root.ClassList.Contains("bit-aiatv-exp"));
        Assert.IsFalse(view.Find(".bit-aiatv-pnl").HasAttribute("data-bit-esc"));
        Assert.AreEqual("false", view.Find("button[aria-label='Expand']").GetAttribute("aria-pressed"));

        view.Find("button[aria-label='Expand']").Click();

        root = view.Find(".bit-aiatv");
        Assert.IsTrue(root.ClassList.Contains("bit-aiatv-exp"));
        Assert.AreEqual("claim", view.Find(".bit-aiatv-pnl").GetAttribute("data-bit-esc"));
        Assert.IsNotNull(view.Find(".bit-aiatv > .bit-aiatv-ovl"));
        Assert.AreEqual("true", view.Find("button[aria-label='Collapse']").GetAttribute("aria-pressed"));

        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        root = view.Find(".bit-aiatv");
        Assert.IsFalse(root.ClassList.Contains("bit-aiatv-exp"));
        Assert.IsFalse(view.Find(".bit-aiatv-pnl").HasAttribute("data-bit-esc"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-ovl"));
    }

    [TestMethod]
    public void AModifiedEscapeLeavesTheFullScreenPanelOpen()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        view.Find("button[aria-label='Expand']").Click();
        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape", ShiftKey = true });

        Assert.IsTrue(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
    }

    [TestMethod]
    public void AnEscapeClaimedInsideTheFullScreenPanelIsLeftToThatPart()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        view.Find("button[aria-label='Expand']").Click();

        view.InvokeAsync(() => view.Instance._OnEscapeVerdict(true));
        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));

        // The verdict is read once: the next press is the panel's own again.
        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
    }

    [TestMethod]
    public void TheFullScreenPanelWatchesTheEscapesClaimedInsideItOnceExpanded()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.Utils.watchEscapeClaim"]);

        view.Find("button[aria-label='Expand']").Click();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.Utils.watchEscapeClaim");
    }

    [TestMethod]
    public void AClickOnTheOverlayCollapsesTheFullScreenPanel()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        view.Find("button[aria-label='Expand']").Click();
        view.Find(".bit-aiatv-ovl").Click();

        Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
    }

    // Laid over the page, the panel is a modal dialog: the Tab that would walk on into the page behind its overlay is kept
    // inside it, and a screen reader keeps to it as well.
    [TestMethod]
    public void TheFullScreenPanelIsAModalDialogThatHoldsTheKeyboard()
    {
        var view = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, Completed(BitAiArtifactKind.Code, "Program", "x"))
            .Add(c => c.Id, "v1"));

        var panel = view.Find(".bit-aiatv > section.bit-aiatv-pnl");
        Assert.AreEqual("v1-pnl", panel.Id);
        Assert.IsFalse(panel.HasAttribute("role"));
        Assert.IsFalse(panel.HasAttribute("aria-modal"));
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiArtifactView.expand"]);

        view.Find("button[aria-label='Expand']").Click();

        panel = view.Find(".bit-aiatv > section.bit-aiatv-pnl");
        Assert.AreEqual("dialog", panel.GetAttribute("role"));
        Assert.AreEqual("true", panel.GetAttribute("aria-modal"));
        Assert.AreEqual("Program", panel.GetAttribute("aria-label"));

        var expand = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.expand");
        Assert.AreEqual("v1-pnl", expand.Arguments[0]);
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiArtifactView.collapse"]);

        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        panel = view.Find(".bit-aiatv > section.bit-aiatv-pnl");
        Assert.IsFalse(panel.HasAttribute("role"));
        Assert.IsFalse(panel.HasAttribute("aria-modal"));

        var collapse = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.collapse");
        Assert.AreEqual("v1-pnl", collapse.Arguments[0]);
        Assert.HasCount(1, Context.JSInterop.Invocations["BitBlazorUI.AiArtifactView.expand"]);
    }

    [TestMethod]
    public async Task APanelTakenOffThePageWhileFullScreenLetsTheKeyboardGo()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        view.Find("button[aria-label='Expand']").Click();
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.expand");

        await Context.DisposeComponentsAsync();

        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.collapse");
    }

    // The keys pressed inside the page of an HTML artifact are its own document's, so the page reports the Escape it left
    // alone, and the full-screen panel collapses on it as on any other.
    [TestMethod]
    public void AnEscapeThePageInTheFrameReportsCollapsesTheFullScreenPanel()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Html, "Page", "<p>hi</p>"));
        var toggle = view.FindComponents<BitButton>().Single(b => b.Instance.Class?.Contains("bit-aiatv-exb") is true);

        // Inline, the panel has nothing to collapse.
        view.InvokeAsync(() => view.Instance._OnFrameEscape());

        Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        view.Find("button[aria-label='Expand']").Click();
        view.InvokeAsync(() => view.Instance._OnFrameEscape());

        view.WaitForAssertion(() => Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp")));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-ovl"));

        // The focus goes back to the toggle, as it does from any other Escape.
        view.WaitForAssertion(() => AssertFocusedLast(toggle, 2));
        Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.collapse");
    }

    [TestMethod]
    public void ThePageInTheFrameIsHandedTheScriptThatReportsItsEscapeAfterIt()
    {
        const string page = "<!doctype html><html><body><p>hi</p></body></html>";

        var view = RenderArtifact(Completed(BitAiArtifactKind.Html, "Page", page));

        // After the page, never ahead of its doctype, which would put it in quirks mode.
        var document = view.Find("iframe.bit-aiatv-frm").GetAttribute("srcdoc")!;
        StringAssert.StartsWith(document, page);
        StringAssert.Contains(document, "parent.postMessage({bitAiArtifactView:'escape'},'*')");
        StringAssert.Contains(document, "!e.defaultPrevented&&!e.isComposing");
        Assert.IsEmpty(view.FindAll(".bit-aiatv-bdy script"));

        // Copy and download act on the page itself.
        Assert.AreEqual(page, view.FindComponent<BitAiCopyButton>().Instance.Content);

        view.Find("button[aria-label='Download']").Click();

        Assert.AreEqual(page, Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText").Arguments[1]);
    }

    // A table written as a JSON array is drawn by a BitAiDataTable, which the panel frames as it does a CSV table.
    [TestMethod]
    public void AJsonTableSitsRightInTheBodyThePanelFrames()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "People", """[{"name":"Ada"}]"""));

        Assert.IsNotNull(view.Find(".bit-aiatv-bdy > .bit-aidtb"));
    }

    // The button pressed is gone once the image is shown, and the focus would drop to the body of the document with it.
    [TestMethod]
    public void AnImageArtifactTheUserLoadedTakesTheFocusOffThePlaceholder()
    {
        var view = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", "Chart"), BitAiImagePolicy.ClickToLoad);

        view.Find(".bit-aiatv-imh button.bit-acb").Click();

        Assert.AreEqual("-1", view.Find("img.bit-aiatv-img").GetAttribute("tabindex"));

        var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];
        Assert.HasCount(1, focus);
        Assert.AreNotEqual(view.Instance.RootElement.Id, ((ElementReference)focus[0].Arguments[0]!).Id);

        // One the policy shows at once was never a button, and takes no focus.
        var shown = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", "Chart"), BitAiImagePolicy.Allow);

        Assert.IsFalse(shown.Find("img.bit-aiatv-img").HasAttribute("tabindex"));
        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
    }

    // A click does not focus a button in WebKit, so without this the focus stays outside the full-screen panel and the
    // next Escape closes the dialog or panel around the artifact instead. The toggle is the one control there both ways.
    [TestMethod]
    public void TheFullScreenToggleTakesTheFocusEachWayThePanelTurns()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));
        var toggle = view.FindComponents<BitButton>().Single(b => b.Instance.Class?.Contains("bit-aiatv-exb") is true);

        Assert.IsEmpty(Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);

        view.Find("button[aria-label='Expand']").Click();

        AssertFocusedLast(toggle, 1);

        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        AssertFocusedLast(toggle, 2);

        view.Find("button[aria-label='Expand']").Click();
        view.Find(".bit-aiatv-ovl").Click();

        AssertFocusedLast(toggle, 4);

        view.Find("button[aria-label='Expand']").Click();
        view.Find("button[aria-label='Collapse']").Click();

        AssertFocusedLast(toggle, 6);
        Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
    }

    [TestMethod]
    public void AnEscapeThePanelLeavesAloneMovesNoFocus()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "x"));

        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        view.Find("button[aria-label='Expand']").Click();
        view.Find(".bit-aiatv-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape", ShiftKey = true });

        Assert.HasCount(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
        Assert.IsTrue(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
    }

    [TestMethod]
    public void AnImageShownFromItsAddressHasNothingToCopyOrDownload()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Image, "Chart");
        artifact.Url = "https://example.com/chart.png";
        artifact.Complete();

        var view = RenderArtifact(artifact);

        Assert.AreEqual("https://example.com/chart.png", view.Find(".bit-aiatv-img").GetAttribute("src"));
        Assert.IsNull(view.FindComponent<BitAiCopyButton>().Instance.Content);
        Assert.AreEqual("true", view.Find(".bit-aicpb button").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", view.Find("button[aria-label='Download']").GetAttribute("aria-disabled"));

        view.Find("button[aria-label='Download']").Click();

        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiArtifactView.downloadText"]);
    }

    [TestMethod]
    public void AnImageArtifactIsShownUnderTheAllowPolicy()
    {
        var view = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", "Chart"), BitAiImagePolicy.Allow);

        var image = view.Find(".bit-aiatv-bdy > img.bit-aiatv-img");
        Assert.AreEqual("https://img.x.dev/chart.png", image.GetAttribute("src"));
        Assert.AreEqual("Chart", image.GetAttribute("alt"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-imh"));
    }

    [TestMethod]
    public void AnImageArtifactTheClickToLoadPolicyHoldsBackIsAButtonThatLoadsIt()
    {
        var view = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", "Chart"), BitAiImagePolicy.ClickToLoad);

        Assert.IsEmpty(view.FindAll("img"));
        var placeholder = view.Find(".bit-aiatv-bdy > .bit-aiatv-imh > button.bit-acb");
        Assert.AreEqual("Load image: Chart", placeholder.TextContent.Trim());
        Assert.AreEqual("https://img.x.dev/chart.png", placeholder.GetAttribute("title"));
        Assert.IsTrue(placeholder.QuerySelector(".bit-acb-ico")!.ClassList.Contains("bit-icon-ai--Photo2"));

        placeholder.Click();

        Assert.AreEqual("https://img.x.dev/chart.png", view.Find("img.bit-aiatv-img").GetAttribute("src"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-imh"));
    }

    [TestMethod]
    public void APlaceholderOfAnUntitledImageArtifactIsNamedAfterItsHost()
    {
        var view = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", ""), BitAiImagePolicy.ClickToLoad);

        Assert.AreEqual("Load image: img.x.dev", view.Find(".bit-aiatv-imh button.bit-acb").TextContent.Trim());
    }

    [TestMethod]
    public void AnImageArtifactTheBlockPolicyHidesLeavesItsTitle()
    {
        var view = RenderImageArtifact(ImageAt("https://img.x.dev/chart.png", "Chart"), BitAiImagePolicy.Block);

        Assert.IsEmpty(view.FindAll("img"));
        Assert.IsEmpty(view.FindAll(".bit-aiatv-bdy button"));
        Assert.AreEqual("Chart", view.Find(".bit-aiatv-bdy > .bit-aiatv-imh > span.bit-aiatv-imb").TextContent);
    }

    [TestMethod]
    public void AnSvgArtifactFollowsThePolicyToo()
    {
        var conversation = new BitAiConversation();
        var remote = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Svg, "Logo");
        remote.Url = "https://img.x.dev/logo.svg";
        remote.Complete();

        var asked = RenderImageArtifact(remote, BitAiImagePolicy.ClickToLoad);
        Assert.IsEmpty(asked.FindAll("img"));
        Assert.AreEqual("Load image: Logo", asked.Find(".bit-aiatv-imh button.bit-acb").TextContent.Trim());

        // Markup the model wrote is shown inline from data, which fetches nothing - unless images are blocked.
        var inline = Completed(BitAiArtifactKind.Svg, "Logo", "<svg></svg>");
        StringAssert.StartsWith(RenderImageArtifact(inline, BitAiImagePolicy.ClickToLoad).Find("img.bit-aiatv-img").GetAttribute("src"), "data:image/svg+xml;base64,");

        var blocked = RenderImageArtifact(inline, BitAiImagePolicy.Block);
        Assert.IsEmpty(blocked.FindAll("img"));
        Assert.AreEqual("Logo", blocked.Find(".bit-aiatv-imb").TextContent);
    }

    [TestMethod]
    public void AnImageArtifactOfTheAppsOwnHostIsShownWhenThatHostIsListed()
    {
        var own = RenderImageArtifact(ImageAt("/charts/a.png", "Chart"), BitAiImagePolicy.AllowListedHosts("localhost"));
        var other = RenderImageArtifact(ImageAt("/charts/a.png", "Chart"), BitAiImagePolicy.AllowListedHosts("cdn.x.dev"));

        Assert.AreEqual("/charts/a.png", own.Find("img.bit-aiatv-img").GetAttribute("src"));
        Assert.IsEmpty(other.FindAll("img"));
        Assert.IsNotNull(other.Find(".bit-aiatv-imh button.bit-acb"));
    }

    [TestMethod]
    public void AScriptSourceOfAnImageArtifactIsNeverLoaded()
    {
        var view = RenderImageArtifact(ImageAt("javascript:alert(1)", "Chart"), BitAiImagePolicy.Allow);

        StringAssert.StartsWith(view.Find("img.bit-aiatv-img").GetAttribute("src"), "data:image/gif;base64,");
        Assert.IsFalse(view.Markup.Contains("javascript:", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ThePlaceholderAndTheBlockedTitleTakeTheClassesAndStylesOfTheirParts()
    {
        var artifact = ImageAt("https://img.x.dev/chart.png", "Chart");
        var classes = new BitAiArtifactViewClassStyles { ImagePlaceholder = new() { Root = "c-placeholder", Icon = "c-icon" }, BlockedImage = "c-blocked" };
        var styles = new BitAiArtifactViewClassStyles { ImagePlaceholder = new() { Root = "margin: 1px" }, BlockedImage = "margin: 2px" };

        var asked = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, artifact)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad })
            .Add(c => c.Classes, classes)
            .Add(c => c.Styles, styles));
        var blocked = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, artifact)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block })
            .Add(c => c.Classes, classes)
            .Add(c => c.Styles, styles));

        var placeholder = asked.Find(".bit-aiatv-imh button.bit-acb");
        Assert.IsTrue(placeholder.ClassList.Contains("c-placeholder"));
        StringAssert.Contains(placeholder.GetAttribute("style"), "margin: 1px");
        var icon = placeholder.QuerySelector(".bit-acb-ico")!;
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Photo2"));
        Assert.IsTrue(icon.ClassList.Contains("c-icon"));

        var title = blocked.Find(".bit-aiatv-imb");
        Assert.IsTrue(title.ClassList.Contains("c-blocked"));
        Assert.AreEqual("margin: 2px", title.GetAttribute("style"));
    }

    private static BitAiArtifact ImageAt(string url, string title)
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Image, title);
        artifact.Url = url;
        artifact.Complete();
        return artifact;
    }

    private IRenderedComponent<BitAiArtifactView> RenderImageArtifact(BitAiArtifact artifact, BitAiImagePolicy policy) =>
        RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, artifact)
            .Add(c => c.CascadedOptions, BitAiUiOptions.Default with { Images = policy }));

    [TestMethod]
    public void AnArtifactWithNoContentYetHasNothingToCopyOrDownloadUntilItHasSome()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Code, "Program");
        artifact.Language = "csharp";

        var view = RenderArtifact(artifact);

        Assert.AreEqual("true", view.Find(".bit-aicpb button").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", view.Find("button[aria-label='Download']").GetAttribute("aria-disabled"));

        view.InvokeAsync(() => artifact.SetContent("var x = 1;"));
        view.WaitForAssertion(() => Assert.IsNull(view.Find("button[aria-label='Download']").GetAttribute("aria-disabled")));

        Assert.AreEqual("var x = 1;", view.FindComponent<BitAiCopyButton>().Instance.Content);
        Assert.IsNull(view.Find(".bit-aicpb button").GetAttribute("aria-disabled"));

        view.Find("button[aria-label='Download']").Click();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText");
        CollectionAssert.AreEqual(new object?[] { "program.cs", "var x = 1;", "text/plain" }, invocation.Arguments.ToArray());
    }

    private void AssertFocusedLast(IRenderedComponent<BitButton> toggle, int count)
    {
        var focus = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];

        Assert.HasCount(count, focus);
        Assert.AreEqual(toggle.Instance.RootElement.Id, ((ElementReference)focus[^1].Arguments[0]!).Id);
    }

    [TestMethod]
    public void TheDownloadHandsTheViewedVersionToTheBrowser()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Code, "Program", "var x = 1;", "csharp"));

        view.Find("button[aria-label='Download']").Click();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText");
        CollectionAssert.AreEqual(new object?[] { "program.cs", "var x = 1;", "text/plain" }, invocation.Arguments.ToArray());
    }

    [TestMethod]
    public void TheDownloadOfAnEarlierVersionIsNamedAfterIt()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Document, "Notes");
        artifact.SetContent("first");
        artifact.Complete();
        var firstVersion = artifact.Version;
        artifact.BeginRevision();
        artifact.SetContent("second");
        artifact.Complete();

        var view = RenderComponent<BitAiArtifactView>(p => p.Add(c => c.Artifact, artifact).Add(c => c.InitialVersion, firstVersion));

        view.Find("button[aria-label='Download']").Click();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText");
        Assert.AreEqual(string.Create(CultureInfo.InvariantCulture, $"notes-v{firstVersion}.md"), invocation.Arguments[0]);
        Assert.AreEqual("first", invocation.Arguments[1]);
    }

    [TestMethod]
    public void ADisabledPanelDisablesItsActions()
    {
        var conversation = new BitAiConversation();
        var artifact = conversation.GetOrAddArtifact("a1", BitAiArtifactKind.Code, "Program");
        artifact.SetContent("first");
        artifact.Complete();
        artifact.BeginRevision();
        artifact.SetContent("second");
        artifact.Complete();

        var view = RenderComponent<BitAiArtifactView>(p => p.Add(c => c.Artifact, artifact).Add(c => c.Disabled, true));

        Assert.IsTrue(view.Find(".bit-aiatv").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", view.Find("button[aria-label='Download']").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", view.Find("button[aria-label='Expand']").GetAttribute("aria-disabled"));
        Assert.IsTrue(view.FindComponent<BitDropdown<BitDropdownItem<int>, int>>().Instance.Disabled);

        view.Find("button[aria-label='Expand']").Click();
        view.Find("button[aria-label='Download']").Click();

        Assert.IsFalse(view.Find(".bit-aiatv").ClassList.Contains("bit-aiatv-exp"));
        Assert.IsEmpty(Context.JSInterop.Invocations["BitBlazorUI.AiArtifactView.downloadText"]);
    }

    [TestMethod]
    public void ThePanelStopsListeningToTheArtifactWhenItIsReplacedOrRemoved()
    {
        var first = Completed(BitAiArtifactKind.Code, "One", "1");
        var second = Completed(BitAiArtifactKind.Code, "Two", "2");

        var view = RenderArtifact(first);

        view.Render(p => p.Add(c => c.Artifact, second));
        first.SetContent("changed");

        Assert.AreEqual("Two", view.Find(".bit-aiatv-ttl").TextContent);
        StringAssert.Contains(view.Find(".bit-aiatv-bdy").TextContent, "2");
    }

    [TestMethod,
        DataRow("document", null, "Project README", "project-readme.md", "text/markdown"),
        DataRow("code", "csharp", "Program", "program.cs", "text/plain"),
        DataRow("html", null, "Landing page!", "landing-page.html", "text/html"),
        DataRow("table", null, "Prices", "prices.csv", "text/csv"),
        DataRow("svg", null, "Logo", "logo.svg", "image/svg+xml"),
        DataRow("document", null, "یادداشت انتشار", "یادداشت-انتشار.md", "text/markdown"),
        DataRow("code", "python", "नमस्ते दुनिया", "नमस्ते-दुनिया.py", "text/plain"),
        DataRow("code", "python", "???", "artifact.py", "text/plain")]
    public void DownloadsGetASensibleFileNameAndMimeType(string kind, string? language, string title, string fileName, string mimeType)
    {
        var view = RenderArtifact(Completed(new BitAiArtifactKind(kind), title, "a,b", language));

        view.Find("button[aria-label='Download']").Click();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText");
        Assert.AreEqual(fileName, invocation.Arguments[0]);
        Assert.AreEqual("a,b", invocation.Arguments[1]);
        Assert.AreEqual(mimeType, invocation.Arguments[2]);
    }

    [TestMethod,
        DataRow("tsv", "prices.tsv", "text/tab-separated-values"),
        DataRow(null, "prices.json", "application/json")]
    public void ATableIsDownloadedInTheFormatItIsWrittenIn(string? language, string fileName, string mimeType)
    {
        var content = language is null ? """[{"a":1}]""" : "a\tb";
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "Prices", content, language));

        view.Find("button[aria-label='Download']").Click();

        var invocation = Context.JSInterop.VerifyInvoke("BitBlazorUI.AiArtifactView.downloadText");
        Assert.AreEqual(fileName, invocation.Arguments[0]);
        Assert.AreEqual(mimeType, invocation.Arguments[2]);
    }

    [TestMethod]
    public void DelimitedParsingHonoursQuotedFieldsAndEscapedQuotes()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "T", "a,b\r\n\"x, \"\"y\"\"\",2"));

        CollectionAssert.AreEqual(new[] { "a", "b" }, view.FindAll("th.bit-aiatv-thc").Select(th => th.TextContent).ToArray());
        CollectionAssert.AreEqual(new[] { "x, \"y\"", "2" }, view.FindAll("td.bit-aiatv-tdc").Select(td => td.TextContent).ToArray());
    }

    [TestMethod]
    public void ATabSeparatedTableIsSplitOnTabs()
    {
        var view = RenderArtifact(Completed(BitAiArtifactKind.Table, "T", "a\tb,c\n1\t2", "tsv"));

        CollectionAssert.AreEqual(new[] { "a", "b,c" }, view.FindAll("th.bit-aiatv-thc").Select(th => th.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiArtifactViewShouldRenderTheRootAndItsParts()
    {
        var view = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, Completed(BitAiArtifactKind.Table, "Prices", "a,b\n1,2"))
            .Add(c => c.Id, "v1")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.AriaLabel, "Price list")
            .Add(c => c.Classes, new()
            {
                Root = "c-root",
                Panel = "c-panel",
                Header = "c-header",
                Icon = "c-icon",
                Title = "c-title",
                Body = "c-body",
                Table = "c-table",
                HeaderCell = "c-th",
                Cell = "c-td",
                CopyButton = new() { Root = "c-copy" },
                DownloadButton = new() { Root = "c-download" },
                ExpandButton = new() { Root = "c-expand" },
            })
            .Add(c => c.Styles, new() { Title = "color:red", CopyButton = new() { Root = "margin:2px" } }));

        var root = view.Find(".bit-aiatv");

        Assert.AreEqual("v1", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("Price list", view.Find(".bit-aiatv-pnl").GetAttribute("aria-label"));
        Assert.IsTrue(view.Find(".bit-aiatv-pnl").ClassList.Contains("c-panel"));
        Assert.IsTrue(view.Find(".bit-aiatv-hdr").ClassList.Contains("c-header"));
        Assert.IsTrue(view.Find(".bit-aiatv-ico").ClassList.Contains("c-icon"));
        Assert.IsTrue(view.Find(".bit-aiatv-ttl").ClassList.Contains("c-title"));
        Assert.AreEqual("color:red", view.Find(".bit-aiatv-ttl").GetAttribute("style"));
        Assert.IsTrue(view.Find(".bit-aiatv-bdy").ClassList.Contains("c-body"));
        Assert.IsTrue(view.Find(".bit-aiatv-tbw").ClassList.Contains("c-table"));
        Assert.IsTrue(view.Find(".bit-aiatv-thc").ClassList.Contains("c-th"));
        Assert.IsTrue(view.Find(".bit-aiatv-tdc").ClassList.Contains("c-td"));
        Assert.IsTrue(view.Find(".bit-aiatv-hdr .bit-aicpb").ClassList.Contains("c-copy"));
        StringAssert.Contains(view.Find(".bit-aiatv-hdr .bit-aicpb").GetAttribute("style"), "margin:2px");
        Assert.IsTrue(view.Find(".bit-aiatv-dlb").ClassList.Contains("c-download"));
        Assert.IsTrue(view.Find(".bit-aiatv-exb").ClassList.Contains("c-expand"));
    }

    [TestMethod,
        DataRow("code", "bit-icon-ai--Code"),
        DataRow("html", "bit-icon-ai--Code"),
        DataRow("image", "bit-icon-ai--Photo2"),
        DataRow("document", "bit-icon-ai--Page"),
        DataRow("custom", "bit-icon-ai--MapLayers")]
    public void BitAiArtifactViewShouldShowTheIconOfTheKind(string kind, string expected)
    {
        var view = RenderArtifact(Completed(new BitAiArtifactKind(kind), "A", "x"));

        var icon = view.Find(".bit-aiatv-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai"));
        Assert.IsTrue(icon.ClassList.Contains(expected));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void TheButtonsDrawTheirGlyphsFromThePackagesIconFont()
    {
        var view = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, Completed(BitAiArtifactKind.Code, "A", "x"))
            .Add(c => c.Classes, new() { DownloadButton = new() { Icon = "c-download-icon" } }));

        var download = view.Find(".bit-aiatv-dlb .bit-btn-icn");
        Assert.IsTrue(download.ClassList.Contains("bit-icon-ai--Download"));
        Assert.IsTrue(download.ClassList.Contains("c-download-icon"));
        Assert.IsTrue(view.Find(".bit-aiatv-exb .bit-btn-icn").ClassList.Contains("bit-icon-ai--FullScreen"));

        view.Find("button[aria-label='Expand']").Click();

        Assert.IsTrue(view.Find(".bit-aiatv-exb .bit-btn-icn").ClassList.Contains("bit-icon-ai--BackToWindow"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiArtifactViewShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var view = RenderComponent<BitAiArtifactView>(p => p
            .Add(c => c.Artifact, Completed(BitAiArtifactKind.Code, "A", "x"))
            .Add(c => c.Visibility, visibility));

        var style = view.Find(".bit-aiatv").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiArtifactViewShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiArtifactViewParams { Class = "cascaded", Classes = new() { Header = "cascaded-header" } }
            });
            p.AddChildContent<BitAiArtifactView>(c => c.Add(x => x.Artifact, Completed(BitAiArtifactKind.Code, "A", "x")));
        });

        Assert.IsTrue(component.Find(".bit-aiatv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aiatv-hdr").ClassList.Contains("cascaded-header"));
    }

    [TestMethod]
    public void BitAiArtifactViewParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiArtifactView", BitAiArtifactViewParams.ParamName);
        Assert.AreEqual(BitAiArtifactViewParams.ParamName, new BitAiArtifactViewParams().Name);
    }
}
