using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// bUnit render/interaction tests for the <see cref="Bmotion"/> component: attribute injection,
/// authored-id adoption, engine registration and the child-content invariants.
/// </summary>
[TestClass]
public class BmotionComponentTests
{
    private static RenderFragment Div(string @class = "box", string? id = null, string? style = null)
        => b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", @class);
            if (id != null) b.AddAttribute(2, "id", id);
            if (style != null) b.AddAttribute(3, "style", style);
            b.AddContent(4, "hi");
            b.CloseElement();
        };

    [TestMethod]
    public void Injects_Id_And_InitialStyle_IntoFirstRootElement()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.ChildContent, Div()));

        var div = cut.Find("div");
        Assert.AreEqual("box", div.Id);
        StringAssert.Contains(div.GetAttribute("style"), "opacity");
        Assert.AreEqual("box", div.GetAttribute("class"));
    }

    [TestMethod]
    public void Adopts_AuthoredId_WhenNoIdParameter()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div(id: "authored")));

        Assert.AreEqual("authored", cut.Find("div").Id);
        // The engine must register the adopted id, not a generated one.
        Assert.IsTrue(ctx.Interop.Calls.Any(c => c.Method == "registerElement" && (string?)c.Args[0] == "authored"));
    }

    [TestMethod]
    public void RegistersElement_WithEngine_OnFirstRender()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Animate, Bm.To(x: 100))
            .Add(p => p.ChildContent, Div()));

        Assert.IsTrue(ctx.Interop.WasCalled("registerElement"));
    }

    [TestMethod]
    public void GeneratesStableId_WhenNoneAuthored()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div()));

        var id = cut.Find("div").Id;
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));
    }

    [TestMethod]
    public void Throws_WhenNoChildContent()
    {
        using var ctx = new BmotionTestContext();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ctx.Render<Bmotion>(ps => ps.Add(p => p.Animate, Bm.To(x: 1))));
    }

    [TestMethod]
    public void Throws_WhenChildContentHasNoRootElement()
    {
        using var ctx = new BmotionTestContext();
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            ctx.Render<Bmotion>(ps => ps
                .Add(p => p.Animate, Bm.To(x: 1))
                .Add(p => p.ChildContent, b => b.AddContent(0, "just text"))));
    }

    [TestMethod]
    public void AuthoredStyle_WinsOver_MotionStyle_OnConflict()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.ChildContent, Div(style: "opacity:0.7;")));

        // Motion style is emitted first, author's second, so author's opacity wins (CSS last-wins).
        // Parse the LAST opacity declaration's value rather than a fragile prefix-substring compare
        // ("opacity:0" is also a prefix of "opacity:0.7").
        var style = cut.Find("div").GetAttribute("style")!;
        var idx = style.LastIndexOf("opacity:", StringComparison.Ordinal);
        Assert.IsTrue(idx >= 0, $"expected an opacity declaration in: {style}");
        var lastOpacity = style[(idx + "opacity:".Length)..].Split(';', 2)[0].Trim();
        Assert.AreEqual("0.7", lastOpacity, $"authored opacity must be the effective (last) one: {style}");
    }
}
