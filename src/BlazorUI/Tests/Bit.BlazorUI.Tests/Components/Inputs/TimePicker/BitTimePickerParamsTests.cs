using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

/// <summary>
/// Covers the BitParams cascade of the TimePicker: what a BitTimePickerParams fills in, what it leaves
/// alone because the picker wrote it for itself, and that the parameters the component reads in a hook of
/// its own - the culture - are still read after the cascade has filled them in.
/// </summary>
[TestClass]
public class BitTimePickerParamsTests : BunitTestContext
{
    private static RenderFragment RenderTimePicker(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitTimePicker>(0);
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    [TestMethod]
    public void BitTimePickerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitTimePicker)}", BitTimePickerParams.ParamName);
        Assert.AreEqual("BitParams.BitTimePicker", BitTimePickerParams.ParamName);
    }

    [TestMethod]
    public void BitTimePickerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitTimePickerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitTimePickerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitTimePickerShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTimePickerParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                Underlined = true,
                HasBorder = false,
                Label = "Cascaded label",
                Placeholder = "Cascaded placeholder",
                TimeFormat = BitTimeFormat.TwelveHours,
                MinuteStep = 15,
                ShowNowButton = true,
                ShowClearButton = true,
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderTimePicker());
        });

        var root = component.Find(".bit-tpc");
        var instance = component.FindComponent<BitTimePicker>().Instance;

        Assert.IsTrue(root.ClassList.Contains("bit-tpc-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-tpc-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-tpc-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-tpc-nbd"));
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual("Cascaded placeholder", instance.Placeholder);
        Assert.AreEqual(BitTimeFormat.TwelveHours, instance.TimeFormat);
        Assert.AreEqual(15, instance.MinuteStep);

        // The cascaded flags reach the markup, not only the instance.
        Assert.AreEqual(2, component.FindAll(".bit-tpc-abn").Count);
        Assert.AreEqual(2, component.FindAll(".bit-tpc-bam, .bit-tpc-bpm").Count);
    }

    [TestMethod]
    public void BitTimePickerShouldApplyTheCascadedReadOnlyAndRequired()
    {
        // The params object is an input's, so the two parameters BitInputBaseParams carries for every input reach the
        // picker too, and a value the picker wrote for itself - a false included - is left alone.
        var cascaded = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTimePickerParams { ReadOnly = true, Required = true } });
            parameters.AddChildContent(RenderTimePicker());
        });

        var picker = cascaded.FindComponent<BitTimePicker>().Instance;
        Assert.IsTrue(picker.ReadOnly);
        Assert.IsTrue(picker.Required);

        var own = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTimePickerParams { ReadOnly = true, Required = true } });
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.ReadOnly), false);
                builder.AddAttribute(2, nameof(BitTimePicker.Required), false);
            }));
        });

        var ownPicker = own.FindComponent<BitTimePicker>().Instance;
        Assert.IsFalse(ownPicker.ReadOnly);
        Assert.IsFalse(ownPicker.Required);
    }

    [TestMethod]
    public void BitTimePickerDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTimePickerParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                MinuteStep = 15,
                Label = "Cascaded label",
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitTimePicker.MinuteStep), 5);
            }));
        });

        var root = component.Find(".bit-tpc");
        var instance = component.FindComponent<BitTimePicker>().Instance;

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(root.ClassList.Contains("bit-tpc-err"));
        Assert.AreEqual(5, instance.MinuteStep);

        // What the picker left unset is still filled in from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-tpc-lg"));
        Assert.AreEqual("Cascaded label", instance.Label);
    }

    [TestMethod]
    public void BitTimePickerParamsShouldApplyTheCultureBeforeTheComponentReadsIt()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTimePickerParams { Culture = CultureInfo.GetCultureInfo("fa-IR") }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.Value), new TimeSpan(13, 5, 0));
            }));
        });

        // A right-to-left culture implies the direction of the picker, which is only read in the hook the
        // cascade has to run again after it has filled the culture in.
        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitTimePickerParamsUpdateParametersShouldSetAllProperties()
    {
        var @params = new BitTimePickerParams
        {
            AllowTextInput = true,
            AriaDescription = "Cascaded aria description",
            AutoAdvance = true,
            AutoClose = true,
            AutoFocus = false,
            CalloutAriaLabel = "Cascaded callout",
            ClearButtonText = "Cascaded clear",
            ClearButtonTitle = "Cascaded clear title",
            CloseButtonTitle = "Cascaded close",
            Color = BitColor.Warning,
            ContinuousSpinDelay = 111,
            ContinuousSpinInterval = 22,
            Description = "Cascaded description",
            DisallowedTimeErrorMessage = "Cascaded disallowed",
            DisableFuture = true,
            DropDirection = BitDropDirection.All,
            HasBorder = false,
            HourInputAriaLabel = "Cascaded hour",
            HourStep = 2,
            IconName = "AlarmClock",
            IconPlacement = BitPlacement.Start,
            InvalidErrorMessage = "Cascaded invalid",
            InvertMouseWheel = true,
            Label = "Cascaded label",
            MaxTime = new TimeSpan(18, 0, 0),
            MinTime = new TimeSpan(8, 0, 0),
            MinuteInputAriaLabel = "Cascaded minute",
            MinuteStep = 15,
            NoMouseWheel = true,
            Now = new TimeSpan(10, 15, 0),
            NowButtonText = "Cascaded now",
            OutOfRangeErrorMessage = "Cascaded out of range",
            Placeholder = "Cascaded placeholder",
            Prefix = "Cascaded prefix",
            Responsive = true,
            SecondInputAriaLabel = "Cascaded second",
            SecondStep = 30,
            ShowClearButton = true,
            ShowCloseButton = true,
            ShowInputClearButton = true,
            ShowNowButton = true,
            ShowSeconds = true,
            Size = BitSize.Small,
            StartingValue = new TimeSpan(9, 30, 0),
            Suffix = "Cascaded suffix",
            TimeFormat = BitTimeFormat.TwelveHours,
            TimeZone = TimeZoneInfo.Utc,
            Underlined = true,
            ValueFormat = "hh-mm.ss",
            AriaLabel = "Cascaded aria label",
            TabIndex = "5",
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(RenderTimePicker());
        });

        var instance = component.FindComponent<BitTimePicker>().Instance;

        Assert.IsTrue(instance.AllowTextInput);
        Assert.AreEqual("Cascaded aria description", instance.AriaDescription);
        Assert.IsTrue(instance.AutoAdvance);
        Assert.IsTrue(instance.AutoClose);
        Assert.IsFalse(instance.AutoFocus);
        Assert.AreEqual("Cascaded callout", instance.CalloutAriaLabel);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonText);
        Assert.AreEqual("Cascaded clear title", instance.ClearButtonTitle);
        Assert.AreEqual("Cascaded close", instance.CloseButtonTitle);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual(111, instance.ContinuousSpinDelay);
        Assert.AreEqual(22, instance.ContinuousSpinInterval);
        Assert.AreEqual("Cascaded description", instance.Description);
        Assert.AreEqual("Cascaded disallowed", instance.DisallowedTimeErrorMessage);
        Assert.IsTrue(instance.DisableFuture);
        Assert.AreEqual(BitDropDirection.All, instance.DropDirection);
        Assert.IsFalse(instance.HasBorder);
        Assert.AreEqual("Cascaded hour", instance.HourInputAriaLabel);
        Assert.AreEqual(2, instance.HourStep);
        Assert.AreEqual("AlarmClock", instance.IconName);
        Assert.AreEqual(BitPlacement.Start, instance.IconPlacement);
        Assert.AreEqual("Cascaded invalid", instance.InvalidErrorMessage);
        Assert.IsTrue(instance.InvertMouseWheel);
        Assert.IsTrue(instance.NoMouseWheel);
        Assert.AreEqual(new TimeSpan(10, 15, 0), instance.Now);
        Assert.AreEqual("Cascaded prefix", instance.Prefix);
        Assert.AreEqual("Cascaded suffix", instance.Suffix);
        Assert.AreEqual(TimeZoneInfo.Utc, instance.TimeZone);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreEqual(new TimeSpan(18, 0, 0), instance.MaxTime);
        Assert.AreEqual(new TimeSpan(8, 0, 0), instance.MinTime);
        Assert.AreEqual("Cascaded minute", instance.MinuteInputAriaLabel);
        Assert.AreEqual(15, instance.MinuteStep);
        Assert.AreEqual("Cascaded now", instance.NowButtonText);
        Assert.AreEqual("Cascaded out of range", instance.OutOfRangeErrorMessage);
        Assert.AreEqual("Cascaded placeholder", instance.Placeholder);
        Assert.IsTrue(instance.Responsive);
        Assert.AreEqual("Cascaded second", instance.SecondInputAriaLabel);
        Assert.AreEqual(30, instance.SecondStep);
        Assert.IsTrue(instance.ShowClearButton);
        Assert.IsTrue(instance.ShowCloseButton);
        Assert.IsTrue(instance.ShowInputClearButton);
        Assert.IsTrue(instance.ShowNowButton);
        Assert.IsTrue(instance.ShowSeconds);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(new TimeSpan(9, 30, 0), instance.StartingValue);
        Assert.AreEqual(BitTimeFormat.TwelveHours, instance.TimeFormat);
        Assert.IsTrue(instance.Underlined);
        Assert.AreEqual("hh-mm.ss", instance.ValueFormat);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitTimePickerParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitTimePickerParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Label = "Params label",
            MinuteStep = 15,
        };

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Label, "Existing label");
            parameters.Add(p => p.MinuteStep, 5);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing label", instance.Label);
        Assert.AreEqual(5, instance.MinuteStep);
    }

    [TestMethod]
    public void BitTimePickerParamsUpdateParametersShouldIgnoreANullPicker()
    {
        var @params = new BitTimePickerParams { Color = BitColor.Success };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitTimePickerParamsShouldMergeTheCalloutHtmlAttributes()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTimePickerParams
            {
                CalloutHtmlAttributes = new Dictionary<string, object>
                {
                    ["data-cascaded"] = "yes",
                    ["data-both"] = "from-cascade",
                }
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.CalloutHtmlAttributes), new Dictionary<string, object>
                {
                    ["data-both"] = "from-instance",
                });
            }));
        });

        var callout = component.Find(".bit-tpc-cac");

        Assert.AreEqual("yes", callout.GetAttribute("data-cascaded"));
        // What the instance wrote for itself is never replaced by the cascade.
        Assert.AreEqual("from-instance", callout.GetAttribute("data-both"));
    }

    [TestMethod]
    public void TheCalloutHtmlAttributesOfTheInstanceShouldWinHoweverEitherCasesThem()
    {
        var own = new Dictionary<string, object> { ["Data-Both"] = "from-instance" };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [new BitTimePickerParams
            {
                CalloutHtmlAttributes = new Dictionary<string, object> { ["data-both"] = "from-cascade", ["data-cascaded"] = "yes" }
            }]);
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.CalloutHtmlAttributes), own);
            }));
        });

        var callout = component.Find(".bit-tpc-cac");

        Assert.AreEqual("from-instance", callout.GetAttribute("data-both"));
        Assert.AreEqual("yes", callout.GetAttribute("data-cascaded"));
        // The dictionary is the app's own, so the cascade is merged into a copy of it.
        Assert.HasCount(1, own);
    }

    [TestMethod]
    public void TheCalloutHtmlAttributesOfADroppedParamsObjectShouldGo()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, [new BitTimePickerParams
            {
                CalloutHtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" }
            }]);
            parameters.AddChildContent(RenderTimePicker());
        });

        Assert.AreEqual("yes", component.Find(".bit-tpc-cac").GetAttribute("data-cascaded"));

        component.Render(parameters => parameters.Add(p => p.Parameters, [new BitTimePickerParams { Label = "Label" }]));

        Assert.IsFalse(component.Find(".bit-tpc-cac").HasAttribute("data-cascaded"));
        Assert.IsEmpty(component.FindComponent<BitTimePicker>().Instance.CalloutHtmlAttributes);
    }

    // What belongs to one field alone - the open state of its callout and the rejection it is currently showing - is
    // written per picker, so it has no place on the params object; its value and callbacks neither.
    private static readonly string[] _notCascaded =
    [
        nameof(BitTimePicker.CascadingParameters),
        nameof(BitTimePicker.IsOpen),
        nameof(BitTimePicker.Invalid),
        nameof(BitTimePicker.ErrorMessage),
        nameof(BitTimePicker.ErrorMessageTemplate),
    ];

    private static bool IsCallback(Type type)
    {
        return type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }

    [TestMethod]
    public void BitTimePickerParamsShouldCarryEveryParameterThatIsNotPerPicker()
    {
        // A parameter added to the picker without being added here is one a BitParams ancestor silently cannot set.
        var parameters = typeof(BitTimePicker).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                                              .Where(p => p.GetCustomAttributes(typeof(ParameterAttribute), false).Length > 0)
                                              .Where(p => IsCallback(p.PropertyType) is false)
                                              .Select(p => p.Name)
                                              .Where(n => _notCascaded.Contains(n) is false)
                                              .ToList();

        var missing = parameters.Where(name => typeof(BitTimePickerParams).GetProperty(name) is null).ToList();

        Assert.AreEqual(0, missing.Count, "BitTimePickerParams is missing: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void BitTimePickerParamsShouldApplyTheRulesTemplatesAndIcons()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitTimePickerParams
            {
                Standalone = true,
                ShowSeconds = true,
                AllowedHours = h => h % 2 == 0,
                AllowedMinutes = m => m % 15 == 0,
                AllowedSeconds = s => s % 30 == 0,
                Icon = BitIconInfo.Css("my-clock"),
                IncreaseHourIcon = BitIconInfo.Css("my-hour-up"),
                DecreaseHourIcon = BitIconInfo.Css("my-hour-down"),
                IncreaseMinuteIconName = "Up",
                DecreaseMinuteIconName = "Down",
                IncreaseSecondIconName = "Up",
                DecreaseSecondIconName = "Down",
                CloseButtonIconName = "ChromeClose",
                ClearButtonIconName = "ChromeClose",
                CalloutHeaderTemplate = builder => builder.AddMarkupContent(0, "<b class=\"tpl-header\">h</b>"),
                CalloutFooterTemplate = builder => builder.AddMarkupContent(0, "<b class=\"tpl-footer\">f</b>"),
                DescriptionTemplate = builder => builder.AddMarkupContent(0, "<b class=\"tpl-description\">d</b>"),
                LabelTemplate = builder => builder.AddMarkupContent(0, "<b class=\"tpl-label\">l</b>"),
                Classes = new() { Root = "cascaded-root", HourInput = "cascaded-hour" },
                Styles = new() { Root = "--s: root;", HourInput = "--s: hour;" },
                Dir = BitDir.Rtl,
                Disabled = true,
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(RenderTimePicker(builder =>
            {
                builder.AddAttribute(1, nameof(BitTimePicker.Value), new TimeSpan(10, 30, 0));
                builder.AddAttribute(2, nameof(BitTimePicker.ValueChanged), EventCallback.Factory.Create<TimeSpan?>(this, (TimeSpan? _) => { }));
            }));
        });

        var instance = component.FindComponent<BitTimePicker>().Instance;
        var root = component.Find(".bit-tpc");

        Assert.IsTrue(root.ClassList.Contains("bit-tpc-sta"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "--s: root;");
        Assert.AreEqual(3, component.FindAll(".bit-tpc-tin").Count);

        var hourInput = component.FindAll(".bit-tpc-tin")[0];
        Assert.IsTrue(hourInput.ClassList.Contains("cascaded-hour"));
        Assert.AreEqual("--s: hour;", hourInput.GetAttribute("style"));

        Assert.IsTrue(component.Find("button[title='Increase hour'] > i").ClassList.Contains("my-hour-up"));
        Assert.IsTrue(component.Find("button[title='Decrease hour'] > i").ClassList.Contains("my-hour-down"));
        Assert.IsTrue(component.Find("button[title='Increase minute'] > i").ClassList.Contains("bit-icon--Up"));
        Assert.IsTrue(component.Find("button[title='Decrease second'] > i").ClassList.Contains("bit-icon--Down"));

        Assert.IsNotNull(component.Find(".bit-tpc-chd > .tpl-header"));
        Assert.IsNotNull(component.Find(".bit-tpc-cft > .tpl-footer"));
        Assert.IsNotNull(component.Find(".bit-tpc-des > .tpl-description"));
        Assert.IsNotNull(component.Find(".bit-tpc > label > .tpl-label"));

        // The predicates and the icons of the parts a standalone picker does not draw still reach the instance.
        Assert.IsNotNull(instance.AllowedHours);
        Assert.IsNotNull(instance.AllowedMinutes);
        Assert.IsNotNull(instance.AllowedSeconds);
        Assert.AreEqual("my-clock", instance.Icon?.GetCssClasses());
        Assert.AreEqual("ChromeClose", instance.CloseButtonIconName);
        Assert.AreEqual("ChromeClose", instance.ClearButtonIconName);
    }

    [TestMethod]
    public void BitTimePickerParamsShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitTimePickerParams() });
            parameters.AddChildContent(RenderTimePicker());
        });

        var root = component.Find(".bit-tpc");
        var instance = component.FindComponent<BitTimePicker>().Instance;

        Assert.AreEqual("bit-tpc", root.GetAttribute("class"));
        Assert.AreEqual(2, component.FindAll(".bit-tpc-tin").Count);
        Assert.AreEqual(BitTimeFormat.TwentyFourHours, instance.TimeFormat);
        Assert.AreEqual(1, instance.MinuteStep);
        Assert.AreEqual("Clock", instance.CalloutAriaLabel);
        Assert.IsNull(instance.Culture);
        Assert.AreEqual(0, component.FindAll(".bit-tpc-lbl").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tpc-act").Count);
    }
}
