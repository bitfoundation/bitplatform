using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AppShell;

/// <summary>
/// Covers the BitParams cascade of the AppShell: what a BitAppShellParams carries (every parameter that describes how
/// a shell behaves rather than which shell it is), and that the values the razor-based cascade test does not reach -
/// the single-edge insets, the overflow and overscroll of the main container, the keyboard tracking and the options
/// the browser side is driven with - follow the cascaded values, and are kept out of the way of the shell's own.
/// </summary>
[TestClass]
public class BitAppShellParamsTests : BunitTestContext
{
    // What makes one shell the one it is: its content, the values it cascades, its callbacks - and the scroll store,
    // which only the one app shell of an application is meant to own.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAppShell.CascadingParameters),
        nameof(BitAppShell.ChildContent),
        nameof(BitAppShell.OnKeyboardInsetChanged),
        nameof(BitAppShell.OnReachedBottom),
        nameof(BitAppShell.OnReachedLeft),
        nameof(BitAppShell.OnReachedRight),
        nameof(BitAppShell.OnReachedTop),
        nameof(BitAppShell.OnScroll),
        nameof(BitAppShell.OnScrollEnd),
        nameof(BitAppShell.OnScrollStart),
        nameof(BitAppShell.PersistScroll),
        nameof(BitAppShell.ScrollRestoration),
        nameof(BitAppShell.ValueList),
        nameof(BitAppShell.Values),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAppShellParams shellParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { shellParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAppShell>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAppShellParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitAppShell)}", BitAppShellParams.ParamName);
        Assert.AreEqual("BitParams.BitAppShell", BitAppShellParams.ParamName);
    }

    [TestMethod]
    public void BitAppShellParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitAppShellParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitAppShellParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitAppShellParamsShouldCarryEveryParameterThatDescribesHowAShellBehaves()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAppShell).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                            .Select(p => p.Name)
                                            .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAppShellParams).GetProperty(name), $"BitAppShellParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitAppShellShouldTakeTheCascadedEdgeInsets()
    {
        var component = RenderWithParams(new BitAppShellParams
        {
            NoTopInset = true,
            NoBottomInset = true,
            NoStartInset = true,
            NoEndInset = true,
        });

        var classes = component.Find(".bit-ash").ClassList;

        Assert.IsTrue(classes.Contains("bit-ash-nit"));
        Assert.IsTrue(classes.Contains("bit-ash-nib"));
        Assert.IsTrue(classes.Contains("bit-ash-nis"));
        Assert.IsTrue(classes.Contains("bit-ash-nie"));
        Assert.IsFalse(classes.Contains("bit-ash-nin"));
    }

    [TestMethod]
    public void BitAppShellShouldTakeTheCascadedOverflowAndOverscroll()
    {
        var component = RenderWithParams(new BitAppShellParams
        {
            OverflowX = BitOverflow.Hidden,
            OverflowY = BitOverflow.Scroll,
            Overscroll = BitOverscroll.Contain,
        });

        var style = component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "overflow-x:hidden");
        StringAssert.Contains(style, "overflow-y:scroll");
        StringAssert.Contains(style, "overscroll-behavior:contain");
    }

    [TestMethod]
    public void BitAppShellShouldTakeACascadedAvoidKeyboard()
    {
        RenderWithParams(new BitAppShellParams { AvoidKeyboard = true });

        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.AppShell.setupKeyboard"));
    }

    [TestMethod]
    public void BitAppShellShouldDriveTheBrowserSideWithTheCascadedOptions()
    {
        RenderWithParams(new BitAppShellParams
        {
            AutoScroll = true,
            AutoScrollThreshold = 48,
            PreserveScroll = true,
            ReachOffset = 16,
            ScrollThrottle = 100,
        });

        var options = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.ScrollablePane.setup").Arguments[3];

        Assert.IsNotNull(options);
        Assert.AreEqual(true, OptionOf(options!, "AutoScroll"));
        Assert.AreEqual(48, OptionOf(options!, "AutoScrollThreshold"));
        Assert.AreEqual(true, OptionOf(options!, "Preserve"));
        Assert.AreEqual(16, OptionOf(options!, "Offset"));
        Assert.AreEqual(100, OptionOf(options!, "Throttle"));
    }

    [TestMethod]
    public void BitAppShellShouldKeepItsOwnEdgeInsetsAndOverflowOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAppShellParams
        {
            NoTopInset = true,
            NoBottomInset = true,
            OverflowX = BitOverflow.Hidden,
            Overscroll = BitOverscroll.Contain,
            ReachOffset = 16,
        }, builder =>
        {
            // An own false is a value too: it opts the shell out of the cascaded true.
            builder.AddAttribute(1, nameof(BitAppShell.NoTopInset), false);
            builder.AddAttribute(2, nameof(BitAppShell.OverflowX), (BitOverflow?)BitOverflow.Auto);
            builder.AddAttribute(3, nameof(BitAppShell.Overscroll), (BitOverscroll?)BitOverscroll.None);
            builder.AddAttribute(4, nameof(BitAppShell.ReachOffset), 4);
            builder.AddAttribute(5, nameof(BitAppShell.OnReachedTop), EventCallback.Factory.Create(this, () => { }));
        });

        var classes = component.Find(".bit-ash").ClassList;

        Assert.IsFalse(classes.Contains("bit-ash-nit"));
        Assert.IsTrue(classes.Contains("bit-ash-nib"), "What the shell did not set is still filled in.");

        var style = component.Find(".bit-ash-main").GetAttribute("style") ?? string.Empty;

        StringAssert.Contains(style, "overflow-x:auto");
        Assert.IsFalse(style.Contains("overflow-x:hidden"));
        StringAssert.Contains(style, "overscroll-behavior:none");

        var options = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.ScrollablePane.setup").Arguments[3];

        Assert.AreEqual(4, OptionOf(options!, "Offset"));
    }

    [TestMethod]
    public void BitAppShellShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitAppShellParams());

        var root = component.Find(".bit-ash");

        Assert.IsFalse(root.ClassList.Any(c => c.StartsWith("bit-ash-", StringComparison.Ordinal)));
        Assert.IsTrue(string.IsNullOrEmpty(component.Find(".bit-ash-main").GetAttribute("style")));
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier is "BitBlazorUI.ScrollablePane.setup" or "BitBlazorUI.AppShell.setupKeyboard"));
    }

    private static object? OptionOf(object options, string name)
    {
        var property = options.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        Assert.IsNotNull(property, $"The options of the browser side carry no {name}.");

        return property!.GetValue(options);
    }
}
