using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ReasoningPartView;

/// <summary>
/// The reasoning panel: open while the model thinks, collapsed once it is done, a user's own choice kept over both,
/// and the reasoning rendered as Markdown.
/// </summary>
[TestClass]
public class BitAiReasoningPartViewTests : BitAiTestBase
{
    private static Delegate? Subscribers(BitAiMessagePart part) =>
        (Delegate?)typeof(BitAiMessagePart)
            .GetField(nameof(BitAiMessagePart.Changed), BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(part);

    private static BitAiReasoningPart Thinking(string text = "thinking")
    {
        var part = new BitAiReasoningPart();
        part.Append(text);
        return part;
    }



    [TestMethod]
    public void BitAiReasoningPartViewShouldRenderItsRootAroundThePanel()
    {
        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, Thinking()));

        var root = component.Find(".bit-airpv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
        Assert.HasCount(1, component.FindComponents<BitAiCollapsible>());
    }

    [TestMethod]
    public void TheHeaderNamesTheReasoningAndShowsDotsWhileItStreams()
    {
        var part = Thinking();

        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));

        var header = component.Find("[aria-expanded]");
        Assert.AreEqual("Reasoning", header.QuerySelector(".bit-airpv-hdr > .bit-airpv-ttl")!.TextContent);
        // The glyph is drawn from the package's own icon subset, and hidden: the title says it all.
        var icon = header.QuerySelector(".bit-airpv-hdr > .bit-airpv-ico")!;
        Assert.IsTrue(icon.ClassList.Contains("bit-icon-ai--Lightbulb"));
        Assert.AreEqual("true", icon.GetAttribute("aria-hidden"));

        // The dots are a picture: no live region inside the button that toggles the panel.
        var dots = component.FindComponent<BitBouncingDotsLoading>();
        Assert.AreEqual("none", dots.Find(".bit-ldn").GetAttribute("role"));

        part.Complete();
        component.Render();

        StringAssert.StartsWith(component.Find(".bit-airpv-ttl").TextContent, "Thought for");
        Assert.IsEmpty(component.FindComponents<BitBouncingDotsLoading>());
    }

    [TestMethod]
    public void ReasoningIsOpenWhileStreamingAndCollapsesWhenDone()
    {
        var part = Thinking();

        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));
        Assert.AreEqual("true", component.Find("[aria-expanded]").GetAttribute("aria-expanded"));

        part.Complete();
        component.Render();

        Assert.AreEqual("false", component.Find("[aria-expanded]").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void AFinishedReasoningStaysOpenWhenTheHostDoesNotCollapseIt()
    {
        var part = Thinking();

        var component = RenderComponent<BitAiReasoningPartView>(p => p
            .Add(x => x.Part, part)
            .Add(x => x.CascadedOptions, BitAiUiOptions.Default with { CollapseReasoning = false }));

        part.Complete();
        component.Render();

        Assert.AreEqual("true", component.Find("[aria-expanded]").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void AUserWhoOpenedTheReasoningKeepsItOpen()
    {
        var part = Thinking();

        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));
        component.Find("[aria-expanded]").Click(); // close it by hand while streaming
        component.Find("[aria-expanded]").Click(); // and open it again: an explicit choice

        part.Complete();
        component.Render();

        Assert.AreEqual("true", component.Find("[aria-expanded]").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void ReasoningRendersAsMarkdown()
    {
        var part = new BitAiReasoningPart("Step **one** then two");
        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));
        component.Find("[aria-expanded]").Click();

        StringAssert.Contains(component.Find(".bit-airpv-bdy .bit-aimdv-par").InnerHtml, "<strong>one</strong>");
    }

    [TestMethod]
    public void TheReasoningFollowsThePartWhileItStreams()
    {
        var part = Thinking("first");

        var component = RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));

        part.Append(" and second");

        component.WaitForAssertion(() => Assert.AreEqual("first and second", component.Find(".bit-airpv-bdy .bit-aimdv-par").TextContent));
    }

    [TestMethod]
    public async Task ThePanelStopsListeningToItsPartWhenRemoved()
    {
        var part = Thinking();
        RenderComponent<BitAiReasoningPartView>(p => p.Add(x => x.Part, part));
        Assert.IsNotNull(Subscribers(part));

        await Context.DisposeComponentsAsync();

        Assert.IsNull(Subscribers(part));
    }

    [TestMethod]
    public void BitAiReasoningPartViewShouldApplyTheClassesAndStylesOfItsParts()
    {
        var component = RenderComponent<BitAiReasoningPartView>(p => p
            .Add(x => x.Part, Thinking())
            .Add(x => x.Class, "custom-class")
            .Add(x => x.Classes, new() { Root = "c-root", Header = "c-hdr", Icon = "c-ico", Title = "c-ttl", Body = "c-bdy", Markdown = new() { Root = "c-smd" } })
            .Add(x => x.Styles, new() { Root = "margin: 1px", Body = "margin: 2px" }));

        var root = component.Find(".bit-airpv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px");
        Assert.IsTrue(component.Find(".bit-airpv-hdr").ClassList.Contains("c-hdr"));
        Assert.IsTrue(component.Find(".bit-airpv-ico").ClassList.Contains("c-ico"));
        Assert.IsTrue(component.Find(".bit-airpv-ttl").ClassList.Contains("c-ttl"));
        Assert.IsTrue(component.Find(".bit-airpv-bdy").ClassList.Contains("c-bdy"));
        Assert.AreEqual("margin: 2px", component.Find(".bit-airpv-bdy").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aismd").ClassList.Contains("c-smd"));
    }

    [TestMethod]
    public void BitAiReasoningPartViewShouldTakeTheCascadedParams()
    {
        var part = Thinking();

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitAiReasoningPartViewParams { Class = "cascaded", Classes = new() { Title = "cascaded-title" } }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiReasoningPartView>(0);
                builder.AddAttribute(1, nameof(BitAiReasoningPartView.Part), part);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-airpv").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-airpv-ttl").ClassList.Contains("cascaded-title"));
        Assert.AreEqual("BitParams.BitAiReasoningPartView", new BitAiReasoningPartViewParams().Name);
    }
}
