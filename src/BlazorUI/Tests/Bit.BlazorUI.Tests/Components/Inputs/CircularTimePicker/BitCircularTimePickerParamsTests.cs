using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.CircularTimePicker;

/// <summary>
/// Covers the BitParams cascade of the CircularTimePicker: what a BitCircularTimePickerParams fills in, what it leaves
/// alone because the picker wrote it for itself, and that the hooks of the parameters it fills in - the edit mode that
/// moves the dial, the seconds that take a ring away - run for the cascaded values exactly as they do for the markup.
/// </summary>
[TestClass]
public class BitCircularTimePickerParamsTests : BunitTestContext
{
    // What one picker alone is bound to - the open state and the callbacks it answers with - is written per picker, so
    // it has no place on the params object. Its label is carried, as it is by BitTimePickerParams and the params of
    // the other inputs that have one.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCircularTimePicker.CascadingParameters),
        nameof(BitCircularTimePicker.IsOpen),
        nameof(BitCircularTimePicker.OnClear),
        nameof(BitCircularTimePicker.OnClick),
        nameof(BitCircularTimePicker.OnClose),
        nameof(BitCircularTimePicker.OnFocus),
        nameof(BitCircularTimePicker.OnFocusIn),
        nameof(BitCircularTimePicker.OnFocusOut),
        nameof(BitCircularTimePicker.OnOpen),
        nameof(BitCircularTimePicker.OnSelectTime),
        nameof(BitCircularTimePicker.OnViewChange),
    ];

    private IRenderedComponent<BitParams> RenderCascaded(BitCircularTimePickerParams @params, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCircularTimePicker>(0);
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    // What a BitParams cascade does to a picker on a parameters-set, without the render plumbing in the way:
    // the values it carries are cascaded as fixed, so a change to them only reaches an already rendered
    // picker through this call - which is also the one that has to re-run the [CallOnSet] hooks it bypassed.
    private static void Cascade(IRenderedComponent<BitCircularTimePicker> component, BitCircularTimePickerParams @params)
    {
        component.InvokeAsync(() => @params.UpdateParameters(component.Instance)).GetAwaiter().GetResult();
    }

    [TestMethod]
    public void BitCircularTimePickerParamsShouldHaveTheCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitCircularTimePicker)}", BitCircularTimePickerParams.ParamName);
        Assert.AreEqual("BitParams.BitCircularTimePicker", BitCircularTimePickerParams.ParamName);
    }

    [TestMethod]
    public void BitCircularTimePickerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCircularTimePickerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCircularTimePickerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCircularTimePickerParamsShouldCarryEveryParameterThatIsNotPerPicker()
    {
        // What the input base declares (the value, its binding and validation) is per instance, and so is every
        // callback, the ones the source generator writes beside a two-way bound parameter included.
        var baseParameters = typeof(BitCircularTimePicker).BaseType!.GetProperties().Select(p => p.Name)
                                 .Concat(typeof(BitCircularTimePicker).GetProperties()
                                                       .Where(p => p.PropertyType == typeof(EventCallback) ||
                                                                   (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>)))
                                                       .Select(p => p.Name))
                                 .ToHashSet();

        var missing = typeof(BitCircularTimePicker).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                   .Select(p => p.Name)
                                                   .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                   .Where(n => typeof(BitCircularTimePickerParams).GetProperty(n) is null)
                                                   .ToArray();

        Assert.AreEqual(0, missing.Length, $"{nameof(BitCircularTimePickerParams)} is missing: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheCascadedParameters()
    {
        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            Size = BitSize.Large,
            Color = BitColor.Success,
            Underlined = true,
            HasBorder = false,
            ShowNowButton = true,
            ShowClearButton = true,
            AriaLabel = "Cascaded label"
        });

        var root = component.Find(".bit-ctp");

        Assert.IsTrue(root.ClassList.Contains("bit-ctp-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-ctp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-ctp-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-ctp-nbd"));
        Assert.AreEqual(2, component.FindAll(".bit-ctp-abn").Count);
        Assert.AreEqual("Cascaded label", component.Find(".bit-ctp-inp").GetAttribute("aria-label"));

        // The callout is a sibling of the root, so it carries the cascaded accent and size itself.
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains("bit-ctp-lg"));
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains("bit-ctp-suc"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldLetItsOwnParametersWinOverTheCascadedOnes()
    {
        var component = RenderCascaded(
            new BitCircularTimePickerParams
            {
                Size = BitSize.Large,
                Color = BitColor.Success,
                Standalone = true,
                ShowClearButton = true
            },
            builder =>
            {
                builder.AddAttribute(1, nameof(BitCircularTimePicker.Size), BitSize.Small);
                builder.AddAttribute(2, nameof(BitCircularTimePicker.Color), BitColor.Error);
            });

        var root = component.Find(".bit-ctp");

        Assert.IsTrue(root.ClassList.Contains("bit-ctp-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-ctp-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-ctp-lg"));
        Assert.IsFalse(root.ClassList.Contains("bit-ctp-suc"));

        // What the markup left alone still comes from the cascade.
        Assert.IsTrue(root.ClassList.Contains("bit-ctp-sta"));
        Assert.AreEqual(1, component.FindAll(".bit-ctp-abn").Count);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheCascadedLabel()
    {
        var component = RenderCascaded(new BitCircularTimePickerParams { Label = "Cascaded label" });

        var label = component.Find(".bit-ctp-lbl");
        Assert.AreEqual("Cascaded label", label.TextContent.Trim());
        Assert.AreEqual(label.Id, component.Find(".bit-ctp-inp").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheCascadedLabelTemplateAheadOfItsOwnLabel()
    {
        // A template, from the markup or the cascade, is what the picker renders in place of a plain label; the
        // picker's own Label still wins over a cascaded one.
        var component = RenderCascaded(
            new BitCircularTimePickerParams
            {
                Label = "Cascaded label",
                LabelTemplate = builder => builder.AddMarkupContent(0, "<b class=\"cascaded-template\">Template</b>")
            },
            builder => builder.AddAttribute(1, nameof(BitCircularTimePicker.Label), "Own label"));

        var picker = component.FindComponent<BitCircularTimePicker>().Instance;
        Assert.AreEqual("Own label", picker.Label);
        Assert.IsNotNull(picker.LabelTemplate);
        Assert.AreEqual(1, component.FindAll(".cascaded-template").Count);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyTheCascadedReadOnlyAndRequired()
    {
        // The params object is an input's, so the two parameters BitInputBaseParams carries for every input reach the
        // picker too, and a value the picker wrote for itself - a false included - is left alone.
        var cascaded = RenderCascaded(new BitCircularTimePickerParams { ReadOnly = true, Required = true });

        var picker = cascaded.FindComponent<BitCircularTimePicker>().Instance;
        Assert.IsTrue(picker.ReadOnly);
        Assert.IsTrue(picker.Required);

        var own = RenderCascaded(new BitCircularTimePickerParams { ReadOnly = true, Required = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCircularTimePicker.ReadOnly), false);
            builder.AddAttribute(2, nameof(BitCircularTimePicker.Required), false);
        });

        var ownPicker = own.FindComponent<BitCircularTimePicker>().Instance;
        Assert.IsFalse(ownPicker.ReadOnly);
        Assert.IsFalse(ownPicker.Required);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyEveryCascadedParameter()
    {
        var culture = CultureInfo.InvariantCulture;
        Func<int, bool> allowedHours = _ => true;
        Func<int, bool> allowedMinutes = _ => true;
        Func<int, bool> allowedSeconds = _ => true;

        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            AllowTextInput = true,
            AllowedHours = allowedHours,
            AllowedMinutes = allowedMinutes,
            AllowedSeconds = allowedSeconds,
            AmPmInClock = true,
            AutoClose = true,
            AutoCloseDelay = 250,
            CalloutAriaLabel = "Cascaded clock",
            ClearButtonText = "Cascaded clear",
            CloseButtonIconName = "Cascaded icon",
            CloseButtonTitle = "Cascaded close",
            Color = BitColor.Warning,
            Culture = culture,
            DisableFuture = true,
            DisablePast = true,
            DropDirection = BitDropDirection.All,
            DisallowedTimeErrorMessage = "Cascaded disallowed",
            EditMode = BitCircularTimePickerEditMode.OnlyMinutes,
            HasBorder = false,
            HourButtonTitle = "Cascaded hour",
            HourStep = 2,
            IconPlacement = BitPlacement.Start,
            IconName = "Cascaded clock icon",
            InvalidErrorMessage = "Cascaded invalid",
            InvertMouseWheel = true,
            Landscape = true,
            MaxTime = new TimeSpan(18, 0, 0),
            MinTime = new TimeSpan(6, 0, 0),
            MinuteButtonTitle = "Cascaded minute",
            MinuteStep = 5,
            NoMouseWheel = true,
            NowButtonText = "Cascaded now",
            OutOfRangeErrorMessage = "Cascaded out of range",
            Placeholder = "Cascaded placeholder",
            Responsive = true,
            SecondButtonTitle = "Cascaded second",
            SecondStep = 10,
            ShowClearButton = true,
            ShowCloseButton = true,
            ShowNowButton = true,
            ShowSeconds = true,
            Size = BitSize.Small,
            StartingValue = new TimeSpan(9, 30, 0),
            StartView = BitCircularTimePickerView.Minute,
            TimeFormat = BitTimeFormat.TwelveHours,
            Underlined = true,
            ValueFormat = "HH:mm:ss"
        });

        var picker = component.FindComponent<BitCircularTimePicker>().Instance;

        Assert.IsTrue(picker.AllowTextInput);
        Assert.AreSame(allowedHours, picker.AllowedHours);
        Assert.AreSame(allowedMinutes, picker.AllowedMinutes);
        Assert.AreSame(allowedSeconds, picker.AllowedSeconds);
        Assert.IsTrue(picker.AmPmInClock);
        Assert.IsTrue(picker.AutoClose);
        Assert.AreEqual(250, picker.AutoCloseDelay);
        Assert.AreEqual("Cascaded clock", picker.CalloutAriaLabel);
        Assert.AreEqual("Cascaded clear", picker.ClearButtonText);
        Assert.AreEqual("Cascaded icon", picker.CloseButtonIconName);
        Assert.AreEqual("Cascaded close", picker.CloseButtonTitle);
        Assert.AreEqual(BitColor.Warning, picker.Color);
        Assert.AreSame(culture, picker.Culture);
        Assert.IsTrue(picker.DisableFuture);
        Assert.IsTrue(picker.DisablePast);
        Assert.AreEqual(BitDropDirection.All, picker.DropDirection);
        Assert.AreEqual("Cascaded disallowed", picker.DisallowedTimeErrorMessage);
        Assert.AreEqual(BitCircularTimePickerEditMode.OnlyMinutes, picker.EditMode);
        Assert.IsFalse(picker.HasBorder);
        Assert.AreEqual("Cascaded hour", picker.HourButtonTitle);
        Assert.AreEqual(2, picker.HourStep);
        Assert.AreEqual(BitPlacement.Start, picker.IconPlacement);
        Assert.AreEqual("Cascaded clock icon", picker.IconName);
        Assert.AreEqual("Cascaded invalid", picker.InvalidErrorMessage);
        Assert.IsTrue(picker.InvertMouseWheel);
        Assert.IsTrue(picker.Landscape);
        Assert.AreEqual(new TimeSpan(18, 0, 0), picker.MaxTime);
        Assert.AreEqual(new TimeSpan(6, 0, 0), picker.MinTime);
        Assert.AreEqual("Cascaded minute", picker.MinuteButtonTitle);
        Assert.AreEqual(5, picker.MinuteStep);
        Assert.IsTrue(picker.NoMouseWheel);
        Assert.AreEqual("Cascaded now", picker.NowButtonText);
        Assert.AreEqual("Cascaded out of range", picker.OutOfRangeErrorMessage);
        Assert.AreEqual("Cascaded placeholder", picker.Placeholder);
        Assert.IsTrue(picker.Responsive);
        Assert.AreEqual("Cascaded second", picker.SecondButtonTitle);
        Assert.AreEqual(10, picker.SecondStep);
        Assert.IsTrue(picker.ShowClearButton);
        Assert.IsTrue(picker.ShowCloseButton);
        Assert.IsTrue(picker.ShowNowButton);
        Assert.IsTrue(picker.ShowSeconds);
        Assert.AreEqual(BitSize.Small, picker.Size);
        Assert.AreEqual(new TimeSpan(9, 30, 0), picker.StartingValue);
        Assert.AreEqual(BitCircularTimePickerView.Minute, picker.StartView);
        Assert.AreEqual(BitTimeFormat.TwelveHours, picker.TimeFormat);
        Assert.IsTrue(picker.Underlined);
        Assert.AreEqual("HH:mm:ss", picker.ValueFormat);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldRunTheHooksOfTheCascadedParameters()
    {
        // The edit mode carries a hook that moves the dial onto the one part it edits, which the cascade
        // bypasses by assigning the property directly - so the hook has to be re-run from the cascade.
        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            Standalone = true,
            EditMode = BitCircularTimePickerEditMode.OnlyMinutes
        });

        Assert.AreEqual(BitCircularTimePickerView.Minute, component.FindComponent<BitCircularTimePicker>().Instance.View);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldKeepTheViewItWasMovedToAcrossARerenderOfTheCascade()
    {
        // Re-running the view hook on every parameters-set would drag the dial back to the hours whenever
        // anything around the picker re-renders.
        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            Standalone = true,
            StartView = BitCircularTimePickerView.Hour
        });

        var picker = component.FindComponent<BitCircularTimePicker>();

        picker.InvokeAsync(() => picker.Instance.SwitchView(BitCircularTimePickerView.Minute)).GetAwaiter().GetResult();

        component.Render();

        Assert.AreEqual(BitCircularTimePickerView.Minute, picker.Instance.View);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldKeepTheViewWhenTheCascadeOnlyTurnsTheSecondsOn()
    {
        // The seconds carry a hook of their own, which only moves the dial off a ring the picker has
        // stopped carrying - unlike the edit-mode hook, which starts the picker over. Running the wrong
        // one here would throw away the part of the time the dial had been moved on to.
        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var picker = component.Instance;

        Cascade(component, new BitCircularTimePickerParams { ShowSeconds = true });

        Assert.IsTrue(picker.ShowSeconds);
        Assert.AreEqual(BitCircularTimePickerView.Hour, picker.View);

        component.InvokeAsync(() => picker.SwitchView(BitCircularTimePickerView.Minute)).GetAwaiter().GetResult();

        // the cascade runs on every parameters-set, so a picker whose seconds it has already turned on must
        // not be started over again by the next re-render of the page around it
        Cascade(component, new BitCircularTimePickerParams { ShowSeconds = true });

        Assert.AreEqual(BitCircularTimePickerView.Minute, picker.View);

        // and a dial left on a ring the picker no longer carries still falls back
        component.InvokeAsync(() => picker.SwitchView(BitCircularTimePickerView.Second)).GetAwaiter().GetResult();

        Cascade(component, new BitCircularTimePickerParams { ShowSeconds = false });

        Assert.IsFalse(picker.ShowSeconds);
        Assert.AreEqual(BitCircularTimePickerView.Hour, picker.View);
    }

    [TestMethod]
    public void BitCircularTimePickerParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitCircularTimePickerParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            Placeholder = "Params placeholder"
        };

        var component = RenderComponent<BitCircularTimePicker>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.Placeholder, "Existing placeholder");
        });

        var instance = component.Instance;

        Cascade(component, @params);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing placeholder", instance.Placeholder);
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            Classes = new BitCircularTimePickerClassStyles { Root = "custom-root", Input = "custom-input", Callout = "custom-callout" },
            Styles = new BitCircularTimePickerClassStyles { Root = "color: red;", Input = "padding: 1px;", Callout = "margin: 2px;" },
        });

        Assert.IsTrue(component.Find(".bit-ctp").ClassList.Contains("custom-root"));
        Assert.IsTrue(component.Find(".bit-ctp").GetAttribute("style")!.Contains("color: red;"));
        Assert.IsTrue(component.Find(".bit-ctp-inp").ClassList.Contains("custom-input"));
        Assert.AreEqual("padding: 1px;", component.Find(".bit-ctp-inp").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains("custom-callout"));
        Assert.AreEqual("margin: 2px;", component.Find(".bit-ctp-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldApplyCascadedBaseParameters()
    {
        var component = RenderCascaded(new BitCircularTimePickerParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
        });

        var root = component.Find(".bit-ctp");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("margin: 1px;"));
        Assert.AreEqual("cascaded-id", root.Id);

        // The disabled state and the direction a cascade hands down reach the field and the callout the same way
        // written ones do.
        Assert.IsTrue(component.Find(".bit-ctp-inp").HasAttribute("disabled"));
        Assert.AreEqual("true", component.Find(".bit-ctp-inp").GetAttribute("aria-disabled"));
        Assert.IsTrue(component.Find(".bit-ctp-cal").ClassList.Contains("bit-dis"));
        Assert.AreEqual("rtl", component.Find(".bit-ctp-cal").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitCircularTimePickerShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderCascaded(new BitCircularTimePickerParams());

        var root = component.Find(".bit-ctp");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and the picker keeps its field.
        Assert.IsFalse(root.ClassList.Contains("bit-ctp-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-ctp-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-ctp-sta"));
        Assert.AreEqual(1, component.FindAll(".bit-ctp-inp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-ctp-abn").Count);
        Assert.AreEqual(BitCircularTimePickerView.Hour, component.FindComponent<BitCircularTimePicker>().Instance.View);
    }
}
