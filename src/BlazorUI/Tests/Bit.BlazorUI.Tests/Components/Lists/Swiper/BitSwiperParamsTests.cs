using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Lists.Swiper;

/// <summary>
/// Covers the BitParams cascade of the Swiper: what a BitSwiperParams fills in, what it leaves alone because
/// the swiper wrote it for itself, and that what the swiper derives from its parameters in a hook of its own
/// (the options handed to the browser, the classes and the item size) follows the cascaded values.
/// </summary>
[TestClass]
public class BitSwiperParamsTests : BunitTestContext
{
    [TestInitialize]
    public void Init()
    {
        Services.AddScoped(_ => new BitPageVisibility(new TestJsRuntime()));
    }

    private static RenderFragment RenderSwiper(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitSwiper>(0);
            extraAttributes?.Invoke(builder);
            builder.AddAttribute(100, nameof(BitSwiper.ChildContent), (RenderFragment)(b =>
            {
                for (int i = 0; i < 3; i++)
                {
                    b.OpenComponent<BitSwiperItem>(0);
                    b.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitSwiperParams swiperParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { swiperParams });
            parameters.AddChildContent(RenderSwiper(extraAttributes));
        });
    }

    [TestMethod]
    public void BitSwiperParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSwiper)}", BitSwiperParams.ParamName);
        Assert.AreEqual("BitParams.BitSwiper", BitSwiperParams.ParamName);
    }

    [TestMethod]
    public void BitSwiperParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitSwiperParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitSwiperParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitSwiperShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitSwiperParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Snap = BitSwiperSnap.Center,
            Vertical = true,
            ShowScrollbar = true,
            NoDrag = true,
            Rewind = true,
            ShowDots = true,
            ScrollItemsCount = 2,
            AnimationDuration = 0.25,
            AriaLabel = "Cascaded swiper",
            NextAriaLabel = "Cascaded next",
            PrevAriaLabel = "Cascaded prev",
        });

        var root = component.Find(".bit-swp");
        var instance = component.FindComponent<BitSwiper>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-swp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-swp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-swp-snc"));
        Assert.IsTrue(root.ClassList.Contains("bit-swp-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-swp-scb"));
        Assert.IsTrue(root.ClassList.Contains("bit-swp-ndr"));
        Assert.AreEqual("Cascaded swiper", root.GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded next", component.Find(".bit-swp-rbt").GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded prev", component.Find(".bit-swp-lbt").GetAttribute("aria-label"));
        Assert.IsTrue(instance.Rewind);
        Assert.IsTrue(instance.ShowDots);
        Assert.AreEqual(2, instance.ScrollItemsCount);

        // The options the browser is set up with are computed after the cascade has filled them in.
        var setup = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Swiper.setup");
        var options = setup.Arguments[4]!;

        Assert.AreEqual(true, options.GetType().GetProperty("Vertical")!.GetValue(options));
        Assert.AreEqual(2, options.GetType().GetProperty("ScrollCount")!.GetValue(options));
        Assert.AreEqual(0.25, options.GetType().GetProperty("Duration")!.GetValue(options));
    }

    [TestMethod]
    public void BitSwiperShouldApplyCascadedLayoutStyles()
    {
        var component = RenderWithParams(new BitSwiperParams
        {
            Gap = "12px",
            VisibleItemsCount = 3,
            Styles = new() { Root = "outline: 1px solid red;" },
            Classes = new() { Root = "cascaded-root" },
        });

        var root = component.Find(".bit-swp");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-swp-gap:12px"));
        Assert.IsTrue(style.Contains("--bit-swp-isz:calc((100% - 2 * var(--bit-swp-gap, 0px)) / 3)"));
        Assert.IsTrue(style.Contains("outline: 1px solid red;"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
    }

    [TestMethod]
    public void BitSwiperDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitSwiperParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            ScrollItemsCount = 3,
            NextAriaLabel = "Cascaded next",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitSwiper.Color), BitColor.Error);
            builder.AddAttribute(2, nameof(BitSwiper.ScrollItemsCount), 1);
        });

        var root = component.Find(".bit-swp");
        var instance = component.FindComponent<BitSwiper>().Instance;

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(root.ClassList.Contains("bit-swp-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-swp-suc"));
        Assert.AreEqual(1, instance.ScrollItemsCount);

        // What the swiper left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-swp-lg"));
        Assert.AreEqual("Cascaded next", component.Find(".bit-swp-rbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitSwiperShouldKeepItsDefaultsWithoutCascadingParameters()
    {
        var component = RenderComponent<BitSwiper>(parameters =>
        {
            parameters.AddChildContent<BitSwiperItem>();
        });

        var instance = component.Instance;

        Assert.IsNull(instance.CascadingParameters);
        Assert.IsFalse(instance.Rewind);
        Assert.AreEqual(1, instance.ScrollItemsCount);
        Assert.AreEqual("Slide", instance.DotAriaLabel);
        Assert.IsFalse(component.Find(".bit-swp").ClassList.Contains("bit-swp-vrt"));
    }

    [TestMethod]
    public void BitSwiperParamsShouldLeaveUnsetValuesAlone()
    {
        var component = RenderWithParams(new BitSwiperParams());

        var instance = component.FindComponent<BitSwiper>().Instance;

        Assert.IsFalse(instance.Rewind);
        Assert.IsFalse(instance.AutoPlay);
        Assert.AreEqual(0.5, instance.AnimationDuration);
        Assert.AreEqual(2000, instance.AutoPlayInterval);
        Assert.AreEqual(5, instance.DragThreshold);
        Assert.AreEqual("Slide", instance.DotAriaLabel);
        Assert.AreEqual("Choose slide to display", instance.DotsAriaLabel);
        Assert.IsTrue(instance.PauseOnHover);
        Assert.IsTrue(instance.PauseOnFocus);
        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Snap);
    }
}
