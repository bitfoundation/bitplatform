using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Message;

/// <summary>
/// Covers the BitParams cascade of the Message: what a BitMessageParams fills in, what it leaves alone because
/// the message wrote it for itself, and that what the message derives from its parameters (its classes, its
/// styles, its buttons and its countdown) follows the cascaded values.
/// </summary>
[TestClass]
public class BitMessageParamsTests : BunitTestContext
{
    private IRenderedComponent<BitParams> RenderWithParams(BitMessageParams messageParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { messageParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMessage>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitMessage.ChildContent), (RenderFragment)(b => b.AddContent(0, "Hello")));
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitMessageParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitMessage)}", BitMessageParams.ParamName);
        Assert.AreEqual("BitParams.BitMessage", BitMessageParams.ParamName);
    }

    [TestMethod]
    public void BitMessageParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitMessageParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitMessageParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedClasses()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Color = BitColor.Error,
            Variant = BitVariant.Outline,
            Size = BitSize.Large,
            Square = true,
            Classes = new() { Root = "cascaded-root", Content = "cascaded-content" },
        });

        var root = component.Find(".bit-msg");

        Assert.IsTrue(root.ClassList.Contains("bit-msg-err"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-otl"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-sqr"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(component.Find(".bit-msg-cnt").ClassList.Contains("cascaded-content"));

        // The icon and the urgency follow the cascaded color, as they follow one set on the message itself.
        Assert.IsTrue(component.Find(".bit-msg-ico").ClassList.Contains("bit-icon--ErrorBadge"));
        Assert.AreEqual("alert", component.Find(".bit-msg-cnc").GetAttribute("role"));
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedStyles()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Alignment = BitAlignment.Center,
            Elevation = 4,
            Multiline = true,
            MaxLines = 2,
            Styles = new() { Root = "outline: 1px solid red;" },
        });

        var root = component.Find(".bit-msg");
        var style = root.GetAttribute("style") ?? string.Empty;

        Assert.IsTrue(style.Contains("--bit-msg-justifycontent:center"));
        Assert.IsTrue(style.Contains("--bit-msg-boxshadow:var(--bit-shd-4)"));
        Assert.IsTrue(style.Contains("--bit-msg-maxlines:2"));
        Assert.IsTrue(style.Contains("outline: 1px solid red;"));
        Assert.IsTrue(component.Find(".bit-msg-cnt").ClassList.Contains("bit-msg-clp"));
    }

    [TestMethod]
    public void BitMessageShouldApplyCascadedBehavior()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Truncate = true,
            Dismissible = true,
            HideIcon = true,
            TitleElement = "h4",
            DismissAriaLabel = "Close",
            ExpandAriaLabel = "Show more",
            DismissIconName = "Blocked2Solid",
        }, builder => builder.AddAttribute(1, nameof(BitMessage.Title), "Heads up"));

        Assert.IsEmpty(component.FindAll(".bit-msg-ict"));
        Assert.AreEqual("H4", component.Find(".bit-msg-ttl").TagName);
        Assert.AreEqual("Show more", component.Find(".bit-msg-exb").GetAttribute("aria-label"));

        var dismiss = component.Find(".bit-msg-dmb");

        Assert.AreEqual("Close", dismiss.GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-msg-dmi").ClassList.Contains("bit-icon--Blocked2Solid"));

        // A cascaded Dismissible hands the message its own dismissal, so it takes itself off the page.
        dismiss.Click();

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageShouldArmACascadedCountdown()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Dismissible = true,
            ShowAutoDismissProgress = true,
            AutoDismissTime = TimeSpan.FromMilliseconds(100),
        });

        Assert.HasCount(1, component.FindAll(".bit-msg-prb"));

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < 5000 && component.FindAll(".bit-msg").Count > 0)
        {
            Thread.Sleep(20);
        }

        Assert.IsEmpty(component.FindAll(".bit-msg"));
    }

    [TestMethod]
    public void BitMessageDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitMessageParams
        {
            Color = BitColor.Success,
            Variant = BitVariant.Outline,
            Size = BitSize.Small,
            DismissAriaLabel = "Close",
            Dismissible = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMessage.Color), BitColor.Warning);
            builder.AddAttribute(2, nameof(BitMessage.Variant), BitVariant.Fill);
            builder.AddAttribute(3, nameof(BitMessage.DismissAriaLabel), "Hide");
        });

        var root = component.Find(".bit-msg");

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(root.ClassList.Contains("bit-msg-wrn"));
        Assert.IsFalse(root.ClassList.Contains("bit-msg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-fil"));
        Assert.IsFalse(root.ClassList.Contains("bit-msg-otl"));
        Assert.AreEqual("Hide", component.Find(".bit-msg-dmb").GetAttribute("aria-label"));

        // What the message left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-msg-sm"));
    }

    [TestMethod]
    public void BitMessageShouldKeepItsDefaultsWithoutCascadingParameters()
    {
        var component = RenderComponent<BitMessage>(parameters => parameters.AddChildContent("Hello"));

        var instance = component.Instance;

        Assert.IsNull(instance.CascadingParameters);
        Assert.IsFalse(instance.Dismissible);
        Assert.IsFalse(instance.Truncate);
        Assert.AreEqual("Dismiss", instance.DismissAriaLabel);
    }

    [TestMethod]
    public void BitMessageParamsShouldLeaveUnsetValuesAlone()
    {
        var component = RenderWithParams(new BitMessageParams());

        var instance = component.FindComponent<BitMessage>().Instance;
        var root = component.Find(".bit-msg");

        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Variant);
        Assert.IsNull(instance.Size);
        Assert.IsNull(instance.AutoDismissTime);
        Assert.IsFalse(instance.Dismissible);
        Assert.IsFalse(instance.Multiline);
        Assert.AreEqual("Dismiss", instance.DismissAriaLabel);
        Assert.AreEqual("Expand", instance.ExpandAriaLabel);
        Assert.AreEqual("Collapse", instance.CollapseAriaLabel);
        Assert.IsTrue(root.ClassList.Contains("bit-msg-inf"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-fil"));
        Assert.IsTrue(root.ClassList.Contains("bit-msg-md"));
        Assert.IsEmpty(component.FindAll(".bit-msg-dmb"));
    }
}
