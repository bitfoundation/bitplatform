using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.TextPartView;

[TestClass]
public class BitAiTextPartViewTests : BitAiTestBase
{
    // The test project sees none of the package's internals, so the helper is reached the way a test reaches any
    // private member.
    private static bool IsInsideCode(string text, int index) =>
        (bool)typeof(BitAiTextPartView)
            .GetMethod("IsInsideCode", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [text, index])!;



    [TestMethod]
    public void BitAiTextPartViewShouldRenderItsRootAroundTheMarkdown()
    {
        var part = new BitAiTextPart("Hello **world**");
        var message = Assistant(part);

        var component = RenderComponent<BitAiTextPartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, message));

        var root = component.Find(".bit-aitpv");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(component.Instance.UniqueId, root.GetAttribute("id"));
        Assert.AreEqual("Hello world", root.QuerySelector(".bit-aismd .bit-aimdv-par")!.TextContent);
    }

    [TestMethod]
    public void BitAiTextPartViewShouldRespectClassStyleAndItsParts()
    {
        var part = new BitAiTextPart("text");
        var message = Assistant(part);

        var component = RenderComponent<BitAiTextPartView>(p => p
            .Add(c => c.Part, part)
            .Add(c => c.Message, message)
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Style, "color: red")
            .Add(c => c.Classes, new() { Root = "c-root", Markdown = new() { Root = "c-smd", Markdown = new() { Paragraph = "c-par" } } }));

        var root = component.Find(".bit-aitpv");

        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
        Assert.IsTrue(component.Find(".bit-aismd").ClassList.Contains("c-smd"));
        Assert.IsTrue(component.Find(".bit-aimdv-par").ClassList.Contains("c-par"));
    }

    [TestMethod]
    public void BitAiTextPartViewShouldTakeTheCascadedParams()
    {
        var part = new BitAiTextPart("text");
        var message = Assistant(part);

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitAiTextPartViewParams { Class = "cascaded" } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAiTextPartView>(0);
                builder.AddAttribute(1, nameof(BitAiTextPartView.Part), part);
                builder.AddAttribute(2, nameof(BitAiTextPartView.Message), message);
                builder.CloseComponent();
            });
        });

        Assert.IsTrue(component.Find(".bit-aitpv").ClassList.Contains("cascaded"));
        Assert.AreEqual("BitParams.BitAiTextPartView", new BitAiTextPartViewParams().Name);
    }

    [TestMethod,
        DataRow("See `foo` here", 6, true),
        DataRow("See `foo` here", 11, false),
        DataRow("```\ncode\n```\nprose", 6, true),
        DataRow("```\ncode\n```\nprose", 16, false),
        DataRow("plain text", 5, false)]
    public void CitationMarkersAreNotInsertedInsideCode(string text, int index, bool insideCode) =>
        Assert.AreEqual(insideCode, IsInsideCode(text, index));

    [TestMethod]
    public void AnAnchoredCitationIsWovenInWhereItsSpanEnds()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        var part = message.AddPart(new BitAiTextPart("The sky is blue. Grass is green."));
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Sky")) { PartId = part.PartId, StartIndex = 0, EndIndex = 16 });

        var component = RenderComponent<BitAiTextPartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, message));

        var marker = component.FindComponent<BitAiCitationMarker>();

        Assert.AreEqual("Sky", marker.Instance.Citation.Source.Title);
        Assert.IsFalse(component.Find(".bit-aimdv-par").TextContent.Contains("[1]"));
    }

    // The markers of one offset are each inserted in front of the one before it, so they are woven from the highest number
    // down, and read in their order.
    [TestMethod]
    public void AnchoredCitationsEndingAtOneOffsetAreWovenInTheirOrder()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        var part = message.AddPart(new BitAiTextPart("The sky is blue. Grass is green."));
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Sky")) { PartId = part.PartId, StartIndex = 0, EndIndex = 16 });
        message.AddCitation(new BitAiCitation("c2", new BitAiCitationSource("s2", "Weather")) { PartId = part.PartId, StartIndex = 4, EndIndex = 16 });

        var component = RenderComponent<BitAiTextPartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, message));

        var markers = component.FindComponents<BitAiCitationMarker>();
        CollectionAssert.AreEqual(new int?[] { 1, 2 }, markers.Select(m => m.Instance.Citation.Number).ToArray());
        CollectionAssert.AreEqual(new[] { "Sky", "Weather" }, markers.Select(m => m.Instance.Citation.Source.Title).ToArray());
    }

    // The text is woven once per change of the part or its citations, compared by version; a view handed another part, or
    // another message, weaves that one's even when its version is the same number - and so it does when the inline
    // citations are turned off or on.
    [TestMethod]
    public void AnotherPartOrMessageOrTheInlineCitationsOptionWeavesTheTextAgain()
    {
        var first = new BitAiTextPart("The first answer.");
        var second = new BitAiTextPart("The second answer.");
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(first);
        message.AddPart(second);
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Docs")) { PartId = second.PartId, StartIndex = 0, EndIndex = 18 });
        Assert.AreEqual(first.Version, second.Version);

        var component = RenderComponent<WovenTextHost>(p => p.Add(c => c.Part, first).Add(c => c.Message, message));
        Assert.AreEqual("The first answer.", component.Find(".bit-aimdv-par").TextContent);

        component.Render(p => p.Add(c => c.Part, second).Add(c => c.Message, message));
        StringAssert.StartsWith(component.Find(".bit-aimdv-par").TextContent, "The second answer.");
        Assert.HasCount(1, component.FindComponents<BitAiCitationMarker>());

        // Off, the marker the text was woven with goes: left in, it would be drawn as the literal "[1]".
        component.Render(p => p
            .Add(c => c.Part, second)
            .Add(c => c.Message, message)
            .Add(c => c.Options, BitAiUiOptions.Default with { InlineCitations = false }));
        Assert.AreEqual("The second answer.", component.Find(".bit-aimdv-par").TextContent);
        Assert.IsEmpty(component.FindComponents<BitAiCitationMarker>());

        var otherPart = new BitAiTextPart("Another message.");
        var other = Assistant(otherPart);
        component.Render(p => p.Add(c => c.Part, otherPart).Add(c => c.Message, other));
        Assert.AreEqual("Another message.", component.Find(".bit-aimdv-par").TextContent);
    }

    [TestMethod]
    public void ACitationEndingInsideACodeSpanIsLeftToTheSourceList()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        var part = message.AddPart(new BitAiTextPart("Run `dotnet build` first."));
        message.AddCitation(new BitAiCitation("c1", new BitAiCitationSource("s1", "Docs")) { PartId = part.PartId, StartIndex = 5, EndIndex = 11 });
        message.AddCitation(new BitAiCitation("c2", new BitAiCitationSource("s2", "Guide")) { PartId = part.PartId, StartIndex = 19, EndIndex = 25 });

        var component = RenderComponent<BitAiTextPartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, message));

        var code = component.Find("code.bit-aimdv-cod");

        StringAssert.Contains(code.TextContent, "dotnet build");
        Assert.IsFalse(code.TextContent.Contains("[1]"));
        Assert.HasCount(1, component.FindComponents<BitAiCitationMarker>());
    }

    [TestMethod]
    public void TheTextFollowsThePartAsItGrows()
    {
        var part = new BitAiTextPart();
        part.Append("Hello");
        var message = Assistant(part);

        var component = RenderComponent<BitAiTextPartView>(p => p.Add(c => c.Part, part).Add(c => c.Message, message));

        Assert.IsNotEmpty(component.FindAll(".bit-aimdv-crt"));

        part.Append(" world");
        part.Complete();
        component.Render();

        Assert.AreEqual("Hello world", component.Find(".bit-aimdv-par").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-aimdv-crt"));
    }



    // A text part view under options cascaded the way a BitAiOptionsProvider cascades them, so a test can change them.
    private sealed class WovenTextHost : ComponentBase
    {
        [Parameter] public BitAiUiOptions Options { get; set; } = BitAiUiOptions.Default;

        [Parameter] public BitAiTextPart Part { get; set; } = default!;

        [Parameter] public BitAiMessage Message { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<BitAiUiOptions>>(0);
            builder.AddComponentParameter(1, "Value", Options);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<BitAiTextPartView>(0);
                child.AddComponentParameter(1, nameof(BitAiTextPartView.Part), Part);
                child.AddComponentParameter(2, nameof(BitAiTextPartView.Message), Message);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
