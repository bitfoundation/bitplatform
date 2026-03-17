using System;
using Bunit;
using Microsoft.AspNetCore.Components;
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

        Assert.IsTrue(firstDayCell.TextContent.Contains("Day-"));
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
            Assert.IsTrue(weekNumbers.Count > 0);
        }
        else
        {
            Assert.AreEqual(0, weekNumbers.Count);
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
}
