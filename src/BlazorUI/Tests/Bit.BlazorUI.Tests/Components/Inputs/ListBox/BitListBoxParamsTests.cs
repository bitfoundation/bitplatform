using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ListBox;

/// <summary>
/// The BitParams cascade of the list box: what a BitListBoxParams carries (what the list boxes of an app agree on), that
/// the list box takes it, and that what is set on the list box itself wins over it.
/// </summary>
[TestClass]
public class BitListBoxParamsTests : BunitTestContext
{
    // What makes one list box the one it is: its items, its values and its label, and what it tells the page - which
    // are also typed by its item and value types, which the params are not.
    private static readonly string[] _notCascaded =
    [
        nameof(BitListBox<BitListBoxItem<string>, string>.CascadingParameters),
        nameof(BitListBox<BitListBoxItem<string>, string>.ChildContent),
        nameof(BitListBox<BitListBoxItem<string>, string>.NameSelectors),
        nameof(BitListBox<BitListBoxItem<string>, string>.Options),
        nameof(BitListBox<BitListBoxItem<string>, string>.DefaultValues),
        nameof(BitListBox<BitListBoxItem<string>, string>.ErrorMessage),
        nameof(BitListBox<BitListBoxItem<string>, string>.Invalid),
        nameof(BitListBox<BitListBoxItem<string>, string>.Items),
        nameof(BitListBox<BitListBoxItem<string>, string>.Label),
        nameof(BitListBox<BitListBoxItem<string>, string>.MultiSelect),
        nameof(BitListBox<BitListBoxItem<string>, string>.OnDeselectItem),
        nameof(BitListBox<BitListBoxItem<string>, string>.OnSelectItem),
        nameof(BitListBox<BitListBoxItem<string>, string>.Values),
        nameof(BitListBox<BitListBoxItem<string>, string>.ValuesChanged),
        nameof(BitListBox<BitListBoxItem<string>, string>.ValuesExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitListBoxParams listBoxParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { listBoxParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitListBox<BitListBoxItem<string>, string>>(0);
                builder.AddComponentParameter(1, nameof(BitListBox<BitListBoxItem<string>, string>.Items), new List<BitListBoxItem<string>>
                {
                    new() { Text = "One", Value = "1" },
                    new() { Text = "Two", Value = "2" }
                });
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitListBoxParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitListBox", BitListBoxParams.ParamName);
        Assert.AreEqual(BitListBoxParams.ParamName, new BitListBoxParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitListBoxParams());
    }

    [TestMethod]
    public void BitListBoxParamsShouldCarryEveryParameterButTheStateOfOneListBox()
    {
        // The parameters every input has (Value, ReadOnly, Required, ...) are not the list box's own.
        var baseParameters = typeof(BitInputBase<string>).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitListBox<BitListBoxItem<string>, string>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                           .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                           .Select(p => p.Name)
                                                                           .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                                           .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitListBoxParams).GetProperty(name), $"BitListBoxParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitListBoxParams).GetProperty(name), $"BitListBoxParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitListBoxShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitListBoxParams
        {
            Class = "cascaded-root",
            Classes = new() { List = "cascaded-list" },
            Styles = new() { Item = "padding:1px" },
            MaxHeight = "5rem",
            ShowValidationMessage = true,
        });

        var instance = component.FindComponent<BitListBox<BitListBoxItem<string>, string>>().Instance;

        Assert.IsTrue(component.Find(".bit-lbx").ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-lbx-lst").ClassList.Contains("cascaded-list"));
        StringAssert.Contains(component.Find(".bit-lbx-lst").GetAttribute("style"), "max-height:5rem");
        Assert.AreEqual("padding:1px", component.Find("li").GetAttribute("style"));
        Assert.IsTrue(instance.ShowValidationMessage);
    }

    [TestMethod]
    public void BitListBoxShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitListBoxParams { MaxHeight = "5rem", ShowValidationMessage = true }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitListBox<BitListBoxItem<string>, string>.MaxHeight), "8rem");
        });

        var instance = component.FindComponent<BitListBox<BitListBoxItem<string>, string>>().Instance;

        Assert.AreEqual("8rem", instance.MaxHeight);
        Assert.IsTrue(instance.ShowValidationMessage, "the cascaded value is taken where the list box sets none");
    }
}
