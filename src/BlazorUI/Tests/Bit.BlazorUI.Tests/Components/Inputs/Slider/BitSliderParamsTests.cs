using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

/// <summary>
/// Covers the BitParams cascade of the Slider: what a BitSliderParams fills in - its own parameters, the ones of the
/// input base and the ones of the component base alike - what it leaves alone because the slider wrote it for itself,
/// and that what the slider derives from its parameters (its scale, its marks, its thumbs, the defaults it picks)
/// follows the cascaded values.
/// </summary>
[TestClass]
public class BitSliderParamsTests : BunitTestContext
{
    // What a slider is about rather than how every slider looks - its label and the values it holds, their defaults
    // and the expression that binds the pair of a range - is written per slider, so it has no place on the params
    // object; neither have the callbacks, the templates and the cascade itself.
    private static readonly string[] _notCascaded =
    [
        nameof(BitSlider.CascadingParameters),
        nameof(BitSlider.DefaultLowerValue),
        nameof(BitSlider.DefaultUpperValue),
        nameof(BitSlider.Label),
        nameof(BitSlider.LowerValue),
        nameof(BitSlider.UpperValue),
        nameof(BitSlider.RangeValue),
        nameof(BitSlider.RangeValueExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitSliderParams sliderParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { sliderParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSlider>(0);
                builder.AddAttribute(1, nameof(BitSlider.Label), "Cascaded");
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitSliderParamsShouldHaveTheCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitSlider)}", BitSliderParams.ParamName);
        Assert.AreEqual("BitParams.BitSlider", BitSliderParams.ParamName);
    }

    [TestMethod]
    public void BitSliderParamsShouldImplementIBitComponentParams()
    {
        var sliderParams = new BitSliderParams();

        Assert.IsInstanceOfType<IBitComponentParams>(sliderParams);
        Assert.IsInstanceOfType<BitInputBaseParams<double>>(sliderParams);
        Assert.AreEqual(BitSliderParams.ParamName, sliderParams.Name);
    }

    [TestMethod]
    public void BitSliderShouldApplyCascadingParametersFromBitParams()
    {
        var com = RenderWithParams(new BitSliderParams
        {
            Min = 0,
            Max = 100,
            Step = 5,
            Color = BitColor.Success,
            Size = BitSize.Large,
            ShowMarks = true,
            MarkStep = 25,
            ReadOnly = true
        });

        var root = com.Find(".bit-sld");

        Assert.IsTrue(root.ClassList.Contains("bit-sld-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-rdl"));

        var input = com.Find(".bit-sld-inp");

        Assert.AreEqual("100", input.GetAttribute("aria-valuemax"));
        Assert.AreEqual("5", input.GetAttribute("step"));
        Assert.AreEqual("true", input.GetAttribute("aria-readonly"));

        // Marks every 25 of a 0..100 scale, so five of them - which also proves the mark caches are built after the
        // cascade has been applied rather than before it.
        Assert.AreEqual(5, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderOwnParametersShouldOverrideTheCascadedOnes()
    {
        var com = RenderWithParams(new BitSliderParams { Max = 100, Color = BitColor.Success, Size = BitSize.Large, ReadOnly = true }, builder =>
        {
            builder.AddAttribute(2, nameof(BitSlider.Max), 4D);
            builder.AddAttribute(3, nameof(BitSlider.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(4, nameof(BitSlider.ReadOnly), false);
        });

        var root = com.Find(".bit-sld");

        Assert.IsTrue(root.ClassList.Contains("bit-sld-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-sld-suc"));
        Assert.IsFalse(root.ClassList.Contains("bit-sld-rdl"));

        // Size was left unset on the slider itself, so it still comes from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-sld-lg"));
        Assert.AreEqual("4", com.Find(".bit-sld-inp").GetAttribute("aria-valuemax"));
    }

    [TestMethod]
    public void BitSliderShouldTakeIsRangedFromTheCascadeBeforeItsDefaultsArePicked()
    {
        // The defaults of an unbound slider are picked once, in OnInitialized, and which ones those are depends on
        // IsRanged - so the cascade has to have been applied by then.
        var com = RenderWithParams(new BitSliderParams { IsRanged = true, Max = 10 }, builder =>
        {
            builder.AddAttribute(2, nameof(BitSlider.DefaultLowerValue), (double?)3D);
            builder.AddAttribute(3, nameof(BitSlider.DefaultUpperValue), (double?)7D);
        });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-sld-rgd"));
        Assert.AreEqual("3", com.Find(".bit-sld-inp-lwr").GetAttribute("value"));
        Assert.AreEqual("7", com.Find(".bit-sld-inp-upr").GetAttribute("value"));
    }

    [TestMethod]
    public void BitSliderShouldTakeTheComponentBaseParametersFromTheCascade()
    {
        var com = RenderWithParams(new BitSliderParams
        {
            Id = "cascaded-id",
            Class = "cascaded",
            Style = "margin: 1px;",
            Dir = BitDir.Rtl,
            Disabled = true,
            TabIndex = "3",
            AriaLabel = "Cascaded volume",
            Visibility = BitVisibility.Hidden,
        });

        var root = com.Find(".bit-sld");
        var input = com.Find(".bit-sld-inp");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));

        Assert.AreEqual("3", input.GetAttribute("tabindex"));
        Assert.AreEqual("Cascaded volume", input.GetAttribute("aria-label"));
        Assert.IsTrue(input.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitSliderShouldTakeRequiredFromTheCascade()
    {
        var com = RenderWithParams(new BitSliderParams { Required = true });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("bit-sld-req"));
        Assert.AreEqual("true", com.Find(".bit-sld-inp").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitSliderParamsShouldApplyClassesAndStyles()
    {
        var com = RenderWithParams(new BitSliderParams
        {
            Classes = new BitSliderClassStyles { Root = "custom-root", Thumb = "custom-thumb" },
            Styles = new BitSliderClassStyles { Root = "color: red;", Thumb = "border-radius: 0;" },
        });

        Assert.IsTrue(com.Find(".bit-sld").ClassList.Contains("custom-root"));
        StringAssert.Contains(com.Find(".bit-sld").GetAttribute("style"), "color: red;");
        Assert.IsTrue(com.Find(".bit-sld-thb").ClassList.Contains("custom-thumb"));
        StringAssert.Contains(com.Find(".bit-sld-thb").GetAttribute("style"), "border-radius: 0;");
    }

    [TestMethod]
    public void BitSliderParamsUpdateParametersShouldSetAllProperties()
    {
        Func<double, string> ariaValueText = v => $"Level {v}";
        Func<double, string> getValueText = v => $"{v} kg";
        var marks = new List<BitSliderMark> { new(0, "Low"), new(50, "Mid"), new(100, "High") };
        var boxAttributes = new Dictionary<string, object> { { "data-box", "yes" } };

        var @params = new BitSliderParams
        {
            AriaDescription = "Cascaded description",
            AriaValueText = ariaValueText,
            AutoFocus = true,
            Classes = new BitSliderClassStyles { Root = "cascaded-root" },
            Color = BitColor.Warning,
            DraggableTrack = true,
            GetValueText = getValueText,
            Inverted = true,
            IsOriginFromZero = true,
            IsRanged = true,
            IsVertical = true,
            LargeStep = 25,
            LowerAriaLabel = "Cascaded lower",
            Marks = marks,
            MarkStep = 10,
            Max = 100,
            MaxRange = 80,
            Min = -20,
            MinRange = 10,
            NoFill = true,
            NoSwap = true,
            Origin = 5,
            Pushable = true,
            RestrictToMarks = true,
            ShowMarkLabels = true,
            ShowMarks = true,
            ShowValue = false,
            Size = BitSize.Small,
            SliderBoxHtmlAttributes = boxAttributes,
            Step = 2,
            Styles = new BitSliderClassStyles { Root = "color: red;" },
            ThumbLabel = BitSliderThumbLabel.On,
            UpperAriaLabel = "Cascaded upper",
            ValueFormat = "0.0",
            ReadOnly = true,
            Required = true,
        };

        var com = RenderWithParams(@params);

        var instance = com.FindComponent<BitSlider>().Instance;

        Assert.AreEqual("Cascaded description", instance.AriaDescription);
        Assert.AreSame(ariaValueText, instance.AriaValueText);
        Assert.IsTrue(instance.AutoFocus);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.IsTrue(instance.DraggableTrack);
        Assert.AreSame(getValueText, instance.GetValueText);
        Assert.IsTrue(instance.Inverted);
        Assert.IsTrue(instance.IsOriginFromZero);
        Assert.IsTrue(instance.IsRanged);
        Assert.IsTrue(instance.IsVertical);
        Assert.AreEqual(25, instance.LargeStep);
        Assert.AreEqual("Cascaded lower", instance.LowerAriaLabel);
        Assert.AreSame(marks, instance.Marks);
        Assert.AreEqual(10, instance.MarkStep);
        Assert.AreEqual(100, instance.Max);
        Assert.AreEqual(80, instance.MaxRange);
        Assert.AreEqual(-20, instance.Min);
        Assert.AreEqual(10, instance.MinRange);
        Assert.IsTrue(instance.NoFill);
        Assert.IsTrue(instance.NoSwap);
        Assert.AreEqual(5, instance.Origin);
        Assert.IsTrue(instance.Pushable);
        Assert.IsTrue(instance.RestrictToMarks);
        Assert.IsTrue(instance.ShowMarkLabels);
        Assert.IsTrue(instance.ShowMarks);
        Assert.IsFalse(instance.ShowValue);
        Assert.AreEqual(BitSize.Small, instance.Size);
        // The cascade hands the component a copy of the dictionary, so an instance never writes into the shared one.
        CollectionAssert.AreEquivalent(boxAttributes.ToList(), instance.SliderBoxHtmlAttributes!.ToList());
        Assert.AreEqual(2, instance.Step);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreEqual(BitSliderThumbLabel.On, instance.ThumbLabel);
        Assert.AreEqual("Cascaded upper", instance.UpperAriaLabel);
        Assert.AreEqual("0.0", instance.ValueFormat);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);

        // And the markup follows what was cascaded, not only the properties.
        var root = com.Find(".bit-sld");

        Assert.IsTrue(root.ClassList.Contains("bit-sld-rgd-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-ivt"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-tlo"));
        Assert.IsTrue(root.ClassList.Contains("bit-sld-rdl"));
        Assert.AreEqual("yes", com.Find(".bit-sld-box").GetAttribute("data-box"));
        Assert.AreEqual("Cascaded lower", com.Find(".bit-sld-inp-lwr").GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded upper", com.Find(".bit-sld-inp-upr").GetAttribute("aria-label"));
        Assert.AreEqual("Cascaded description", com.Find(".bit-sld-dsc").TextContent);
        Assert.AreEqual(0, com.FindAll(".bit-sld-vlb").Count);
        Assert.AreEqual(0, com.FindAll(".bit-sld-fil").Count);
        Assert.AreEqual(3, com.FindAll(".bit-sld-mlb").Count);
        Assert.AreEqual("Low", com.FindAll(".bit-sld-mlb")[0].TextContent.Trim());

        // The explicit marks and the per-value text reach both thumbs.
        foreach (var input in com.FindAll(".bit-sld-inp"))
        {
            Assert.AreEqual("2", input.GetAttribute("step"));
            StringAssert.StartsWith(input.GetAttribute("aria-valuetext"), "Level ");
        }
    }

    [TestMethod]
    public void BitSliderParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitSliderParams
        {
            Max = 100,
            Color = BitColor.Success,
            ShowValue = false,
        };

        var com = RenderComponent<BitSlider>(parameters =>
        {
            parameters.Add(p => p.Max, 4D);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.ShowValue, true);
        });

        var instance = com.Instance;

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(4, instance.Max);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.IsTrue(instance.ShowValue);
    }

    [TestMethod]
    public void BitSliderShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var com = RenderWithParams(new BitSliderParams());

        var root = com.Find(".bit-sld");
        var input = com.Find(".bit-sld-inp");

        // Nothing cascades, so the defaults hold: a single horizontal thumb on a 0..10 scale, with its value label and
        // no color or size class.
        Assert.IsTrue(root.ClassList.Contains("bit-sld-hrz"));
        Assert.IsFalse(root.ClassList.Contains("bit-sld-rgd"));
        Assert.IsFalse(root.ClassList.Contains("bit-sld-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-sld-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("0", input.GetAttribute("min"));
        Assert.AreEqual("10", input.GetAttribute("max"));
        Assert.AreEqual("1", input.GetAttribute("step"));
        Assert.AreEqual(1, com.FindAll(".bit-sld-vlb").Count);
        Assert.AreEqual(0, com.FindAll(".bit-sld-mrk").Count);
    }

    [TestMethod]
    public void BitSliderShouldNotCascadeTheValuesOfASingleField()
    {
        // The values of one slider - the single one, the two ends of a range and their defaults - identify that
        // slider, so they are deliberately not part of the params class.
        var properties = typeof(BitSliderParams).GetProperties().Select(p => p.Name).ToHashSet();

        foreach (var name in new[] { nameof(BitSlider.Value), nameof(BitSlider.DefaultValue), nameof(BitSlider.LowerValue),
                                     nameof(BitSlider.UpperValue), nameof(BitSlider.RangeValue), nameof(BitSlider.DefaultLowerValue),
                                     nameof(BitSlider.DefaultUpperValue) })
        {
            Assert.IsFalse(properties.Contains(name), $"BitSliderParams cascades {name}.");
        }
    }

    [TestMethod]
    public void BitSliderParamsShouldCarryEveryParameterThatIsNotPerSlider()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks, templates, the cascade itself and what is written per slider are the ones deliberately
        // left out.
        var paramsProperties = typeof(BitSliderParams).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitSlider).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                       .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                       .Where(p => p.PropertyType.Name.StartsWith("EventCallback") is false)
                                       .Where(p => p.PropertyType.Name.StartsWith("RenderFragment") is false)
                                       .Select(p => p.Name)
                                       .Where(n => _notCascaded.Contains(n) is false)
                                       .Where(n => paramsProperties.Contains(n) is false)
                                       .ToList();

        CollectionAssert.AreEqual(new List<string>(), missing, string.Join(", ", missing));
    }
}
