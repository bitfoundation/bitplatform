using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CopyButton;

/// <summary>
/// The BitParams cascade of the copy button: what a BitCopyButtonParams carries (what the copy buttons of an app agree
/// on), that the copy button takes it, and that what is set on the copy button itself wins over it.
/// </summary>
[TestClass]
public class BitCopyButtonParamsTests : BunitTestContext
{
    // What makes one copy button the one it is: the value it copies and what its copy does.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCopyButton.CascadingParameters),
        nameof(BitCopyButton.OnCopy),
        nameof(BitCopyButton.OnCopyFailed),
        nameof(BitCopyButton.Value),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCopyButtonParams copyButtonParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { copyButtonParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCopyButton>(0);
                builder.AddComponentParameter(1, nameof(BitCopyButton.Value), "hello");
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCopyButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitCopyButton", BitCopyButtonParams.ParamName);
        Assert.AreEqual(BitCopyButtonParams.ParamName, new BitCopyButtonParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitCopyButtonParams());
    }

    [TestMethod]
    public void BitCopyButtonParamsShouldCarryEveryParameterButTheStateOfOneCopyButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCopyButton).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                              .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCopyButtonParams).GetProperty(name), $"BitCopyButtonParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitCopyButtonParams).GetProperty(name), $"BitCopyButtonParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitCopyButtonShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitCopyButtonParams
        {
            Class = "cascaded-root",
            Color = BitColor.Error,
            CopiedDuration = TimeSpan.FromSeconds(7),
            CopiedIconName = "CompletedSolid",
            CopiedText = "Done",
            IconName = "Link",
            Size = BitSize.Small,
            Text = "Copy it",
            Variant = BitVariant.Outline,
            Classes = new() { LiveRegion = "cascaded-live" },
            Styles = new() { Root = "margin:1px" },
        });

        var instance = component.FindComponent<BitCopyButton>().Instance;
        var root = component.Find(".bit-cpb");
        var button = component.Find("button");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.AreEqual("margin:1px", root.GetAttribute("style"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-err"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-sm"));
        Assert.IsTrue(button.ClassList.Contains("bit-btn-otl"));
        Assert.IsNotNull(button.QuerySelector("i.bit-icon--Link"));
        Assert.AreEqual("Copy it", component.Find(".bit-btn-prt").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-cpb-liv").ClassList.Contains("cascaded-live"));
        Assert.AreEqual(TimeSpan.FromSeconds(7), instance.CopiedDuration);
        Assert.AreEqual("CompletedSolid", instance.CopiedIconName);
        Assert.AreEqual("Done", instance.CopiedText);
    }

    [TestMethod]
    public void BitCopyButtonShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCopyButtonParams { Text = "Copy it", Variant = BitVariant.Outline }, builder =>
        {
            builder.AddComponentParameter(2, nameof(BitCopyButton.Variant), BitVariant.Text);
        });

        Assert.IsTrue(component.Find("button").ClassList.Contains("bit-btn-txt"));
        Assert.AreEqual("Copy it", component.Find(".bit-btn-prt").TextContent.Trim(), "the cascaded value is taken where the copy button sets none");
    }
}
