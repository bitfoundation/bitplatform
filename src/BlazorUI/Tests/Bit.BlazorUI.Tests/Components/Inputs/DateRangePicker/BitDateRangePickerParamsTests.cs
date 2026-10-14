using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

/// <summary>
/// Covers the BitParams cascade of the DateRangePicker: what a BitDateRangePickerParams fills in - every property it
/// carries, the ones of the input base and of the component base included - what it leaves alone because the picker
/// wrote it for itself, that the params object carries every parameter that is not the state of one picker, and that
/// the parameters the calendar is built from while initializing are in by then.
/// </summary>
[TestClass]
public class BitDateRangePickerParamsTests : BunitTestContext
{
    // Whether one picker's callout is open is the state of that picker, moved by its own clicks and keys.
    private static readonly string[] _notCascaded =
    [
        nameof(BitDateRangePicker.CascadingParameters),
        nameof(BitDateRangePicker.IsOpen),
    ];

    private static RenderFragment RenderDateRangePicker(Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<BitDateRangePicker>(0);
            extraAttributes?.Invoke(builder);
            builder.CloseComponent();
        };
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitDateRangePickerParams pickerParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { pickerParams });
            parameters.AddChildContent(RenderDateRangePicker(extraAttributes));
        });
    }

    [TestMethod]
    public void BitDateRangePickerParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitDateRangePicker)}", BitDateRangePickerParams.ParamName);
        Assert.AreEqual("BitParams.BitDateRangePicker", BitDateRangePickerParams.ParamName);
    }

    [TestMethod]
    public void BitDateRangePickerParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitDateRangePickerParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitDateRangePickerParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitDateRangePickerParamsShouldCarryEveryParameterThatIsNotPerPicker()
    {
        var baseParameters = typeof(BitInputBase<BitDateRangePickerValue?>).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitDateRangePicker).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                   .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                   // An event is wired per picker, never cascaded.
                                                   .Where(p => IsEventCallback(p.PropertyType) is false)
                                                   .Select(p => p.Name)
                                                   .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitDateRangePickerParams).GetProperty(name), $"BitDateRangePickerParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitDateRangePickerParamsUpdateParametersShouldSetAllProperties()
    {
        var classes = new BitDateRangePickerClassStyles { Root = "cascaded-root" };
        var styles = new BitDateRangePickerClassStyles { Root = "color: red;" };
        var culture = CultureInfo.InvariantCulture;
        var disabledDates = new[] { Utc(2024, 3, 10) };
        var disabledDaysOfWeek = new[] { DayOfWeek.Sunday };
        var highlightedDates = new[] { Utc(2024, 3, 12) };
        var presets = new[] { new BitDateRangePickerPreset { Text = "Cascaded preset", Value = new() { StartDate = Utc(2024, 3, 1), EndDate = Utc(2024, 3, 3) } } };
        var startingValue = new BitDateRangePickerValue { StartDate = Utc(2024, 3, 1), EndDate = Utc(2024, 3, 5) };
        var maxDate = Utc(2024, 4, 30);
        var minDate = Utc(2024, 2, 1);
        var today = Utc(2024, 3, 15);
        Func<DateTimeOffset, string?> getDayClass = _ => "cascaded-day";
        Func<DateTimeOffset, bool> isDateDisabled = d => d.Day == 20;
        RenderFragment footer = builder => builder.AddContent(0, "footer");
        RenderFragment header = builder => builder.AddContent(0, "header");
        RenderFragment iconTemplate = builder => builder.AddContent(0, "icon");
        RenderFragment labelTemplate = builder => builder.AddContent(0, "label");
        RenderFragment<DateTimeOffset> dayCell = d => builder => builder.AddContent(0, d.Day);
        RenderFragment<DateTimeOffset> monthCell = d => builder => builder.AddContent(0, d.Month);
        RenderFragment<int> yearCell = y => builder => builder.AddContent(0, y);
        var icon = BitIconInfo.Css("fa fa-calendar");
        var clearButtonIcon = BitIconInfo.Css("fa fa-x");
        var closeButtonIcon = BitIconInfo.Css("fa fa-close");
        var goToTodayIcon = BitIconInfo.Css("fa fa-today");
        var hideTimePickerIcon = BitIconInfo.Css("fa fa-hide");
        var showTimePickerIcon = BitIconInfo.Css("fa fa-show");
        var nextMonthNavIcon = BitIconInfo.Css("fa fa-nm");
        var nextYearNavIcon = BitIconInfo.Css("fa fa-ny");
        var nextYearRangeNavIcon = BitIconInfo.Css("fa fa-nyr");
        var prevMonthNavIcon = BitIconInfo.Css("fa fa-pm");
        var prevYearNavIcon = BitIconInfo.Css("fa fa-py");
        var prevYearRangeNavIcon = BitIconInfo.Css("fa fa-pyr");
        var startIncHourIcon = BitIconInfo.Css("fa fa-sih");
        var startDecHourIcon = BitIconInfo.Css("fa fa-sdh");
        var startIncMinuteIcon = BitIconInfo.Css("fa fa-sim");
        var startDecMinuteIcon = BitIconInfo.Css("fa fa-sdm");
        var endIncHourIcon = BitIconInfo.Css("fa fa-eih");
        var endDecHourIcon = BitIconInfo.Css("fa fa-edh");
        var endIncMinuteIcon = BitIconInfo.Css("fa fa-eim");
        var endDecMinuteIcon = BitIconInfo.Css("fa fa-edm");
        var calloutHtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" };

        var @params = new BitDateRangePickerParams
        {
            AllowTextInput = true,
            ApplyButtonText = "Cascaded apply",
            AutoApply = false,
            AutoClose = false,
            AutoFocus = true,
            CalloutAriaLabel = "Cascaded callout",
            CalloutFooterTemplate = footer,
            CalloutHeaderTemplate = header,
            CalloutHtmlAttributes = calloutHtmlAttributes,
            CancelButtonText = "Cascaded cancel",
            Classes = classes,
            ClearButtonIcon = clearButtonIcon,
            ClearButtonIconName = "ClearName",
            ClearButtonTitle = "Cascaded clear",
            CloseButtonIcon = closeButtonIcon,
            CloseButtonIconName = "CloseName",
            CloseButtonTitle = "Cascaded close",
            Color = BitColor.Warning,
            ContinuousSpinDelay = 500,
            ContinuousSpinInterval = 90,
            Culture = culture,
            DateFormat = "yyyy-MM-dd",
            DayCellTemplate = dayCell,
            DisableFuture = false,
            DisablePast = false,
            DisabledDateErrorMessage = "Cascaded disabled",
            DisabledDates = disabledDates,
            DisabledDaysOfWeek = disabledDaysOfWeek,
            DropDirection = BitDropDirection.All,
            EndTimeDecreaseHourIcon = endDecHourIcon,
            EndTimeDecreaseHourIconName = "EdhName",
            EndTimeDecreaseHourTitle = "Cascaded end dec hour",
            EndTimeDecreaseMinuteIcon = endDecMinuteIcon,
            EndTimeDecreaseMinuteIconName = "EdmName",
            EndTimeDecreaseMinuteTitle = "Cascaded end dec minute",
            EndTimeHourInputAriaLabel = "Cascaded end hour",
            EndTimeIncreaseHourIcon = endIncHourIcon,
            EndTimeIncreaseHourIconName = "EihName",
            EndTimeIncreaseHourTitle = "Cascaded end inc hour",
            EndTimeIncreaseMinuteIcon = endIncMinuteIcon,
            EndTimeIncreaseMinuteIconName = "EimName",
            EndTimeIncreaseMinuteTitle = "Cascaded end inc minute",
            EndTimeMinuteInputAriaLabel = "Cascaded end minute",
            ExcludeDisabledDates = true,
            FirstDayOfWeek = DayOfWeek.Monday,
            FixedWeeks = true,
            GetDayClass = getDayClass,
            GoToNextMonthTitle = "Cascaded next month",
            GoToNextYearRangeTitle = "Cascaded next year range",
            GoToNextYearTitle = "Cascaded next year",
            GoToPrevMonthTitle = "Cascaded prev month",
            GoToPrevYearRangeTitle = "Cascaded prev year range",
            GoToPrevYearTitle = "Cascaded prev year",
            GoToTodayIcon = goToTodayIcon,
            GoToTodayIconName = "TodayName",
            GoToTodayTitle = "Cascaded today",
            HasBorder = false,
            HideTimePickerIcon = hideTimePickerIcon,
            HideTimePickerIconName = "HideName",
            HideTimePickerTitle = "Cascaded hide time",
            HighlightCurrentMonth = true,
            HighlightSelectedMonth = true,
            HighlightToday = false,
            HighlightedDates = highlightedDates,
            HourStep = 2,
            Icon = icon,
            IconName = "IconName",
            IconPlacement = BitPlacement.Start,
            IconTemplate = iconTemplate,
            InvalidErrorMessage = "Cascaded invalid",
            IsDateDisabled = isDateDisabled,
            IsMonthPickerVisible = false,
            Label = "Cascaded label",
            LabelTemplate = labelTemplate,
            MaxDate = maxDate,
            MaxRange = TimeSpan.FromDays(10),
            MinDate = minDate,
            MinRange = TimeSpan.FromDays(1),
            MinuteStep = 5,
            MonthCellTemplate = monthCell,
            MonthCount = 2,
            MonthPickerToggleTitle = "Cascaded month toggle {0}",
            NextMonthNavIcon = nextMonthNavIcon,
            NextMonthNavIconName = "NmName",
            NextYearNavIcon = nextYearNavIcon,
            NextYearNavIconName = "NyName",
            NextYearRangeNavIcon = nextYearRangeNavIcon,
            NextYearRangeNavIconName = "NyrName",
            NoDateText = "n/a",
            OutOfRangeErrorMessage = "Cascaded range",
            PagedNavigation = true,
            Placeholder = "Cascaded placeholder",
            Presets = presets,
            PresetsAriaLabel = "Cascaded presets",
            PrevMonthNavIcon = prevMonthNavIcon,
            PrevMonthNavIconName = "PmName",
            PrevYearNavIcon = prevYearNavIcon,
            PrevYearNavIconName = "PyName",
            PrevYearRangeNavIcon = prevYearRangeNavIcon,
            PrevYearRangeNavIconName = "PyrName",
            Responsive = true,
            SelectedDateAriaAtomic = "Cascaded selected {0}",
            ShowClearButton = true,
            ShowCloseButton = true,
            ShowGoToToday = false,
            ShowMonthPickerAsOverlay = true,
            ShowOutsideDays = false,
            ShowTimePicker = true,
            ShowTimePickerAsOverlay = true,
            ShowTimePickerIcon = showTimePickerIcon,
            ShowTimePickerIconName = "ShowName",
            ShowTimePickerTitle = "Cascaded show time",
            ShowWeekNumbers = true,
            Size = BitSize.Small,
            Standalone = false,
            StartTimeDecreaseHourIcon = startDecHourIcon,
            StartTimeDecreaseHourIconName = "SdhName",
            StartTimeDecreaseHourTitle = "Cascaded start dec hour",
            StartTimeDecreaseMinuteIcon = startDecMinuteIcon,
            StartTimeDecreaseMinuteIconName = "SdmName",
            StartTimeDecreaseMinuteTitle = "Cascaded start dec minute",
            StartTimeHourInputAriaLabel = "Cascaded start hour",
            StartTimeIncreaseHourIcon = startIncHourIcon,
            StartTimeIncreaseHourIconName = "SihName",
            StartTimeIncreaseHourTitle = "Cascaded start inc hour",
            StartTimeIncreaseMinuteIcon = startIncMinuteIcon,
            StartTimeIncreaseMinuteIconName = "SimName",
            StartTimeIncreaseMinuteTitle = "Cascaded start inc minute",
            StartTimeMinuteInputAriaLabel = "Cascaded start minute",
            StartingValue = startingValue,
            Styles = styles,
            TimeFormat = BitTimeFormat.TwelveHours,
            TimeZone = TimeZoneInfo.Utc,
            Today = today,
            Underlined = true,
            ValueFormat = "{0} ~ {1}",
            WeekNumberRule = CalendarWeekRule.FirstFourDayWeek,
            WeekNumberTitle = "Cascaded week {0}",
            WeekNumbersHeaderTitle = "Cascaded weeks",
            YearCellTemplate = yearCell,
            YearPickerToggleTitle = "Cascaded year toggle {0}",
            YearRangePickerToggleTitle = "Cascaded year range toggle {0} {1}",
            AriaLabel = "Cascaded aria label",
            Class = "cascaded-class",
            Dir = BitDir.Ltr,
            Id = "cascaded-id",
            Style = "margin: 1px;",
            TabIndex = "5",
            Visibility = BitVisibility.Hidden,
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitDateRangePicker>().Instance;

        Assert.IsTrue(instance.AllowTextInput);
        Assert.AreEqual("Cascaded apply", instance.ApplyButtonText);
        Assert.IsFalse(instance.AutoApply);
        Assert.IsFalse(instance.AutoClose);
        Assert.IsTrue(instance.AutoFocus);
        Assert.AreEqual("Cascaded callout", instance.CalloutAriaLabel);
        Assert.AreSame(footer, instance.CalloutFooterTemplate);
        Assert.AreSame(header, instance.CalloutHeaderTemplate);
        // The cascade hands the component a copy of the dictionary, so an instance never writes into the shared one.
        CollectionAssert.AreEquivalent(calloutHtmlAttributes.ToList(), instance.CalloutHtmlAttributes.ToList());
        Assert.AreEqual("Cascaded cancel", instance.CancelButtonText);
        Assert.AreSame(classes, instance.Classes);
        Assert.AreSame(clearButtonIcon, instance.ClearButtonIcon);
        Assert.AreEqual("ClearName", instance.ClearButtonIconName);
        Assert.AreEqual("Cascaded clear", instance.ClearButtonTitle);
        Assert.AreSame(closeButtonIcon, instance.CloseButtonIcon);
        Assert.AreEqual("CloseName", instance.CloseButtonIconName);
        Assert.AreEqual("Cascaded close", instance.CloseButtonTitle);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual(500, instance.ContinuousSpinDelay);
        Assert.AreEqual(90, instance.ContinuousSpinInterval);
        Assert.AreSame(culture, instance.Culture);
        Assert.AreEqual("yyyy-MM-dd", instance.DateFormat);
        Assert.AreSame(dayCell, instance.DayCellTemplate);
        Assert.IsFalse(instance.DisableFuture);
        Assert.IsFalse(instance.DisablePast);
        Assert.AreEqual("Cascaded disabled", instance.DisabledDateErrorMessage);
        Assert.AreSame(disabledDates, instance.DisabledDates);
        Assert.AreSame(disabledDaysOfWeek, instance.DisabledDaysOfWeek);
        Assert.AreEqual(BitDropDirection.All, instance.DropDirection);
        Assert.AreSame(endDecHourIcon, instance.EndTimeDecreaseHourIcon);
        Assert.AreEqual("EdhName", instance.EndTimeDecreaseHourIconName);
        Assert.AreEqual("Cascaded end dec hour", instance.EndTimeDecreaseHourTitle);
        Assert.AreSame(endDecMinuteIcon, instance.EndTimeDecreaseMinuteIcon);
        Assert.AreEqual("EdmName", instance.EndTimeDecreaseMinuteIconName);
        Assert.AreEqual("Cascaded end dec minute", instance.EndTimeDecreaseMinuteTitle);
        Assert.AreEqual("Cascaded end hour", instance.EndTimeHourInputAriaLabel);
        Assert.AreSame(endIncHourIcon, instance.EndTimeIncreaseHourIcon);
        Assert.AreEqual("EihName", instance.EndTimeIncreaseHourIconName);
        Assert.AreEqual("Cascaded end inc hour", instance.EndTimeIncreaseHourTitle);
        Assert.AreSame(endIncMinuteIcon, instance.EndTimeIncreaseMinuteIcon);
        Assert.AreEqual("EimName", instance.EndTimeIncreaseMinuteIconName);
        Assert.AreEqual("Cascaded end inc minute", instance.EndTimeIncreaseMinuteTitle);
        Assert.AreEqual("Cascaded end minute", instance.EndTimeMinuteInputAriaLabel);
        Assert.IsTrue(instance.ExcludeDisabledDates);
        Assert.AreEqual(DayOfWeek.Monday, instance.FirstDayOfWeek);
        Assert.IsTrue(instance.FixedWeeks);
        Assert.AreSame(getDayClass, instance.GetDayClass);
        Assert.AreEqual("Cascaded next month", instance.GoToNextMonthTitle);
        Assert.AreEqual("Cascaded next year range", instance.GoToNextYearRangeTitle);
        Assert.AreEqual("Cascaded next year", instance.GoToNextYearTitle);
        Assert.AreEqual("Cascaded prev month", instance.GoToPrevMonthTitle);
        Assert.AreEqual("Cascaded prev year range", instance.GoToPrevYearRangeTitle);
        Assert.AreEqual("Cascaded prev year", instance.GoToPrevYearTitle);
        Assert.AreSame(goToTodayIcon, instance.GoToTodayIcon);
        Assert.AreEqual("TodayName", instance.GoToTodayIconName);
        Assert.AreEqual("Cascaded today", instance.GoToTodayTitle);
        Assert.IsFalse(instance.HasBorder);
        Assert.AreSame(hideTimePickerIcon, instance.HideTimePickerIcon);
        Assert.AreEqual("HideName", instance.HideTimePickerIconName);
        Assert.AreEqual("Cascaded hide time", instance.HideTimePickerTitle);
        Assert.IsTrue(instance.HighlightCurrentMonth);
        Assert.IsTrue(instance.HighlightSelectedMonth);
        Assert.IsFalse(instance.HighlightToday);
        Assert.AreSame(highlightedDates, instance.HighlightedDates);
        Assert.AreEqual(2, instance.HourStep);
        Assert.AreSame(icon, instance.Icon);
        Assert.AreEqual("IconName", instance.IconName);
        Assert.AreEqual(BitPlacement.Start, instance.IconPlacement);
        Assert.AreSame(iconTemplate, instance.IconTemplate);
        Assert.AreEqual("Cascaded invalid", instance.InvalidErrorMessage);
        Assert.AreSame(isDateDisabled, instance.IsDateDisabled);
        Assert.IsFalse(instance.IsMonthPickerVisible);
        Assert.AreEqual("Cascaded label", instance.Label);
        Assert.AreSame(labelTemplate, instance.LabelTemplate);
        Assert.AreEqual(maxDate, instance.MaxDate);
        Assert.AreEqual(TimeSpan.FromDays(10), instance.MaxRange);
        Assert.AreEqual(minDate, instance.MinDate);
        Assert.AreEqual(TimeSpan.FromDays(1), instance.MinRange);
        Assert.AreEqual(5, instance.MinuteStep);
        Assert.AreSame(monthCell, instance.MonthCellTemplate);
        Assert.AreEqual(2, instance.MonthCount);
        Assert.AreEqual("Cascaded month toggle {0}", instance.MonthPickerToggleTitle);
        Assert.AreSame(nextMonthNavIcon, instance.NextMonthNavIcon);
        Assert.AreEqual("NmName", instance.NextMonthNavIconName);
        Assert.AreSame(nextYearNavIcon, instance.NextYearNavIcon);
        Assert.AreEqual("NyName", instance.NextYearNavIconName);
        Assert.AreSame(nextYearRangeNavIcon, instance.NextYearRangeNavIcon);
        Assert.AreEqual("NyrName", instance.NextYearRangeNavIconName);
        Assert.AreEqual("n/a", instance.NoDateText);
        Assert.AreEqual("Cascaded range", instance.OutOfRangeErrorMessage);
        Assert.IsTrue(instance.PagedNavigation);
        Assert.AreEqual("Cascaded placeholder", instance.Placeholder);
        Assert.AreSame(presets, instance.Presets);
        Assert.AreEqual("Cascaded presets", instance.PresetsAriaLabel);
        Assert.AreSame(prevMonthNavIcon, instance.PrevMonthNavIcon);
        Assert.AreEqual("PmName", instance.PrevMonthNavIconName);
        Assert.AreSame(prevYearNavIcon, instance.PrevYearNavIcon);
        Assert.AreEqual("PyName", instance.PrevYearNavIconName);
        Assert.AreSame(prevYearRangeNavIcon, instance.PrevYearRangeNavIcon);
        Assert.AreEqual("PyrName", instance.PrevYearRangeNavIconName);
        Assert.IsTrue(instance.Responsive);
        Assert.AreEqual("Cascaded selected {0}", instance.SelectedDateAriaAtomic);
        Assert.IsTrue(instance.ShowClearButton);
        Assert.IsTrue(instance.ShowCloseButton);
        Assert.IsFalse(instance.ShowGoToToday);
        Assert.IsTrue(instance.ShowMonthPickerAsOverlay);
        Assert.IsFalse(instance.ShowOutsideDays);
        Assert.IsTrue(instance.ShowTimePicker);
        Assert.IsTrue(instance.ShowTimePickerAsOverlay);
        Assert.AreSame(showTimePickerIcon, instance.ShowTimePickerIcon);
        Assert.AreEqual("ShowName", instance.ShowTimePickerIconName);
        Assert.AreEqual("Cascaded show time", instance.ShowTimePickerTitle);
        Assert.IsTrue(instance.ShowWeekNumbers);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.IsFalse(instance.Standalone);
        Assert.AreSame(startDecHourIcon, instance.StartTimeDecreaseHourIcon);
        Assert.AreEqual("SdhName", instance.StartTimeDecreaseHourIconName);
        Assert.AreEqual("Cascaded start dec hour", instance.StartTimeDecreaseHourTitle);
        Assert.AreSame(startDecMinuteIcon, instance.StartTimeDecreaseMinuteIcon);
        Assert.AreEqual("SdmName", instance.StartTimeDecreaseMinuteIconName);
        Assert.AreEqual("Cascaded start dec minute", instance.StartTimeDecreaseMinuteTitle);
        Assert.AreEqual("Cascaded start hour", instance.StartTimeHourInputAriaLabel);
        Assert.AreSame(startIncHourIcon, instance.StartTimeIncreaseHourIcon);
        Assert.AreEqual("SihName", instance.StartTimeIncreaseHourIconName);
        Assert.AreEqual("Cascaded start inc hour", instance.StartTimeIncreaseHourTitle);
        Assert.AreSame(startIncMinuteIcon, instance.StartTimeIncreaseMinuteIcon);
        Assert.AreEqual("SimName", instance.StartTimeIncreaseMinuteIconName);
        Assert.AreEqual("Cascaded start inc minute", instance.StartTimeIncreaseMinuteTitle);
        Assert.AreEqual("Cascaded start minute", instance.StartTimeMinuteInputAriaLabel);
        Assert.AreSame(startingValue, instance.StartingValue);
        Assert.AreSame(styles, instance.Styles);
        Assert.AreEqual(BitTimeFormat.TwelveHours, instance.TimeFormat);
        Assert.AreEqual(TimeZoneInfo.Utc, instance.TimeZone);
        Assert.AreEqual(today, instance.Today);
        Assert.IsTrue(instance.Underlined);
        Assert.AreEqual("{0} ~ {1}", instance.ValueFormat);
        Assert.AreEqual(CalendarWeekRule.FirstFourDayWeek, instance.WeekNumberRule);
        Assert.AreEqual("Cascaded week {0}", instance.WeekNumberTitle);
        Assert.AreEqual("Cascaded weeks", instance.WeekNumbersHeaderTitle);
        Assert.AreSame(yearCell, instance.YearCellTemplate);
        Assert.AreEqual("Cascaded year toggle {0}", instance.YearPickerToggleTitle);
        Assert.AreEqual("Cascaded year range toggle {0} {1}", instance.YearRangePickerToggleTitle);
        Assert.AreEqual("Cascaded aria label", instance.AriaLabel);
        Assert.AreEqual("cascaded-class", instance.Class);
        Assert.AreEqual(BitDir.Ltr, instance.Dir);
        Assert.AreEqual("cascaded-id", instance.Id);
        Assert.AreEqual("margin: 1px;", instance.Style);
        Assert.AreEqual("5", instance.TabIndex);
        Assert.AreEqual(BitVisibility.Hidden, instance.Visibility);

        // And the markup follows them: the base parameters on the root, the field and the callout.
        var root = component.Find(".bit-dtrp");
        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-wrn"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-sm"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-lic"));
        Assert.AreEqual("ltr", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
        StringAssert.Contains(root.GetAttribute("style"), "color: red;");
        StringAssert.Contains(root.GetAttribute("style"), "visibility:hidden");
        Assert.AreEqual("Cascaded placeholder", input.GetAttribute("placeholder"));
        Assert.AreEqual("Cascaded aria label", input.GetAttribute("aria-label"));
        Assert.AreEqual("5", input.GetAttribute("tabindex"));
        Assert.AreEqual("yes", component.Find(".bit-dtrp-cac").GetAttribute("data-cascaded"));
        Assert.AreEqual("Cascaded callout", component.Find(".bit-dtrp-cac").GetAttribute("aria-label"));
        // The starting value only opens the calendar on its month, so the time fields show its times while the field
        // itself holds no value yet.
        Assert.IsFalse(input.HasAttribute("value"));
        Assert.AreEqual("March 2024", component.Find(".bit-dtrp-grd").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitDateRangePickerParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            GoToTodayTitle = "Params title",
            MonthCount = 3,
            ShowClearButton = true,
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Error);
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.GoToTodayTitle, "Existing title");
            parameters.Add(p => p.MonthCount, 1);
            parameters.Add(p => p.ShowClearButton, false);
        });

        var instance = component.Instance;

        @params.UpdateParameters(instance);

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing title", instance.GoToTodayTitle);
        Assert.AreEqual(1, instance.MonthCount);
        // A false written on the picker is a choice too.
        Assert.IsFalse(instance.ShowClearButton);
    }

    [TestMethod]
    public void BitDateRangePickerParamsUpdateParametersShouldIgnoreANullPicker()
    {
        var @params = new BitDateRangePickerParams { Color = BitColor.Success };

        @params.UpdateParameters(null!);
    }

    [TestMethod]
    public void BitDateRangePickerShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitDateRangePickerParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            ShowWeekNumbers = true,
            Placeholder = "Cascaded placeholder",
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitDateRangePicker.Color), (BitColor?)BitColor.Error);
            builder.AddAttribute(2, nameof(BitDateRangePicker.Size), (BitSize?)BitSize.Small);
            builder.AddAttribute(3, nameof(BitDateRangePicker.Placeholder), "Own placeholder");
        });

        var root = component.Find(".bit-dtrp");

        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-err"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-lg"));
        Assert.AreEqual("Own placeholder", component.Find(".bit-dtrp-inp").GetAttribute("placeholder"));

        // What the picker left unset is still filled in from the cascade.
        Assert.IsTrue(component.FindAll(".bit-dtrp-wnm").Count > 0);
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyCascadedClassesAndStyles()
    {
        var component = RenderWithParams(new BitDateRangePickerParams
        {
            Classes = new BitDateRangePickerClassStyles { Root = "cascaded-root", Input = "cascaded-input", Callout = "cascaded-callout" },
            Styles = new BitDateRangePickerClassStyles { Root = "color: red;", Input = "color: blue;", Callout = "padding: 2px;" },
        });

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("cascaded-root"));
        StringAssert.Contains(component.Find(".bit-dtrp").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-dtrp-inp").ClassList.Contains("cascaded-input"));
        Assert.AreEqual("color: blue;", component.Find(".bit-dtrp-inp").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains("cascaded-callout"));
        Assert.AreEqual("padding: 2px;", component.Find(".bit-dtrp-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerParamsShouldApplyTheCultureBeforeTheCalendarIsBuilt()
    {
        var component = RenderWithParams(new BitDateRangePickerParams
        {
            Culture = CultureInfo.GetCultureInfo("fa-IR"),
            TimeZone = TimeZoneInfo.Utc,
            StartingValue = new BitDateRangePickerValue { StartDate = Utc(2025, 3, 15) },
        });

        // A right-to-left culture implies the direction of the picker and lays the calendar out in its own calendar,
        // which is built while initializing - before OnParametersSet has ever run - so a cascaded culture has to be
        // in by then.
        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dtrp-rtl"));
        Assert.AreEqual("1403", component.Find(".bit-dtrp-mwp .bit-dtrp-ptb").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerCascadingParametersShouldNotResetTheViewOnEveryRender()
    {
        // One value for every render of the ancestor: a new instance would be a new value, which the picker rightly
        // moves its calendar to.
        var value = new BitDateRangePickerValue { StartDate = Utc(2026, 1, 15), EndDate = Utc(2026, 1, 20) };

        var component = RenderWithParams(new BitDateRangePickerParams
        {
            FixedWeeks = true,
            Standalone = true,
            IsMonthPickerVisible = false,
            Culture = CultureInfo.GetCultureInfo("en-US"),
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitDateRangePicker.Value), value);
        });

        component.Find(".bit-dtrp-dwp [title='Go to next month']").Click();

        Assert.AreEqual("February 2026", component.Find(".bit-dtrp-grd").GetAttribute("aria-label"));

        // The ancestor holding the BitParams rerenders and cascades exactly what it cascaded before, which
        // must leave the calendar on the month it was navigated to.
        component.Render();

        Assert.AreEqual("February 2026", component.Find(".bit-dtrp-grd").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldTakeTheCalloutHtmlAttributesFromTheCascadeUnlessItWroteItsOwn()
    {
        var component = RenderWithParams(new BitDateRangePickerParams
        {
            CalloutHtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" }
        });

        Assert.AreEqual("yes", component.Find(".bit-dtrp-cac").GetAttribute("data-cascaded"));

        // A dictionary the picker wrote for itself is its choice, which the cascade leaves alone.
        var own = RenderWithParams(new BitDateRangePickerParams
        {
            CalloutHtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes" }
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitDateRangePicker.CalloutHtmlAttributes), new Dictionary<string, object> { ["data-own"] = "yes" });
        });

        Assert.AreEqual("yes", own.Find(".bit-dtrp-cac").GetAttribute("data-own"));
        Assert.IsFalse(own.Find(".bit-dtrp-cac").HasAttribute("data-cascaded"));
    }

    [TestMethod]
    public void BitDateRangePickerParamsShouldMergeTheHtmlAttributes()
    {
        var component = RenderWithParams(new BitDateRangePickerParams
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

        var root = component.Find(".bit-dtrp");

        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        // What the instance wrote for itself is never replaced by the cascade.
        Assert.AreEqual("from-instance", root.GetAttribute("data-both"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitDateRangePickerParams());

        var instance = component.FindComponent<BitDateRangePicker>().Instance;
        var root = component.Find(".bit-dtrp");

        Assert.IsNull(instance.Color);
        Assert.IsNull(instance.Size);
        Assert.AreEqual(1, instance.MonthCount);
        Assert.IsTrue(instance.AutoApply);
        Assert.IsTrue(instance.HasBorder);
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-und"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-nbd"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-sm"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-lg"));
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp").Count);
    }

    private static bool IsEventCallback(Type type)
    {
        return type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }

    private static DateTimeOffset Utc(int year, int month, int day) => new(new DateTime(year, month, day), TimeSpan.Zero);
}
