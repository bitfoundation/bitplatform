using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PhoneInput;

/// <summary>
/// Covers the BitParams cascade of the PhoneInput: what a BitPhoneInputParams fills in, what it leaves alone
/// because the field wrote it for itself, and that what the field derives from its parameters (its classes, its
/// country, its list and its texts) follows the cascaded values.
/// </summary>
[TestClass]
public class BitPhoneInputParamsTests : BunitTestContext
{
    // What belongs to a single field rather than to a group of them: what identifies it and carries its value,
    // the state it two-way binds, what says something about its value alone, and its event callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitPhoneInput.AutoFocus),
        nameof(BitPhoneInput.CascadingParameters),
        nameof(BitPhoneInput.Country),
        nameof(BitPhoneInput.CountryChanged),
        nameof(BitPhoneInput.CountryName),
        nameof(BitPhoneInput.DefaultValue),
        nameof(BitPhoneInput.DisplayName),
        nameof(BitPhoneInput.ErrorMessage),
        nameof(BitPhoneInput.ErrorMessageTemplate),
        nameof(BitPhoneInput.InputHtmlAttributes),
        nameof(BitPhoneInput.Invalid),
        nameof(BitPhoneInput.IsOpen),
        nameof(BitPhoneInput.IsOpenChanged),
        nameof(BitPhoneInput.Name),
        nameof(BitPhoneInput.NoValidate),
        nameof(BitPhoneInput.Number),
        nameof(BitPhoneInput.NumberChanged),
        nameof(BitPhoneInput.OnBlur),
        nameof(BitPhoneInput.OnChange),
        nameof(BitPhoneInput.OnClear),
        nameof(BitPhoneInput.OnClick),
        nameof(BitPhoneInput.OnClose),
        nameof(BitPhoneInput.OnCountryChange),
        nameof(BitPhoneInput.OnEnter),
        nameof(BitPhoneInput.OnEscape),
        nameof(BitPhoneInput.OnFocus),
        nameof(BitPhoneInput.OnFocusIn),
        nameof(BitPhoneInput.OnFocusOut),
        nameof(BitPhoneInput.OnKeyDown),
        nameof(BitPhoneInput.OnOpen),
        nameof(BitPhoneInput.OnSearch),
        nameof(BitPhoneInput.Value),
        nameof(BitPhoneInput.ValueChanged),
        nameof(BitPhoneInput.ValueExpression),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitPhoneInputParams phoneParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { phoneParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPhoneInput>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitPhoneInputParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitPhoneInput", BitPhoneInputParams.ParamName);
    }

    [TestMethod]
    public void BitPhoneInputParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitPhoneInputParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitPhoneInputParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitPhoneInputParamsShouldCarryEveryParameterThatBelongsToAGroupOfFields()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitPhoneInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitPhoneInputParams).GetProperty(name), $"BitPhoneInputParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitPhoneInputParamsShouldOnlyCarryParametersOfTheField()
    {
        var own = typeof(BitPhoneInputParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                             .Select(p => p.Name)
                                             .Where(n => n is not nameof(IBitComponentParams.Name));

        foreach (var name in own)
        {
            var target = typeof(BitPhoneInput).GetProperty(name);

            Assert.IsNotNull(target?.GetCustomAttribute<ParameterAttribute>(), $"BitPhoneInput has no {name} parameter.");
            Assert.IsFalse(_notCascaded.Contains(name), $"{name} belongs to a single field and must not be cascaded.");
        }
    }

    [TestMethod]
    public void BitPhoneInputShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitPhoneInputParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Underlined = true,
            FullWidth = true,
            Label = "Cascaded label",
            Placeholder = "Cascaded placeholder",
            MaxLength = 12,
            Title = "Cascaded title",
            DropdownAriaLabel = "Land",
            ClearButtonAriaLabel = "Löschen",
            ShowClearButton = true,
            Class = "cascaded",
            Classes = new() { FieldGroup = "cascaded-field-group" },
            Styles = new() { Root = "margin:1px" },
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.DefaultCountry), BitCountries.Germany);
            builder.AddAttribute(2, nameof(BitPhoneInput.Value), "+491701234567");
        });

        var root = component.Find(".bit-phi");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-fwd"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("Cascaded title", root.GetAttribute("title"));

        Assert.IsTrue(component.Find(".bit-phi-fgp").ClassList.Contains("cascaded-field-group"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-phi-lbl").TextContent.Trim());

        var input = component.Find("input.bit-phi-inp");
        Assert.AreEqual("Cascaded placeholder", input.GetAttribute("placeholder"));
        Assert.AreEqual("12", input.GetAttribute("maxlength"));

        Assert.AreEqual("Land: Germany", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));
        Assert.AreEqual("Löschen", component.Find(".bit-phi-cbt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitPhoneInputShouldTakeTheCascadedColorKindsAndClearedAnnouncement()
    {
        var component = RenderWithParams(new BitPhoneInputParams
        {
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Transparent,
            ClearedAnnouncement = "Gelöscht",
            ShowClearButton = true,
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.DefaultCountry), BitCountries.Germany);
        });

        var root = component.Find(".bit-phi");

        Assert.IsTrue(root.ClassList.Contains("bit-phi-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-brn"));

        component.Find("input.bit-phi-inp").Change("1701234567");
        component.Find("button.bit-phi-cbt").Click();

        StringAssert.StartsWith(component.Find(".bit-phi-lvr").TextContent, "Gelöscht");
    }

    [TestMethod]
    public void BitPhoneInputShouldTakeACascadedDefaultCountry()
    {
        // The default country is applied in the first pass of the field, before OnParametersSet, so the cascade
        // has to have been read by then.
        string? value = null;

        var component = RenderWithParams(new BitPhoneInputParams { DefaultCountry = BitCountries.France }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.ValueChanged), EventCallback.Factory.Create<string?>(this, v => value = v));
        });

        Assert.AreEqual("Country: France", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));

        component.Find("input.bit-phi-inp").Change("612345678");

        Assert.AreEqual("+33612345678", value);
    }

    [TestMethod]
    public void BitPhoneInputShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitPhoneInputParams
        {
            Size = BitSize.Large,
            DefaultCountry = BitCountries.France,
            Placeholder = "Cascaded placeholder",
            Class = "cascaded",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.Size), BitSize.Small);
            builder.AddAttribute(2, nameof(BitPhoneInput.DefaultCountry), BitCountries.Japan);
            builder.AddAttribute(3, nameof(BitComponentBase.Class), "own");
        });

        var root = component.Find(".bit-phi");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-phi-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-phi-lg"));
        Assert.AreEqual("Country: Japan", component.Find("button.bit-phi-drp").GetAttribute("aria-label"));

        // What the field did not write for itself still comes from the cascade.
        Assert.AreEqual("Cascaded placeholder", component.Find("input.bit-phi-inp").GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitPhoneInputShouldApplyCascadedParametersOfTheInputBaseClasses()
    {
        // ReadOnly, Required, Immediate and AutoComplete are declared by the input base classes rather than by the
        // component, so they are tracked in sets of their own, apart from the parameters the component declares.
        var component = RenderWithParams(new BitPhoneInputParams
        {
            ReadOnly = true,
            Required = true,
            AutoComplete = "tel-national",
        });

        var input = component.Find("input.bit-phi-inp");

        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.IsTrue(input.HasAttribute("required"));
        Assert.AreEqual("tel-national", input.GetAttribute("autocomplete"));
    }

    [TestMethod]
    public void BitPhoneInputOwnInputBaseParametersShouldWinOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitPhoneInputParams { ReadOnly = true, Required = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.ReadOnly), false);
        });

        var input = component.Find("input.bit-phi-inp");

        Assert.IsFalse(input.HasAttribute("readonly"));
        Assert.IsTrue(input.HasAttribute("required"));
    }

    [TestMethod]
    public void BitPhoneInputShouldTakeACascadedCountryListAndPattern()
    {
        var component = RenderWithParams(new BitPhoneInputParams
        {
            Countries = [BitCountries.UnitedStates, BitCountries.Canada],
            Mask = "(###) ###-####",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitPhoneInput.DefaultCountry), BitCountries.UnitedStates);
        });

        component.Find("input.bit-phi-inp").Change("4155550123");

        Assert.AreEqual("(415) 555-0123", component.Find("input.bit-phi-inp").GetAttribute("value"));

        component.Find("button.bit-phi-drp").Click();

        Assert.AreEqual(2, component.FindAll("button.bit-phi-itm").Count);
    }
}
