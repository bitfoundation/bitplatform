using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AvatarGroup;

/// <summary>
/// The BitParams cascade of the avatar group: what a BitAvatarGroupParams carries (what the avatar groups of an app agree
/// on), that the avatar group takes it, and that what is set on the avatar group itself wins over it.
/// </summary>
[TestClass]
public class BitAvatarGroupParamsTests : BunitTestContext
{
    // What makes one avatar group the one it is: its people and what a click on them does.
    private static readonly string[] _notCascaded =
    [
        nameof(BitAvatarGroup.CascadingParameters),
        nameof(BitAvatarGroup.Items),
        nameof(BitAvatarGroup.OnItemClick),
        nameof(BitAvatarGroup.OnOverflowClick),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitAvatarGroupParams groupParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { groupParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitAvatarGroup>(0);
                builder.AddComponentParameter(1, nameof(BitAvatarGroup.Items), new List<BitAvatarGroupItem>
                {
                    new() { Text = "Annie Lindqvist" },
                    new() { Text = "Aaron Reid" },
                    new() { Text = "Alex Lundberg" },
                });
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitAvatarGroupParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAvatarGroup", BitAvatarGroupParams.ParamName);
        Assert.AreEqual(BitAvatarGroupParams.ParamName, new BitAvatarGroupParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitAvatarGroupParams());
    }

    [TestMethod]
    public void BitAvatarGroupParamsShouldCarryEveryParameterButTheStateOfOneGroup()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitAvatarGroup).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                               .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitAvatarGroupParams).GetProperty(name), $"BitAvatarGroupParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitAvatarGroupParams).GetProperty(name), $"BitAvatarGroupParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitAvatarGroupShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitAvatarGroupParams
        {
            Class = "cascaded-root",
            MaxDisplayed = 1,
            OverflowAriaLabelFormat = "{0} others",
            Size = BitPersonaSize.Size48,
            Spread = true,
            Classes = new() { Item = "cascaded-item" },
            Styles = new() { Overflow = "margin:1px" },
        });

        var root = component.Find(".bit-avg");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-avg-s48"));
        Assert.IsTrue(root.ClassList.Contains("bit-avg-spr"));
        Assert.AreEqual(1, component.FindAll(".bit-avg-itm.cascaded-item").Count);
        Assert.AreEqual("2 others", component.Find(".bit-avg-ovf").GetAttribute("aria-label"));
        StringAssert.Contains(component.Find(".bit-avg-ovf").GetAttribute("style"), "margin:1px");
    }

    [TestMethod]
    public void BitAvatarGroupShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitAvatarGroupParams { MaxDisplayed = 1, Size = BitPersonaSize.Size48 }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitAvatarGroup.MaxDisplayed), 2);
        });

        Assert.AreEqual(2, component.FindAll(".bit-avg-itm").Count);
        Assert.IsTrue(component.Find(".bit-avg").ClassList.Contains("bit-avg-s48"), "the cascaded value is taken where the group sets none");
    }
}
