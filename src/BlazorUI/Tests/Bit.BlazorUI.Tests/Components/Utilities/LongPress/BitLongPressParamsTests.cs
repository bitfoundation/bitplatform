using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.LongPress;

/// <summary>
/// The BitParams cascade of the long press: what a BitLongPressParams carries (how the long presses of an app are
/// held), that the long press takes it, and that what is set on the long press itself wins over it.
/// </summary>
[TestClass]
public class BitLongPressParamsTests : BunitTestContext
{
    // What one long press is: its content and what pressing it does.
    private static readonly string[] _notCascaded =
    [
        nameof(BitLongPress.CascadingParameters),
        nameof(BitLongPress.ChildContent),
        nameof(BitLongPress.OnLongPress),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitLongPressParams longPressParams, Action<RenderTreeBuilder>? attributes = null)
    {
        Context.JSInterop.SetupVoid("BitBlazorUI.LongPress.setup");

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { longPressParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitLongPress>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitLongPressParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitLongPress", BitLongPressParams.ParamName);
        Assert.AreEqual(BitLongPressParams.ParamName, new BitLongPressParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitLongPressParams());
    }

    [TestMethod]
    public void BitLongPressParamsShouldCarryEveryParameterButWhatMakesOneLongPressItsOwn()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitLongPress).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                             .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                             .Select(p => p.Name)
                                             .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                             .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitLongPressParams).GetProperty(name), $"BitLongPressParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitLongPressParams).GetProperty(name), $"BitLongPressParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitLongPressShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitLongPressParams
        {
            Class = "cascaded-root",
            Delay = 900,
            MoveTolerance = 20m,
            TouchOnly = true,
            KeyboardTrigger = true,
        });

        var root = component.Find(".bit-lpr");
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.AreEqual("0", root.GetAttribute("tabindex"), "the cascaded KeyboardTrigger makes it a tab stop");

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.LongPress.setup");
        Assert.AreEqual(900, setup.Arguments[2]);
        Assert.AreEqual(20m, setup.Arguments[3]);
        Assert.AreEqual(true, setup.Arguments[4]);
        Assert.AreEqual(true, setup.Arguments[5]);
    }

    [TestMethod]
    public void BitLongPressShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        RenderWithParams(new BitLongPressParams { Delay = 900, MoveTolerance = 20m }, builder =>
        {
            builder.AddComponentParameter(1, nameof(BitLongPress.Delay), 250);
        });

        var setup = Context.JSInterop.VerifyInvoke("BitBlazorUI.LongPress.setup");
        Assert.AreEqual(250, setup.Arguments[2]);
        Assert.AreEqual(20m, setup.Arguments[3], "the cascaded value is taken where the long press sets none");
    }
}
