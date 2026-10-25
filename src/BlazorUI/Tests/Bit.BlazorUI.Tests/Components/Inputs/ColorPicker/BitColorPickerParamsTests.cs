using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// Covers the BitParams cascade of the ColorPicker: what a BitColorPickerParams fills in - every property it carries,
/// the ones of the input base and of the component base included - what it leaves alone because the picker wrote it
/// for itself, and that the params object carries every parameter that is not the state of one picker.
/// </summary>
[TestClass]
public class BitColorPickerParamsTests : BunitTestContext
{
    // The color a picker is on and the model its fields are typed in are the state of that one picker, which its
    // own switch and its own gestures move: a cascaded one would be re-imposed on every render of the parent. Only
    // the STARTING mode (DefaultInputsMode) is cascaded.
    private static readonly string[] _notCascaded =
    [
        nameof(BitColorPicker.CascadingParameters),
        nameof(BitColorPicker.Alpha),
        nameof(BitColorPicker.InputsMode),
    ];

    private static RenderFragment RenderColorPicker(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitColorPicker>(0);
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitColorPickerParams colorPickerParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { colorPickerParams });
            parameters.AddChildContent(RenderColorPicker(extraAttributes));
        });
    }

    [TestMethod]
    public void BitColorPickerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitColorPicker)}", BitColorPickerParams.ParamName);
        Assert.AreEqual("BitParams.BitColorPicker", BitColorPickerParams.ParamName);
    }

    [TestMethod]
    public void BitColorPickerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitColorPickerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitColorPickerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitColorPickerParamsShouldCarryEveryParameterThatIsNotPerPicker()
    {
        // The parameters of the input base and of the component base are carried by the base params classes.
        var baseParameters = typeof(BitInputBase<string?>).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitColorPicker).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                               // An event is wired per picker, never cascaded.
                                               .Where(p => IsEventCallback(p.PropertyType) is false)
                                               .Select(p => p.Name)
                                               .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitColorPickerParams).GetProperty(name), $"BitColorPickerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitColorPickerParamsUpdateParametersShouldSetAllProperties()
    {
        var presets = new[] { "#FF0000", "#00FF00" };
        var classes = new BitColorPickerClassStyles { Root = "cascaded-root" };
        var styles = new BitColorPickerClassStyles { Root = "z-index: 1;" };
        var texts = new BitColorPickerTexts { HueLabel = "Farbton" };
        var eyeDropperIcon = BitIconInfo.Fa("solid eye-dropper");
        var switchIcon = BitIconInfo.Fa("solid sort");
        RenderFragment labelTemplate = builder => builder.AddContent(0, "Cascaded label template");

        var @params = new BitColorPickerParams
        {
            AutoFocus = true,
            Classes = classes,
            ContrastColor = "#000000",
            DefaultInputsMode = BitColorInputsMode.Hsl,
            EyeDropperIcon = eyeDropperIcon,
            EyeDropperIconName = "Color",
            Format = BitColorFormat.Hsla,
            InputsModeSwitchIcon = switchIcon,
            InputsModeSwitchIconName = "Switch",
            Label = "Cascaded label",
            LabelTemplate = labelTemplate,
            Presets = presets,
            PresetsPerRow = 3,
            ShowAlphaSlider = true,
            ShowContrast = true,
            ShowEyeDropper = true,
            ShowHueSlider = false,
            ShowInputs = true,
            ShowInputsModeSwitch = true,
            ShowPreview = true,
            ShowSaturationArea = false,
            Size = BitSize.Small,
            Styles = styles,
            Texts = texts,
            ReadOnly = true,
            Required = true,
            AriaLabel = "Cascaded aria label",
            Class = "cascaded-class",
            Dir = BitDir.Rtl,
            Id = "cascaded-id",
            Style = "margin: 1px;",
            TabIndex = "5",
            Visibility = BitVisibility.Hidden,
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitColorPicker>().Instance;

        Assert.IsTrue(instance.AutoFocus);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreEqual("#000000", instance.ContrastColor);
        Assert.AreEqual(BitColorInputsMode.Hsl, instance.DefaultInputsMode);
        Assert.AreSame(eyeDropperIcon, instance.EyeDropperIcon);
        Assert.AreEqual("Color", instance.EyeDropperIconName);
        Assert.AreEqual(BitColorFormat.Hsla, instance.Format);
        Assert.AreSame(switchIcon, instance.InputsModeSwitchIcon);
        Assert.AreEqual("Switch", instance.InputsModeSwitchIconName);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreSame(labelTemplate, instance.LabelTemplate);
        Assert.AreSame(presets, instance.Presets);
        Assert.AreEqual(3, instance.PresetsPerRow);
        Assert.IsTrue(instance.ShowAlphaSlider);
        Assert.IsTrue(instance.ShowContrast);
        Assert.IsTrue(instance.ShowEyeDropper);
        Assert.IsFalse(instance.ShowHueSlider);
        Assert.IsTrue(instance.ShowInputs);
        Assert.IsTrue(instance.ShowInputsModeSwitch);
        Assert.IsTrue(instance.ShowPreview);
        Assert.IsFalse(instance.ShowSaturationArea);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreSame(texts, instance.Texts);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.AreEqual("cascaded-class", instance.Class);
        Assert.AreEqual(BitDir.Rtl, instance.Dir);
        Assert.AreEqual("cascaded-id", instance.Id);
        Assert.AreEqual("margin: 1px;", instance.Style);
        Assert.AreEqual("5", instance.TabIndex);
        Assert.AreEqual(BitVisibility.Hidden, instance.Visibility);

        // And what the markup derives from them follows: the mode the fields start in, the notation the value is
        // written in from the first render, the parts of the panel, and the base parameters on the root.
        var root = component.Find(".bit-clp");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-clp-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-clp-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("Cascaded aria label", root.GetAttribute("aria-label"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "z-index: 1;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("hsla(0,0%,100%,1)", instance.Value);
        Assert.AreEqual(BitColorInputsMode.Hsl, instance.InputsMode);
        Assert.AreEqual("Cascaded label template", component.Find(".bit-clp-lbc").TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-clp-rec").Count);
        Assert.AreEqual(0, component.FindAll(".bit-clp-hsd").Count);
        Assert.AreEqual(1, component.FindAll(".bit-clp-asd").Count);
        Assert.AreEqual(1, component.FindAll(".bit-clp-pre").Count);
        Assert.AreEqual(1, component.FindAll(".bit-clp-cnr").Count);
        Assert.AreEqual(2, component.FindAll(".bit-clp-prt").Count);
        Assert.IsTrue(component.Find(".bit-clp-prs").ClassList.Contains("bit-clp-prg"));
        Assert.AreEqual("Farbton", component.FindAll(".bit-clp-fin")[0].GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-clp-swi").ClassList.Contains("fa-sort"));
        Assert.AreEqual("5", component.FindAll(".bit-clp-fin")[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitColorPickerParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitColorPickerParams
        {
            Size = BitSize.Large,
            Format = BitColorFormat.Hsl,
            Label = "Params label",
            PresetsPerRow = 3,
            ShowInputs = true,
        };

        var component = RenderComponent<BitColorPicker>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Format, BitColorFormat.Hex);
            parameters.Add(p => p.Label, "Existing label");
            parameters.Add(p => p.PresetsPerRow, 5);
            parameters.Add(p => p.ShowInputs, false);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(BitColorFormat.Hex, instance.Format);
        Assert.AreEqual("Existing label", instance.Label);
        Assert.AreEqual(5, instance.PresetsPerRow);
        // A false written on the picker is a choice too.
        Assert.IsFalse(instance.ShowInputs);
    }

    [TestMethod]
    public void BitColorPickerParamsUpdateParametersShouldIgnoreANullPicker()
    {
        var @params = new BitColorPickerParams { Size = BitSize.Large };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitColorPickerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitColorPickerParams
        {
            Size = BitSize.Large,
            ShowPreview = true,
            ShowInputs = true,
            Label = "Cascaded label",
            Format = BitColorFormat.Hsl,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitColorPicker.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(2, nameof(BitColorPicker.ShowPreview), false);
            builder.AddAttribute(3, nameof(BitColorPicker.Format), (BitColorFormat?)BitColorFormat.Hex);
        });

        var root = component.Find(".bit-clp");
        var instance = component.FindComponent<BitColorPicker>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-clp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-clp-lg"));
        Assert.AreEqual(0, component.FindAll(".bit-clp-pre").Count);
        Assert.AreEqual("#FFFFFF", instance.Value);

        // What the picker left unset is still filled in from the cascade.
        Assert.AreEqual(4, component.FindAll(".bit-clp-fin").Count);
        Assert.AreEqual("Cascaded label", component.Find(".bit-clp-lbl").TextContent);
    }

    [TestMethod]
    public void BitColorPickerShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitColorPickerParams
        {
            ShowPreview = true,
            Classes = new() { Root = "cascaded-root", SaturationPicker = "cascaded-area", Preview = "cascaded-preview" },
            Styles = new() { Root = "z-index: 1;", SaturationPicker = "border-radius: 1rem;", Preview = "border-radius: 50%;" },
        });

        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("cascaded-root"));
        StringAssert.Contains(component.Find(".bit-clp").GetAttribute("style"), "z-index: 1;");
        Assert.IsTrue(component.Find(".bit-clp-rec").ClassList.Contains("cascaded-area"));
        StringAssert.Contains(component.Find(".bit-clp-rec").GetAttribute("style"), "border-radius: 1rem;");
        Assert.IsTrue(component.Find(".bit-clp-pre").ClassList.Contains("cascaded-preview"));
        StringAssert.Contains(component.Find(".bit-clp-pre").GetAttribute("style"), "border-radius: 50%;");
    }

    [TestMethod]
    public void BitColorPickerParamsShouldMergeTheHtmlAttributes()
    {
        var component = RenderWithParams(new BitColorPickerParams
        {
            HtmlAttributes = new Dictionary<string, object>
            {
                ["data-cascaded"] = "yes",
                ["data-both"] = "from-cascade",
            }
        }, builder =>
        {
            builder.AddAttribute(1, "data-both", "from-instance");
        });

        var root = component.Find(".bit-clp");

        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        // What the instance wrote for itself is never replaced by the cascade.
        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
    }

    [TestMethod]
    public void BitColorPickerShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitColorPickerParams());

        var instance = component.FindComponent<BitColorPicker>().Instance;
        var root = component.Find(".bit-clp");

        Assert.AreEqual("rgb(255,255,255)", instance.Value);
        Assert.IsNull(instance.Size);
        Assert.IsTrue(instance.ShowHueSlider);
        Assert.IsTrue(instance.ShowSaturationArea);
        Assert.IsFalse(instance.ShowInputs);
        Assert.AreEqual(1, component.FindAll(".bit-clp-rec").Count);
        Assert.AreEqual(1, component.FindAll(".bit-clp-hsd").Count);
        Assert.AreEqual(0, component.FindAll(".bit-clp-fin").Count);
        Assert.IsFalse(root.ClassList.Contains("bit-clp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-clp-lg"));
    }

    private static bool IsEventCallback(Type type)
    {
        return type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }
}
