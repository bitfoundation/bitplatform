using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.VirtualKeyboard;

/// <summary>
/// The BitParams cascade of the virtual keyboard: what the keyboards of an app agree on cascades; what one keyboard types
/// into and holds does not.
/// </summary>
[TestClass]
public class BitVirtualKeyboardParamsTests : BunitTestContext
{
    private static readonly string[] _notCascaded =
    [
        nameof(BitVirtualKeyboard.CascadingParameters),
        nameof(BitVirtualKeyboard.DefaultOpen),
        nameof(BitVirtualKeyboard.DefaultValue),
        nameof(BitVirtualKeyboard.FollowFocus),
        nameof(BitVirtualKeyboard.KeyTemplate),
        nameof(BitVirtualKeyboard.Layout),
        nameof(BitVirtualKeyboard.LayoutSelector),
        nameof(BitVirtualKeyboard.MaxLength),
        nameof(BitVirtualKeyboard.OnCommand),
        nameof(BitVirtualKeyboard.OnEnter),
        nameof(BitVirtualKeyboard.OnInput),
        nameof(BitVirtualKeyboard.OnKeyPress),
        nameof(BitVirtualKeyboard.OnLayerChange),
        nameof(BitVirtualKeyboard.OnLayoutChange),
        nameof(BitVirtualKeyboard.Open),
        nameof(BitVirtualKeyboard.OpenChanged),
        nameof(BitVirtualKeyboard.Purpose),
        nameof(BitVirtualKeyboard.Scramble),
        nameof(BitVirtualKeyboard.Target),
        nameof(BitVirtualKeyboard.TargetSelector),
        nameof(BitVirtualKeyboard.Value),
        nameof(BitVirtualKeyboard.ValueChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitVirtualKeyboardParams keyboardParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { keyboardParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitVirtualKeyboard>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitVirtualKeyboardParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitVirtualKeyboard", BitVirtualKeyboardParams.ParamName);
        Assert.AreEqual(BitVirtualKeyboardParams.ParamName, new BitVirtualKeyboardParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitVirtualKeyboardParams());
    }

    [TestMethod]
    public void BitVirtualKeyboardParamsShouldCarryEveryParameterButTheStateOfOneKeyboard()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitVirtualKeyboard).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                   .Select(p => p.Name)
                                                   .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                   .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitVirtualKeyboardParams).GetProperty(name), $"BitVirtualKeyboardParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitVirtualKeyboardParams).GetProperty(name), $"BitVirtualKeyboardParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitVirtualKeyboardShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitVirtualKeyboardParams
        {
            Class = "cascaded-root",
            Form = BitVirtualKeyboardForm.Full,
            Size = BitSize.Large,
            RepeatDelay = 300,
            Strings = new() { RoleDescription = "teclado" },
            Classes = new() { Panel = "cascaded-pnl" },
        });

        var instance = component.FindComponent<BitVirtualKeyboard>().Instance;
        var root = component.Find(".bit-vkb");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-vkb-lg"));
        Assert.AreEqual("teclado", root.GetAttribute("aria-roledescription"));
        Assert.IsTrue(component.Find(".bit-vkb-pnl").ClassList.Contains("cascaded-pnl"));
        Assert.AreEqual(300, instance.RepeatDelay);
        Assert.AreEqual("en", instance.ActiveLayout!.Id, "the Full form");
    }

    [TestMethod]
    public void BitVirtualKeyboardShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitVirtualKeyboardParams { Form = BitVirtualKeyboardForm.Full, RepeatDelay = 300 }, builder =>
        {
            builder.AddComponentParameter(1, nameof(BitVirtualKeyboard.Form), BitVirtualKeyboardForm.Compact);
        });

        var instance = component.FindComponent<BitVirtualKeyboard>().Instance;

        Assert.AreEqual("en-compact", instance.ActiveLayout!.Id);
        Assert.AreEqual(300, instance.RepeatDelay, "the cascaded value is taken where the keyboard sets none");
    }
}