using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NotificationCenter;

/// <summary>
/// The BitParams cascade of the notification center: what a BitNotificationCenterParams carries (everything the centers
/// of an app agree on), that the center takes it, and that what is set on the center itself wins over it.
/// </summary>
[TestClass]
public class BitNotificationCenterParamsTests : BunitTestContext
{
    // What makes one center the one it is: whether it is open, and the values it is cascaded.
    private static readonly string[] _notCascaded =
    [
        nameof(BitNotificationCenter.CascadingParameters),
        nameof(BitNotificationCenter.IsOpen),
        nameof(BitNotificationCenter.IsOpenChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitNotificationCenterParams centerParams, Action<RenderTreeBuilder>? attributes = null)
    {
        Services.AddSingleton(new BitSnackBarService());

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { centerParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitNotificationCenter>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitNotificationCenterParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitNotificationCenter", BitNotificationCenterParams.ParamName);
        Assert.AreEqual(BitNotificationCenterParams.ParamName, new BitNotificationCenterParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitNotificationCenterParams());
    }

    [TestMethod]
    public void BitNotificationCenterParamsShouldCarryEveryParameterButTheOpenState()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitNotificationCenter).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                      .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                      .Select(p => p.Name)
                                                      .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                      .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitNotificationCenterParams).GetProperty(name), $"BitNotificationCenterParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitNotificationCenterParams).GetProperty(name), $"BitNotificationCenterParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitNotificationCenterShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitNotificationCenterParams
        {
            Class = "cascaded-root",
            Classes = new() { Badge = "cascaded-badge" },
            HeaderText = "Inbox",
            EmptyText = "All caught up",
            IconName = "Mail",
        });

        Assert.IsTrue(component.Find(".bit-ntc").ClassList.Contains("cascaded-root"));
        Assert.AreEqual("Inbox", component.Find(".bit-ntc-htx").TextContent.Trim());
        Assert.AreEqual("All caught up", component.Find(".bit-ntc-emp").TextContent.Trim());
        Assert.IsNotNull(component.Find(".bit-ntc-icn.bit-icon--Mail"));
    }

    [TestMethod]
    public void BitNotificationCenterShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitNotificationCenterParams { HeaderText = "Inbox", HistoryLimit = 5 }, builder =>
        {
            builder.AddComponentParameter(1, nameof(BitNotificationCenter.HeaderText), "Mine");
        });

        Assert.AreEqual("Mine", component.Find(".bit-ntc-htx").TextContent.Trim());
        Assert.AreEqual(5, Services.GetRequiredService<BitSnackBarService>().HistoryLimit, "the cascaded limit is taken where the center sets none");
    }
}
