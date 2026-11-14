using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PopConfirm;

/// <summary>
/// The BitParams cascade of the pop confirm: what a BitPopConfirmParams carries (what the pop confirms of an app agree
/// on), that the pop confirm takes it, and that what is set on the pop confirm itself wins over it.
/// </summary>
[TestClass]
public class BitPopConfirmParamsTests : BunitTestContext
{
    // What makes one pop confirm the one it is: its trigger, its question, whether it is open and what its answers do.
    private static readonly string[] _notCascaded =
    [
        nameof(BitPopConfirm.CascadingParameters),
        nameof(BitPopConfirm.ChildContent),
        nameof(BitPopConfirm.IsOpen),
        nameof(BitPopConfirm.IsOpenChanged),
        nameof(BitPopConfirm.Message),
        nameof(BitPopConfirm.OnCancel),
        nameof(BitPopConfirm.OnConfirm),
        nameof(BitPopConfirm.Title),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitPopConfirmParams popConfirmParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { popConfirmParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPopConfirm>(0);
                builder.AddComponentParameter(1, nameof(BitPopConfirm.Title), "Delete the file?");
                builder.AddComponentParameter(2, nameof(BitPopConfirm.Message), "It cannot be brought back.");
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitPopConfirmParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitPopConfirm", BitPopConfirmParams.ParamName);
        Assert.AreEqual(BitPopConfirmParams.ParamName, new BitPopConfirmParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitPopConfirmParams());
    }

    [TestMethod]
    public void BitPopConfirmParamsShouldCarryEveryParameterButTheStateOfOnePopConfirm()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitPopConfirm).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                              .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitPopConfirmParams).GetProperty(name), $"BitPopConfirmParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitPopConfirmParams).GetProperty(name), $"BitPopConfirmParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitPopConfirmShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitPopConfirmParams
        {
            Class = "cascaded-root",
            ConfirmText = "Delete",
            CancelText = "Keep",
            ConfirmColor = BitColor.Error,
            Placement = BitPlacement.Bottom,
            Classes = new() { Title = "cascaded-title" },
            Styles = new() { Message = "margin:1px" },
        });

        var instance = component.FindComponent<BitPopConfirm>().Instance;

        Assert.IsTrue(component.Find(".bit-pcf").ClassList.Contains("cascaded-root"));
        Assert.AreEqual("Delete", component.Find(".bit-pcf-obt").TextContent.Trim());
        Assert.AreEqual("Keep", component.Find(".bit-pcf-cbt").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-pcf-obt").ClassList.Contains("bit-btn-err"));
        Assert.AreEqual(BitPlacement.Bottom, instance.Placement);
        Assert.AreEqual(BitPlacement.Bottom, component.FindComponent<BitCallout>().Instance.Placement);
        Assert.IsTrue(component.Find(".bit-pcf-ttl").ClassList.Contains("cascaded-title"));
        Assert.AreEqual("margin:1px", component.Find(".bit-pcf-msg").GetAttribute("style"));
    }

    [TestMethod]
    public void BitPopConfirmShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitPopConfirmParams { ConfirmText = "Delete", CancelText = "Keep" }, builder =>
        {
            builder.AddComponentParameter(3, nameof(BitPopConfirm.ConfirmText), "Remove");
        });

        Assert.AreEqual("Remove", component.Find(".bit-pcf-obt").TextContent.Trim());
        Assert.AreEqual("Keep", component.Find(".bit-pcf-cbt").TextContent.Trim(), "the cascaded value is taken where the pop confirm sets none");
    }
}
