using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ContextMenu;

/// <summary>
/// The BitParams cascade of the context menu: what a BitContextMenuParams carries (what the context menus of an app agree
/// on), that the context menu takes it, and that what is set on the context menu itself wins over it.
/// </summary>
[TestClass]
public class BitContextMenuParamsTests : BunitTestContext
{
    // What makes one context menu the one it is: its area, its items and whether it is open - which are also typed by
    // its item type, which the params are not.
    private static readonly string[] _notCascaded =
    [
        nameof(BitContextMenu<BitMenuButtonItem>.CascadingParameters),
        nameof(BitContextMenu<BitMenuButtonItem>.ChildContent),
        nameof(BitContextMenu<BitMenuButtonItem>.IsOpen),
        nameof(BitContextMenu<BitMenuButtonItem>.IsOpenChanged),
        nameof(BitContextMenu<BitMenuButtonItem>.Items),
        nameof(BitContextMenu<BitMenuButtonItem>.ItemTemplate),
        nameof(BitContextMenu<BitMenuButtonItem>.NameSelectors),
        nameof(BitContextMenu<BitMenuButtonItem>.OnItemClick),
        nameof(BitContextMenu<BitMenuButtonItem>.Options),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitContextMenuParams menuParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { menuParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitContextMenu<BitMenuButtonItem>>(0);
                builder.AddComponentParameter(1, nameof(BitContextMenu<BitMenuButtonItem>.Items), new List<BitMenuButtonItem> { new() { Text = "Cut" } });
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitContextMenuParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitContextMenu", BitContextMenuParams.ParamName);
        Assert.AreEqual(BitContextMenuParams.ParamName, new BitContextMenuParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitContextMenuParams());
    }

    [TestMethod]
    public void BitContextMenuParamsShouldCarryEveryParameterButTheStateOfOneMenu()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitContextMenu<BitMenuButtonItem>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                  .Select(p => p.Name)
                                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                                  .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitContextMenuParams).GetProperty(name), $"BitContextMenuParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitContextMenuParams).GetProperty(name), $"BitContextMenuParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitContextMenuShouldTakeTheCascadedValues()
    {
        var classes = new BitMenuButtonClassStyles { Callout = "cascaded-callout" };
        var styles = new BitMenuButtonClassStyles { Callout = "color: red;" };

        var component = RenderWithParams(new BitContextMenuParams
        {
            Class = "cascaded-root",
            AriaLabel = "Cascaded menu",
            CloseOnItemClick = false,
            MenuClasses = classes,
            MenuStyles = styles,
        });

        var instance = component.FindComponent<BitContextMenu<BitMenuButtonItem>>().Instance;

        Assert.IsTrue(component.Find(".bit-ctm").ClassList.Contains("cascaded-root"));
        Assert.AreEqual("Cascaded menu", instance.AriaLabel);
        Assert.IsFalse(instance.CloseOnItemClick);
        Assert.AreSame(classes, instance.MenuClasses);
        Assert.AreSame(styles, instance.MenuStyles);

        // And they reach the menu.
        Assert.IsTrue(component.Find(".bit-mnb-cal").ClassList.Contains("cascaded-callout"));
        Assert.AreEqual("Cascaded menu", component.Find(".bit-mnb-opb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitContextMenuShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitContextMenuParams { AriaLabel = "Cascaded menu", CloseOnItemClick = false }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitContextMenu<BitMenuButtonItem>.AriaLabel), "Mine");
        });

        var instance = component.FindComponent<BitContextMenu<BitMenuButtonItem>>().Instance;

        Assert.AreEqual("Mine", instance.AriaLabel);
        Assert.IsFalse(instance.CloseOnItemClick, "the cascaded value is taken where the context menu sets none");
    }
}
