using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.ToggleButton;

/// <summary>
/// Covers the BitParams cascade of the ToggleButton: what a BitToggleButtonParams fills in, what it leaves alone
/// because the toggle button wrote it for itself, that the checked state is only seeded through DefaultIsChecked, and
/// that what the toggle button derives from its parameters follows the cascaded values.
/// </summary>
[TestClass]
public class BitToggleButtonParamsTests : BunitTestContext
{
    // What a toggle button is about rather than how it looks - its content, its templates, its callbacks and the
    // checked state the page binds to - is written per toggle button, so it has no place on the params object. The
    // IsLoadingChanged callback is the other half of a binding the page owns.
    private static readonly string[] _notCascaded =
    [
        nameof(BitToggleButton.CascadingParameters),
        nameof(BitToggleButton.ChildContent),
        nameof(BitToggleButton.OnTemplate),
        nameof(BitToggleButton.OffTemplate),
        nameof(BitToggleButton.LoadingTemplate),
        nameof(BitToggleButton.OnChange),
        nameof(BitToggleButton.OnChanging),
        nameof(BitToggleButton.OnClick),
        nameof(BitToggleButton.IsChecked),
        nameof(BitToggleButton.IsCheckedChanged),
        nameof(BitToggleButton.IsLoadingChanged),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitToggleButtonParams toggleParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { toggleParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitToggleButton>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitToggleButton)}", BitToggleButtonParams.ParamName);
        Assert.AreEqual("BitParams.BitToggleButton", BitToggleButtonParams.ParamName);
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitToggleButtonParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitToggleButtonParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldCarryEveryParameterThatIsNotPerToggleButton()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitToggleButton).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                .Select(p => p.Name)
                                                .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitToggleButtonParams).GetProperty(name), $"BitToggleButtonParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitToggleButtonShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitToggleButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Variant = BitVariant.Outline,
            IconName = "Add",
            Title = "Cascaded Title",
            FullWidth = true,
            NoWrap = true,
            FixedColor = true,
            IconPlacement = BitPlacement.End,
            Text = "Cascaded Text",
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-suc"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-lg"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-otl"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-flw"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-nwr"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-fxc"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-eni"));
        Assert.AreEqual("Cascaded Title", bitToggleButton.GetAttribute("title"));
        Assert.AreEqual("Cascaded Text", component.Find(".bit-tgb-btx").TextContent);

        var icon = component.Find(".bit-tgb-ico");
        Assert.IsTrue(icon.ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitToggleButtonDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitToggleButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            IconName = "Add",
            Title = "Cascaded Title"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitToggleButton.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(2, nameof(BitToggleButton.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitToggleButton.Title), "Direct Title");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-err"));
        Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-tgb-suc"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-sm"));
        Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-tgb-lg"));
        Assert.AreEqual("Direct Title", bitToggleButton.GetAttribute("title"));

        // the icon that was not written by hand still comes from the cascading parameters
        Assert.IsTrue(component.Find(".bit-tgb-ico").ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitToggleButtonParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitToggleButtonClassStyles { Root = "cascaded-root" };
        var styles = new BitToggleButtonClassStyles { Root = "color: red;" };
        var icon = BitIconInfo.Fa("solid share");
        var onIcon = BitIconInfo.Fa("solid microphone-slash");
        var offIcon = BitIconInfo.Fa("solid microphone");
        var checkMarkIcon = BitIconInfo.Fa("solid check");

        var @params = new BitToggleButtonParams
        {
            AllowDisabledFocus = false,
            AriaControls = "the-panel",
            AriaDescription = "Test description",
            AriaHidden = true,
            AriaLabelledBy = "the-label",
            AriaMode = BitToggleButtonAriaMode.Switch,
            AutoFocus = true,
            AutoLoading = true,
            CheckMarkIcon = checkMarkIcon,
            CheckMarkIconName = "CheckMark",
            Classes = classes,
            Color = BitColor.Warning,
            DefaultIsChecked = true,
            FixedCheckMark = true,
            FixedColor = true,
            FullWidth = true,
            Icon = icon,
            IconName = "Share",
            IconOnly = true,
            IconPlacement = BitPlacement.End,
            IsLoading = true,
            LoadingDelay = 300,
            LoadingLabel = "Saving...",
            LoadingLabelPlacement = BitPlacement.Top,
            NoWrap = true,
            OffAriaLabel = "Off label",
            OffColor = BitColor.Info,
            OffIcon = offIcon,
            OffIconName = "Microphone",
            OffText = "Unmuted",
            OffTitle = "Click to mute",
            OffVariant = BitVariant.Text,
            OnAriaLabel = "On label",
            OnColor = BitColor.Error,
            OnIcon = onIcon,
            OnIconName = "MicOff",
            OnText = "Muted",
            OnTitle = "Click to unmute",
            OnVariant = BitVariant.Outline,
            Reclickable = true,
            ShowCheckMark = true,
            Size = BitSize.Small,
            StopPropagation = true,
            Styles = styles,
            Text = "Test Text",
            Title = "Test Title",
            Variant = BitVariant.Outline,
            AriaLabel = "Test Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitToggleButton>().Instance;

        Assert.IsFalse(instance.AllowDisabledFocus);
        Assert.AreEqual("the-panel", instance.AriaControls);
        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.IsTrue(instance.AriaHidden);
        Assert.AreEqual("the-label", instance.AriaLabelledBy);
        Assert.AreEqual(BitToggleButtonAriaMode.Switch, instance.AriaMode);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreSame(checkMarkIcon, instance.CheckMarkIcon);
        Assert.AreEqual("CheckMark", instance.CheckMarkIconName);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.IsTrue(instance.DefaultIsChecked);
        Assert.IsTrue(instance.FixedCheckMark);
        Assert.IsTrue(instance.FixedColor);
        Assert.IsTrue(instance.FullWidth);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("Share", instance.IconName);
        Assert.IsTrue(instance.IconOnly);
        Assert.AreEqual(BitPlacement.End, instance.IconPlacement);
        Assert.IsTrue(instance.IsLoading);
        Assert.AreEqual(300, instance.LoadingDelay);
        Assert.AreEqual("Saving...", instance.LoadingLabel);
        Assert.AreEqual(BitPlacement.Top, instance.LoadingLabelPlacement);
        Assert.IsTrue(instance.NoWrap);
        Assert.AreEqual("Off label", instance.OffAriaLabel);
        Assert.AreEqual(BitColor.Info, instance.OffColor);
        Assert.AreSame(offIcon, instance.OffIcon);
        Assert.AreEqual("Microphone", instance.OffIconName);
        Assert.AreEqual("Unmuted", instance.OffText);
        Assert.AreEqual("Click to mute", instance.OffTitle);
        Assert.AreEqual(BitVariant.Text, instance.OffVariant);
        Assert.AreEqual("On label", instance.OnAriaLabel);
        Assert.AreEqual(BitColor.Error, instance.OnColor);
        Assert.AreSame(onIcon, instance.OnIcon);
        Assert.AreEqual("MicOff", instance.OnIconName);
        Assert.AreEqual("Muted", instance.OnText);
        Assert.AreEqual("Click to unmute", instance.OnTitle);
        Assert.AreEqual(BitVariant.Outline, instance.OnVariant);
        Assert.IsTrue(instance.Reclickable);
        Assert.IsTrue(instance.ShowCheckMark);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StopPropagation);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual("Test Text", instance.Text);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.AreEqual(BitVariant.Outline, instance.Variant);
        Assert.AreEqual("Test Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitToggleButtonParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitToggleButtonParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Params Title"
        };

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Title, "Existing Title");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.Title);
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldSeedTheCheckedStateThroughDefaultIsChecked()
    {
        // The checked state itself is left out of the params class on purpose - a toggle button writes to it on
        // every click, so a cascaded value would be written back over the click on the next render.
        var component = RenderWithParams(new BitToggleButtonParams { DefaultIsChecked = true });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-chk"));
        Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-pressed"));

        bitToggleButton.Click();

        Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-tgb-chk"));
        Assert.AreEqual("false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldApplyThePerStateValuesOfTheCurrentState()
    {
        var component = RenderWithParams(new BitToggleButtonParams
        {
            DefaultIsChecked = true,
            OnText = "Muted",
            OffText = "Unmuted",
            OnColor = BitColor.Error,
            OffColor = BitColor.Info,
            OnVariant = BitVariant.Outline,
            OffVariant = BitVariant.Text,
            OnIconName = "MicOff",
            OffIconName = "Microphone",
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("Muted", component.Find(".bit-tgb-btx").TextContent);
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-err"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-otl"));
        Assert.IsTrue(component.Find(".bit-tgb-ico").ClassList.Contains("bit-icon--MicOff"));

        bitToggleButton.Click();

        Assert.AreEqual("Unmuted", component.Find(".bit-tgb-btx").TextContent);
        Assert.IsTrue(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-inf"));
        Assert.IsTrue(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-txt"));
        Assert.IsTrue(component.Find(".bit-tgb-ico").ClassList.Contains("bit-icon--Microphone"));
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldApplyClassesAndStyles()
    {
        var classes = new BitToggleButtonClassStyles { Root = "custom-root", Icon = "custom-icon", Text = "custom-text" };
        var styles = new BitToggleButtonClassStyles { Root = "color: red;", Icon = "margin: 5px;", Text = "padding: 10px;" };

        var component = RenderWithParams(new BitToggleButtonParams
        {
            Classes = classes,
            Styles = styles,
            IconName = "Add",
            Text = "Content"
        });

        var bitToggleButton = component.Find(".bit-tgb");
        var icon = component.Find(".bit-tgb-ico");
        var text = component.Find(".bit-tgb-btx");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("custom-root"));
        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.IsTrue(text.ClassList.Contains("custom-text"));
        StringAssert.Contains(bitToggleButton.GetAttribute("style"), "color: red;");
        Assert.AreEqual("margin: 5px;", icon.GetAttribute("style"));
        Assert.AreEqual("padding: 10px;", text.GetAttribute("style"));
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldNotApplyWhenEmpty()
    {
        var component = RenderWithParams(new BitToggleButtonParams(), builder =>
        {
            builder.AddAttribute(1, nameof(BitToggleButton.Color), (BitColor?)BitColor.Primary);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-pri"));
        CollectionAssert.AreEquivalent(new[] { "bit-tgb", "bit-tgb-ntx", "bit-tgb-pri", "bit-tgb-fil" }, bitToggleButton.ClassList.ToArray());
        Assert.IsFalse(bitToggleButton.HasAttribute("style"));
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldApplyBaseParameters()
    {
        var component = RenderWithParams(new BitToggleButtonParams
        {
            AriaLabel = "Base Label",
            Id = "test-id",
            Disabled = true,
            TabIndex = "3",
            Style = "background: blue;",
            Class = "base-class"
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("Base Label", bitToggleButton.GetAttribute("aria-label"));
        Assert.AreEqual("test-id", bitToggleButton.GetAttribute("id"));
        // The disabled toggle button keeps its place in the tab order by default and says so with aria-disabled.
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-dis"));
        Assert.IsFalse(bitToggleButton.HasAttribute("disabled"));
        Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-disabled"));
        Assert.AreEqual("3", bitToggleButton.GetAttribute("tabindex"));
        StringAssert.Contains(bitToggleButton.GetAttribute("style"), "background: blue;");
        Assert.IsTrue(bitToggleButton.ClassList.Contains("base-class"));
    }

    [TestMethod]
    public void BitToggleButtonParamsShouldApplyTheCascadedDirectionVisibilityAndAttributes()
    {
        var component = RenderWithParams(new BitToggleButtonParams
        {
            Dir = BitDir.Rtl,
            Visibility = BitVisibility.Collapsed,
            ForceAnimation = true,
            HtmlAttributes = new() { ["data-cascaded"] = "yes" },
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("rtl", bitToggleButton.GetAttribute("dir"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-fam"));
        StringAssert.Contains(bitToggleButton.GetAttribute("style"), "display:none");
        Assert.AreEqual("yes", bitToggleButton.GetAttribute("data-cascaded"));
    }
}
