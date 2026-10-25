using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

/// <summary>
/// Covers the BitParams cascade of the Toggle: every value a BitToggleParams carries reaches the toggle and its
/// markup, what the toggle wrote for itself is left alone, and an empty params object changes nothing. The shape of
/// the params object itself (its name, the parameters it has to carry) is pinned beside the toggle's own tests.
/// </summary>
[TestClass]
public class BitToggleParamsTests : BunitTestContext
{
    private IRenderedComponent<BitParams> RenderWithParams(BitToggleParams toggleParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { toggleParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitToggle>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitToggleParamsShouldApplyEveryValueItCarries()
    {
        var component = RenderWithParams(new BitToggleParams
        {
            AllowDisabledFocus = true,
            AriaControls = "the-panel",
            AriaDescription = "Cascaded aria description",
            AriaDescribedby = "an-external-hint",
            AriaLabelledby = "an-external-heading",
            AutoFocus = true,
            AutoLoading = true,
            Color = BitColor.Warning,
            Description = "Cascaded description",
            ErrorMessage = "Cascaded error",
            FullWidth = true,
            Inline = true,
            Invalid = true,
            Label = "Cascaded label",
            LabelPlacement = BitPlacement.Start,
            Loading = true,
            OffIcon = BitIconInfo.Css("off-glyph"),
            OffIconName = "Cancel",
            OffText = "Cascaded off",
            OnIcon = BitIconInfo.Css("on-glyph"),
            OnIconName = "Accept",
            OnText = "Cascaded on",
            Reversed = true,
            Role = "checkbox",
            Size = BitSize.Small,
            StopPropagation = true,
            Text = "Cascaded text",
            Title = "Cascaded title",
            AriaLabel = "Cascaded aria label",
            TabIndex = "4",
        });

        var instance = component.FindComponent<BitToggle>().Instance;
        var root = component.Find(".bit-tgl");
        var button = component.Find("button");

        Assert.IsTrue(instance.AllowDisabledFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreEqual(BitIconInfo.Css("off-glyph").GetCssClasses(), instance.OffIcon?.GetCssClasses());
        Assert.AreEqual(BitIconInfo.Css("on-glyph").GetCssClasses(), instance.OnIcon?.GetCssClasses());
        Assert.AreEqual("Cancel", instance.OffIconName);
        Assert.AreEqual("Accept", instance.OnIconName);
        Assert.AreEqual("Cascaded on", instance.OnText);
        Assert.AreEqual("Cascaded text", instance.Text);

        // ... and the ones the markup shows are checked where they land.
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-inl"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-rvs"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-lst"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-ldg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-tic"));
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));

        Assert.AreEqual("Cascaded label", component.Find(".bit-tgl-lbl").TextContent.Trim());
        Assert.AreEqual("Cascaded off", component.Find(".bit-tgl-stx").TextContent.Trim());
        Assert.AreEqual("Cascaded description", component.Find(".bit-tgl-des").TextContent.Trim());
        Assert.AreEqual("Cascaded error", component.Find(".bit-tgl-erm").TextContent.Trim());
        Assert.AreEqual("Cascaded aria description", component.Find(".bit-tgl-dsc:not([role])").TextContent.Trim());

        Assert.AreEqual("checkbox", button.GetAttribute("role"));
        Assert.AreEqual("the-panel", button.GetAttribute("aria-controls"));
        Assert.AreEqual("an-external-heading", button.GetAttribute("aria-labelledby"));
        StringAssert.Contains(button.GetAttribute("aria-describedby"), "an-external-hint");
        Assert.AreEqual("Cascaded aria label", button.GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded title", button.GetAttribute("title"));
        Assert.AreEqual("4", button.GetAttribute("tabindex"));
        Assert.IsTrue(button.HasAttribute("autofocus"));
        Assert.AreEqual("true", button.GetAttribute("aria-busy"));
        Assert.AreEqual(1, component.FindAll(".bit-tgl-spn").Count);
    }

    [TestMethod]
    public void BitToggleParamsShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitToggleParams
        {
            Label = "Styled",
            Text = "A state",
            Classes = new() { Root = "cascaded-root", Label = "cascaded-label", Button = "cascaded-button", Text = "cascaded-text" },
            Styles = new() { Root = "color: red;", Label = "color: green;", Button = "color: blue;", Text = "color: gray;" },
        });

        var root = component.Find(".bit-tgl");

        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-tgl-lbl").ClassList.Contains("cascaded-label"));
        Assert.AreEqual("color: green;", component.Find(".bit-tgl-lbl").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-tgl-btn").ClassList.Contains("cascaded-button"));
        Assert.AreEqual("color: blue;", component.Find(".bit-tgl-btn").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-tgl-stx").ClassList.Contains("cascaded-text"));
        Assert.AreEqual("color: gray;", component.Find(".bit-tgl-stx").GetAttribute("style"));
    }

    [TestMethod]
    public void BitToggleParamsShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitToggleParams { Dir = BitDir.Rtl, Disabled = true, Class = "cascaded", Style = "margin: 1px;" });

        var root = component.Find(".bit-tgl");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        Assert.IsTrue(component.Find("button").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitToggleShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitToggleParams
        {
            Label = "Cascaded label",
            Role = "checkbox",
            LabelPlacement = BitPlacement.Start,
            Description = "Cascaded description",
            Loading = true,
            Invalid = true,
            Title = "Cascaded title",
            Reversed = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitToggle.Label), "Own label");
            builder.AddAttribute(2, nameof(BitToggle.Role), "switch");
            builder.AddAttribute(3, nameof(BitToggle.LabelPlacement), (BitPlacement?)BitPlacement.End);
            builder.AddAttribute(4, nameof(BitToggle.Description), "Own description");
            builder.AddAttribute(5, nameof(BitToggle.Loading), false);
            builder.AddAttribute(6, nameof(BitToggle.Invalid), false);
        });

        var root = component.Find(".bit-tgl");
        var button = component.Find("button");

        // What the toggle wrote for itself - a false included - is a choice the cascade does not override.
        Assert.AreEqual("Own label", component.Find(".bit-tgl-lbl").TextContent.Trim());
        Assert.AreEqual("switch", button.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-lnd"));
        Assert.IsFalse(root.ClassList.Contains("bit-tgl-lst"));
        Assert.AreEqual("Own description", component.Find(".bit-tgl-des").TextContent.Trim());
        Assert.IsFalse(root.ClassList.Contains("bit-tgl-ldg"));
        Assert.IsFalse(root.ClassList.Contains("bit-inv"));

        // What it left unset is still filled in from the cascade.
        Assert.AreEqual("Cascaded title", button.GetAttribute("title"));
        Assert.IsTrue(root.ClassList.Contains("bit-tgl-rvs"));
    }

    [TestMethod]
    public void BitToggleParamsUpdateParametersShouldNotOverwriteWhatTheToggleSetItself()
    {
        var @params = new BitToggleParams
        {
            Label = "Cascaded label",
            Color = BitColor.Success,
            Size = BitSize.Large,
            Role = "checkbox",
            Text = "Cascaded text",
            StopPropagation = true,
        };

        var component = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Label, "Own label");
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Role, "switch");
            parameters.Add(p => p.Text, "Own text");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual("Own label", instance.Label);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("switch", instance.Role);
        Assert.AreEqual("Own text", instance.Text);

        // Only what the toggle did not set itself is taken.
        Assert.IsTrue(instance.StopPropagation);
    }

    [TestMethod]
    public void BitToggleParamsUpdateParametersShouldIgnoreANullToggle()
    {
        var @params = new BitToggleParams { Label = "Cascaded label" };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitToggleShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitToggleParams());

        var root = component.Find(".bit-tgl");
        var button = component.Find("button");

        Assert.AreEqual("bit-tgl", root.GetAttribute("class"));
        Assert.IsNull(root.GetAttribute("style"));
        Assert.AreEqual("switch", button.GetAttribute("role"));
        Assert.IsFalse(button.HasAttribute("title"));
        Assert.AreEqual(0, component.FindAll(".bit-tgl-lbl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tgl-stx").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tgl-des").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tgl-erm").Count);
    }
}
