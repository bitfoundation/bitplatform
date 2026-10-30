using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.RendererBoundary;

[TestClass]
public class BitAiRendererBoundaryTests : BitAiTestBase
{
    private static RenderFragment Content(string text) => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "content");
        builder.AddContent(2, text);
        builder.CloseElement();
    };

    private static RenderFragment Throwing(string message) => builder =>
    {
        builder.OpenComponent<Thrower>(0);
        builder.AddComponentParameter(1, nameof(Thrower.Message), message);
        builder.CloseComponent();
    };



    [TestMethod]
    public void ContentThatRendersIsRenderedWithNoElementOfTheBoundarysOwn()
    {
        var component = RenderComponent<BitAiRendererBoundary>(p => p
            .Add(c => c.Class, "custom-class")
            .Add(c => c.ChildContent, Content("fine")));

        Assert.AreEqual("fine", component.Find(".content").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-airbd"));
        Assert.IsEmpty(component.FindAll(".custom-class"));
    }

    [TestMethod]
    public void AFailingRendererIsReplacedByANoticeRatherThanTakingThePageDown()
    {
        var component = RenderComponent<BitAiRendererBoundary>(p => p
            .Add(c => c.Label, "The renderer for 'chart' failed")
            .Add(c => c.Class, "custom-class")
            .Add(c => c.Id, "boundary")
            .Add(c => c.ChildContent, Throwing("unexpected shape")));

        // Phrasing content, so a boundary around inline content leaves the paragraph it sits in whole.
        var root = component.Find(".bit-airbd");
        Assert.AreEqual("SPAN", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.AreEqual("boundary", root.Id);

        var tag = component.FindComponent<BitTag>();
        Assert.AreEqual("The renderer for 'chart' failed", tag.Instance.Text);
        Assert.AreEqual("unexpected shape", tag.Instance.Title);
        StringAssert.Contains(root.TextContent, "The renderer for 'chart' failed");
    }

    [TestMethod]
    public void AFailureWithoutALabelSaysTheContentCouldNotBeRendered()
    {
        var component = RenderComponent<BitAiRendererBoundary>(p => p.Add(c => c.ChildContent, Throwing("boom")));

        StringAssert.Contains(component.Find(".bit-airbd").TextContent, "This content could not be rendered");
    }

    [TestMethod]
    public void ANewRenderOfTheBoundaryTriesTheContentAgain()
    {
        var component = RenderComponent<BitAiRendererBoundary>(p => p.Add(c => c.ChildContent, Throwing("partial")));
        Assert.IsNotNull(component.Find(".bit-airbd"));

        component.Render(p => p.Add(c => c.ChildContent, Content("complete")));

        Assert.AreEqual("complete", component.Find(".content").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-airbd"));
    }

    // A renderer handed the same partial value on every render of a streaming message throws again each time: the notice
    // has to survive those renders, whether the content throws while it renders or in a lifecycle method - and the
    // content has to come back once it renders.
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void AContentThatKeepsFailingKeepsItsNoticeAcrossTheRendersOfItsParent(bool throwWhileRendering)
    {
        var host = RenderComponent<Host>(p => p.Add(h => h.Content, throwWhileRendering ? Throwing("partial") : ThrowingOnParameters("partial")));

        for (var tick = 1; tick <= 3; tick++)
        {
            host.Render(p => p.Add(h => h.Tick, tick));

            Assert.HasCount(1, host.FindAll(".bit-airbd"), $"render {tick}");
            Assert.AreEqual("partial", host.FindComponent<BitTag>().Instance.Title, $"render {tick}");
        }

        host.Render(p => p.Add(h => h.Content, Content("complete")));

        Assert.AreEqual("complete", host.Find(".content").TextContent);
        Assert.IsEmpty(host.FindAll(".bit-airbd"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void ANewRenderOfTheBoundaryItselfWithTheSameFailingContentKeepsTheNotice(bool throwWhileRendering)
    {
        var content = throwWhileRendering ? Throwing("partial") : ThrowingOnParameters("partial");
        var component = RenderComponent<BitAiRendererBoundary>(p => p.Add(c => c.ChildContent, content));

        component.Render(p => p.Add(c => c.ChildContent, content));
        component.Render(p => p.Add(c => c.ChildContent, content));

        Assert.HasCount(1, component.FindAll(".bit-airbd"));

        component.Render(p => p.Add(c => c.ChildContent, Content("complete")));

        Assert.AreEqual("complete", component.Find(".content").TextContent);
    }

    [TestMethod]
    public void AContentThatRendersIsNotRecreatedByTheRendersOfItsParent()
    {
        var host = RenderComponent<Host>(p => p.Add(h => h.Content, Counting()));
        var counter = host.FindComponent<Counter>().Instance;

        host.Render(p => p.Add(h => h.Tick, 1));
        host.Render(p => p.Add(h => h.Tick, 2));

        Assert.AreSame(counter, host.FindComponent<Counter>().Instance);
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void TheNoticeShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderComponent<BitAiRendererBoundary>(p => p
            .Add(c => c.ChildContent, Throwing("boom"))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-airbd").GetAttribute("style") ?? string.Empty;

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }



    private static RenderFragment ThrowingOnParameters(string message) => builder =>
    {
        builder.OpenComponent<ParametersThrower>(0);
        builder.AddComponentParameter(1, nameof(ParametersThrower.Message), message);
        builder.CloseComponent();
    };

    private static RenderFragment Counting() => builder =>
    {
        builder.OpenComponent<Counter>(0);
        builder.CloseComponent();
    };



    // Throws in the middle of its own render, with elements of its own already opened.
    private sealed class Thrower : ComponentBase
    {
        [Parameter] public string Message { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.OpenElement(1, "ul");
            throw new InvalidOperationException(Message);
        }
    }

    private sealed class ParametersThrower : ComponentBase
    {
        [Parameter] public string Message { get; set; } = string.Empty;

        protected override void OnParametersSet() => throw new InvalidOperationException(Message);
    }

    private sealed class Counter : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddMarkupContent(0, "<span class=\"counter\"></span>");
    }

    // The parent of the boundary: every render of it is a render of the boundary, as a message streaming its parts is.
    private sealed class Host : ComponentBase
    {
        [Parameter] public RenderFragment? Content { get; set; }

        [Parameter] public int Tick { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-tick", Tick);
            builder.OpenComponent<BitAiRendererBoundary>(2);
            builder.AddComponentParameter(3, nameof(BitAiRendererBoundary.ChildContent), Content);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}
