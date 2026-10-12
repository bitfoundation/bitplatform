using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CountryPicker;

/// <summary>
/// Covers the BitParams cascade of the CountryPicker: what a BitCountryPickerParams fills in, what it leaves alone
/// because the field wrote it for itself, and that what the field derives from its parameters (its classes, its
/// starting country, its list and its texts) follows the cascaded values.
/// </summary>
[TestClass]
public class BitCountryPickerParamsTests : BunitTestContext
{
    // What belongs to a single field rather than to a group of them: what identifies it and carries its value,
    // the state it two-way binds, what says something about its value alone, and its event callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCountryPicker.CascadingParameters),
        nameof(BitCountryPicker.DefaultValue),
        nameof(BitCountryPicker.DisplayName),
        nameof(BitCountryPicker.ErrorMessage),
        nameof(BitCountryPicker.ErrorMessageTemplate),
        nameof(BitCountryPicker.InputHtmlAttributes),
        nameof(BitCountryPicker.Invalid),
        nameof(BitCountryPicker.IsOpen),
        nameof(BitCountryPicker.IsOpenChanged),
        nameof(BitCountryPicker.Name),
        nameof(BitCountryPicker.NoValidate),
        nameof(BitCountryPicker.OnBlur),
        nameof(BitCountryPicker.OnChange),
        nameof(BitCountryPicker.OnClose),
        nameof(BitCountryPicker.OnFocus),
        nameof(BitCountryPicker.OnOpen),
        nameof(BitCountryPicker.OnSearch),
        nameof(BitCountryPicker.Value),
        nameof(BitCountryPicker.ValueChanged),
        nameof(BitCountryPicker.ValueExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCountryPickerParams pickerParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { pickerParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCountryPicker>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCountryPickerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitCountryPicker", BitCountryPickerParams.ParamName);
        Assert.AreEqual(BitCountryPickerParams.ParamName, new BitCountryPickerParams().Name);
    }

    [TestMethod]
    public void BitCountryPickerParamsShouldCarryEveryParameterThatBelongsToAGroupOfFields()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCountryPicker).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                 .Select(p => p.Name)
                                                 .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCountryPickerParams).GetProperty(name), $"BitCountryPickerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitCountryPickerParamsShouldOnlyCarryParametersOfTheField()
    {
        var own = typeof(BitCountryPickerParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                .Select(p => p.Name)
                                                .Where(n => n is not nameof(IBitComponentParams.Name));

        foreach (var name in own)
        {
            var target = typeof(BitCountryPicker).GetProperty(name);

            Assert.IsNotNull(target?.GetCustomAttribute<ParameterAttribute>(), $"BitCountryPicker has no {name} parameter.");
            Assert.IsFalse(_notCascaded.Contains(name), $"{name} belongs to a single field and must not be cascaded.");
        }
    }

    [TestMethod]
    public void BitCountryPickerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitCountryPickerParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Transparent,
            Underlined = true,
            Label = "Cascaded label",
            Placeholder = "Cascaded placeholder",
            Title = "Cascaded title",
            NoFlags = true,
            Class = "cascaded",
            Classes = new() { Field = "cascaded-field" },
            Styles = new() { Root = "margin:1px" },
        });

        var root = component.Find(".bit-cnp");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-brn"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-und"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("Cascaded title", root.GetAttribute("title"));

        Assert.IsTrue(component.Find(".bit-cnp-fld").ClassList.Contains("cascaded-field"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-cnp-lbl").TextContent.Trim());
        Assert.AreEqual("Cascaded placeholder", component.Find(".bit-cnp-pls").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldTakeACascadedDefaultCountry()
    {
        // The default country is applied in the first pass of the field, before OnParametersSet, so the cascade has to
        // have been read by then.
        var component = RenderWithParams(new BitCountryPickerParams { DefaultCountry = BitCountries.France });

        Assert.AreEqual("France", component.Find(".bit-cnp-txt").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCountryPickerParams
        {
            Size = BitSize.Large,
            DefaultCountry = BitCountries.France,
            Label = "Cascaded label",
            Class = "cascaded",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCountryPicker.Size), BitSize.Small);
            builder.AddAttribute(2, nameof(BitCountryPicker.DefaultCountry), BitCountries.Japan);
            builder.AddAttribute(3, nameof(BitComponentBase.Class), "own");
        });

        var root = component.Find(".bit-cnp");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-cnp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-cnp-lg"));
        Assert.AreEqual("Japan", component.Find(".bit-cnp-txt").TextContent.Trim());

        // What the field did not write for itself still comes from the cascade.
        Assert.AreEqual("Cascaded label", component.Find(".bit-cnp-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void BitCountryPickerShouldApplyCascadedParametersOfTheInputBaseClass()
    {
        // ReadOnly and Required are declared by the input base class rather than by the component, so they are tracked
        // in a set of their own, apart from the parameters the component declares.
        var component = RenderWithParams(new BitCountryPickerParams { ReadOnly = true, Required = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCountryPicker.ReadOnly), false);
        });

        var field = component.Find("button.bit-cnp-fld");

        Assert.IsNull(field.GetAttribute("aria-readonly"));
        Assert.AreEqual("true", field.GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitCountryPickerShouldTakeACascadedCountryListAndTexts()
    {
        var component = RenderWithParams(new BitCountryPickerParams
        {
            Countries = [BitCountries.UnitedStates, BitCountries.Canada, BitCountries.Mexico],
            PreferredCountries = [BitCountries.Mexico],
            ExcludeCountries = [BitCountries.Canada],
            SearchBoxPlaceholder = "Buscar",
            NoResultsMessage = "Sin resultados",
        });

        component.Find("button.bit-cnp-fld").Click();

        var titles = component.FindAll("button.bit-cnp-itm").Select(i => i.GetAttribute("title")).ToArray();

        CollectionAssert.AreEqual(new[] { "Mexico", "United States" }, titles);
        Assert.AreEqual("Buscar", component.Find(".bit-cnp-srch").GetAttribute("placeholder"));

        component.Find(".bit-cnp-srch").Input("zzzz");

        Assert.AreEqual("Sin resultados", component.Find(".bit-cnp-nor").TextContent.Trim());
    }
}
