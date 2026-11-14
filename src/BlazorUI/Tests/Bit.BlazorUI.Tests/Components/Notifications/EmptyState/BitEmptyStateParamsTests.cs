using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.EmptyState;

/// <summary>
/// The BitParams cascade of the empty state: what a BitEmptyStateParams carries (what the empty states of an app agree
/// on), that the empty state takes it, and that what is set on the empty state itself wins over it.
/// </summary>
[TestClass]
public class BitEmptyStateParamsTests : BunitTestContext
{
    // What one empty state says and offers: its words and its actions.
    private static readonly string[] _notCascaded =
    [
        nameof(BitEmptyState.CascadingParameters),
        nameof(BitEmptyState.ChildContent),
        nameof(BitEmptyState.Description),
        nameof(BitEmptyState.DescriptionTemplate),
        nameof(BitEmptyState.Title),
        nameof(BitEmptyState.TitleTemplate),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitEmptyStateParams emptyStateParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { emptyStateParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitEmptyState>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitEmptyStateParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitEmptyState", BitEmptyStateParams.ParamName);
        Assert.AreEqual(BitEmptyStateParams.ParamName, new BitEmptyStateParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitEmptyStateParams());
    }

    [TestMethod]
    public void BitEmptyStateParamsShouldCarryEveryParameterButTheWordsAndActionsOfOneEmptyState()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitEmptyState).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                              .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitEmptyStateParams).GetProperty(name), $"BitEmptyStateParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitEmptyStateParams).GetProperty(name), $"BitEmptyStateParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitEmptyStateShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitEmptyStateParams
        {
            Class = "cascaded-root",
            HeadingLevel = 3,
            IconName = "Inbox",
            Classes = new() { Title = "cascaded-title" },
            Styles = new() { Icon = "color:red" },
        });

        Assert.IsTrue(component.Find(".bit-ems").ClassList.Contains("cascaded-root"));
        Assert.AreEqual("3", component.Find(".bit-ems-ttl").GetAttribute("aria-level"));
        Assert.IsTrue(component.Find(".bit-ems-ttl").ClassList.Contains("cascaded-title"));
        Assert.IsTrue(component.Find(".bit-ems-ico").ClassList.Contains("bit-icon--Inbox"));
        Assert.AreEqual("color:red", component.Find(".bit-ems-ico").GetAttribute("style"));
    }

    [TestMethod]
    public void BitEmptyStateShouldTakeACascadedIconAndIconTemplate()
    {
        var withIcon = RenderWithParams(new BitEmptyStateParams { Icon = BitIconInfo.Fa("solid box-open") });
        Assert.IsTrue(withIcon.Find(".bit-ems-ico").ClassList.Contains("fa-box-open"));

        var withTemplate = RenderWithParams(new BitEmptyStateParams
        {
            IconTemplate = builder => builder.AddMarkupContent(0, "<span class=\"cascaded-art\"></span>"),
        });
        Assert.IsNotNull(withTemplate.Find(".bit-ems-ico > .cascaded-art"));
    }

    [TestMethod]
    public void BitEmptyStateShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitEmptyStateParams { HeadingLevel = 3, IconName = "Inbox" }, builder =>
        {
            builder.AddComponentParameter(1, nameof(BitEmptyState.HeadingLevel), 2);
        });

        Assert.AreEqual("2", component.Find(".bit-ems-ttl").GetAttribute("aria-level"));
        Assert.IsTrue(component.Find(".bit-ems-ico").ClassList.Contains("bit-icon--Inbox"), "the cascaded value is taken where the empty state sets none");
    }
}
