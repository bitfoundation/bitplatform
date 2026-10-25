using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Covers the BitParams cascade of the TimeZonePicker: what a BitTimeZonePickerParams fills in - for a picker of any
/// value type alike - what it leaves alone because the field wrote it for itself, and that what the field derives from
/// its parameters (its classes, its list and its texts) follows the cascaded values.
/// </summary>
[TestClass]
public class BitTimeZonePickerParamsTests : BunitTestContext
{
    // What belongs to a single field rather than to a group of them: what identifies it and carries its value, the state
    // it two-way binds, what says something about its value alone, and its event callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTimeZonePicker<string>.CascadingParameters),
        nameof(BitTimeZonePicker<string>.DefaultValue),
        nameof(BitTimeZonePicker<string>.DisplayName),
        nameof(BitTimeZonePicker<string>.ErrorMessage),
        nameof(BitTimeZonePicker<string>.ErrorMessageTemplate),
        nameof(BitTimeZonePicker<string>.InputHtmlAttributes),
        nameof(BitTimeZonePicker<string>.Invalid),
        nameof(BitTimeZonePicker<string>.IsOpen),
        nameof(BitTimeZonePicker<string>.IsOpenChanged),
        nameof(BitTimeZonePicker<string>.Name),
        nameof(BitTimeZonePicker<string>.NoValidate),
        nameof(BitTimeZonePicker<string>.OnBlur),
        nameof(BitTimeZonePicker<string>.OnChange),
        nameof(BitTimeZonePicker<string>.OnClose),
        nameof(BitTimeZonePicker<string>.OnFocus),
        nameof(BitTimeZonePicker<string>.OnOpen),
        nameof(BitTimeZonePicker<string>.OnSearch),
        nameof(BitTimeZonePicker<string>.Value),
        nameof(BitTimeZonePicker<string>.ValueChanged),
        nameof(BitTimeZonePicker<string>.ValueExpression),
    ];

    private static readonly List<BitTimeZone> ThreeTimeZones =
    [
        BitTimeZones.Find("America/Mexico_City")!,
        BitTimeZones.Find("America/New_York")!,
        BitTimeZones.Find("America/Los_Angeles")!,
    ];

    private IRenderedComponent<BitParams> RenderWithParams<TValue>(BitTimeZonePickerParams pickerParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { pickerParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitTimeZonePicker<TValue>>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitTimeZonePickerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitTimeZonePicker", BitTimeZonePickerParams.ParamName);
        Assert.AreEqual(BitTimeZonePickerParams.ParamName, new BitTimeZonePickerParams().Name);
    }

    [TestMethod]
    public void BitTimeZonePickerParamsShouldCarryEveryParameterThatBelongsToAGroupOfFields()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitTimeZonePicker<string>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                          .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                          .Select(p => p.Name)
                                                          .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitTimeZonePickerParams).GetProperty(name), $"BitTimeZonePickerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitTimeZonePickerParamsShouldOnlyCarryParametersOfTheField()
    {
        var own = typeof(BitTimeZonePickerParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                 .Select(p => p.Name)
                                                 .Where(n => n is not nameof(IBitComponentParams.Name));

        foreach (var name in own)
        {
            var target = typeof(BitTimeZonePicker<string>).GetProperty(name);

            Assert.IsNotNull(target?.GetCustomAttribute<ParameterAttribute>(), $"BitTimeZonePicker has no {name} parameter.");
            Assert.IsFalse(_notCascaded.Contains(name), $"{name} belongs to a single field and must not be cascaded.");
        }
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams<string>(new BitTimeZonePickerParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Background = BitColorKind.Secondary,
            Border = BitColorKind.Transparent,
            Underlined = true,
            Label = "Cascaded label",
            Placeholder = "Cascaded placeholder",
            Title = "Cascaded title",
            Class = "cascaded",
            Classes = new() { Field = "cascaded-field" },
            Styles = new() { Root = "margin:1px" },
        });

        var root = component.Find(".bit-tzp");

        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-bse"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-brn"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-und"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("Cascaded title", root.GetAttribute("title"));

        Assert.IsTrue(component.Find(".bit-tzp-fld").ClassList.Contains("cascaded-field"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-tzp-lbl").TextContent.Trim());
        Assert.AreEqual("Cascaded placeholder", component.Find(".bit-tzp-pls").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTakeTheCascadedValuesWhateverItIsBoundTo()
    {
        var component = RenderWithParams<TimeZoneInfo>(new BitTimeZonePickerParams { Size = BitSize.Small, Label = "Cascaded label" });

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-sm"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-tzp-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams<string>(new BitTimeZonePickerParams
        {
            Size = BitSize.Large,
            Label = "Cascaded label",
            Placeholder = "Cascaded placeholder",
            Class = "cascaded",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.Size), BitSize.Small);
            builder.AddAttribute(2, nameof(BitTimeZonePicker<string>.Placeholder), "Own placeholder");
            builder.AddAttribute(3, nameof(BitComponentBase.Class), "own");
        });

        var root = component.Find(".bit-tzp");

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-tzp-lg"));
        Assert.AreEqual("Own placeholder", component.Find(".bit-tzp-pls").TextContent.Trim());

        // What the field did not write for itself still comes from the cascade.
        Assert.AreEqual("Cascaded label", component.Find(".bit-tzp-lbl").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldApplyCascadedParametersOfTheInputBaseClass()
    {
        var component = RenderWithParams<string>(new BitTimeZonePickerParams { ReadOnly = true, Required = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.ReadOnly), false);
        });

        var field = component.Find("button.bit-tzp-fld");

        Assert.IsNull(field.GetAttribute("aria-readonly"));
        Assert.AreEqual("true", field.GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTakeACascadedTimeZoneListAndTexts()
    {
        var component = RenderWithParams<string>(new BitTimeZonePickerParams
        {
            TimeZones = ThreeTimeZones,
            PreferredTimeZones = ["America/New_York"],
            ReferenceDate = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            NoCountry = true,
            SearchBoxPlaceholder = "Buscar",
            NoResultsMessage = "Sin resultados",
        });

        component.Find("button.bit-tzp-fld").Click();

        var titles = component.FindAll("button.bit-tzp-itm").Select(i => i.GetAttribute("title")).ToArray();

        CollectionAssert.AreEqual(new[] { "(UTC-05:00) New York", "(UTC-08:00) Los Angeles", "(UTC-06:00) Mexico City" }, titles);
        Assert.AreEqual("Buscar", component.Find(".bit-tzp-srch").GetAttribute("placeholder"));

        component.Find(".bit-tzp-srch").Input("zzzz");

        Assert.AreEqual("Sin resultados", component.Find(".bit-tzp-nor").TextContent.Trim());
    }
}
