using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Calendar;

/// <summary>
/// Covers the BitParams cascade of the Calendar: what a BitCalendarParams fills in, what it leaves alone because the
/// calendar wrote it for itself, and that the view the calendar builds from its parameters - the months on screen, the
/// culture, the time zone - is rebuilt for the cascaded values, which reach it only after the first pass has run.
/// </summary>
[TestClass]
public class BitCalendarParamsTests : BunitTestContext
{
    // What a calendar answers with - the callbacks of a selection and of a month change - is written per calendar,
    // so it has no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCalendar.CascadingParameters),
        nameof(BitCalendar.OnMonthChange),
        nameof(BitCalendar.OnSelectDate),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitCalendarParams calendarParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { calendarParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCalendarParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitCalendar)}", BitCalendarParams.ParamName);
        Assert.AreEqual("BitParams.BitCalendar", BitCalendarParams.ParamName);
    }

    [TestMethod]
    public void BitCalendarParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCalendarParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCalendarParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCalendarParamsShouldCarryEveryParameterThatIsNotPerCalendar()
    {
        // What the input base declares (the value, its binding and validation) is per instance, and so is every
        // callback, the ones the source generator writes beside a two-way bound parameter included.
        var baseParameters = typeof(BitCalendar).BaseType!.GetProperties().Select(p => p.Name)
                                 .Concat(typeof(BitCalendar).GetProperties()
                                                       .Where(p => p.PropertyType == typeof(EventCallback) ||
                                                                   (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>)))
                                                       .Select(p => p.Name))
                                 .ToHashSet();

        var missing = typeof(BitCalendar).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                         .Select(p => p.Name)
                                         .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                         .Where(n => typeof(BitCalendarParams).GetProperty(n) is null)
                                         .ToArray();

        Assert.AreEqual(0, missing.Length, $"{nameof(BitCalendarParams)} is missing: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void BitCalendarShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            ShowWeekNumbers = true,
            ShowGoToToday = false,
            AriaLabel = "Cascaded Label"
        });

        var calendar = component.Find(".bit-cal");

        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-suc"));
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-lg"));
        Assert.AreEqual("Cascaded Label", calendar.GetAttribute("aria-label"));
        Assert.AreEqual("group", calendar.GetAttribute("role"));
        Assert.IsTrue(component.FindAll(".bit-cal-wnm").Count > 0);
        Assert.AreEqual(0, component.FindAll(".bit-cal-gtb").Count);
    }

    [TestMethod]
    public void BitCalendarDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            ShowWeekNumbers = true
        }, builder =>
        {
            builder.AddAttribute(1, nameof(BitCalendar.Color), BitColor.Error);
            builder.AddAttribute(2, nameof(BitCalendar.Size), BitSize.Small);
        });

        var calendar = component.Find(".bit-cal");

        // Direct parameters win over the cascaded ones.
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-err"));
        Assert.IsFalse(calendar.ClassList.Contains("bit-cal-suc"));
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-sm"));
        Assert.IsFalse(calendar.ClassList.Contains("bit-cal-lg"));

        // What the calendar left unset is still filled in from the cascade.
        Assert.IsTrue(component.FindAll(".bit-cal-wnm").Count > 0);
    }

    [TestMethod]
    public void BitCalendarParamsUpdateParametersShouldSetAllProperties()
    {
        var maxDate = new DateTimeOffset(2021, 4, 15, 0, 0, 0, TimeSpan.Zero);
        var minDate = new DateTimeOffset(2021, 2, 15, 0, 0, 0, TimeSpan.Zero);
        var today = new DateTimeOffset(2021, 3, 15, 0, 0, 0, TimeSpan.Zero);
        var startingValue = new DateTimeOffset(2021, 3, 10, 9, 30, 0, TimeSpan.Zero);

        var @params = new BitCalendarParams
        {
            AllowDeselect = true,
            Color = BitColor.Warning,
            ContinuousSpinDelay = 500,
            ContinuousSpinInterval = 90,
            Culture = CultureInfo.InvariantCulture,
            DateFormat = "yyyy/MM/dd",
            DisableFuture = true,
            FirstDayOfWeek = DayOfWeek.Monday,
            FixedWeeks = true,
            GoToTodayTitle = "Cascaded today",
            HighlightCurrentMonth = true,
            HighlightSelectedMonth = true,
            HighlightToday = false,
            HourStep = 2,
            InvalidErrorMessage = "Cascaded invalid",
            MaxDate = maxDate,
            MinDate = minDate,
            MinuteStep = 5,
            MonthCount = 2,
            MonthPickerToggleTitle = "Cascaded month toggle",
            PagedNavigation = true,
            ShowEventDetails = false,
            ShowGoToToday = false,
            ShowMonthPicker = false,
            ShowNowButton = false,
            ShowOutsideDays = false,
            ShowTimePicker = true,
            ShowWeekNumbers = true,
            Size = BitSize.Small,
            StartingValue = startingValue,
            TimeFormat = BitTimeFormat.TwelveHours,
            TimePickerHourTitle = "Cascaded hour",
            TimeZone = TimeZoneInfo.Utc,
            Today = today,
            WeekNumberRule = CalendarWeekRule.FirstFourDayWeek,
            WeekNumbersHeaderTitle = "Cascaded week",
            AriaLabel = "Cascaded Label",
            Disabled = true,
            TabIndex = "5"
        };

        var component = RenderWithParams(@params);

        var instance = component.FindComponent<BitCalendar>().Instance;

        Assert.IsTrue(instance.AllowDeselect);
        Assert.AreEqual(BitColor.Warning, instance.Color);
        Assert.AreEqual(500, instance.ContinuousSpinDelay);
        Assert.AreEqual(90, instance.ContinuousSpinInterval);
        Assert.AreEqual(CultureInfo.InvariantCulture, instance.Culture);
        Assert.AreEqual("yyyy/MM/dd", instance.DateFormat);
        Assert.IsTrue(instance.DisableFuture);
        Assert.AreEqual(DayOfWeek.Monday, instance.FirstDayOfWeek);
        Assert.IsTrue(instance.FixedWeeks);
        Assert.AreEqual("Cascaded today", instance.GoToTodayTitle);
        Assert.IsTrue(instance.HighlightCurrentMonth);
        Assert.IsTrue(instance.HighlightSelectedMonth);
        Assert.IsFalse(instance.HighlightToday);
        Assert.AreEqual(2, instance.HourStep);
        Assert.AreEqual("Cascaded invalid", instance.InvalidErrorMessage);
        Assert.AreEqual(maxDate, instance.MaxDate);
        Assert.AreEqual(minDate, instance.MinDate);
        Assert.AreEqual(5, instance.MinuteStep);
        Assert.AreEqual(2, instance.MonthCount);
        Assert.AreEqual("Cascaded month toggle", instance.MonthPickerToggleTitle);
        Assert.IsTrue(instance.PagedNavigation);
        Assert.IsFalse(instance.ShowEventDetails);
        Assert.IsFalse(instance.ShowGoToToday);
        Assert.IsFalse(instance.ShowMonthPicker);
        Assert.IsFalse(instance.ShowNowButton);
        Assert.IsFalse(instance.ShowOutsideDays);
        Assert.IsTrue(instance.ShowTimePicker);
        Assert.IsTrue(instance.ShowWeekNumbers);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual(startingValue, instance.StartingValue);
        Assert.AreEqual(BitTimeFormat.TwelveHours, instance.TimeFormat);
        Assert.AreEqual("Cascaded hour", instance.TimePickerHourTitle);
        Assert.AreEqual(TimeZoneInfo.Utc, instance.TimeZone);
        Assert.AreEqual(today, instance.Today);
        Assert.AreEqual(CalendarWeekRule.FirstFourDayWeek, instance.WeekNumberRule);
        Assert.AreEqual("Cascaded week", instance.WeekNumbersHeaderTitle);
        Assert.AreEqual("Cascaded Label", instance.AriaLabel);
        Assert.IsTrue(instance.Disabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitCalendarParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        var @params = new BitCalendarParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            GoToTodayTitle = "Params Title"
        };

        var component = RenderComponent<BitCalendar>(p =>
        {
            p.Add(x => x.Color, BitColor.Error);
            p.Add(x => x.Size, BitSize.Small);
            p.Add(x => x.GoToTodayTitle, "Existing Title");
        });

        var instance = component.Instance;

        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.GoToTodayTitle);

        @params.UpdateParameters(instance);

        // The values stay put because HasNotBeenSet returns false for every one of them.
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.GoToTodayTitle);
    }

    [TestMethod]
    public void BitCalendarParamsShouldApplyClassesAndStyles()
    {
        var classes = new BitCalendarClassStyles
        {
            Root = "custom-root",
            Container = "custom-container",
            DaysGrid = "custom-grid"
        };

        var styles = new BitCalendarClassStyles
        {
            Root = "color: red;",
            Container = "margin: 5px;",
            DaysGrid = "padding: 10px;"
        };

        var component = RenderWithParams(new BitCalendarParams { Classes = classes, Styles = styles });

        var calendar = component.Find(".bit-cal");
        var container = component.Find(".bit-cal-cnt");
        var grid = component.Find(".bit-cal-grd");

        Assert.IsTrue(calendar.ClassList.Contains("custom-root"));
        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        Assert.IsTrue(grid.ClassList.Contains("custom-grid"));
        Assert.IsTrue(calendar.GetAttribute("style")?.Contains("color: red;"));
        Assert.AreEqual("margin: 5px;", container.GetAttribute("style"));
        Assert.AreEqual("padding: 10px;", grid.GetAttribute("style"));
    }

    [TestMethod]
    public void BitCalendarParamsShouldRebuildTheViewForWhatItCarries()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            // Each of these is a parameter the calendar builds its view from, and the cascade reaches the
            // component only after the pass that reads them has already run once.
            MonthCount = 3,
            Culture = CultureInfo.InvariantCulture,
            TimeZone = TimeZoneInfo.Utc,
            StartingValue = new DateTimeOffset(2021, 3, 10, 0, 0, 0, TimeSpan.Zero)
        });

        var calendar = component.Find(".bit-cal");

        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-mcv"));
        Assert.AreEqual(3, component.FindAll(".bit-cal-dwp").Count);

        // The strip opens on the month the cascaded StartingValue falls in, which only the rebuilt view knows about.
        Assert.IsTrue(component.FindAll(".bit-cal-dbt").Any(b => b.Id!.EndsWith("day-2021-03-10")));
        Assert.AreEqual("March 2021", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            Dir = BitDir.Rtl,
            Disabled = true,
            Class = "cascaded",
            Style = "margin: 1px;",
            Id = "cascaded-id",
        });

        var root = component.Find(".bit-cal");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("margin: 1px;"));
        Assert.AreEqual("cascaded-id", root.Id);

        // The disabled state a cascade hands down reaches the cells the same way a written one does.
        Assert.IsTrue(component.FindAll(".bit-cal-dbt").All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitCalendarShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var component = RenderWithParams(new BitCalendarParams());

        var root = component.Find(".bit-cal");

        // Nothing cascades, so Color and Size stay unset and publish nothing, and the single-month view is kept.
        Assert.IsFalse(root.ClassList.Contains("bit-cal-pri"));
        Assert.IsFalse(root.ClassList.Contains("bit-cal-md"));
        Assert.IsFalse(root.ClassList.Contains("bit-cal-mcv"));
        Assert.IsFalse(root.HasAttribute("role"));
        Assert.AreEqual(1, component.FindAll(".bit-cal-dwp").Count);
    }

    [TestMethod]
    public void BitCalendarParamsShouldOpenTheCalendarOnTheMonthOfTheCascadedToday()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            Culture = CultureInfo.InvariantCulture,
            TimeZone = TimeZoneInfo.Utc,
            Today = new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero),
        });

        // The view a calendar opens on is built while initializing, so the cascaded Today has to be in by then - one
        // applied after it would highlight its day on a calendar showing the month of the real clock.
        Assert.AreEqual("March 2024", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
        Assert.AreEqual("date", component.Find("[id$='-day-2024-03-15']").GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitCalendarParamsShouldNotPageTheCalendarWhenTheCascadedTodayMovesOn()
    {
        var component = RenderWithParams(new BitCalendarParams
        {
            Culture = CultureInfo.InvariantCulture,
            TimeZone = TimeZoneInfo.Utc,
            Today = new DateTimeOffset(2024, 3, 31, 23, 59, 0, TimeSpan.Zero),
        });

        // A time provider ticking past midnight moves Today on, which must not take the calendar away from the month
        // the user is looking at.
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams>
        {
            new BitCalendarParams
            {
                Culture = CultureInfo.InvariantCulture,
                TimeZone = TimeZoneInfo.Utc,
                Today = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero),
            }
        }));

        Assert.AreEqual(new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero), component.FindComponent<BitCalendar>().Instance.Today);
        Assert.AreEqual("March 2024", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarParamsShouldCarryTheInputStatesOfAForm()
    {
        var @params = new BitCalendarParams { ReadOnly = true, Required = true };

        Assert.IsInstanceOfType<BitInputBaseParams<DateTimeOffset?>>(@params);

        var calendar = RenderWithParams(@params).FindComponent<BitCalendar>().Instance;

        Assert.IsTrue(calendar.ReadOnly);
        Assert.IsTrue(calendar.Required);
    }
}
