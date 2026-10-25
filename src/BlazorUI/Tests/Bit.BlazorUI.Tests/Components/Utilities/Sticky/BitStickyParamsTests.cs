using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.Sticky;

/// <summary>
/// Covers the BitParams cascade of the Sticky: what a BitStickyParams fills in, what it leaves alone because the
/// sticky wrote it for itself, and that what the sticky derives from its parameters (its element, its offsets, its
/// classes and the look it takes on while stuck) follows the cascaded values.
/// </summary>
[TestClass]
public class BitStickyParamsTests : BunitTestContext
{
    // What a sticky holds and what it reports back are written per sticky, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitSticky.CascadingParameters),
        nameof(BitSticky.ChildContent),
        nameof(BitSticky.OnStuckChanged),
        nameof(BitSticky.OnStuckEdgesChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitStickyParams stickyParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { stickyParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSticky>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitSticky.ChildContent), (RenderFragment)(b => b.AddContent(0, "Pinned")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitStickyParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSticky)}", BitStickyParams.ParamName);
        Assert.AreEqual("BitParams.BitSticky", BitStickyParams.ParamName);
    }

    [TestMethod]
    public void BitStickyParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitStickyParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitStickyParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitStickyParamsShouldCarryEveryParameterThatIsNotPerSticky()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitSticky).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                          .Select(p => p.Name)
                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitStickyParams).GetProperty(name), $"BitStickyParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitStickyShouldApplyCascadedElementAndPlacement()
    {
        var component = RenderWithParams(new BitStickyParams { Element = "header", Placement = BitPlacement.StartAndEnd });

        var root = component.Find(".bit-stk");

        Assert.AreEqual("HEADER", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-stk-sae"));
        Assert.IsFalse(root.ClassList.Contains("bit-stk-top"));
        Assert.AreEqual("Pinned", root.TextContent);
    }

    [TestMethod]
    public void BitStickyShouldApplyEveryCascadedOffset()
    {
        var component = RenderWithParams(new BitStickyParams { Top = "1rem", Bottom = "2rem", Left = "3", Right = "4%", ZIndex = 7 });

        var root = component.Find(".bit-stk");
        var style = root.GetAttribute("style")!;

        // An offset set takes the place of the default top placement, cascaded or not, and a bare number is still pixels.
        StringAssert.Contains(style, "top: 1rem");
        StringAssert.Contains(style, "bottom: 2rem");
        StringAssert.Contains(style, "left: 3px");
        StringAssert.Contains(style, "right: 4%");
        StringAssert.Contains(style, "z-index: 7");
        Assert.IsFalse(root.ClassList.Contains("bit-stk-top"));
    }

    [TestMethod]
    public async Task BitStickyShouldApplyTheCascadedStuckClassAndStyle()
    {
        var component = RenderWithParams(new BitStickyParams { StuckClass = "cascaded-stuck", StuckStyle = "color: red", ElevateOnStuck = true });

        var root = component.Find(".bit-stk");

        // Both wait for the element to be pinned, the way they do when the sticky writes them itself.
        Assert.IsFalse(root.ClassList.Contains("cascaded-stuck"));
        Assert.IsNull(root.GetAttribute("style"));
        Assert.IsTrue(root.ClassList.Contains("bit-stk-elv"));

        var sticky = component.FindComponent<BitSticky>();

        await component.InvokeAsync(() => sticky.Instance._OnStuckChange((int)BitStickyEdges.Top));

        root = component.Find(".bit-stk");

        Assert.IsTrue(root.ClassList.Contains("bit-stk-stc"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-stuck"));
        Assert.AreEqual("color: red", root.GetAttribute("style"));
    }

    [TestMethod]
    public void BitStickyShouldAttachTheDetectionForACascadedStuckClass()
    {
        RenderWithParams(new BitStickyParams { StuckClass = "cascaded-stuck", ScrollPadding = true });

        // The cascade is applied before the script is set up, so what it fills in is what decides whether the
        // detection is attached - and with which halves.
        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Stickies.setup");

        Assert.IsTrue((bool)setup.Arguments[2]!);
        Assert.IsTrue((bool)setup.Arguments[3]!);
    }

    [TestMethod]
    public void BitStickyShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitStickyParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded", Style = "margin: 0", AriaLabel = "Filters" });

        var root = component.Find(".bit-stk");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("margin: 0", root.GetAttribute("style"));
        Assert.AreEqual("Filters", root.GetAttribute("aria-label"));
        Assert.AreEqual("group", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitStickyShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitStickyParams
        {
            Element = "header",
            Placement = BitPlacement.Bottom,
            Top = "1rem",
            ZIndex = 4,
            ElevateOnStuck = true,
            ScrollPadding = true,
            StuckClass = "cascaded-stuck",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSticky.Element), "footer");
            builder.AddAttribute(2, nameof(BitSticky.Placement), (BitPlacement?)BitPlacement.Start);
            builder.AddAttribute(3, nameof(BitSticky.Top), "5rem");
            builder.AddAttribute(4, nameof(BitSticky.ZIndex), (int?)9);
            builder.AddAttribute(5, nameof(BitSticky.ElevateOnStuck), false);
            builder.AddAttribute(6, nameof(BitSticky.ScrollPadding), false);
            builder.AddAttribute(7, nameof(BitSticky.StuckClass), "own-stuck");
        });

        var root = component.Find(".bit-stk");
        var style = root.GetAttribute("style")!;

        Assert.AreEqual("FOOTER", root.TagName);
        Assert.IsTrue(root.ClassList.Contains("bit-stk-srt"));
        Assert.IsFalse(root.ClassList.Contains("bit-stk-btm"));
        StringAssert.Contains(style, "top: 5rem");
        Assert.IsFalse(style.Contains("1rem"));
        StringAssert.Contains(style, "z-index: 9");

        // A false written by the sticky is a value the cascade must not overwrite, so neither half of the script attaches.
        Assert.IsFalse(root.ClassList.Contains("bit-stk-elv"));
        Assert.IsFalse(component.FindComponent<BitSticky>().Instance.ScrollPadding);

        var setup = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Stickies.setup");

        Assert.IsTrue((bool)setup.Arguments[2]!, "The sticky's own StuckClass still attaches the detection.");
        Assert.IsFalse((bool)setup.Arguments[3]!);
    }

    [TestMethod]
    public void BitStickyShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitStickyParams());

        component.MarkupMatches(@"<div class=""bit-stk bit-stk-top"" id:ignore>Pinned</div>");
        Assert.IsEmpty(Context.JSInterop.Invocations.Where(i => i.Identifier == "BitBlazorUI.Stickies.setup"));
    }
}
