using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmViewport.Root"/> - the scroll-container root for WhileInView
/// (IntersectionObserver's <c>root</c>). Covers the serialised option the bridge receives and the
/// re-observation that a changed root must trigger.
/// </summary>
[TestClass]
public class ViewportRootTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static IDictionary<string, object?> JsOptions(BmViewport viewport)
        => (IDictionary<string, object?>)viewport.ToJsObject();

    [TestMethod]
    public void Root_IsSerialisedForTheObserver()
    {
        var options = JsOptions(new BmViewport { Root = ".chat-scroller" });

        Assert.AreEqual(".chat-scroller", options["root"]);
    }

    [TestMethod]
    public void Root_DefaultsToNull_MeaningTheBrowserViewport()
    {
        Assert.IsNull(JsOptions(new BmViewport())["root"]);
    }

    [TestMethod]
    public void Root_WhitespaceIsNormalisedToNull()
    {
        // An empty selector would make querySelector throw; null is the API's "use the viewport".
        Assert.IsNull(JsOptions(new BmViewport { Root = "   " })["root"]);
    }

    [TestMethod]
    public void Root_IsPassedThroughToTheObserveCall()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.Viewport, new BmViewport { Root = "#scroller" })
            .Add(p => p.ChildContent, Div));

        var call = ctx.Interop.Calls.Last(c => c.Method == "observeViewport");
        Assert.AreEqual("#scroller", ((BmViewport)call.Args[1]!).Root);
    }

    [TestMethod]
    public void ChangingRoot_ReObservesTheElement()
    {
        using var ctx = new BmotionTestContext();
        var component = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.Viewport, new BmViewport { Root = "#a" })
            .Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("observeViewport");

        component.Render(ps => ps
            .Add(p => p.Viewport, new BmViewport { Root = "#b" }));

        Assert.AreEqual(before + 1, ctx.Interop.CountOf("observeViewport"),
            "a changed root must re-observe; otherwise the element keeps watching the old container");
    }

    [TestMethod]
    public void UnchangedRoot_DoesNotReObserve()
    {
        using var ctx = new BmotionTestContext();
        var component = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.Viewport, new BmViewport { Root = "#a" })
            .Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("observeViewport");

        // A fresh BmViewport instance with identical options is the idiomatic re-render shape.
        component.Render(ps => ps
            .Add(p => p.Viewport, new BmViewport { Root = "#a" }));

        Assert.AreEqual(before, ctx.Interop.CountOf("observeViewport"));
    }
}
