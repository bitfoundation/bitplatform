using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Calendar;

[TestClass]
public class BitCalendarTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCalendarShouldRespectIsEnabled(bool isEnabled)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var calendar = component.Find(".bit-cal");

        if (isEnabled)
        {
            Assert.IsFalse(calendar.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(calendar.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod]
    public void BitCalendarShouldRenderDayCellTemplate()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.DayCellTemplate, (RenderFragment<DateTimeOffset>)(context =>
            {
                RenderFragment fragment = builder => builder.AddContent(0, $"Day-{context.Day}");
                return fragment;
            }));
        });

        var firstDayCell = component.Find(".bit-cal-dbt");

        Assert.Contains("Day-", firstDayCell.TextContent);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCalendarShouldRespectShowWeekNumbers(bool showWeekNumbers)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowWeekNumbers, showWeekNumbers);
        });

        var weekNumbers = component.FindAll(".bit-cal-wnm");

        if (showWeekNumbers)
        {
            Assert.IsNotEmpty(weekNumbers);
        }
        else
        {
            Assert.IsEmpty(weekNumbers);
        }
    }

    [TestMethod]
    public void BitCalendarShouldRespectGoToTodayTitle()
    {
        var title = "Go now";

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.GoToTodayTitle, title);
        });

        var goToTodayButton = component.Find(".bit-cal-gtb");

        Assert.AreEqual(title, goToTodayButton.GetAttribute("title"));
    }

    [TestMethod]
    public void BitCalendarSelectingTodayShouldUpdateValue()
    {
        var component = RenderComponent<BitCalendar>();

        Assert.IsNull(component.Instance.Value);

        var todayButton = component.Find(".bit-cal-dtd");

        todayButton.Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(DateTimeOffset.Now.Date, component.Instance.Value!.Value.Date);
        Assert.AreEqual(TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now), component.Instance.Value!.Value.Offset);
    }

    [TestMethod,
        DataRow("Up", null, "bit-icon bit-icon--Up"),
        DataRow(null, "chevron-left", "fa fa-chevron-left"),
        DataRow("ChevronLeft", "chevron-left", "fa fa-chevron-left")]
    public void BitCalendarShouldRespectPrevMonthNavIconName(string? iconName, string? externalIconName, string expectedClass)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            if (iconName is not null)
            {
                parameters.Add(p => p.PrevMonthNavIconName, iconName);
            }

            if (externalIconName is not null)
            {
                parameters.Add(p => p.PrevMonthNavIcon, new BitIconInfo(externalIconName, "fa", "fa-"));
            }
        });

        var icon = component.Find(".bit-cal-nbt:first-child i");

        foreach (var expectedCls in expectedClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.IsTrue(icon.ClassList.Contains(expectedCls));
        }
    }

    [TestMethod]
    public void BitCalendarPrevMonthNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);

            parameters.Add(p => p.PrevMonthNavIconName, "Up");
            parameters.Add(p => p.PrevMonthNavIcon, BitIconInfo.Css("fa-solid fa-arrow-left"));
        });

        var icon = component.Find(".bit-cal-nbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
    }

    [TestMethod]
    public void BitCalendarGoToTodayIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.GoToTodayIconName, "CustomTodayIcon");
        });

        var icon = component.Find(".bit-cal-gtb i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomTodayIcon"));
    }

    [TestMethod]
    public void BitCalendarGoToTodayIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.GoToTodayIcon, BitIconInfo.Css("fa-solid fa-calendar-day"));
        });

        var icon = component.Find(".bit-cal-gtb i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-calendar-day"));
    }

    [TestMethod]
    public void BitCalendarGoToTodayIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.GoToTodayIconName, "GotoToday");
            parameters.Add(p => p.GoToTodayIcon, BitIconInfo.Css("fa-solid fa-calendar-check"));
        });

        var icon = component.Find(".bit-cal-gtb i");

        Assert.IsTrue(icon.ClassList.Contains("fa-calendar-check"));
    }

    [TestMethod]
    public void BitCalendarShowTimePickerIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.ShowTimePickerIconName, "CustomClockIcon");
            parameters.Add(p => p.Classes, new BitCalendarClassStyles { ShowTimePickerIcon = "picker-icon" });
        });

        var icon = component.Find(".picker-icon");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomClockIcon"));
    }

    [TestMethod]
    public void BitCalendarShowTimePickerIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.ShowGoToToday, false);
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.ShowTimePickerIcon, BitIconInfo.Css("fa-solid fa-clock"));
            parameters.Add(p => p.Classes, new BitCalendarClassStyles { ShowTimePickerIcon = "picker-icon" });
        });

        var icon = component.Find(".picker-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-clock"));
    }

    [TestMethod]
    public void BitCalendarNowButtonIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.NowButtonIconName, "CustomNowIcon");
        });

        var icon = component.Find(".bit-cal-gtn i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomNowIcon"));
    }

    [TestMethod]
    public void BitCalendarNowButtonIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.NowButtonIcon, BitIconInfo.Css("bi bi-clock"));
        });

        var icon = component.Find(".bit-cal-gtn i");

        Assert.IsTrue(icon.ClassList.Contains("bi"));
        Assert.IsTrue(icon.ClassList.Contains("bi-clock"));
    }

    [TestMethod]
    public void BitCalendarTimePickerIncreaseHourIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseHourIconName, "CustomUpIcon");
        });

        var icon = component.Find(".bit-cal-tpr:first-child .bit-cal-tbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomUpIcon"));
    }

    [TestMethod]
    public void BitCalendarTimePickerIncreaseHourIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseHourIcon, BitIconInfo.Css("bi bi-chevron-up"));
        });

        var icon = component.Find(".bit-cal-tpr:first-child .bit-cal-tbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("bi-chevron-up"));
    }

    [TestMethod]
    public void BitCalendarTimePickerDecreaseHourIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseHourIconName, "CustomDownIcon");
        });

        var icon = component.Find(".bit-cal-tpr:first-child .bit-cal-tbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomDownIcon"));
    }

    [TestMethod]
    public void BitCalendarTimePickerDecreaseHourIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseHourIcon, BitIconInfo.Css("bi bi-chevron-down"));
        });

        var icon = component.Find(".bit-cal-tpr:first-child .bit-cal-tbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("bi-chevron-down"));
    }

    [TestMethod]
    public void BitCalendarNextMonthNavIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.NextMonthNavIconName, "CustomNextIcon");
        });

        var icon = component.Find(".bit-cal-nbc .bit-cal-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomNextIcon"));
    }

    [TestMethod]
    public void BitCalendarNextMonthNavIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.NextMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-right"));
        });

        var icon = component.Find(".bit-cal-nbc .bit-cal-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-right"));
    }

    [TestMethod]
    public void BitCalendarNextMonthNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.NextMonthNavIconName, "ShouldNotRender");
            parameters.Add(p => p.NextMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-right"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-chevron-right", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarPrevYearNavIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearNavIconName, "CustomPrevYearIcon");
        });

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomPrevYearIcon", markup);
    }

    [TestMethod]
    public void BitCalendarPrevYearNavIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-left"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-angles-left", markup);
    }

    [TestMethod]
    public void BitCalendarPrevYearNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearNavIconName, "ShouldNotRender");
            parameters.Add(p => p.PrevYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-left"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-angles-left", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarNextYearNavIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearNavIconName, "CustomNextYearIcon");
        });

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomNextYearIcon", markup);
    }

    [TestMethod]
    public void BitCalendarNextYearNavIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-right"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-angles-right", markup);
    }

    [TestMethod]
    public void BitCalendarNextYearNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearNavIconName, "ShouldNotRender");
            parameters.Add(p => p.NextYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-right"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-angles-right", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarPrevYearRangeNavIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearRangeNavIconName, "CustomPrevYearRangeIcon");
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomPrevYearRangeIcon", markup);
    }

    [TestMethod]
    public void BitCalendarPrevYearRangeNavIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-backward"));
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("fa-backward", markup);
    }

    [TestMethod]
    public void BitCalendarPrevYearRangeNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.PrevYearRangeNavIconName, "ShouldNotRender");
            parameters.Add(p => p.PrevYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-backward"));
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("fa-backward", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarNextYearRangeNavIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearRangeNavIconName, "CustomNextYearRangeIcon");
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomNextYearRangeIcon", markup);
    }

    [TestMethod]
    public void BitCalendarNextYearRangeNavIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-forward"));
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("fa-forward", markup);
    }

    [TestMethod]
    public void BitCalendarNextYearRangeNavIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.NextYearRangeNavIconName, "ShouldNotRender");
            parameters.Add(p => p.NextYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-forward"));
        });

        var ptb = component.Find(".bit-cal-ptb");

        ptb.Click();

        var markup = component.Markup;

        Assert.Contains("fa-forward", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarTimePickerIncreaseMinuteIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseMinuteIconName, "CustomIncMinuteIcon");
        });

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomIncMinuteIcon", markup);
    }

    [TestMethod]
    public void BitCalendarTimePickerIncreaseMinuteIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-chevron-up", markup);
    }

    [TestMethod]
    public void BitCalendarTimePickerIncreaseMinuteIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseMinuteIconName, "ShouldNotRender");
            parameters.Add(p => p.TimePickerIncreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-chevron-up", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarTimePickerDecreaseMinuteIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseMinuteIconName, "CustomDecMinuteIcon");
        });

        var markup = component.Markup;

        Assert.Contains("bit-icon--CustomDecMinuteIcon", markup);
    }

    [TestMethod]
    public void BitCalendarTimePickerDecreaseMinuteIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-chevron-down", markup);
    }


    [TestMethod]
    public void BitCalendarTimePickerDecreaseMinuteIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseMinuteIconName, "ShouldNotRender");
            parameters.Add(p => p.TimePickerDecreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
        });

        var markup = component.Markup;

        Assert.Contains("fa-chevron-down", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarHideTimePickerIconShouldHideTimePickerIcons()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, false);
            parameters.Add(p => p.TimePickerIncreaseMinuteIconName, "HiddenIncMinuteIcon");
            parameters.Add(p => p.TimePickerDecreaseMinuteIconName, "HiddenDecMinuteIcon");
        });

        var markup = component.Markup;

        Assert.IsFalse(markup.Contains("bit-icon--HiddenIncMinuteIcon", StringComparison.Ordinal));
        Assert.IsFalse(markup.Contains("bit-icon--HiddenDecMinuteIcon", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BitCalendarTimePickerDecreaseHourIconShouldTakePrecedenceOverIconName()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseHourIconName, "ShouldNotRender");
            parameters.Add(p => p.TimePickerDecreaseHourIcon, BitIconInfo.Css("bi bi-chevron-down"));
        });

        var markup = component.Markup;

        Assert.Contains("bi-chevron-down", markup);
        Assert.IsFalse(markup.Contains("bit-icon--ShouldNotRender", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task BitCalendarDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitCalendar>(p =>
        {
            p.Add(x => x.Value, DateTimeOffset.UtcNow);
            p.Add(x => x.ShowTimePicker, true);
        });

        await component.Instance.DisposeAsync();
    }

    // ── Events feature ────────────────────────────────────────────────────────

    [TestMethod]
    public void BitCalendarEventsShouldShowIndicatorOnDayWithEvent()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Body = "Details", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        var day15 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");

        Assert.IsNotNull(day15.QuerySelector(".bit-cal-evi"));
    }

    [TestMethod]
    public void BitCalendarEventsShouldNotShowIndicatorOnDayWithoutEvent()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Body = "Details", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        var day10 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "10");

        Assert.IsNull(day10.QuerySelector(".bit-cal-evi"));
    }

    [TestMethod]
    public void BitCalendarEventsShouldShowNoIndicatorsWhenNoEvents()
    {
        var component = RenderComponent<BitCalendar>();

        Assert.IsEmpty(component.FindAll(".bit-cal-evi"));
    }

    [TestMethod]
    public void BitCalendarEventsDayButtonShouldHaveTooltipWithEventTitle()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Team Standup", Body = "Details", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        var day15 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");

        Assert.IsTrue(day15.GetAttribute("title")?.Contains("Team Standup"));
    }

    [TestMethod]
    public void BitCalendarEventsTooltipShouldIncludeStartTimeWhenPresent()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            // the time of an event is written the way its culture writes a time of day, so the one it is
            // read back against is pinned rather than left to whatever the machine running the test is set to
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
            parameters.Add(p => p.Events, [
                new ()
                {
                    Title = "Sync",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    StartTime = new TimeOnly(9, 30)
                }
            ]);
        });

        var day15 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");
        var title = day15.GetAttribute("title");
        var expectedTime = new TimeOnly(9, 30).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        Assert.IsTrue(title?.Contains("Sync"));
        Assert.IsTrue(title?.Contains(expectedTime));
    }

    [TestMethod]
    public void BitCalendarEventsTimeShouldKeepTheSeparatorsOfTheCulture()
    {
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "H.mm";

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.Events, [
                new ()
                {
                    Title = "Sync",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    StartTime = new TimeOnly(9, 30)
                }
            ]);
        });

        var day15 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");

        // The hour is padded whatever the culture writes it like, so the assertion is on the padded time
        // rather than on a substring of it that an unpadded "9.30" would match just as well.
        Assert.IsTrue(day15.GetAttribute("title")?.Contains("09.30"));
    }

    [TestMethod]
    public void BitCalendarEventsTimeShouldPadTheHourOfACultureThatWritesItNarrow()
    {
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortTimePattern = "H:mm"; // en-US and fa-IR write the hour like this

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.Events, [
                new ()
                {
                    Title = "Sync",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    StartTime = new TimeOnly(9, 5)
                }
            ]);
        });

        var day15 = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");

        // The separators and the order come from the culture, but the parts are padded, so the times of
        // a list of events line up under one another.
        Assert.IsTrue(day15.GetAttribute("title")?.Contains("09:05"));
    }

    [TestMethod]
    public void BitCalendarEventsClickingDayWithEventShouldOpenModal()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Body = "Details", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsNotNull(component.Find(".bit-cal-eov"));
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowEventTitle()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Sprint Review", Body = "Demo day", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.AreEqual("Sprint Review", component.Find(".bit-cal-eit").TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowEventBody()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Body = "Room 3A", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.AreEqual("Room 3A", component.Find(".bit-cal-eib").TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowMultipleEvents()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Event A", Body = "Body A", Date = new DateOnly(2026, 1, 15) },
                new BitCalendarEvent { Title = "Event B", Body = "Body B", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.HasCount(2, component.FindAll(".bit-cal-emi"));
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowBothTimesWithSeparator()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            // the times of an event are written the way its culture writes a time of day, so the one they
            // are read back against is pinned rather than left to whatever the machine is set to
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
            parameters.Add(p => p.Events, [
                new()
                {
                    Title = "Workshop",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 30)
                }
            ]);
        });

        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var startFormatted = new TimeOnly(9, 0).ToString("HH:mm", culture);
        var endFormatted = new TimeOnly(11, 30).ToString("HH:mm", culture);

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");

        Assert.Contains(startFormatted, timeEl.TextContent);
        Assert.Contains(endFormatted, timeEl.TextContent);
        Assert.Contains("\u2013", timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowFromTextForStartOnlyTime()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Event", Body = "Details", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(10, 0) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");
        var expectedTime = new TimeOnly(10, 0).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains("From", timeEl.TextContent);
        Assert.Contains(expectedTime, timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowUntilTextForEndOnlyTime()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
            parameters.Add(p => p.Events, [
                new ()
                {
                    Title = "Deadline",
                    Body = "Submit by",
                    Date = new DateOnly(2026, 1, 15),
                    EndTime = new TimeOnly(17, 0)
                }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");
        var expectedTime = new TimeOnly(17, 0).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains("Until", timeEl.TextContent);
        Assert.Contains(expectedTime, timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldNotShowTimeRowWhenTimesAbsent()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Reminder", Body = "All day", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsEmpty(component.FindAll(".bit-cal-eis"));
    }

    [TestMethod]
    public void BitCalendarEventsCloseButtonShouldCloseModal()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Body = "Details", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsNotNull(component.Find(".bit-cal-eov"));

        component.Find(".bit-cal-emx").Click();

        Assert.IsEmpty(component.FindAll(".bit-cal-eov"));
    }

    [TestMethod]
    public void BitCalendarEventsClickingOverlayShouldCloseModal()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new()
                {
                    Title = "Meeting",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15)
                }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsNotNull(component.Find(".bit-cal-eov"));

        component.Find(".bit-cal-eov").Click();

        Assert.IsEmpty(component.FindAll(".bit-cal-eov"));
    }

    [TestMethod]
    public void BitCalendarEventsCustomFromTextShouldAppearInModal()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.EventTimeFromText, "Ab");
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Event", Body = "Details", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(9, 0) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");

        Assert.Contains("Ab", timeEl.TextContent);
        Assert.DoesNotContain("From", timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsCustomUntilTextShouldAppearInModal()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.EventTimeUntilText, "Bis");
            parameters.Add(p => p.Events, [
                new()
                {
                    Title = "Deadline",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    EndTime = new TimeOnly(18, 0)
                }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");

        Assert.Contains("Bis", timeEl.TextContent);
        Assert.DoesNotContain("Until", timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsTwelveHourFormatShouldUseAmPmInModal()
    {
        var culture = new System.Globalization.CultureInfo("en-US");

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Events, [
                new()
                {
                    Title = "Lunch",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15),
                    StartTime = new TimeOnly(14, 30)
                }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        // The calendar writes the time with the pattern of the culture, whose separator before the
        // designator is a plain space on some ICU versions and a narrow no-break space on others, while
        // the pattern below carries the plain space it is written with (see TestStrings.NormalizeSpaces).
        var timeText = component.Find(".bit-cal-eis").TextContent.NormalizeSpaces();
        var expectedFormatted = new TimeOnly(14, 30).ToString("h:mm tt", culture).NormalizeSpaces();  // "2:30 PM"
        var unexpected24h = new TimeOnly(14, 30).ToString("HH:mm", culture);     // "14:30"

        Assert.DoesNotContain(unexpected24h, timeText, "Should not use 24h format in 12h mode");
        Assert.Contains(expectedFormatted, timeText, $"Should contain '{expectedFormatted}' including AM/PM designator");
    }

    [TestMethod]
    public void BitCalendarEventsLookupShouldUpdateWhenEventsParamChanges()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        Assert.IsEmpty(component.FindAll(".bit-cal-evi"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new()
                {
                    Title = "New Event",
                    Body = "Details",
                    Date = new DateOnly(2026, 1, 15)
                }
            ]);
        });

        Assert.IsNotEmpty(component.FindAll(".bit-cal-evi"));
    }

    [TestMethod]
    public void BitCalendarShouldRespectDefaultValue()
    {
        var defaultValue = new DateTimeOffset(2020, 1, 15, 0, 0, 0, DateTimeOffset.Now.Offset);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, component.Instance.Value);
    }

    [TestMethod]
    public void BitCalendarShouldRespectDisabledDaysOfWeek()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        Assert.IsEmpty(component.FindAll(".bit-cal-dbt[disabled]"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDaysOfWeek, [DayOfWeek.Saturday, DayOfWeek.Sunday]);
        });

        var disabledButtons = component.FindAll(".bit-cal-dbt[disabled]");

        // two disabled days per rendered week
        Assert.AreEqual(component.FindAll(".bit-cal-dgr").Count * 2, disabledButtons.Count);
    }

    [TestMethod]
    public void BitCalendarShouldRespectDisabledDates()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDates, [new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15)))]);
        });

        var disabledButtons = component.FindAll(".bit-cal-dbt[disabled]");

        Assert.HasCount(1, disabledButtons);
        Assert.AreEqual("15", disabledButtons[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldRespectIsDateDisabled()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.IsDateDisabled, d => d.Day % 2 == 1);
        });

        var disabledButtons = component.FindAll(".bit-cal-dbt[disabled]");

        Assert.IsNotEmpty(disabledButtons);
        Assert.IsTrue(disabledButtons.All(b => int.Parse(b.TextContent.Trim()) % 2 == 1));
    }

    [TestMethod]
    public void BitCalendarShouldNotSelectDisabledDate()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDates, [new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15)))]);
        });

        var disabledButton = component.Find(".bit-cal-dbt[disabled]");

        disabledButton.Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitCalendarShouldRespectHighlightedDates()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.HighlightedDates, [new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15)))]);
        });

        var highlightedButton = component.Find(".bit-cal-dhl");

        Assert.AreEqual("15", highlightedButton.TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldRespectGetDayClass()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.GetDayClass, d => d.Day == 15 ? "custom-day-class" : null);
        });

        var customButtons = component.FindAll(".custom-day-class");

        Assert.HasCount(1, customButtons);
        Assert.AreEqual("15", customButtons[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldRespectFirstDayOfWeek()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Monday);
        });

        var firstDayHeader = component.Find(".bit-cal-dgh .bit-cal-wlb");

        Assert.AreEqual(CultureInfo.CurrentUICulture.DateTimeFormat.GetShortestDayName(DayOfWeek.Monday), firstDayHeader.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCalendarShouldRespectFixedWeeks(bool fixedWeeks)
    {
        // February 2026 fits in exactly 4 weeks when the week starts on Sunday
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Add(p => p.FixedWeeks, fixedWeeks);
        });

        Assert.HasCount(fixedWeeks ? 6 : 4, component.FindAll(".bit-cal-dgr"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCalendarShouldRespectShowOutsideDays(bool showOutsideDays)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Add(p => p.ShowOutsideDays, showOutsideDays);
        });

        if (showOutsideDays)
        {
            Assert.IsNotEmpty(component.FindAll(".bit-cal-dbo"));
            Assert.IsEmpty(component.FindAll(".bit-cal-dbe"));
        }
        else
        {
            Assert.IsEmpty(component.FindAll(".bit-cal-dbo"));
            Assert.IsNotEmpty(component.FindAll(".bit-cal-dbe"));
        }
    }

    [TestMethod]
    public void BitCalendarShouldRespectToday()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Today, new DateTimeOffset(2021, 3, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2021, 3, 15))));
        });

        var todayButton = component.Find(".bit-cal-dtd");

        Assert.AreEqual("15", todayButton.TextContent.Trim());
        Assert.AreEqual("date", todayButton.GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitCalendarShouldRespectOnMonthChange()
    {
        DateTimeOffset? changedMonth = null;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.OnMonthChange, (DateTimeOffset month) => changedMonth = month);
        });

        var nextMonthButton = component.FindAll(".bit-cal-nbt")[1];

        nextMonthButton.Click();

        Assert.IsNotNull(changedMonth);
        Assert.AreEqual(new DateTime(2026, 2, 1), changedMonth!.Value.Date);
    }

    [TestMethod]
    public void BitCalendarKeyboardNavigationShouldMoveFocusToNextDay()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15))));
        });

        var focusedButton = component.Find(".bit-cal-dbt[tabindex='0']");

        Assert.AreEqual("15", focusedButton.TextContent.Trim());

        focusedButton.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("16", component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarKeyboardNavigationShouldSkipDisabledDays()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15))));
            parameters.Add(p => p.DisabledDates, [new DateTimeOffset(2026, 1, 16, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 16)))]);
        });

        var focusedButton = component.Find(".bit-cal-dbt[tabindex='0']");

        focusedButton.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("17", component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarKeyboardNavigationShouldChangeMonthOnPageDown()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15))));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        var focusedButton = component.Find(".bit-cal-dbt[tabindex='0']");

        focusedButton.KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        var monthTitle = component.Find(".bit-cal-pkt, .bit-cal-ptb");

        Assert.Contains("February", monthTitle.TextContent);
        Assert.AreEqual("15", component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod,
        DataRow("ArrowLeft", "14"),
        DataRow("ArrowUp", "8"),
        DataRow("ArrowDown", "22"),
        DataRow("Home", "11"),
        DataRow("End", "17")]
    public void BitCalendarKeyboardNavigationShouldMoveFocusWithinMonth(string key, string expectedDay)
    {
        // January 15, 2026 is a Thursday; the week starts on Sunday, January 11 and ends on Saturday, January 17
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15))));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
        });

        var focusedButton = component.Find(".bit-cal-dbt[tabindex='0']");

        Assert.AreEqual("15", focusedButton.TextContent.Trim());

        focusedButton.KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedDay, component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod,
        DataRow("PageUp", "January 2025"),
        DataRow("PageDown", "January 2027")]
    public void BitCalendarKeyboardNavigationShouldChangeYearOnShiftPage(string key, string expectedTitle)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 1, 15))));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        var focusedButton = component.Find(".bit-cal-dbt[tabindex='0']");

        focusedButton.KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = true });

        var monthTitle = component.Find(".bit-cal-pkt, .bit-cal-ptb");

        Assert.Contains(expectedTitle, monthTitle.TextContent);
        Assert.AreEqual("15", component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldDisableThePastAndTheFutureLikeTheBounds()
    {
        // Today carries a time of day, the way the current instant always does, so that the bounds are
        // measured against the whole day and not against the moment the test happens to name.
        var now = new DateTime(2026, 1, 15, 14, 30, 0);
        var today = new DateTimeOffset(now, TimeZoneInfo.Local.GetUtcOffset(now));

        var past = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
        });

        // A day before today is out of the range exactly as it would be with a MinDate of today,
        // while today itself is the first day still in it.
        Assert.IsTrue(past.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "14").HasAttribute("disabled"));
        Assert.IsFalse(past.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").HasAttribute("disabled"));
        Assert.IsFalse(past.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "16").HasAttribute("disabled"));

        var future = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisableFuture, true);
            parameters.Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture);
        });

        Assert.IsFalse(future.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "14").HasAttribute("disabled"));
        Assert.IsFalse(future.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").HasAttribute("disabled"));
        Assert.IsTrue(future.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "16").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitCalendarDisablePastShouldNotOverwriteTheTimeOfTheValue()
    {
        var now = new DateTime(2026, 1, 15, 14, 30, 0);
        var today = new DateTimeOffset(now, TimeSpan.Zero);

        DateTimeOffset? value = null;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The bound rules out the days before today, not the hours before this instant: today's midnight is
        // in range, so it is left where it is rather than being pulled up to 14:30 - which the next day
        // picked would then silently be given too.
        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.AreEqual(0, value!.Value.Hour);
        Assert.AreEqual(0, value!.Value.Minute);

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "16").Click();

        Assert.AreEqual(16, value!.Value.Day);
        Assert.AreEqual(0, value!.Value.Hour);
        Assert.AreEqual(0, value!.Value.Minute);
    }

    [TestMethod]
    public void BitCalendarHourStepShouldLayAGridOverTheDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.HourStep, 3);
            parameters.Add(p => p.ShowTimePicker, true);
            // The time picker reads the hour of the value in the TimeZone of the component, so the test pins
            // it rather than letting the machine's own zone decide which hour the grid is stepped from.
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The step lays a grid over the day - 0, 3, 6 ... 21 - rather than adding itself to whatever the hour
        // happens to be, so an hour that sits between two grid points moves onto the next one, not three past
        // itself.
        var increaseHour = component.FindAll(".bit-cal-tbt")[0];
        increaseHour.PointerDown();
        increaseHour.PointerUp();

        Assert.AreEqual(12, value!.Value.Hour);
    }

    [TestMethod]
    public async Task BitCalendarShouldNotStartTheContinuousSpinBeforeTheContinuousSpinDelay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            // Longer than the press below, so the held button contributes the one step every press makes
            // and the continuous spin never starts.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var increaseHour = component.FindAll(".bit-cal-tbt")[0];
        increaseHour.PointerDown();

        await Task.Delay(600);

        Assert.AreEqual(11, value!.Value.Hour);

        component.FindAll(".bit-cal-tbt")[0].PointerUp();
    }

    [TestMethod]
    public void BitCalendarMinuteStepShouldLayAGridOverTheHour()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 10, 7, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var decreaseMinute = component.FindAll(".bit-cal-tbt")[3];
        decreaseMinute.PointerDown();
        decreaseMinute.PointerUp();

        Assert.AreEqual(0, value!.Value.Minute);
    }

    [TestMethod,
        DataRow("ArrowRight", "Feb"),
        DataRow("ArrowLeft", "Jan"),
        DataRow("ArrowDown", "May"),
        DataRow("End", "Dec")]
    public void BitCalendarMonthGridShouldMoveFocusWithTheArrowKeys(string key, string expectedMonth)
    {
        // The month grid is four cells wide, so ArrowDown moves four months on, and it wraps at neither end:
        // January is the first cell of the first row, so ArrowLeft from it moves nowhere.
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        var focusedMonth = component.Find(".bit-cal-pkb[tabindex='0']");

        Assert.AreEqual("Jan", focusedMonth.TextContent.Trim());

        focusedMonth.KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedMonth, component.Find(".bit-cal-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthGridShouldChangeYearOnPageDown()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        component.Find(".bit-cal-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.AreEqual("2027", component.Find(".bit-cal-ptb").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarYearGridShouldMoveFocusWithTheArrowKeys()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        // The year picker replaces the month grid when the header of the month picker is activated.
        component.Find(".bit-cal-ptb").Click();

        var focusedYear = component.Find(".bit-cal-pkb[tabindex='0']");

        Assert.AreEqual("2026", focusedYear.TextContent.Trim());

        focusedYear.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("2027", component.Find(".bit-cal-pkb[tabindex='0']").TextContent.Trim());

        component.Find(".bit-cal-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual("2031", component.Find(".bit-cal-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthGridShouldSkipTheMonthsOutOfRange()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.MinDate, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero));
        });

        // April onwards is out of range, so End lands on the last month that is not.
        component.Find(".bit-cal-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("Mar", component.Find(".bit-cal-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldRespectAllowDeselect()
    {
        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.AllowDeselect, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsNull(value);
        // The month the day was deselected in stays on screen rather than jumping back onto today.
        Assert.Contains("January 2026", component.Find(".bit-cal-pkt, .bit-cal-ptb").TextContent);
    }

    [TestMethod]
    public void BitCalendarDeselectShouldChangeNothingButTheValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2050, 6, 10, 8, 45, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.AllowDeselect, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "10").Click();

        Assert.IsNull(value);
        // Emptying the value rebuilds the whole view around today, so everything a deselection is not about -
        // the month on screen and the time the picker holds - is put back the way the user left it.
        Assert.Contains("June 2050", component.Find(".bit-cal-pkt, .bit-cal-ptb").TextContent);
        Assert.AreEqual("8", component.FindAll(".bit-cal-tin")[0].GetAttribute("value"));
        Assert.AreEqual("45", component.FindAll(".bit-cal-tin")[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitCalendarWithoutAllowDeselectShouldKeepTheValueOnReselect()
    {
        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        Assert.IsNotNull(value);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitCalendarShouldRespectHighlightToday(bool highlightToday)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.HighlightToday, highlightToday);
        });

        Assert.AreEqual(highlightToday, component.FindAll(".bit-cal-dtd").Count > 0);
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-cal-sm"),
        DataRow(BitSize.Medium, "bit-cal-md"),
        DataRow(BitSize.Large, "bit-cal-lg")]
    public void BitCalendarShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitCalendarShouldRenderHeaderAndFooterTemplates()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.HeaderTemplate, (RenderFragment)(builder => builder.AddContent(0, "the-header")));
            parameters.Add(p => p.FooterTemplate, (RenderFragment)(builder => builder.AddContent(0, "the-footer")));
        });

        Assert.AreEqual("the-header", component.Find(".bit-cal-hdr").TextContent.Trim());
        Assert.AreEqual("the-footer", component.Find(".bit-cal-ftr").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarEventDialogShouldBeAModalDialogNamedByItsHeading()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var dialog = component.Find(".bit-cal-emc");

        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));

        var labelId = dialog.GetAttribute("aria-labelledby");

        Assert.IsFalse(string.IsNullOrEmpty(labelId));
        // An attribute selector rather than "#id": the generated id can start with a digit, which is not a
        // valid CSS id selector.
        Assert.IsNotNull(component.Find($"[id='{labelId}']"));
        Assert.IsFalse(string.IsNullOrEmpty(component.Find(".bit-cal-emx").GetAttribute("aria-label")));
    }

    [TestMethod]
    public void BitCalendarEventDialogShouldCloseOnEscape()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        component.Find(".bit-cal-emc").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsEmpty(component.FindAll(".bit-cal-eov"));
    }

    [TestMethod]
    public void BitCalendarShouldRespectShowEventDetails()
    {
        DateTimeOffset? value = null;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowEventDetails, false);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        var day = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");

        Assert.IsFalse(day.HasAttribute("aria-haspopup"));

        day.Click();

        // The dialog stays away, but the day is selected all the same and the indicator still reports the event.
        Assert.IsEmpty(component.FindAll(".bit-cal-eov"));
        Assert.AreEqual(15, value!.Value.Day);
        Assert.IsNotEmpty(component.FindAll(".bit-cal-evi"));
    }

    [TestMethod]
    public void BitCalendarEventDetailsShouldNotDeselectTheDayItIsOpenedFrom()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = null;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.AllowDeselect, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Meeting", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("15")).Click();

        Assert.AreEqual(15, value!.Value.Day);

        component.Find(".bit-cal-emc").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // Opening the details of the selected day again is a second look at its events, not a second press of
        // the deselect toggle, so the value survives it.
        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("15")).Click();

        Assert.IsNotNull(value);
        Assert.IsNotEmpty(component.FindAll(".bit-cal-eov"));
    }

    [TestMethod]
    public void BitCalendarDayShouldNameItsEventsForScreenReaders()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Standup", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(9, 0) }
            ]);
        });

        var day = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");
        var label = day.GetAttribute("aria-label");

        Assert.Contains("Standup", label!);
        Assert.AreEqual("dialog", day.GetAttribute("aria-haspopup"));
        // The indicator dots say the same thing to everyone else, so they are not read out a second time.
        Assert.AreEqual("true", component.Find(".bit-cal-evc").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitCalendarEventsShouldBeOrderedLikeAnAgenda()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Afternoon", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(15, 0) },
                new BitCalendarEvent { Title = "Morning", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(9, 0) },
                new BitCalendarEvent { Title = "AllDay", Date = new DateOnly(2026, 1, 15) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var titles = component.FindAll(".bit-cal-eit").Select(t => t.TextContent.Trim()).ToList();

        CollectionAssert.AreEqual(new[] { "AllDay", "Morning", "Afternoon" }, titles);
    }

    [TestMethod]
    public void BitCalendarTimePickerButtonsShouldHaveAccessibleNames()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseHourTitle, "Up an hour");
        });

        var buttons = component.FindAll(".bit-cal-tbt");

        Assert.AreEqual("Up an hour", buttons[0].GetAttribute("aria-label"));

        foreach (var button in buttons)
        {
            Assert.IsFalse(string.IsNullOrEmpty(button.GetAttribute("aria-label")) && button.TextContent.Trim().Length == 0,
                "every spin button of the time picker has a name of its own");
        }
    }

    [TestMethod]
    public void BitCalendarAmPmButtonsShouldReportTheirPressedState()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, new CultureInfo("en-US"));
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        });

        Assert.AreEqual("true", component.Find(".bit-cal-bam").GetAttribute("aria-pressed"));
        Assert.AreEqual("false", component.Find(".bit-cal-bpm").GetAttribute("aria-pressed"));

        component.Find(".bit-cal-bpm").Click();

        Assert.AreEqual("false", component.Find(".bit-cal-bam").GetAttribute("aria-pressed"));
        Assert.AreEqual("true", component.Find(".bit-cal-bpm").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitCalendarTimePickerOverlayShouldStayOpenWhileTheTimeIsChanged()
    {
        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.ShowMonthPicker, false);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The clock button of the day picker's header opens the overlay, which is the last of its nav buttons.
        component.FindAll(".bit-cal-dwp .bit-cal-nbt").Last().Click();

        Assert.IsNotEmpty(component.FindAll(".bit-cal-twp"));

        var increaseHour = component.FindAll(".bit-cal-tbt")[0];
        increaseHour.PointerDown();
        increaseHour.PointerUp();

        // Picking a time is not a parameter change, so the overlay it was picked in is still on screen.
        Assert.AreEqual(11, value!.Value.Hour);
        Assert.IsNotEmpty(component.FindAll(".bit-cal-twp"));
    }

    [TestMethod]
    public void BitCalendarShouldReportItsStateOnTheDaysGrid()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Required, true);
        });

        var grid = component.Find(".bit-cal-grd");

        Assert.AreEqual("true", grid.GetAttribute("aria-readonly"));
        Assert.AreEqual("true", grid.GetAttribute("aria-required"));
        // A read-only day still browses, so it stays focusable and reports that it selects nothing instead.
        Assert.AreEqual("true", component.Find(".bit-cal-dbt").GetAttribute("aria-disabled"));
        Assert.IsFalse(component.Find(".bit-cal-dbt").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitCalendarShouldBeANamedGroupOnlyWhenItIsGivenAName()
    {
        var component = RenderComponent<BitCalendar>();

        Assert.IsFalse(component.Find(".bit-cal").HasAttribute("role"));

        component.Render(parameters => parameters.Add(p => p.AriaLabel, "Departure date"));

        Assert.AreEqual("group", component.Find(".bit-cal").GetAttribute("role"));
        Assert.AreEqual("Departure date", component.Find(".bit-cal").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCalendarShouldSeparateTheStylesOfTheStatesOfADay()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Value, DateTimeOffset.Now);
            parameters.Add(p => p.Styles, new BitCalendarClassStyles
            {
                DayButton = "opacity:1",
                TodayDayButton = "color:red",
                SelectedDayButton = "background:blue"
            });
        });

        // Today and the selection are the same day here, so three styles land on one cell: without a semicolon
        // between them the last declaration of one and the first of the next run into a single invalid one.
        var style = component.Find(".bit-cal-dbt.bit-cal-dbs").GetAttribute("style");

        Assert.Contains("background:blue;", style!);
        Assert.Contains("color:red;", style!);
        Assert.Contains("opacity:1", style!);
    }

    [TestMethod]
    public void BitCalendarShouldRenderEveryMonthOfACalendarWithThirteenOfThem()
    {
        // A leap year of the Hebrew calendar has thirteen months, and a grid that hardcodes twelve either loses
        // one of them or reads past the end of the year. The calendar of a CultureInfo is read-only, so it is
        // replaced the same way the demo's CultureInfoHelper replaces it for the Persian calendar.
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        culture.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!
               .SetValue(culture, new HebrewCalendar());
        culture.DateTimeFormat.Calendar = new HebrewCalendar();

        // 15 March 2024 falls in the Hebrew year 5784, which is one of the leap years of the cycle.
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero));
        });

        Assert.AreEqual(13, component.FindAll(".bit-cal-pkb").Count);
    }

    [TestMethod]
    public void BitCalendarShouldNameTheMonthsOfACommonYearOfAThirteenMonthCalendar()
    {
        // The month names of a Hebrew culture are the thirteen of a leap year, so a common year names its
        // seventh month onwards one place further along that table - as .NET's own formatter does. 5785 is a
        // common year, and the tenth of April 2025 falls in its seventh month, which is Nisan.
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        culture.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!
               .SetValue(culture, new HebrewCalendar());
        culture.DateTimeFormat.Calendar = new HebrewCalendar();

        var inNisan = new DateTime(2025, 4, 10);
        var nisan = inNisan.ToString("MMMM", culture);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(inNisan, TimeSpan.Zero));
        });

        // The seventh name of the table is the second Adar, which a common year does not have at all: reading
        // the names straight off it would spell the month differently from the date the calendar is showing.
        Assert.AreNotEqual(nisan, culture.DateTimeFormat.GetMonthName(7));
        Assert.AreEqual(12, component.FindAll(".bit-cal-pkb").Count);
        Assert.AreEqual(nisan, component.FindAll(".bit-cal-pkb")[6].GetAttribute("title"));
        Assert.Contains(nisan, component.Find(".bit-cal-pkt, .bit-cal-ptb").TextContent);
    }

    [TestMethod]
    public void BitCalendarShouldSurviveACultureWithoutShortestDayNames()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.DateTimeFormat.ShortestDayNames = ["", "", "", "", "", "", ""];

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
        });

        Assert.AreEqual(7, component.FindAll(".bit-cal-dgh .bit-cal-wlb").Count);
    }

    [TestMethod]
    public void BitCalendarShouldShowOneIndicatorPerEventUpToThree()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "One", Date = new DateOnly(2026, 1, 15) },
                new BitCalendarEvent { Title = "Two", Date = new DateOnly(2026, 1, 15) },
                new BitCalendarEvent { Title = "Three", Date = new DateOnly(2026, 1, 16) },
                new BitCalendarEvent { Title = "Four", Date = new DateOnly(2026, 1, 16) },
                new BitCalendarEvent { Title = "Five", Date = new DateOnly(2026, 1, 16) },
                new BitCalendarEvent { Title = "Six", Date = new DateOnly(2026, 1, 16) }
            ]);
        });

        var two = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("15"));
        var six = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("16"));

        Assert.AreEqual(2, two.QuerySelectorAll(".bit-cal-evi").Length);
        // Past three the row of dots says no more than "several"; the count stays in the label and the tooltip.
        Assert.AreEqual(3, six.QuerySelectorAll(".bit-cal-evi").Length);
    }

    [TestMethod]
    public void BitCalendarEventShouldPaintItsIndicatorWithItsOwnColor()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Colored", Date = new DateOnly(2026, 1, 15), Color = BitColor.Success },
                new BitCalendarEvent { Title = "Plain", Date = new DateOnly(2026, 1, 16) }
            ]);
        });

        var colored = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("15"));
        var plain = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim().StartsWith("16"));

        Assert.IsTrue(colored.QuerySelector(".bit-cal-evi")!.ClassList.Contains("bit-cal-evi-suc"));
        // An event of no color of its own takes the calendar's, which is a fallback rather than a class.
        Assert.IsFalse(plain.QuerySelector(".bit-cal-evi")!.ClassList.Any(c => c.StartsWith("bit-cal-evi-")));
    }

    [TestMethod]
    public void BitCalendarTypedHourShouldStayInTheHalfOfTheDayItIsIn()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 15, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // 4 typed into an afternoon time is 16:00, not a silent flip to the morning: which half of the day
        // the time is in is what the AM/PM pair is there to change.
        component.Find(".bit-cal-tin").Input("4");

        Assert.AreEqual(16, value!.Value.Hour);
    }

    [TestMethod]
    public void BitCalendarTypedTimeShouldBeHeldToTheStepGrid()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // What is typed is left alone while it is being typed, so a two-digit value can be reached a digit at
        // a time, and is held to the grid once the field is committed - 20 is nearer to 15 than to 30.
        component.FindAll(".bit-cal-tin")[1].Input("2");
        Assert.AreEqual(2, value!.Value.Minute);

        component.FindAll(".bit-cal-tin")[1].Input("20");
        Assert.AreEqual(20, value!.Value.Minute);

        component.FindAll(".bit-cal-tin")[1].Change("20");
        Assert.AreEqual(15, value!.Value.Minute);
    }

    [TestMethod]
    public void BitCalendarTimeInputShouldStepWithPageKeys()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.HourStep, 3);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-cal-tin").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

        Assert.AreEqual(12, value!.Value.Hour);
    }

    [TestMethod]
    public void BitCalendarEscapeShouldLeaveTheMonthPickerOverlay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.ShowMonthPickerAsOverlay, true);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        component.Find(".bit-cal-ptb").Click();
        Assert.IsNotEmpty(component.FindAll(".bit-cal-pkb"));

        component.Find(".bit-cal-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsEmpty(component.FindAll(".bit-cal-pkb"));
        Assert.IsNotEmpty(component.FindAll(".bit-cal-dbt"));
    }

    [TestMethod]
    public void BitCalendarEscapeShouldLeaveTheYearGridForTheMonthGrid()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        // The year toggle of the month pane, which swaps the months for the years of the range.
        component.FindAll(".bit-cal-ptb").Last().Click();
        Assert.Contains("2026", component.FindAll(".bit-cal-pkb").Select(b => b.TextContent.Trim()).ToList());

        component.Find(".bit-cal-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Contains("Jan", component.FindAll(".bit-cal-pkb").Select(b => b.TextContent.Trim()).ToList());
    }

    [TestMethod]
    public void BitCalendarShouldReportTheCurrentMonthAndYearToScreenReaders()
    {
        var today = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.StartingValue, today);
            // The current month is reported whether or not it is painted.
            parameters.Add(p => p.HighlightCurrentMonth, false);
        });

        var months = component.FindAll(".bit-cal-pkb");
        Assert.AreEqual("date", months[4].GetAttribute("aria-current"));
        Assert.IsNull(months[3].GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitCalendarShouldNameTheWeekNumbersColumnHeader()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.ShowWeekNumbers, true);
            parameters.Add(p => p.WeekNumbersHeaderTitle, "Wk");
        });

        Assert.AreEqual("Wk", component.Find(".bit-cal-dgh .bit-cal-wlb").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCalendarShouldMarkOnlyTheSelectedCellAsSelected()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        var selected = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15");
        var other = component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "16");

        Assert.AreEqual("true", selected.GetAttribute("aria-selected"));
        // An explicit false on the other forty-one would be read out on every day arrowed over.
        Assert.IsNull(other.GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitCalendarReadOnlyShouldReportItsTimeControlsAsUnavailable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
        });

        // Every control that would set the time - the spin buttons, the meridiem pair, the now button - says
        // it is unavailable rather than looking pressable and doing nothing.
        foreach (var button in component.FindAll(".bit-cal-tbt, .bit-cal-gtn"))
        {
            Assert.AreEqual("true", button.GetAttribute("aria-disabled"));
        }

        Assert.IsTrue(component.FindAll(".bit-cal-tin").All(i => i.HasAttribute("readonly")));
    }

    [TestMethod]
    public void BitCalendarReadOnlyTimeInputShouldNotChangeTheValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-cal-tin").Input("11");
        component.Find(".bit-cal-tin").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

        Assert.AreEqual(9, value!.Value.Hour);
    }

    [TestMethod]
    public void BitCalendarShouldNotCutASurrogatePairOutOfADayNameHeader()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        // A name whose first character is written as a surrogate pair: half of it is not a character at all.
        culture.DateTimeFormat.ShortestDayNames = ["\U0001D400a", "Mo", "Tu", "We", "Th", "Fr", "Sa"];

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
        });

        Assert.AreEqual("\U0001D400", component.FindAll(".bit-cal-dgh .bit-cal-wlb")[0].TextContent.Trim());
    }

    [TestMethod,
        DataRow("ArrowLeft"),
        DataRow("ArrowUp"),
        DataRow("Home"),
        DataRow("PageUp")]
    public void BitCalendarKeyboardNavigationShouldStopAtTheEdgeOfTheSupportedRange(string key)
    {
        // The first day a DateTime can represent: every one of these keys steps off the end of the range,
        // which has to leave the focus where it is rather than throw.
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero));
        });

        var focused = component.Find(".bit-cal-dbt[tabindex='0']");
        var day = focused.TextContent.Trim();

        focused.KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(day, component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShiftPageUpShouldStopAtTheEdgeOfTheSupportedRange()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero));
        });

        var focused = component.Find(".bit-cal-dbt[tabindex='0']");
        var day = focused.TextContent.Trim();

        focused.KeyDown(new KeyboardEventArgs { Key = "PageUp", ShiftKey = true });

        Assert.AreEqual(day, component.Find(".bit-cal-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod,
        DataRow(1, 1),
        DataRow(2, 2),
        DataRow(3, 3),
        DataRow(7, 3),
        DataRow(0, 1)]
    public void BitCalendarMonthCountShouldRenderConsecutiveMonths(int monthCount, int expectedCount)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, monthCount);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        var titles = component.FindAll(".bit-cal-pkt");

        Assert.AreEqual(expectedCount, component.FindAll(".bit-cal-dwp").Count);
        Assert.AreEqual(expectedCount, titles.Count);

        var expectedTitles = new[] { "January 2026", "February 2026", "March 2026" };
        for (var i = 0; i < expectedCount; i++)
        {
            Assert.AreEqual(expectedTitles[i], titles[i].TextContent.Trim());
        }
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldNotRenderTheDaysOfTheAdjacentMonthsTwice()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 3);
            parameters.Add(p => p.ShowOutsideDays, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        // January, February and March of 2026, each day of them once: a day of the adjacent months would
        // be a day of the pane beside it, so the grids render nothing but their own months.
        Assert.AreEqual(31 + 28 + 31, component.FindAll(".bit-cal-dbt").Count);

        // And the months keep an even height beside one another, whatever FixedWeeks says.
        Assert.AreEqual(3 * 6, component.FindAll(".bit-cal-dgr").Count);
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldNavigateAcrossItsMonthsWithoutPagingThem()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero));
        });

        component.Find(".bit-cal-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        // The last day of the first month is next to the first day of the second, which the view already
        // shows - so the focus crosses into it and the calendar stays where it is.
        Assert.IsTrue(component.Find(".bit-cal-dbt[tabindex='0']").Id!.EndsWith("day-2026-02-01"));
        Assert.AreEqual("January 2026", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
        Assert.AreEqual("February 2026", component.FindAll(".bit-cal-pkt")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldScrollTheViewOntoADayBeyondIt()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero));
        });

        component.Find(".bit-cal-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        // March is past the end of the view, so it scrolls by the least that brings it in - which leaves
        // the day the keyboard landed on in the pane it was reached from, the last one.
        Assert.AreEqual("February 2026", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
        Assert.AreEqual("March 2026", component.FindAll(".bit-cal-pkt")[1].TextContent.Trim());
        Assert.IsTrue(component.Find(".bit-cal-dbt[tabindex='0']").Id!.EndsWith("day-2026-03-15"));
    }

    [TestMethod,
        DataRow(false, "February 2026", "March 2026"),
        DataRow(true, "March 2026", "April 2026")]
    public void BitCalendarPagedNavigationShouldMoveAWholePageOfMonths(bool pagedNavigation, string firstTitle, string secondTitle)
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.PagedNavigation, pagedNavigation);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        // The first pane heads the navigation backwards and the last one forwards, so the next button is
        // the last of them.
        component.FindAll(".bit-cal-dwp .bit-cal-nbt").Last().Click();

        Assert.AreEqual(firstTitle, component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
        Assert.AreEqual(secondTitle, component.FindAll(".bit-cal-pkt")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldStopTheNavigationAtTheBoundOfItsLastMonth()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 2, 20, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        });

        // The bound is already in view - in the second of the two months - so there is nowhere to go next.
        Assert.IsTrue(component.FindAll(".bit-cal-dwp .bit-cal-nbt").Last().HasAttribute("disabled"));
        Assert.IsFalse(component.FindAll(".bit-cal-dwp .bit-cal-nbt").First().HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldKeepTheViewWhenADayOfAnotherOfItsMonthsIsSelected()
    {
        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.Id!.EndsWith("day-2026-02-10")).Click();

        Assert.AreEqual(new DateTime(2026, 2, 10), value!.Value.DateTime);
        Assert.AreEqual("January 2026", component.FindAll(".bit-cal-pkt")[0].TextContent.Trim());
        Assert.AreEqual("February 2026", component.FindAll(".bit-cal-pkt")[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldFollowTheMonthsOfTheCultureOwnCalendar()
    {
        // The thirteenth month of a Hebrew leap year is a month like any other, so the strip walks through
        // it rather than over it: the last month of 5784 is followed by the first of 5785.
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        culture.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!
               .SetValue(culture, new HebrewCalendar());
        culture.DateTimeFormat.Calendar = new HebrewCalendar();

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 3);
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero));
        });

        var titles = component.FindAll(".bit-cal-pkt").Select(t => t.TextContent.Trim()).ToArray();

        Assert.AreEqual(3, titles.Length);
        Assert.AreEqual(3, titles.Distinct().Count());

        // Every pane holds its own month from end to end, whatever its length.
        var days = component.FindAll(".bit-cal-dbt").Count;
        Assert.AreEqual(days, component.FindAll(".bit-cal-dbt").Select(b => b.Id).Distinct().Count());
    }

    [TestMethod]
    public void BitCalendarMonthCountShouldNotRepeatTheLastMonthOfTheSupportedRange()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.MonthCount, 3);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeSpan.Zero));
        });

        // The months after the last one the calendar can represent do not exist, so the view holds the last
        // three rather than the last one three times - which would repeat the ids of its days.
        var titles = component.FindAll(".bit-cal-pkt").Select(t => t.TextContent.Trim()).ToArray();

        Assert.AreEqual(3, titles.Distinct().Count());
        Assert.AreEqual("October 9999", titles[0]);
        Assert.AreEqual("December 9999", titles[2]);
    }

    [TestMethod]
    public void BitCalendarEventTemplateShouldReplaceTheRowsOfTheDetailsDialog()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var day = DateTime.Today;

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitCalendarEvent { Title = "Retro", Body = "Sprint retrospective.", Date = DateOnly.FromDateTime(day) }
            ]);
            parameters.Add(p => p.EventTemplate, (BitCalendarEvent evt) => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-event");
                builder.AddContent(2, evt.Title);
                builder.CloseElement();
            });
        });

        component.FindAll(".bit-cal-dbt").First(b => b.Id!.EndsWith($"day-{day:yyyy-MM-dd}")).Click();

        Assert.AreEqual("Retro", component.Find(".bit-cal-emi .custom-event").TextContent.Trim());

        // The template is the whole of the row, so the default title and body it replaces are gone.
        Assert.IsEmpty(component.FindAll(".bit-cal-eit"));
        Assert.IsEmpty(component.FindAll(".bit-cal-eib"));
    }

    [TestMethod]
    public void BitCalendarTimePickerShouldStayInsideTheBoundsOfTheSelectedDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 14, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MinDate, new DateTimeOffset(2026, 1, 15, 9, 30, 0, TimeSpan.Zero));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // A bound carrying a time of day bounds the hours of the very day it falls on, so an hour typed
        // below it lands on it rather than producing an instant the bound rules out.
        var hourInput = component.FindAll(".bit-cal-tin")[0];
        hourInput.Input("3");
        hourInput.Change("3");

        Assert.AreEqual(9, value!.Value.Hour);
        Assert.AreEqual(30, value!.Value.Minute);

        // And the minute is bounded alongside it, but only within the hour the bound falls in.
        var minuteInput = component.FindAll(".bit-cal-tin")[1];
        minuteInput.Input("5");
        minuteInput.Change("5");

        Assert.AreEqual(9, value!.Value.Hour);
        Assert.AreEqual(30, value!.Value.Minute);
    }

    [TestMethod]
    public void BitCalendarTimePickerShouldBringTheCarriedTimeIntoTheBoundsOfTheDayPicked()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        DateTimeOffset? value = new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 1, 20, 10, 0, 0, TimeSpan.Zero));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The evening the picker is holding is past what the last allowed day allows, so it arrives at the
        // bound instead of pushing the value beyond it.
        component.FindAll(".bit-cal-dbt").First(b => b.Id!.EndsWith("day-2026-01-20")).Click();

        Assert.AreEqual(new DateTime(2026, 1, 20, 10, 0, 0), value!.Value.DateTime);
    }

    [TestMethod]
    public void BitCalendarParamsShouldHaveCorrectParamName()
    {
        var paramName = BitCalendarParams.ParamName;
        var expectedName = $"{nameof(BitParams)}.{nameof(BitCalendar)}";

        Assert.AreEqual(expectedName, paramName);
    }

    [TestMethod]
    public void BitCalendarParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitCalendarParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitCalendarParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitCalendarShouldApplyCascadingParametersFromBitParams()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var paramsList = new List<IBitComponentParams>
        {
            new BitCalendarParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                ShowWeekNumbers = true,
                ShowGoToToday = false,
                AriaLabel = "Cascaded Label"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                builder.CloseComponent();
            });
        });

        var calendar = component.Find(".bit-cal");

        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-suc"));
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-lg"));
        Assert.AreEqual("Cascaded Label", calendar.GetAttribute("aria-label"));
        Assert.IsTrue(component.FindAll(".bit-cal-wnm").Count > 0);
        Assert.AreEqual(0, component.FindAll(".bit-cal-gtb").Count);
    }

    [TestMethod]
    public void BitCalendarDirectParametersShouldOverrideCascadingParameters()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var paramsList = new List<IBitComponentParams>
        {
            new BitCalendarParams
            {
                Color = BitColor.Success,
                Size = BitSize.Large,
                ShowWeekNumbers = true
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                builder.AddAttribute(1, nameof(BitCalendar.Color), BitColor.Error);
                builder.AddAttribute(2, nameof(BitCalendar.Size), BitSize.Small);
                builder.CloseComponent();
            });
        });

        var calendar = component.Find(".bit-cal");

        // Direct parameters should override cascading ones
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-err"));
        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-sm"));

        // ShowWeekNumbers from cascading params should still apply (not overridden)
        Assert.IsTrue(component.FindAll(".bit-cal-wnm").Count > 0);
    }

    [TestMethod]
    public void BitCalendarParamsUpdateParametersShouldSetAllProperties()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var maxDate = DateTimeOffset.Now.AddMonths(1);
        var minDate = DateTimeOffset.Now.AddMonths(-1);
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
            IsEnabled = false,
            TabIndex = "5"
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                builder.CloseComponent();
            });
        });

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
        Assert.IsFalse(instance.IsEnabled);
        Assert.AreEqual("5", instance.TabIndex);
    }

    [TestMethod]
    public void BitCalendarParamsUpdateParametersShouldNotOverwriteExistingValues()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var @params = new BitCalendarParams
        {
            Color = BitColor.Success,
            Size = BitSize.Large,
            GoToTodayTitle = "Params Title"
        };

        // First render with direct parameters
        var component = RenderComponent<BitCalendar>(p =>
        {
            p.Add(x => x.Color, BitColor.Error);
            p.Add(x => x.Size, BitSize.Small);
            p.Add(x => x.GoToTodayTitle, "Existing Title");
        });

        var instance = component.Instance;

        // Verify initial values
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.GoToTodayTitle);

        // Now try to update with param, should not overwrite since properties were already set
        @params.UpdateParameters(instance);

        // Values should remain unchanged because HasNotBeenSet returns false
        Assert.AreEqual(BitColor.Error, instance.Color);
        Assert.AreEqual(BitSize.Small, instance.Size);
        Assert.AreEqual("Existing Title", instance.GoToTodayTitle);
    }

    [TestMethod]
    public void BitCalendarParamsShouldApplyClassesAndStyles()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

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

        var paramsList = new List<IBitComponentParams>
        {
            new BitCalendarParams { Classes = classes, Styles = styles }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                builder.CloseComponent();
            });
        });

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
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var paramsList = new List<IBitComponentParams>
        {
            new BitCalendarParams
            {
                // Each of these is a parameter the calendar builds its view from, and the cascade reaches the
                // component only after the pass that reads them has already run once.
                MonthCount = 3,
                Culture = CultureInfo.InvariantCulture,
                TimeZone = TimeZoneInfo.Utc,
                StartingValue = new DateTimeOffset(2021, 3, 10, 0, 0, 0, TimeSpan.Zero)
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCalendar>(0);
                builder.CloseComponent();
            });
        });

        var calendar = component.Find(".bit-cal");

        Assert.IsTrue(calendar.ClassList.Contains("bit-cal-mcv"));
        Assert.AreEqual(3, component.FindAll(".bit-cal-dwp").Count);

        // The strip opens on the month the cascaded StartingValue falls in, which only the rebuilt view knows about.
        Assert.IsTrue(component.FindAll(".bit-cal-dbt").Any(b => b.Id!.EndsWith("day-2021-03-10")));
    }
}
