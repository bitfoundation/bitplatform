using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.ScrollablePane;

/// <summary>
/// Covers the BitParams cascade of the ScrollablePane: what a BitScrollablePaneParams fills in, what it leaves alone
/// because the pane wrote it for itself, that every plain parameter has a counterpart on the params object and every
/// property of it reaches the pane, and that what the pane decides from its parameters - its styles, its classes,
/// its accessibility and the options it hands the browser side - follows the cascaded values.
/// </summary>
[TestClass]
public class BitScrollablePaneParamsTests : BunitTestContext
{
    private const string Setup = "BitBlazorUI.ScrollablePane.setup";

    // Where the pane opens is decided once, for one pane, and the content is per pane; the rest is shared.
    private static readonly string[] _notCascaded =
    [
        nameof(BitScrollablePane.CascadingParameters),
        nameof(BitScrollablePane.InitialScrollLeft),
        nameof(BitScrollablePane.InitialScrollTop),
    ];

    [TestMethod]
    public void BitScrollablePaneParamsShouldHaveCorrectParamName()
    {
        var @params = new BitScrollablePaneParams();

        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitScrollablePane)}", BitScrollablePaneParams.ParamName);
        Assert.AreEqual("BitParams.BitScrollablePane", BitScrollablePaneParams.ParamName);
        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitScrollablePaneParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitScrollablePaneParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks and templates are deliberately left out, and so is what belongs to one pane alone.
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitScrollablePane).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                               .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .Where(n => typeof(BitScrollablePaneParams).GetProperty(n) is null)
                                               .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }

    [TestMethod]
    public void BitScrollablePaneParamsShouldApplyEveryPropertyItCarries()
    {
        // The other direction: a property added here but forgotten in UpdateParameters is accepted by the cascade
        // and then never reaches the pane.
        var paneParams = new BitScrollablePaneParams();
        var pane = new BitScrollablePane();

        var properties = typeof(BitScrollablePaneParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                        .Where(p => p.CanWrite)
                                                        .ToArray();

        foreach (var property in properties)
        {
            var target = typeof(BitScrollablePane).GetProperty(property.Name)!;

            property.SetValue(paneParams, SampleValue(property.PropertyType, target.GetValue(pane)));
        }

        paneParams.UpdateParameters(pane);

        foreach (var property in properties)
        {
            var target = typeof(BitScrollablePane).GetProperty(property.Name)!;

            Assert.AreEqual(property.GetValue(paneParams), target.GetValue(pane), $"{property.Name} is not applied.");
        }

        // A value unlike the component's default, so the assertion above cannot pass by coincidence.
        static object SampleValue(Type type, object? current)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            if (underlying == typeof(bool)) return current is true ? false : true;
            if (underlying == typeof(int)) return current is 7 ? 8 : 7;
            if (underlying == typeof(string)) return "sample";
            if (underlying.IsEnum)
            {
                var values = Enum.GetValues(underlying).Cast<object>().ToArray();
                return values.First(v => Equals(v, current) is false);
            }

            throw new NotSupportedException($"Add a sample value for {type}.");
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldApplyCascadingStylesAndClassesFromBitParams()
    {
        var component = RenderInParams(new BitScrollablePaneParams
        {
            Height = "10rem",
            MaxWidth = "30rem",
            Horizontal = true,
            Overscroll = BitOverscroll.Contain,
            Gutter = BitScrollbarGutter.Stable,
            ScrollPadding = "1rem",
            Snap = BitScrollSnap.Proximity,
            SnapAlign = BitScrollSnapAlign.Center,
            SnapStop = true,
            ExpandOnPrint = true,
            Modern = true,
            AutoHideScrollbar = true,
            Smooth = true,
            FadeSize = "3rem",
            Class = "cascaded",
        });

        var root = component.Find(".bit-scp");
        var style = root.GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "height:10rem");
        StringAssert.Contains(style, "max-width:30rem");
        StringAssert.Contains(style, "overflow-x:auto");
        StringAssert.Contains(style, "overscroll-behavior:contain");
        StringAssert.Contains(style, "scrollbar-gutter:stable");
        StringAssert.Contains(style, "scroll-padding:1rem");
        StringAssert.Contains(style, "scroll-snap-type:both proximity");
        StringAssert.Contains(style, "--bit-scp-fsz:3rem");

        foreach (var cls in new[] { "bit-scp-hor", "bit-scp-sna-cnt", "bit-scp-sns", "bit-scp-eop", "bit-scp-mod", "bit-scp-ahs", "bit-scp-smt", "cascaded" })
        {
            Assert.IsTrue(root.ClassList.Contains(cls), cls);
        }
    }

    [TestMethod]
    public void BitScrollablePaneShouldApplyCascadingAccessibilityFromBitParams()
    {
        var component = RenderInParams(new BitScrollablePaneParams
        {
            Focusable = true,
            Role = "region",
            AriaLabel = "Cascaded",
        });

        var root = component.Find(".bit-scp");

        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Cascaded", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldApplyCascadedBaseParameters()
    {
        var component = RenderInParams(new BitScrollablePaneParams { Dir = BitDir.Rtl, Disabled = true, Focusable = true, Fade = true });

        var root = component.Find(".bit-scp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        // A cascaded Disabled is read by everything the pane decides from it: out of the tab order, no browser side.
        Assert.AreEqual("-1", root.GetAttribute("tabindex"));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == Setup));
    }

    [TestMethod]
    public void BitScrollablePaneOwnParametersShouldWinOverCascadingOnes()
    {
        var component = RenderInParams(new BitScrollablePaneParams
        {
            Height = "10rem",
            Modern = true,
            Role = "region",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitScrollablePane.Height), "5rem");
            builder.AddAttribute(2, nameof(BitScrollablePane.Modern), false);
            builder.AddAttribute(3, nameof(BitScrollablePane.Role), "group");
        });

        var root = component.Find(".bit-scp");

        StringAssert.Contains(root.GetAttribute("style"), "height:5rem");
        Assert.IsFalse(root.GetAttribute("style")!.Contains("height:10rem"));
        Assert.IsFalse(root.ClassList.Contains("bit-scp-mod"));
        Assert.AreEqual("group", root.GetAttribute("role"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldHandCascadingBrowserSideOptionsToJs()
    {
        RenderInParams(new BitScrollablePaneParams
        {
            Fade = true,
            DragScroll = true,
            DragMomentum = true,
            HorizontalWheel = true,
            PreserveScroll = true,
            ReachOffset = 120,
            ScrollThrottle = 50,
            AutoScrollThreshold = 16,
            Modern = true,
            AutoHideScrollbar = true,
            AutoHideDelay = 300,
            NoScroll = true,
        });

        var setups = Context.JSInterop.Invocations.Where(i => i.Identifier == Setup).ToArray();

        Assert.AreEqual(1, setups.Length);

        var options = setups[0].Arguments[3]!;

        Assert.AreEqual(true, Option(options, "Fade"));
        Assert.AreEqual(true, Option(options, "Drag"));
        Assert.AreEqual(true, Option(options, "Momentum"));
        Assert.AreEqual(true, Option(options, "Wheel"));
        Assert.AreEqual(true, Option(options, "Preserve"));
        Assert.AreEqual(120, Option(options, "Offset"));
        Assert.AreEqual(50, Option(options, "Throttle"));
        Assert.AreEqual(16, Option(options, "AutoScrollThreshold"));
        Assert.AreEqual(true, Option(options, "AutoHide"));
        Assert.AreEqual(300, Option(options, "AutoHideDelay"));
        Assert.AreEqual(true, Option(options, "NoScroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldPinACascadedAutoScroll()
    {
        RenderInParams(new BitScrollablePaneParams { AutoScroll = true });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.ScrollablePane.autoScroll"));
    }

    [TestMethod]
    public void BitScrollablePaneShouldNotBeTouchedByAnEmptyCascade()
    {
        var component = RenderInParams(new BitScrollablePaneParams());

        var root = component.Find(".bit-scp");

        Assert.AreEqual("bit-scp", root.GetAttribute("class"));
        Assert.IsTrue(string.IsNullOrEmpty(root.GetAttribute("style")));
        Assert.IsNull(root.GetAttribute("tabindex"));
        Assert.IsNull(root.GetAttribute("role"));
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == Setup));
    }

    private IRenderedComponent<BitParams> RenderInParams(BitScrollablePaneParams @params, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitScrollablePane>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    // The options class is internal to the library, so the handed over instance is read by name rather than cast.
    private static object? Option(object options, string name)
    {
        return options.GetType().GetProperty(name)!.GetValue(options);
    }
}
