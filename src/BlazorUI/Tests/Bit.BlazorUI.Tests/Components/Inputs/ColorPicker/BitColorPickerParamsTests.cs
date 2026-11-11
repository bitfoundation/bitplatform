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
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitColorPicker)}", new BitColorPickerParams().Name);
        Assert.AreEqual("BitParams.BitColorPicker", new BitColorPickerParams().Name);
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
        RenderFragment errorTemplate = builder => builder.AddContent(0, "Cascaded error template");

        var @params = new BitColorPickerParams
        {
            AutoFocus = true,
            Classes = classes,
            ContrastColor = "#000000",
            DefaultInputsMode = BitColorInputsMode.Hsl,
            ErrorMessage = "Cascaded error",
            ErrorMessageAriaLabel = "Cascaded error label",
            ErrorMessageTemplate = errorTemplate,
            EyeDropperIcon = eyeDropperIcon,
            EyeDropperIconName = "Color",
            Format = BitColorFormat.Hsla,
            InputsModeSwitchIcon = switchIcon,
            InputsModeSwitchIconName = "Switch",
            Invalid = true,
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
            ShowValidationMessage = true,
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
        Assert.AreEqual("Cascaded error", instance.ErrorMessage);
        Assert.AreEqual("Cascaded error label", instance.ErrorMessageAriaLabel);
        Assert.AreSame(errorTemplate, instance.ErrorMessageTemplate);
        Assert.AreSame(eyeDropperIcon, instance.EyeDropperIcon);
        Assert.AreEqual("Color", instance.EyeDropperIconName);
        Assert.AreEqual(BitColorFormat.Hsla, instance.Format);
        Assert.AreSame(switchIcon, instance.InputsModeSwitchIcon);
        Assert.AreEqual("Switch", instance.InputsModeSwitchIconName);
        Assert.IsTrue(instance.Invalid);
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
        Assert.IsTrue(instance.ShowValidationMessage);
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
        Assert.Contains("margin: 1px;", root.GetAttribute("style")!);
        Assert.Contains("z-index: 1;", root.GetAttribute("style")!);
        Assert.Contains("visibility:hidden", root.GetAttribute("style")!);
        Assert.AreEqual("hsla(0,0%,100%,1)", instance.Value);
        Assert.AreEqual(BitColorInputsMode.Hsl, instance.InputsMode);
        Assert.AreEqual("Cascaded label template", component.Find(".bit-clp-lbc").TextContent.Trim());
        Assert.IsEmpty(component.FindAll(".bit-clp-rec"));
        Assert.IsEmpty(component.FindAll(".bit-clp-hsd"));
        Assert.ContainsSingle(component.FindAll(".bit-clp-asd"));
        Assert.ContainsSingle(component.FindAll(".bit-clp-pre"));
        Assert.ContainsSingle(component.FindAll(".bit-clp-cnr"));
        Assert.HasCount(2, component.FindAll(".bit-clp-prt"));
        Assert.IsTrue(component.Find(".bit-clp-prs").ClassList.Contains("bit-clp-prg"));
        Assert.AreEqual("Farbton", component.FindAll(".bit-clp-fin")[0].GetAttribute("title"));
        Assert.IsTrue(component.Find(".bit-clp-swi").ClassList.Contains("fa-sort"));
        Assert.AreEqual("5", component.FindAll(".bit-clp-fin")[0].GetAttribute("tabindex"));
        Assert.AreEqual("Cascaded error template", component.Find(".bit-clp-erm").TextContent.Trim());
        Assert.IsTrue(root.ClassList.Contains("bit-inv"));
    }

    [TestMethod]
    public void BitColorPickerShouldShowAndAnnounceACascadedErrorMessage()
    {
        var component = RenderWithParams(new BitColorPickerParams { ShowInputs = true, ErrorMessage = "Not in the brand palette." });

        Assert.AreEqual("Not in the brand palette.", component.Find(".bit-clp-erm").TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-clp-rec").GetAttribute("aria-invalid"));
        Assert.IsTrue(component.FindAll(".bit-clp-fin").All(i => i.GetAttribute("aria-invalid") == "true"));
        Assert.AreEqual("Not in the brand palette.", component.Find("[role=status]").TextContent.Trim());
    }

    [TestMethod]
    public void BitColorPickerShouldRenderACascadedErrorMessageTemplate()
    {
        RenderFragment template = builder => builder.AddMarkupContent(0, "<span class=\"cascaded-error\">Refused</span>");

        var component = RenderWithParams(new BitColorPickerParams { ErrorMessageTemplate = template });

        Assert.AreEqual("Refused", component.Find(".bit-clp-erm .cascaded-error").TextContent);
        Assert.IsTrue(component.Find(".bit-clp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("Invalid input", component.Find("[role=status]").TextContent.Trim());
    }

    /// <summary>
    /// A cascaded ErrorMessageAriaLabel is not written by the markup, yet it is announced the moment Invalid turns on,
    /// so the live region is on the page before that - a region that arrives with its text already in it is missed.
    /// </summary>
    [TestMethod]
    public void BitColorPickerShouldRenderTheLiveRegionBeforeACascadedErrorMessageAriaLabelIsAnnounced()
    {
        var invalid = false;

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitColorPickerParams { ErrorMessageAriaLabel = "Refused by the server." } });
            parameters.AddChildContent(RenderColorPicker(builder => builder.AddAttribute(1, nameof(BitColorPicker.Invalid), invalid)));
        });

        Assert.AreEqual(string.Empty, component.Find("[role=status]").TextContent.Trim());

        invalid = true;
        component.Render();

        Assert.AreEqual("Refused by the server.", component.Find("[role=status]").TextContent.Trim());
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
        Assert.IsEmpty(component.FindAll(".bit-clp-pre"));
        Assert.AreEqual("#FFFFFF", instance.Value);

        // What the picker left unset is still filled in from the cascade.
        Assert.HasCount(4, component.FindAll(".bit-clp-fin"));
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
        Assert.Contains("z-index: 1;", component.Find(".bit-clp").GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-clp-rec").ClassList.Contains("cascaded-area"));
        Assert.Contains("border-radius: 1rem;", component.Find(".bit-clp-rec").GetAttribute("style")!);
        Assert.IsTrue(component.Find(".bit-clp-pre").ClassList.Contains("cascaded-preview"));
        Assert.Contains("border-radius: 50%;", component.Find(".bit-clp-pre").GetAttribute("style")!);
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
        Assert.ContainsSingle(component.FindAll(".bit-clp-rec"));
        Assert.ContainsSingle(component.FindAll(".bit-clp-hsd"));
        Assert.IsEmpty(component.FindAll(".bit-clp-fin"));
        Assert.IsFalse(root.ClassList.Contains("bit-clp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-clp-lg"));
    }

    private static bool IsEventCallback(Type type)
    {
        return type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }
}
