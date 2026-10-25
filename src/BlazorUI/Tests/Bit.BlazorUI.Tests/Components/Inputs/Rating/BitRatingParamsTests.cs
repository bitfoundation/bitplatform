using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

/// <summary>
/// Covers the BitParams cascade of the Rating: what a BitRatingParams fills in - its own parameters, the ones of the
/// input base and the ones of the component base alike - what it leaves alone because the rating wrote it for itself,
/// and that what the rating derives from its parameters (its items, its icons, its names and its range) follows the
/// cascaded values.
/// </summary>
[TestClass]
public class BitRatingParamsTests : BunitTestContext
{
    // The callbacks, the templates and the cascade itself are left out of the params object on purpose.
    private static readonly string[] _notCascaded =
    [
        nameof(BitRating.CascadingParameters),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitRatingParams ratingParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { ratingParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRating>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitRatingParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitRating)}", BitRatingParams.ParamName);
        Assert.AreEqual("BitParams.BitRating", BitRatingParams.ParamName);
    }

    [TestMethod]
    public void BitRatingParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitRatingParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.IsInstanceOfType<BitInputBaseParams<double>>(@params);
        Assert.AreEqual(BitRatingParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitRatingShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitRatingParams
        {
            Max = 3,
            Color = BitColor.Success,
            Size = BitSize.Large,
            Vertical = true,
            ReadOnly = true,
            Label = "Cascaded label",
            LabelPlacement = BitPlacement.End,
            SelectedIconName = "HeartFill",
            UnselectedIconName = "Heart"
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual(3, component.FindAll(".bit-rtg-btn").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-led"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-rtg-lbl").TextContent);
        Assert.IsTrue(component.FindAll(".bit-rtg-iem")[2].ClassList.Contains("bit-icon--Heart"));
        Assert.IsTrue(component.FindAll(".bit-rtg-ifl")[0].ClassList.Contains("bit-icon--HeartFill"));
    }

    [TestMethod]
    public void BitRatingDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitRatingParams
        {
            Max = 3,
            Color = BitColor.Success,
            ReadOnly = true,
            Label = "Cascaded label"
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitRating.Max), 6);
            builder.AddAttribute(2, nameof(BitRating.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(3, nameof(BitRating.ReadOnly), false);
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual(6, component.FindAll(".bit-rtg-btn").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-suc"));

        // An explicit false is a written parameter like any other, so the cascade does not fill it in.
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-rdl"));

        // What the markup left unwritten still comes from the cascade.
        Assert.AreEqual("Cascaded label", component.Find(".bit-rtg-lbl").TextContent);
    }

    [TestMethod]
    public void BitRatingShouldApplyTheCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitRatingParams
        {
            Id = "cascaded-id",
            Class = "cascaded-class",
            Style = "margin: 1px;",
            Dir = BitDir.Rtl,
            Disabled = true,
            TabIndex = "3",
            AriaLabel = "Cascaded rating",
            Visibility = BitVisibility.Hidden,
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", root.GetAttribute("aria-disabled"));
        Assert.AreEqual("Cascaded rating", root.GetAttribute("aria-label"));

        // The TabIndex reaches the one item that holds the tab stop of the group.
        Assert.AreEqual("3", component.Find(".bit-rtg-btn").GetAttribute("tabindex"));
        Assert.IsTrue(component.Find(".bit-rtg-btn").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRatingShouldTakeRequiredFromTheCascade()
    {
        var component = RenderWithParams(new BitRatingParams { Label = "Quality", Required = true, AllowZeroStars = true });

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-rtg-req"));
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-required"));
        Assert.IsTrue(component.Find(".bit-input-hidden").HasAttribute("required"));
        Assert.AreEqual("0", component.Find(".bit-input-hidden").GetAttribute("min"));
    }

    [TestMethod]
    public void BitRatingParamsShouldApplyClassesAndStyles()
    {
        var component = RenderWithParams(new BitRatingParams
        {
            Classes = new BitRatingClassStyles { Root = "custom-root", Button = "custom-button" },
            Styles = new BitRatingClassStyles { Root = "color: red;", Button = "color: blue;" },
        });

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-rtg").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-rtg-btn").ClassList.Contains("custom-button"));
        Assert.AreEqual("color: blue;", component.Find(".bit-rtg-btn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitRatingParamsUpdateParametersShouldSetAllProperties()
    {
        Func<double, double, string> getAriaLabel = (value, max) => $"{value} of {max}";
        Func<int, BitIconInfo?> getSelectedIcon = index => BitIconInfo.Bit("LikeSolid");
        Func<int, BitIconInfo?> getUnselectedIcon = index => BitIconInfo.Bit("Like");
        var titles = new List<string> { "Bad", "Fair", "Good" };
        var selectedIcon = BitIconInfo.Bi("star-fill");
        var unselectedIcon = BitIconInfo.Bi("star");

        var @params = new BitRatingParams
        {
            AllowClear = true,
            AllowZeroStars = true,
            AriaLabelFormat = "Pick {0} of {1}",
            AriaLabelledBy = "external-heading",
            AutoFocus = true,
            Classes = new BitRatingClassStyles { Root = "cascaded-root" },
            Color = BitColor.Warning,
            Description = "Cascaded description",
            GetAriaLabel = getAriaLabel,
            GetSelectedIcon = getSelectedIcon,
            GetUnselectedIcon = getUnselectedIcon,
            HighlightSelectedOnly = true,
            ItemTitles = titles,
            Label = "Cascaded label",
            LabelPlacement = BitPlacement.Start,
            Max = 3,
            NoHoverPreview = true,
            Precision = 0.5,
            SelectedIcon = selectedIcon,
            SelectedIconName = "HeartFill",
            Size = BitSize.Small,
            Styles = new BitRatingClassStyles { Root = "color: red;" },
            UnselectedIcon = unselectedIcon,
            UnselectedIconName = "Heart",
            ValueTextFormat = "{0} out of {1}",
            Vertical = true,
            ReadOnly = true,
            Required = true,
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitRating>().Instance;

        Assert.IsTrue(instance.AllowClear);
        Assert.IsTrue(instance.AllowZeroStars);
        Assert.AreEqual("Pick {0} of {1}", instance.AriaLabelFormat);
        Assert.AreEqual("external-heading", instance.AriaLabelledBy);
        Assert.IsTrue(instance.AutoFocus);
        Assert.AreSame(@params.Classes, instance.Classes);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.AreSame(getAriaLabel, instance.GetAriaLabel);
        Assert.AreSame(getSelectedIcon, instance.GetSelectedIcon);
        Assert.AreSame(getUnselectedIcon, instance.GetUnselectedIcon);
        Assert.IsTrue(instance.HighlightSelectedOnly);
        Assert.AreSame(titles, instance.ItemTitles);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual(BitPlacement.Start, instance.LabelPlacement);
        Assert.AreEqual(3, instance.Max);
        Assert.IsTrue(instance.NoHoverPreview);
        Assert.AreEqual(0.5, instance.Precision);
        Assert.AreSame(selectedIcon, instance.SelectedIcon);
        Assert.AreEqual("HeartFill", instance.SelectedIconName);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreSame(@params.Styles, instance.Styles);
        Assert.AreSame(unselectedIcon, instance.UnselectedIcon);
        Assert.AreEqual("Heart", instance.UnselectedIconName);
        Assert.AreEqual("{0} out of {1}", instance.ValueTextFormat);
        Assert.IsTrue(instance.Vertical);
        Assert.IsTrue(instance.ReadOnly);
        Assert.IsTrue(instance.Required);

        // And the markup follows what was cascaded, not only the properties.
        var root = component.Find(".bit-rtg");

        Assert.AreEqual(3, component.FindAll(".bit-rtg-btn").Count);
        Assert.AreEqual("external-heading", root.GetAttribute("aria-labelledby")?.Split(' ')[0]);
        Assert.AreEqual("Bad", component.Find(".bit-rtg-btn").GetAttribute("title"));
        Assert.AreEqual("Cascaded description", component.Find(".bit-rtg-dsc").TextContent.Trim());
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-lst"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-nhp"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-rdl"));
    }

    [TestMethod]
    public void BitRatingParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitRatingParams
        {
            Max = 3,
            Color = BitColor.Success,
            Label = "Params label",
        };

        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 6);
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Label, "Existing label");
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(6, instance.Max);
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual("Existing label", instance.Label);
    }

    [TestMethod]
    public void BitRatingShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitRatingParams());

        var root = component.Find(".bit-rtg");

        // Nothing cascades, so the defaults hold: five interactive stars, no color and no size class.
        Assert.AreEqual(5, component.FindAll(".bit-rtg-btn").Count);
        Assert.AreEqual("radiogroup", root.GetAttribute("role"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-rdl"));
        Assert.IsFalse(root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(0, component.FindAll(".bit-rtg-lbc").Count);
    }

    [TestMethod]
    public void BitRatingShouldNotCascadeTheValueOfASingleField()
    {
        // Value and DefaultValue identify one field, so they are deliberately not part of the params class: a rating
        // shared by every input under the cascade is never what a consumer means.
        var properties = typeof(BitRatingParams).GetProperties();

        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitRating.Value)));
        Assert.IsNull(Array.Find(properties, p => p.Name == nameof(BitRating.DefaultValue)));
    }

    [TestMethod]
    public void BitRatingParamsShouldCarryEveryPlainParameterOfTheComponent()
    {
        // A parameter added to the component without its counterpart here is one a BitParams cascade silently
        // ignores. Callbacks, templates and the cascade itself are the ones deliberately left out.
        var paramsProperties = typeof(BitRatingParams).GetProperties().Select(p => p.Name).ToHashSet();

        var missing = typeof(BitRating).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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
