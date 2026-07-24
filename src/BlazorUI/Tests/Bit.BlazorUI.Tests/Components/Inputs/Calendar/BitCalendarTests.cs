using System;
using System.Linq;
using System.Globalization;
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
    public void BitCalendarGoToNowIconNameShouldRenderCustomIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.GoToNowIconName, "CustomNowIcon");
        });

        var icon = component.Find(".bit-cal-gtn i");

        Assert.IsTrue(icon.ClassList.Contains("bit-icon--CustomNowIcon"));
    }

    [TestMethod]
    public void BitCalendarGoToNowIconShouldRenderExternalIcon()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.GoToNowIcon, BitIconInfo.Css("bi bi-clock"));
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
        var expectedTime = new TimeOnly(9, 30).ToString("HH:mm", System.Globalization.CultureInfo.CurrentUICulture);

        Assert.IsTrue(title?.Contains("Sync"));
        Assert.IsTrue(title?.Contains(expectedTime));
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

        var culture = System.Globalization.CultureInfo.CurrentUICulture;
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
            parameters.Add(p => p.Events, [
                new BitCalendarEvent { Title = "Event", Body = "Details", Date = new DateOnly(2026, 1, 15), StartTime = new TimeOnly(10, 0) }
            ]);
        });

        component.FindAll(".bit-cal-dbt").First(b => b.TextContent.Trim() == "15").Click();

        var timeEl = component.Find(".bit-cal-eis");
        var expectedTime = new TimeOnly(10, 0).ToString("HH:mm", System.Globalization.CultureInfo.CurrentUICulture);

        Assert.Contains("From", timeEl.TextContent);
        Assert.Contains(expectedTime, timeEl.TextContent);
    }

    [TestMethod]
    public void BitCalendarEventsModalShouldShowUntilTextForEndOnlyTime()
    {
        var component = RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.StartingValue, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
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
        var expectedTime = new TimeOnly(17, 0).ToString("HH:mm", System.Globalization.CultureInfo.CurrentUICulture);

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

        var timeText = component.Find(".bit-cal-eis").TextContent;
        var expectedFormatted = new TimeOnly(14, 30).ToString("h:mm tt", culture);  // "2:30 PM"
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
}
