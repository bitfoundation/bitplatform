using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Stepper;

/// <summary>
/// The BitParams cascade of the stepper: what a BitStepperParams carries (what the steppers of an app agree on), that the
/// stepper takes it, and that what is set on the stepper itself wins over it.
/// </summary>
[TestClass]
public class BitStepperParamsTests : BunitTestContext
{
    // What makes one stepper the one it is: its steps, where it is, and what it tells the page - which are also typed by
    // its item type, which the params are not.
    private static readonly string[] _notCascaded =
    [
        nameof(BitStepper<BitStepperItem>.CascadingParameters),
        nameof(BitStepper<BitStepperItem>.ChildContent),
        nameof(BitStepper<BitStepperItem>.CurrentIndex),
        nameof(BitStepper<BitStepperItem>.CurrentIndexChanged),
        nameof(BitStepper<BitStepperItem>.Items),
        nameof(BitStepper<BitStepperItem>.NameSelectors),
        nameof(BitStepper<BitStepperItem>.OnChange),
        nameof(BitStepper<BitStepperItem>.OnItemClick),
        nameof(BitStepper<BitStepperItem>.Options),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitStepperParams stepperParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { stepperParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitStepper<BitStepperItem>>(0);
                builder.AddComponentParameter(1, nameof(BitStepper<BitStepperItem>.Items), new List<BitStepperItem>
                {
                    new() { Text = "One" },
                    new() { Text = "Two", IsError = true },
                    new() { Text = "Three" }
                });
                builder.AddComponentParameter(2, nameof(BitStepper<BitStepperItem>.CurrentIndex), 2);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitStepperParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitStepper", BitStepperParams.ParamName);
        Assert.AreEqual(BitStepperParams.ParamName, new BitStepperParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitStepperParams());
    }

    [TestMethod]
    public void BitStepperParamsShouldCarryEveryParameterButTheStateOfOneStepper()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitStepper<BitStepperItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                           .Select(p => p.Name)
                                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                           .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitStepperParams).GetProperty(name), $"BitStepperParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitStepperParams).GetProperty(name), $"BitStepperParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitStepperShouldTakeTheCascadedValues()
    {
        var classes = new BitStepperClassStyles { Step = "cascaded-step" };
        var styles = new BitStepperClassStyles { Root = "color:red" };

        var component = RenderWithParams(new BitStepperParams
        {
            Class = "cascaded-root",
            Classes = classes,
            Styles = styles,
            CompletedIconName = "Accept",
            ErrorIcon = BitIconInfo.Fa("solid xmark"),
            Linear = true,
            ReadOnly = true,
            Vertical = true,
        });

        var instance = component.FindComponent<BitStepper<BitStepperItem>>().Instance;
        var root = component.Find(".bit-stp");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-stp-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-stp-rdo"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");
        Assert.IsTrue(component.FindAll("li").All(li => li.ClassList.Contains("cascaded-step")));
        Assert.IsTrue(instance.Linear);

        var icons = component.FindAll(".bit-stp-ico");
        Assert.IsTrue(icons[0].ClassList.Contains("bit-icon--Accept"));
        Assert.IsTrue(icons[1].ClassList.Contains("fa-xmark"));
    }

    [TestMethod]
    public void BitStepperParamsShouldCascadeTheCompletedIconAndTheErrorIconName()
    {
        var component = RenderWithParams(new BitStepperParams
        {
            CompletedIcon = BitIconInfo.Fa("solid check"),
            ErrorIconName = "Warning",
        });

        var icons = component.FindAll(".bit-stp-ico");
        Assert.IsTrue(icons[0].ClassList.Contains("fa-check"));
        Assert.IsTrue(icons[1].ClassList.Contains("bit-icon--Warning"));
    }

    [TestMethod]
    public void BitStepperShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitStepperParams { Vertical = true, CompletedIconName = "Accept", ReadOnly = true }, builder =>
        {
            builder.AddComponentParameter(3, nameof(BitStepper<BitStepperItem>.Vertical), false);
            builder.AddComponentParameter(4, nameof(BitStepper<BitStepperItem>.CompletedIconName), "Mine");
        });

        var root = component.Find(".bit-stp");

        Assert.IsTrue(root.ClassList.Contains("bit-stp-hrz"));
        Assert.IsTrue(component.Find(".bit-stp-ico").ClassList.Contains("bit-icon--Mine"));
        Assert.IsTrue(root.ClassList.Contains("bit-stp-rdo"), "the cascaded value is taken where the stepper sets none");
    }
}
