using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Checkbox;

/// <summary>
/// Covers the BitParams cascade of the Checkbox: what a BitCheckboxParams fills in, what it leaves alone because the
/// checkbox wrote it for itself, and that what the checkbox derives from its parameters (its classes, its styles, its
/// icons and the mixed state it starts in) follows the cascaded values.
/// </summary>
[TestClass]
public class BitCheckboxParamsTests : BunitTestContext
{
    // What a checkbox is about rather than how it looks - its content, its templates and the callbacks it answers
    // with - is written per checkbox, so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCheckbox.CascadingParameters),
        nameof(BitCheckbox.ChildContent),
        nameof(BitCheckbox.DescriptionTemplate),
        nameof(BitCheckbox.LabelTemplate),
        nameof(BitCheckbox.OnBlur),
        nameof(BitCheckbox.OnChanging),
        nameof(BitCheckbox.OnClick),
        nameof(BitCheckbox.OnFocus),
        nameof(BitCheckbox.OnFocusIn),
        nameof(BitCheckbox.OnFocusOut),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCheckboxParams checkboxParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { checkboxParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCheckbox>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCheckboxParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitCheckbox)}", BitCheckboxParams.ParamName);
        Assert.AreEqual("BitParams.BitCheckbox", BitCheckboxParams.ParamName);
    }

    [TestMethod]
    public void BitCheckboxParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCheckboxParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCheckboxParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCheckboxParamsShouldCarryEveryParameterThatIsNotPerCheckbox()
    {
        // The cascading parameters are the shared configuration of every checkbox of an application, so a parameter
        // that is added to the component and forgotten here is one that cannot be set once for the whole of it.
        // What the input base declares (the value, its binding and validation) is per instance, and so is every
        // callback, the ones the source generator writes beside a two-way bound parameter included.
        var baseParameters = typeof(BitCheckbox).BaseType!.GetProperties().Select(p => p.Name)
                                 .Concat(typeof(BitCheckbox).GetProperties()
                                                       .Where(p => p.PropertyType == typeof(EventCallback) ||
                                                                   (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>)))
                                                       .Select(p => p.Name))
                                 .ToHashSet();

        var missing = typeof(BitCheckbox).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                         .Where(n => typeof(BitCheckboxParams).GetProperty(n) is null)
                                         .ToArray();

        Assert.AreEqual(0, missing.Length, $"{nameof(BitCheckboxParams)} is missing: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void BitCheckboxShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitCheckboxParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Label = "Cascaded Label",
            Title = "Cascaded Title",
            CheckIconName = "Add",
            FullWidth = true,
            Reversed = true,
            NoWrap = true,
            LabelPlacement = BitPlacement.Top,
        });

        var checkbox = component.Find(".bit-chb");

        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-suc"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-lg"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-fwi"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-rvs"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-nwr"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-ltp"));
        Assert.AreEqual("Cascaded Label", component.Find(".bit-chb-txt").TextContent.Trim());
        Assert.AreEqual("Cascaded Title", component.Find(".bit-chb-lbl").GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-chb-ico").ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitCheckboxDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitCheckboxParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Cascaded Title",
            CheckIconName = "Add"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCheckbox.Color), BitColor.Error);
            builder.AddAttribute(2, nameof(BitCheckbox.Size), BitSize.Small);
            builder.AddAttribute(3, nameof(BitCheckbox.Title), "Direct Title");
        });

        var checkbox = component.Find(".bit-chb");

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-err"));
        Assert.IsFalse(checkbox.ClassList.Contains("bit-chb-suc"));
        Assert.IsTrue(checkbox.ClassList.Contains("bit-chb-sm"));
        Assert.IsFalse(checkbox.ClassList.Contains("bit-chb-lg"));
        Assert.AreEqual("Direct Title", component.Find(".bit-chb-lbl").GetAttribute("title"));

        // What the checkbox left unset is still filled in from the cascade.
        Assert.IsTrue(component.Find(".bit-chb-ico").ClassList.Contains("bit-icon--Add"));
    }

    [TestMethod]
    public void BitCheckboxParamsUpdateParametersShouldSetAllProperties()
    {
        var @params = new BitCheckboxParams
        {
            AllowDisabledFocus = true,
            AriaControls = "the-list",
            AriaDescribedby = "the-hint",
            AriaDescription = "Test description",
            AriaLabelledby = "the-heading",
            AriaPositionInSet = 2,
            AriaSetSize = 7,
            AutoFocus = true,
            AutoLoading = true,
            CheckIcon = BitIconInfo.Fa("solid check"),
            CheckIconAriaLabel = "Checked",
            CheckIconName = "Accept",
            Color = BitColor.Warning,
            DefaultIndeterminate = true,
            Description = "Test description line",
            FullWidth = true,
            Indeterminate = true,
            IndeterminateIcon = BitIconInfo.Fa("solid minus"),
            IndeterminateIconName = "Remove",
            Label = "Test Label",
            LabelPlacement = BitPlacement.Top,
            Loading = true,
            NoWrap = true,
            Reversed = true,
            Size = BitSize.Small,
            StopPropagation = true,
            ThreeState = true,
            Title = "Test Title",
            UncheckedIcon = BitIconInfo.Fa("regular square"),
            UncheckedIconName = "Cancel",
            AriaLabel = "Test Aria Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitCheckbox>().Instance;

        Assert.IsTrue(instance.AllowDisabledFocus);
        Assert.AreEqual("the-list", instance.AriaControls);
        Assert.AreEqual("the-hint", instance.AriaDescribedby);
        Assert.AreEqual("Test description", instance.AriaDescription);
        Assert.AreEqual("the-heading", instance.AriaLabelledby);
        Assert.AreEqual(2, instance.AriaPositionInSet);
        Assert.AreEqual(7, instance.AriaSetSize);
        Assert.IsTrue(instance.AutoFocus);
        Assert.IsTrue(instance.AutoLoading);
        Assert.AreEqual(@params.CheckIcon, instance.CheckIcon);
        Assert.AreEqual("Checked", instance.CheckIconAriaLabel);
        Assert.AreEqual("Accept", instance.CheckIconName);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.IsTrue(instance.DefaultIndeterminate);
        Assert.AreEqual("Test description line", instance.Description);
        Assert.IsTrue(instance.FullWidth);
        Assert.IsTrue(instance.Indeterminate);
        Assert.AreEqual(@params.IndeterminateIcon, instance.IndeterminateIcon);
        Assert.AreEqual("Remove", instance.IndeterminateIconName);
        Assert.AreEqual("Test Label", instance.Label);
        Assert.AreEqual(BitPlacement.Top, instance.LabelPlacement);
        Assert.IsTrue(instance.Loading);
        Assert.IsTrue(instance.NoWrap);
        Assert.IsTrue(instance.Reversed);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsTrue(instance.StopPropagation);
        Assert.IsTrue(instance.ThreeState);
        Assert.AreEqual("Test Title", instance.Title);
        Assert.AreEqual(@params.UncheckedIcon, instance.UncheckedIcon);
        Assert.AreEqual("Cancel", instance.UncheckedIconName);
        Assert.AreEqual("Test Aria Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitCheckboxParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitCheckboxParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Title = "Params Title"
        };

        var component = RenderComponent<BitCheckbox>(p =>
        {
            p.Add(x => x.Color, BitColor.Error);
            p.Add(x => x.Size, BitSize.Small);
            p.Add(x => x.Title, "Existing Title");
        });

        var instance = component.Instance;

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.Title);

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.Title);
    }

    [TestMethod]
    public void BitCheckboxParamsShouldApplyClassesAndStyles()
    {
        var classes = new BitCheckboxClassStyles
        {
            Root = "custom-root",
            Container = "custom-container",
            Box = "custom-box",
            Icon = "custom-icon",
            Label = "custom-label",
            Description = "custom-description"
        };

        var styles = new BitCheckboxClassStyles
        {
            Root = "color: red;",
            Container = "margin: 5px;",
            Box = "padding: 10px;",
            Icon = "opacity: 0.5;",
            Label = "font-weight: bold;",
            Description = "font-size: 10px;"
        };

        var component = RenderWithParams(new BitCheckboxParams
        {
            Classes = classes,
            Styles = styles,
            Label = "Label",
            Description = "Description"
        });

        var checkbox = component.Find(".bit-chb");
        var container = component.Find(".bit-chb-lbl");
        var box = component.Find(".bit-chb-box");
        var icon = component.Find(".bit-chb-ico");
        var label = component.Find(".bit-chb-txt");
        var description = component.Find(".bit-chb-des");

        Assert.IsTrue(checkbox.ClassList.Contains("custom-root"));
        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        Assert.IsTrue(box.ClassList.Contains("custom-box"));
        Assert.IsTrue(icon.ClassList.Contains("custom-icon"));
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.IsTrue(description.ClassList.Contains("custom-description"));

        Assert.IsTrue(checkbox.GetAttribute("style")?.Contains("color: red;"));
        Assert.AreEqual("margin: 5px;", container.GetAttribute("style"));
        Assert.AreEqual("padding: 10px;", box.GetAttribute("style"));
        Assert.AreEqual("opacity: 0.5;", icon.GetAttribute("style"));
        Assert.AreEqual("font-weight: bold;", label.GetAttribute("style"));
        Assert.AreEqual("font-size: 10px;", description.GetAttribute("style"));
    }

    /// <summary>
    /// DefaultIndeterminate is acted on while the checkbox initializes, before OnParametersSet has run for the first
    /// time, so the cascade is read there as well - a cascaded default that only arrived later would find the state it
    /// is the default for already settled. The mixed state a cascade hands down is announced and pushed onto the
    /// element exactly like one the markup wrote.
    /// </summary>
    [TestMethod]
    public void BitCheckboxShouldStartMixedFromACascadedDefaultIndeterminate()
    {
        var component = RenderWithParams(new BitCheckboxParams { DefaultIndeterminate = true });

        Assert.IsTrue(component.Find(".bit-chb").ClassList.Contains("bit-chb-ind"));
        Assert.AreEqual("mixed", component.Find("input").GetAttribute("aria-checked"));
        Assert.IsTrue(component.Find(".bit-chb-ico").ClassList.Contains("bit-chb-idi"));

        var pushed = Context.JSInterop.Invocations.Single(i => i.Identifier == "BitBlazorUI.Utils.setProperty");

        Assert.AreEqual("indeterminate", pushed.Arguments[1]);
        Assert.AreEqual(true, pushed.Arguments[2]);
    }

    [TestMethod]
    public void BitCheckboxShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitCheckboxParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
        });

        var root = component.Find(".bit-chb");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("margin: 1px;"));
        Assert.AreEqual("cascaded-id", root.Id);

        // The disabled state a cascade hands down reaches the native control the same way a written one does.
        Assert.IsTrue(component.Find("input").HasAttribute("disabled"));
        Assert.AreEqual("true", component.Find("input").GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitCheckboxShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitCheckboxParams());

        var root = component.Find(".bit-chb");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and no label or description is drawn.
        Assert.IsFalse(root.ClassList.Contains("bit-chb-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-chb-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-chb-ind"));
        Assert.IsFalse(root.ClassList.Contains("bit-chb-ckd"));
        Assert.AreEqual(0, component.FindAll(".bit-chb-txt").Count);
        Assert.AreEqual(0, component.FindAll(".bit-chb-des").Count);
        Assert.IsFalse(component.Find("input").HasAttribute("disabled"));
    }
}
