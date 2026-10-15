using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

[TestClass]
public class BitDateRangePickerTests : BunitTestContext
{
    // Every path that opens the callout - a click, OpenCallout and an initial IsOpen alike - fits it to the width of
    // the viewport first, and a viewport the script answers no width for is too narrow for anything but overlays. So
    // the range picker is laid out on a desktop-wide viewport here, and a test of a narrow one sets the width it needs.
    [TestInitialize]
    public void SetupViewportWidth()
    {
        Context.JSInterop.Setup<decimal>("BitBlazorUI.Utils.getBodyWidth").SetResult(1920m);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitDateRangePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        var bitDatePicker = component.Find(".bit-dtrp");

        if (isEnabled)
        {
            Assert.IsFalse(bitDatePicker.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitDatePicker.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitDateRangePickerShouldRenderLabelFragment(string labelTemplate)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var bitDateRangePickerLabelChild = component.Find(".bit-dtrp > label").ChildNodes;
        bitDateRangePickerLabelChild.MarkupMatches(labelTemplate);
    }

    [TestMethod, DataRow("go to today text")]
    public void BitDateRangePickerShouldGiveValueToGoToToday(string goToToday)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.GoToTodayTitle, goToToday);
            parameters.Add(p => p.IsOpen, true);
        });

        var goToTodayButton = component.Find(".bit-dtrp-gtb");

        Assert.AreEqual(goToToday, goToTodayButton.GetAttribute("title"));
    }

    [TestMethod,
      DataRow(true, 1),
      DataRow(false, 0)
    ]
    public void BitDateRangePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var clickedValue = 0;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var bitDateRangePickerInput = component.Find(".bit-dtrp-wrp");
        bitDateRangePickerInput.Click();

        Assert.AreEqual(count, clickedValue);
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitDateRangePickerCalendarItemsShouldRespectDisabled(bool isEnabled)
    {
        var isOpen = true;
        var changeValue = 0;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Disabled, isEnabled is false);
            parameters.Add(p => p.OnChange, () => changeValue++);
        });

        // A day of the displayed month rather than one picked at random, so a failure is the same failure every run.
        component.FindAll(".bit-dtrp-dbt").First(d => d.TextContent.Trim() == "20" && d.ClassList.Contains("bit-dtrp-dbo") is false).Click();

        Assert.AreEqual(isEnabled ? 1 : 0, changeValue);
    }

    [TestMethod]
    public void BitDateRangePickerCalendarSelectTodayDate()
    {
        var isOpen = true;
        var today = FixedDate(2024, 6, 12);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.Today, today);
        });

        Assert.IsNull(component.Instance.Value);

        component.Find(".bit-dtrp-dtd").Click();

        // The first pick starts the range on today's date at the start of the day, in the offset of the component's
        // time zone, and leaves the end open.
        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(new DateTimeOffset(today.Date, today.Offset), component.Instance.Value.StartDate);
        Assert.IsNull(component.Instance.Value.EndDate);

        component.Find(".bit-dtrp-dtd").Click();

        // The second pick of the same day closes the range on it, at the last minute of the day.
        Assert.AreEqual(new DateTimeOffset(today.Date, today.Offset), component.Instance.Value.StartDate);
        Assert.AreEqual(new DateTimeOffset(today.Date, today.Offset).Add(new TimeSpan(23, 59, 0)), component.Instance.Value.EndDate);
    }

    [TestMethod,
        DataRow("DateRangePicker")
    ]
    public void BitDateRangePickerAriaLabelTest(string pickerAriaLabel)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutAriaLabel, pickerAriaLabel);
        });

        var bitDateRangePickerCallout = component.Find(".bit-dtrp-cac");
        var calloutAriaLabel = bitDateRangePickerCallout.GetAttribute("aria-label");

        Assert.AreEqual(pickerAriaLabel, calloutAriaLabel);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerShowGoToTodayTest(bool showGoToToday)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowGoToToday, showGoToToday);
        });

        var goToTodayBtnElms = component.FindAll(".bit-dtrp-gtb");

        if (showGoToToday)
        {
            Assert.AreEqual(1, goToTodayBtnElms.Count);
        }
        else
        {
            Assert.AreEqual(0, goToTodayBtnElms.Count);
        }
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerShowCloseButtonTest(bool showCloseButton)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, showCloseButton);
        });

        // The close button shares the nav buttons' class, so it is told apart by the name it is announced by.
        var closeBtnElms = component.FindAll("button[aria-label='Close date range picker']");

        Assert.HasCount(showCloseButton ? 1 : 0, closeBtnElms);
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotRenderACloseButtonWhenStandalone()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Standalone, true);
        });

        // A standalone calendar is not in a callout, so there is nothing for the button to close.
        Assert.HasCount(0, component.FindAll("button[aria-label='Close date range picker']"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerHighlightCurrentMonthTest(bool highlightCurrentMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.HighlightCurrentMonth, highlightCurrentMonth);
        });

        var currentMonthCells = component.FindAll(".bit-dtrp-pcm");

        if (highlightCurrentMonth)
        {
            Assert.AreEqual(1, currentMonthCells.Count);
        }
        else
        {
            Assert.AreEqual(0, currentMonthCells.Count);
        }
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerHighlightSelectedMonthTest(bool highlightSelectedMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.HighlightSelectedMonth, highlightSelectedMonth);
        });


        var selectedMonthCells = component.FindAll(".bit-dtrp-psm");

        if (highlightSelectedMonth)
        {
            Assert.AreEqual(1, selectedMonthCells.Count);
        }
        else
        {
            Assert.AreEqual(0, selectedMonthCells.Count);
        }
    }

    [TestMethod]
    public void BitDateRangePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            {"style", "color: blue" }
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var bitDateRangePickerCallout = component.Find(".bit-dtrp-cac");
        var calloutStyle = bitDateRangePickerCallout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }

    [TestMethod,
        DataRow("ChevronLeft", "bit-icon--ChevronLeft"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerPrevMonthNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { PrevMonthNavIcon = "prev-month-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevMonthNavIconName, iconName);
            }
        });

        var icon = component.Find(".prev-month-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerPrevMonthNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-left"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { PrevMonthNavIcon = "prev-month-icon" });
        });

        var icon = component.Find(".prev-month-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-left"),
            $"Expected 'fa-chevron-left' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerNextMonthNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { NextMonthNavIcon = "next-month-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.NextMonthNavIconName, iconName);
            }
        });

        var icon = component.Find(".next-month-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerNextMonthNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-right"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { NextMonthNavIcon = "next-month-icon" });
        });

        var icon = component.Find(".next-month-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-right"),
            $"Expected 'fa-chevron-right' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("CalendarDay", "bit-icon--CalendarDay"),
        DataRow(null, "bit-icon--GotoToday")
    ]
    public void BitDateRangePickerGoToTodayIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowGoToToday, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { GoToTodayIcon = "goto-today-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.GoToTodayIconName, iconName);
            }
        });

        var icon = component.Find(".goto-today-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerGoToTodayIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowGoToToday, true);
            parameters.Add(p => p.GoToTodayIcon, BitIconInfo.Css("fa-solid fa-calendar-day"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { GoToTodayIcon = "goto-today-icon" });
        });

        var icon = component.Find(".goto-today-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-calendar-day"),
            $"Expected 'fa-calendar-day' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("X", "bit-icon--X"),
        DataRow(null, "bit-icon--Cancel")
    ]
    public void BitDateRangePickerCloseButtonIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { CloseButtonIcon = "close-btn-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.CloseButtonIconName, iconName);
            }
        });

        var icon = component.Find(".close-btn-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerCloseButtonIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { CloseButtonIcon = "close-btn-icon" });
        });

        var icon = component.Find(".close-btn-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"),
            $"Expected 'fa-xmark' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("XmarkCircle", "bit-icon--XmarkCircle"),
        DataRow(null, "bit-icon--Cancel")
    ]
    public void BitDateRangePickerClearButtonIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = DateTimeOffset.Now });
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { ClearButtonIcon = "clear-btn-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.ClearButtonIconName, iconName);
            }
        });

        var icon = component.Find(".clear-btn-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerClearButtonIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = DateTimeOffset.Now });
            parameters.Add(p => p.ClearButtonIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { ClearButtonIcon = "clear-btn-icon" });
        });

        var icon = component.Find(".clear-btn-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"),
            $"Expected 'fa-xmark' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("CalendarMirroredSolid", "bit-icon--CalendarMirroredSolid"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerPrevYearNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { PrevYearNavIcon = "prev-year-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevYearNavIconName, iconName);
            }
        });

        var icon = component.Find(".prev-year-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerPrevYearNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-left"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { PrevYearNavIcon = "prev-year-icon" });
        });

        var icon = component.Find(".prev-year-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-left"),
            $"Expected 'fa-angles-left' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerNextYearNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { NextYearNavIcon = "next-year-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.NextYearNavIconName, iconName);
            }
        });

        var icon = component.Find(".next-year-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerNextYearNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-right"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { NextYearNavIcon = "next-year-icon" });
        });

        var icon = component.Find(".next-year-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-right"),
            $"Expected 'fa-angles-right' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronLeft", "bit-icon--ChevronLeft"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerPrevYearRangeNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                YearPickerToggleButton = "year-picker-toggle",
                PrevYearRangeNavIcon = "prev-year-range-icon"
            });

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevYearRangeNavIconName, iconName);
            }
        });

        // Click the year picker toggle to navigate from year view to year-range view
        component.Find(".year-picker-toggle").Click();

        var icon = component.Find(".prev-year-range-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerPrevYearRangeNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-angles-left"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                YearPickerToggleButton = "year-picker-toggle",
                PrevYearRangeNavIcon = "prev-year-range-icon"
            });
        });

        // Click the year picker toggle to navigate from year view to year-range view
        component.Find(".year-picker-toggle").Click();

        var icon = component.Find(".prev-year-range-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on PrevYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-left"),
            $"Expected 'fa-angles-left' on PrevYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDateRangePickerNextYearRangeNavIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                YearPickerToggleButton = "year-picker-toggle",
                NextYearRangeNavIcon = "next-year-range-icon"
            });

            if (iconName is not null)
            {
                parameters.Add(p => p.NextYearRangeNavIconName, iconName);
            }
        });

        // Click the year picker toggle to navigate from year view to year-range view
        component.Find(".year-picker-toggle").Click();

        var icon = component.Find(".next-year-range-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerNextYearRangeNavIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-angles-right"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                YearPickerToggleButton = "year-picker-toggle",
                NextYearRangeNavIcon = "next-year-range-icon"
            });
        });

        // Click the year picker toggle to navigate from year view to year-range view
        component.Find(".year-picker-toggle").Click();

        var icon = component.Find(".next-year-range-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on NextYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-right"),
            $"Expected 'fa-angles-right' on NextYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    // The show/hide time-picker buttons only exist once the callout has been opened through a click,
    // since that is what puts the time picker into its overlay mode.
    private static IRenderedComponent<BitDateRangePicker> RenderOpenedOverlayTimePicker(
        Action<ComponentParameterCollectionBuilder<BitDateRangePicker>> parameterBuilder,
        BunitContext context)
    {
        var isOpen = false;
        var component = context.Render<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameterBuilder(parameters);
        });

        component.Find(".bit-dtrp-wrp").Click();

        return component;
    }

    [TestMethod,
        DataRow("Clock", "bit-icon--Clock"),
        DataRow(null, "bit-icon--Clock")
    ]
    public void BitDateRangePickerShowTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderOpenedOverlayTimePicker(parameters =>
        {
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { ShowTimePickerIcon = "show-time-picker-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.ShowTimePickerIconName, iconName);
            }
        }, Context);

        var icon = component.Find(".show-time-picker-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on ShowTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerShowTimePickerIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderOpenedOverlayTimePicker(parameters =>
        {
            parameters.Add(p => p.ShowTimePickerIcon, BitIconInfo.Css("fa-solid fa-clock"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { ShowTimePickerIcon = "show-time-picker-icon" });
        }, Context);

        var icon = component.Find(".show-time-picker-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on ShowTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-clock"),
            $"Expected 'fa-clock' on ShowTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("CalendarSolid", "bit-icon--CalendarSolid"),
        DataRow(null, "bit-icon--CalendarMirrored")
    ]
    public void BitDateRangePickerHideTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderOpenedOverlayTimePicker(parameters =>
        {
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                ShowTimePickerButton = "show-time-picker-button",
                HideTimePickerIcon = "hide-time-picker-icon"
            });

            if (iconName is not null)
            {
                parameters.Add(p => p.HideTimePickerIconName, iconName);
            }
        }, Context);

        // Bring the time picker overlay on top so its hide button gets rendered.
        component.Find(".show-time-picker-button").Click();

        var icon = component.Find(".hide-time-picker-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on HideTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerHideTimePickerIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderOpenedOverlayTimePicker(parameters =>
        {
            parameters.Add(p => p.HideTimePickerIcon, BitIconInfo.Css("fa-solid fa-calendar"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                ShowTimePickerButton = "show-time-picker-button",
                HideTimePickerIcon = "hide-time-picker-icon"
            });
        }, Context);

        component.Find(".show-time-picker-button").Click();

        var icon = component.Find(".hide-time-picker-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on HideTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-calendar"),
            $"Expected 'fa-calendar' on HideTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUp", "bit-icon--ChevronUp"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerStartTimeIncreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeIncreaseHourIcon = "start-inc-hour-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.StartTimeIncreaseHourIconName, iconName);
            }
        });

        var icon = component.Find(".start-inc-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on StartTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeIncreaseHourIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartTimeIncreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeIncreaseHourIcon = "start-inc-hour-icon" });
        });

        var icon = component.Find(".start-inc-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on StartTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on StartTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronDown", "bit-icon--ChevronDown"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerStartTimeDecreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeDecreaseHourIcon = "start-dec-hour-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.StartTimeDecreaseHourIconName, iconName);
            }
        });

        var icon = component.Find(".start-dec-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on StartTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeDecreaseHourIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartTimeDecreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeDecreaseHourIcon = "start-dec-hour-icon" });
        });

        var icon = component.Find(".start-dec-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on StartTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on StartTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUp", "bit-icon--ChevronUp"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerStartTimeIncreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeIncreaseMinuteIcon = "start-inc-min-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.StartTimeIncreaseMinuteIconName, iconName);
            }
        });

        var icon = component.Find(".start-inc-min-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on StartTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeIncreaseMinuteIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartTimeIncreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeIncreaseMinuteIcon = "start-inc-min-icon" });
        });

        var icon = component.Find(".start-inc-min-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on StartTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on StartTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronDown", "bit-icon--ChevronDown"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerStartTimeDecreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeDecreaseMinuteIcon = "start-dec-min-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.StartTimeDecreaseMinuteIconName, iconName);
            }
        });

        var icon = component.Find(".start-dec-min-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on StartTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeDecreaseMinuteIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartTimeDecreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { StartTimeDecreaseMinuteIcon = "start-dec-min-icon" });
        });

        var icon = component.Find(".start-dec-min-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on StartTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on StartTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUp", "bit-icon--ChevronUp"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerEndTimeIncreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeIncreaseHourIcon = "end-inc-hour-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.EndTimeIncreaseHourIconName, iconName);
            }
        });

        var icon = component.Find(".end-inc-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on EndTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeIncreaseHourIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.EndTimeIncreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeIncreaseHourIcon = "end-inc-hour-icon" });
        });

        var icon = component.Find(".end-inc-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on EndTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on EndTimeIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronDown", "bit-icon--ChevronDown"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerEndTimeDecreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeDecreaseHourIcon = "end-dec-hour-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.EndTimeDecreaseHourIconName, iconName);
            }
        });

        var icon = component.Find(".end-dec-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on EndTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeDecreaseHourIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.EndTimeDecreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeDecreaseHourIcon = "end-dec-hour-icon" });
        });

        var icon = component.Find(".end-dec-hour-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on EndTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on EndTimeDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUp", "bit-icon--ChevronUp"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerEndTimeIncreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeIncreaseMinuteIcon = "end-inc-min-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.EndTimeIncreaseMinuteIconName, iconName);
            }
        });

        var icon = component.Find(".end-inc-min-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on EndTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeIncreaseMinuteIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.EndTimeIncreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeIncreaseMinuteIcon = "end-inc-min-icon" });
        });

        var icon = component.Find(".end-inc-min-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on EndTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on EndTimeIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronDown", "bit-icon--ChevronDown"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDateRangePickerEndTimeDecreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeDecreaseMinuteIcon = "end-dec-min-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.EndTimeDecreaseMinuteIconName, iconName);
            }
        });

        var icon = component.Find(".end-dec-min-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on EndTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeDecreaseMinuteIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.EndTimeDecreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { EndTimeDecreaseMinuteIcon = "end-dec-min-icon" });
        });

        var icon = component.Find(".end-dec-min-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on EndTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on EndTimeDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public async Task BitDateRangePickerDisposeShouldNotThrow()
    {
        var component = RenderComponent<BitDateRangePicker>(p =>
        {
            p.Add(x => x.ShowTimePicker, true);
        });

        await component.Instance.DisposeAsync();
    }

    // A Dir that names no direction, under a culture that does not imply one, leaves it to the page (unset) or to the
    // content (Auto), which is read off the picker for the horizontal arrows alone.
    [TestMethod,
        DataRow(null, true),
        DataRow(BitDir.Auto, true),
        DataRow(BitDir.Ltr, false)
    ]
    public void BitDateRangePickerKeyboardNavigationShouldFollowAResolvedDirection(BitDir? dir, bool rtl)
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.Utils.isRtl", _ => true).SetResult(true);
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = new DateTimeOffset(2020, 1, 15, 0, 0, 0, DateTimeOffset.Now.Offset) });
        });

        var before = int.Parse(component.Find(".bit-dtrp-dbt[tabindex='0']").TextContent.Trim());

        component.Find(".bit-dtrp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var after = int.Parse(component.Find(".bit-dtrp-dbt[tabindex='0']").TextContent.Trim());

        Assert.AreEqual(rtl ? before - 1 : before + 1, after);
    }

    [TestMethod]
    public void BitDateRangePickerShouldRespectDefaultValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var defaultValue = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(2020, 1, 15, 0, 0, 0, DateTimeOffset.Now.Offset),
            EndDate = new DateTimeOffset(2020, 1, 20, 0, 0, 0, DateTimeOffset.Now.Offset)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, component.Instance.Value);
        Assert.AreEqual(defaultValue.StartDate, component.Instance.Value!.StartDate);
        Assert.AreEqual(defaultValue.EndDate, component.Instance.Value!.EndDate);
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-dtrp-pri"),
        DataRow(BitColor.Secondary, "bit-dtrp-sec"),
        DataRow(BitColor.Tertiary, "bit-dtrp-ter"),
        DataRow(BitColor.Info, "bit-dtrp-inf"),
        DataRow(BitColor.Success, "bit-dtrp-suc"),
        DataRow(BitColor.Warning, "bit-dtrp-wrn"),
        DataRow(BitColor.SevereWarning, "bit-dtrp-swr"),
        DataRow(BitColor.Error, "bit-dtrp-err")
    ]
    public void BitDateRangePickerColorTest(BitColor color, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains(expectedClass));
        // The callout is rendered outside of the root element, so it carries the color class on its own.
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDateRangePickerDisabledDaysOfWeekTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDaysOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        });

        var days = component.FindAll(".bit-dtrp-dbt");

        // With the week pinned to start on Sunday, the very first rendered cell is a disabled weekend day.
        Assert.IsTrue(days.Count(d => d.HasAttribute("disabled")) > 0);
        Assert.IsTrue(days[0].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerDisabledDatesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);
        var disabledDate = today.AddDays(1);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisabledDates, new[] { disabledDate });
        });

        var disabledCount = component.FindAll(".bit-dtrp-dbt").Count(d => d.HasAttribute("disabled"));

        Assert.AreEqual(1, disabledCount);
    }

    [TestMethod]
    public void BitDateRangePickerIsDateDisabledTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsDateDisabled, (DateTimeOffset date) => date.Day % 2 == 0);
        });

        var days = component.FindAll(".bit-dtrp-dbt");

        Assert.IsTrue(days.Count(d => d.HasAttribute("disabled")) > 0);
        Assert.IsTrue(days.All(d => d.HasAttribute("disabled") is false || int.Parse(d.TextContent.Trim()) % 2 == 0));
    }

    [TestMethod]
    public void BitDateRangePickerDisabledDateShouldNotBeSelectable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsDateDisabled, (DateTimeOffset _) => true);
        });

        component.FindAll(".bit-dtrp-dbt")[0].Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerHighlightedDatesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);
        var highlightedDate = today.AddDays(1);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.HighlightedDates, new[] { highlightedDate });
        });

        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dhl").Count);
    }

    [TestMethod]
    public void BitDateRangePickerGetDayClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.GetDayClass, (DateTimeOffset date) => date.DayOfWeek == DayOfWeek.Friday ? "custom-day" : null);
        });

        var fridays = component.FindAll(".custom-day");

        Assert.IsTrue(fridays.Count >= 4);
    }

    [TestMethod]
    public void BitDateRangePickerFirstDayOfWeekTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Monday);
        });

        var firstHeaderCell = component.FindAll(".bit-dtrp-dgh .bit-dtrp-wlb")[0];

        Assert.AreEqual("Monday", firstHeaderCell.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDateRangePickerShowOutsideDaysTest(bool showOutsideDays)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowOutsideDays, showOutsideDays);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
        });

        var emptyCells = component.FindAll(".bit-dtrp-dbe");
        var outsideDays = component.FindAll(".bit-dtrp-dbo");

        if (showOutsideDays)
        {
            Assert.AreEqual(0, emptyCells.Count);
            Assert.IsTrue(outsideDays.Count > 0);
        }
        else
        {
            Assert.IsTrue(emptyCells.Count > 0);
            Assert.AreEqual(0, outsideDays.Count);
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDateRangePickerFixedWeeksTest(bool fixedWeeks)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        // February 2021 starts on a Monday and has exactly 28 days, so it only fills 5 rows
        // (4 without the leading week) unless FixedWeeks pads it up to 6.
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FixedWeeks, fixedWeeks);
            parameters.Add(p => p.Today, new DateTimeOffset(2021, 2, 10, 0, 0, 0, TimeSpan.Zero));
        });

        var rows = component.FindAll(".bit-dtrp-dgr");

        if (fixedWeeks)
        {
            Assert.AreEqual(6, rows.Count);
        }
        else
        {
            Assert.IsTrue(rows.Count < 6);
        }
    }

    [TestMethod]
    public void BitDateRangePickerTodayTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, new DateTimeOffset(2020, 12, 4, 0, 0, 0, TimeSpan.Zero));
        });

        var todayCell = component.Find(".bit-dtrp-dtd");

        Assert.AreEqual("4", todayCell.TextContent.Trim());
        Assert.AreEqual("date", todayCell.GetAttribute("aria-current"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDateRangePickerIsMonthPickerVisibleTest(bool isMonthPickerVisible)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsMonthPickerVisible, isMonthPickerVisible);
        });

        // The day picker must always stay visible.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp").Count);
        Assert.AreEqual(isMonthPickerVisible ? 1 : 0, component.FindAll(".bit-dtrp-mwp").Count);
    }

    [TestMethod]
    public void BitDateRangePickerHiddenMonthPickerWithTimePickerTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.IsMonthPickerVisible, false);
        });

        // Hiding the month picker must not take the day picker or the time picker down with it.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-twp").Count);
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-mwp").Count);
    }

    [TestMethod]
    public void BitDateRangePickerWeekNumberTitleTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowWeekNumbers, true);
            parameters.Add(p => p.WeekNumberTitle, "W {0}");
        });

        var weekNumber = component.FindAll(".bit-dtrp-wnm")[0];

        Assert.AreEqual($"W {weekNumber.TextContent.Trim()}", weekNumber.GetAttribute("title"));
        Assert.AreEqual(weekNumber.GetAttribute("title"), weekNumber.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerSelectedDateAriaAtomicTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.SelectedDateAriaAtomic, "Picked {0}");
            parameters.Add(p => p.ValueFormat, "{0}~{1}");
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.Value, new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero)
            });
        });

        Assert.AreEqual("Picked 2024-03-01~2024-03-05", component.Find(".bit-dtrp-sdt").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerFormatValueAsStringTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.Value, new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero)
            });
        });

        // A range without an end date renders the "---" placeholder.
        Assert.AreEqual("Start: 2024-03-01 - End: ---", component.Find(".bit-dtrp-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-05");

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(new DateTime(2024, 3, 1), component.Instance.Value!.StartDate!.Value.DateTime);
        Assert.AreEqual(new DateTime(2024, 3, 5), component.Instance.Value!.EndDate!.Value.DateTime);
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputShouldFocusTheInputWithoutScrollingWhenAClickOpensIt()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
        });

        // Read before the click: a re-render leaves the reference off the markup of an element it keeps.
        var input = component.Find(".bit-dtrp-inp").GetAttribute("blazor:elementReference");

        component.Find(".bit-dtrp-wrp").Click();

        Assert.IsTrue(component.Instance.IsOpen);

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        // The field was just clicked, so it is in view, and a focus that scrolled to it would scroll a sticky
        // header's container back to where the header sits in the flow.
        Assert.AreEqual(1, focused.Count);
        Assert.AreEqual(input, ((ElementReference)focused[0].Arguments[0]!).Id);
        Assert.AreEqual(true, focused[0].Arguments[1]);
    }

    [TestMethod]
    public async Task BitDateRangePickerAllowTextInputOpenCalloutShouldBringTheInputIntoView()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
        });

        var input = component.Find(".bit-dtrp-inp").GetAttribute("blazor:elementReference");

        await component.InvokeAsync(() => component.Instance.OpenCallout());

        Assert.IsTrue(component.Instance.IsOpen);

        var focused = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].ToList();

        // Opened from anywhere on the page, so the field may well be out of view: the focus is what brings it there.
        Assert.AreEqual(1, focused.Count);
        Assert.AreEqual(input, ((ElementReference)focused[0].Arguments[0]!).Id);
        Assert.AreEqual(false, focused[0].Arguments[1]);
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputShouldSwapReversedDates()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-05 - 2024-03-01");

        Assert.AreEqual(new DateTime(2024, 3, 1), component.Instance.Value!.StartDate!.Value.DateTime);
        Assert.AreEqual(new DateTime(2024, 3, 5), component.Instance.Value!.EndDate!.Value.DateTime);
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputShouldAcceptAnOpenEndedRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - ---");

        Assert.AreEqual(new DateTime(2024, 3, 1), component.Instance.Value!.StartDate!.Value.DateTime);
        Assert.IsNull(component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputShouldClearOnEmptyText()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-inp").Change(string.Empty);

        Assert.IsNull(component.Instance.Value);
        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitDateRangePickerAllowTextInputShouldRejectAnInvalidText()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
        });

        component.Find(".bit-dtrp-inp").Change("not a date range");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTextInputShouldBeIgnoredWhenNotAllowed()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-05");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerMinRangeTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var today = FixedDate(2024, 3, 20);
        var startDate = today.AddDays(-15);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.MinRange, TimeSpan.FromDays(3));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = startDate });
        });

        var startCell = component.Find(".bit-dtrp-dss");

        // The start date itself and the two days around it cannot close a 3-day-minimum range.
        Assert.IsTrue(startCell.HasAttribute("disabled"));

        var enabledDays = component.FindAll(".bit-dtrp-dbt").Count(d => d.HasAttribute("disabled") is false);
        Assert.IsTrue(enabledDays > 0);
    }

    [TestMethod]
    public void BitDateRangePickerMinRangeShouldNotDisableAnythingWithoutAStartDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MinRange, TimeSpan.FromDays(3));
        });

        Assert.AreEqual(0, component.FindAll(".bit-dtrp-dbt").Count(d => d.HasAttribute("disabled")));
    }

    [TestMethod]
    public void BitDateRangePickerHoverRangePreviewTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
        });

        var days = component.FindAll(".bit-dtrp-dbt");

        // No start date yet, so hovering must not preview anything.
        days[10].PointerEnter();
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-dhr").Count);

        component.FindAll(".bit-dtrp-dbt")[10].Click();
        component.FindAll(".bit-dtrp-dbt")[14].PointerEnter();

        // The four days between the start date (exclusive) and the hovered one (inclusive).
        Assert.AreEqual(4, component.FindAll(".bit-dtrp-dhr").Count);

        // Closing the range clears the preview.
        component.FindAll(".bit-dtrp-dbt")[14].Click();
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-dhr").Count);
    }

    [TestMethod]
    public void BitDateRangePickerDayKeyboardNavigationTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
        });

        var monthTitleBefore = component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Trim();

        // Today is the roving-tabindex day, so PageDown from it moves the calendar to the next month.
        component.Find(".bit-dtrp-dtd").KeyDown(Key.PageDown);

        var monthTitleAfter = component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Trim();

        Assert.AreNotEqual(monthTitleBefore, monthTitleAfter);
    }

    [TestMethod]
    public void BitDateRangePickerKeyboardNavigationShouldSkipDisabledDays()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            // A gap of disabled days right after today: the arrow key has to jump over it,
            // not stop at its edge.
            parameters.Add(p => p.DisabledDates, new[]
            {
                today.AddDays(1),
                today.AddDays(2),
                today.AddDays(3),
            });
        });

        var focusedBefore = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        Assert.AreEqual("12", focusedBefore.TextContent.Trim());

        focusedBefore.KeyDown(Key.Right);

        var focusedAfter = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");

        Assert.AreEqual("16", focusedAfter.TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerKeyboardNavigationShouldJumpOverTheMinRangeGap()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MinRange, TimeSpan.FromDays(4));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = today });
        });

        // June 12 is the start date, so June 9 to June 15 cannot close a 4-day-minimum range.
        // Moving right from June 8 has to jump the whole gap instead of stopping at its edge.
        var june8 = component.FindAll(".bit-dtrp-dbt")
                             .Single(d => d.TextContent.Trim() == "8" && d.ClassList.Contains("bit-dtrp-dbo") is false);
        Assert.IsFalse(june8.HasAttribute("disabled"));

        june8.KeyDown(Key.Right);

        var focusedAfter = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");

        Assert.AreEqual("16", focusedAfter.TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerKeyboardNavigationShouldStopAtMaxDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MaxDate, today);
        });

        var focused = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        focused.KeyDown(Key.Right);

        var focusedAfter = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");

        Assert.AreEqual("12", focusedAfter.TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerRovingTabIndexTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var tabbableDays = component.FindAll(".bit-dtrp-dbt").Where(d => d.GetAttribute("tabindex") == "0").ToList();

        Assert.AreEqual(1, tabbableDays.Count);
    }

    [TestMethod]
    public void BitDateRangePickerOnMonthChangeTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        DateTimeOffset? changedMonth = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.OnMonthChange, (DateTimeOffset date) => changedMonth = date);
        });

        component.FindAll(".bit-dtrp-nbt")[1].Click();

        Assert.IsNotNull(changedMonth);
        Assert.AreEqual(1, changedMonth!.Value.Day);
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeAmPmButtonsTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new BitDateRangePickerValue
            {
                // 14:00 is PM, so the AM button must be the enabled one.
                StartDate = new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero)
            });
        });

        var endAmButton = component.FindAll(".bit-dtrp-eic .bit-dtrp-bam")[0];
        var endPmButton = component.FindAll(".bit-dtrp-eic .bit-dtrp-bpm")[0];

        Assert.IsFalse(endAmButton.HasAttribute("disabled"));
        Assert.IsTrue(endPmButton.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerEndTimeAmClickShouldSwitchToAm()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-dtrp-eic .bit-dtrp-bam")[0].Click();

        Assert.AreEqual(2, component.Instance.Value!.EndDate!.Value.Hour);
    }

    [TestMethod]
    public void BitDateRangePickerSelectDateShouldNotMutateTheBoundInstance()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        // The range stays inside the month today falls in: the grid opens on the month of the start
        // date, and a today cell belonging to an adjacent month renders as an outside day, which
        // carries no today class to click.
        var today = FixedDate(2024, 6, 12);
        var monthStart = FixedDate(2024, 6, 1);
        var boundValue = new BitDateRangePickerValue
        {
            StartDate = monthStart,
            EndDate = monthStart.AddDays(1)
        };
        var originalStart = boundValue.StartDate;
        var originalEnd = boundValue.EndDate;
        BitDateRangePickerValue? currentValue = boundValue;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Today, today);
            parameters.Bind(p => p.Value, currentValue, v => currentValue = v);
        });

        // A third click restarts the range, which used to null out the caller's own instance.
        component.Find(".bit-dtrp-dtd").Click();

        Assert.AreEqual(originalStart, boundValue.StartDate);
        Assert.AreEqual(originalEnd, boundValue.EndDate);
        Assert.AreNotSame(boundValue, component.Instance.Value);
        Assert.AreEqual(today.Date, component.Instance.Value!.StartDate!.Value.Date);
        Assert.IsNull(component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerPresetsShouldNotRenderWithoutPresets()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-dtrp-prc").Count);
    }

    [TestMethod]
    public void BitDateRangePickerPresetsRenderTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PresetsAriaLabel, "Quick ranges");
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Today", Value = new() { StartDate = DateTimeOffset.Now.Date, EndDate = DateTimeOffset.Now.Date } },
                new() { Text = "Disabled one", IsDisabled = true, Title = "Not yet" },
            });
        });

        var container = component.Find(".bit-dtrp-prc");
        var buttons = component.FindAll(".bit-dtrp-prb");

        Assert.AreEqual("Quick ranges", container.GetAttribute("aria-label"));
        Assert.AreEqual(2, buttons.Count);
        Assert.AreEqual("Today", buttons[0].TextContent.Trim());
        Assert.IsFalse(buttons[0].HasAttribute("disabled"));
        Assert.IsTrue(buttons[1].HasAttribute("disabled"));
        Assert.AreEqual("Not yet", buttons[1].GetAttribute("title"));
    }

    [TestMethod]
    public void BitDateRangePickerPresetSelectionTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var start = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "March 1-5", Value = new() { StartDate = start, EndDate = end } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.AreEqual(start, component.Instance.Value!.StartDate);
        Assert.AreEqual(end, component.Instance.Value!.EndDate);
        // AutoClose is on by default, so selecting a preset closes the callout.
        Assert.IsFalse(isOpen);
        // The matching preset is marked as the selected one.
        Assert.IsTrue(component.Find(".bit-dtrp-prb").ClassList.Contains("bit-dtrp-prs"));
        Assert.AreEqual("true", component.Find(".bit-dtrp-prb").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitDateRangePickerPresetValueProviderShouldTakePrecedence()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        var providedStart = new DateTimeOffset(2024, 5, 10, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new()
                {
                    Text = "Provided",
                    Value = new() { StartDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) },
                    ValueProvider = () => new() { StartDate = providedStart, EndDate = providedStart.AddDays(2) }
                },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.AreEqual(providedStart, component.Instance.Value!.StartDate);
        Assert.AreEqual(providedStart.AddDays(2), component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerReadOnlyShouldNotAllowSelectingAPreset()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Today", Value = new() { StartDate = DateTimeOffset.Now.Date } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerDisabledPresetShouldNotBeSelectable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Today", IsDisabled = true, Value = new() { StartDate = DateTimeOffset.Now.Date } },
            });
        });

        var preset = component.Find(".bit-dtrp-prb");

        Assert.IsTrue(preset.HasAttribute("disabled"));

        preset.Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPresetShouldNavigateTheCalendarToItsStartDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new()
                {
                    Text = "March 2024",
                    Value = new()
                    {
                        StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2024, 3, 31, 0, 0, 0, TimeSpan.Zero)
                    }
                },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsTrue(component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Contains(MonthTitle(2024, 3)));
    }

    [TestMethod]
    public void BitDateRangePickerInputArrowDownShouldOpenTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = false;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtrp-inp").KeyDown(Key.Down);

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDateRangePickerInputEscapeShouldCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtrp-inp").KeyDown(Key.Escape);

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDateRangePickerTabOnTheInputShouldLeaveTheCalloutToWhereTheFocusLands()
    {
        var closed = 0;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.OnClose, () => closed++);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // Whether a Tab dismisses the callout depends on where the focus lands - the clear button beside the
        // field keeps it open, a control elsewhere on the page does not - which only the JS side knows.
        component.Find(".bit-dtrp-inp").KeyDown(Key.Tab);

        Assert.IsTrue(isOpen);
        Assert.AreEqual(0, closed);
    }

    [TestMethod]
    public async Task BitDateRangePickerShouldCloseWithoutTakingTheFocusBackWhenTheJsSideDismissesIt()
    {
        var closed = 0;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.OnClose, () => closed++);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        var before = Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;

        // The JS side dismisses the callout once the focus has moved on from both the field and it, which
        // reaches the picker as the CloseCallout callback.
        await component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, closed);

        // The focus is already on whatever it was moved to, so it is not pulled back onto the field.
        Assert.AreEqual(before, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public async Task BitDateRangePickerShouldRollBackATypedRangeWhenTheFocusMovesOnWithoutApplying()
    {
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-wrp").Click();

        // A Tab off the field commits what was typed into it through the change event, which the browser fires
        // before the focus leaves - while the callout, and the transaction it is, is still open.
        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-05");

        Assert.IsNotNull(value);

        // The focus then lands outside the picker and the JS side dismisses the callout, which is not Apply.
        await component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened());

        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitDateRangePickerInputEnterShouldNotOpenWhenTextInputIsAllowed()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = false;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AllowTextInput, true);
        });

        component.Find(".bit-dtrp-inp").KeyDown(Key.Enter);

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDateRangePickerCalloutEscapeShouldCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        // Escape has to work from anywhere inside the dialog, not only from the day buttons.
        component.Find(".bit-dtrp-nbt").KeyDown(Key.Escape);

        Assert.IsFalse(isOpen);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDateRangePickerCalloutShouldCarryTheDisabledClass(bool isEnabled)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, isEnabled is false);
        });

        // The callout renders outside of the root element, so it needs the marker of its own.
        Assert.AreEqual(isEnabled is false, component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitDateRangePickerCalloutShouldCarryTheDir()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", component.Find(".bit-dtrp-cal").GetAttribute("dir"));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dtrp-rtl"));
    }

    [TestMethod]
    public void BitDateRangePickerAriaLiveShouldStayEmptyWithoutAValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>();

        Assert.AreEqual(string.Empty, component.Find(".bit-dtrp-sdt").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeMinuteShouldRespectMaxRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var value = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 1, 11, 0, 0, TimeSpan.Zero)
        };
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(2));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // 10:59 to 11:00 still fits the two-hour MaxRange, so the typed minute is applied,
        // and the start time can never get past the end time of the same day.
        component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[1].Input("59");

        Assert.AreEqual(59, component.Instance.Value!.StartDate!.Value.Minute);
        Assert.IsTrue(component.Instance.Value!.StartDate <= component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldRenderConsecutiveMonths()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 10) });
        });

        var titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(2, titles.Count);
        Assert.AreEqual(MonthTitle(2024, 3), titles[0].TextContent.Trim());
        Assert.AreEqual(MonthTitle(2024, 4), titles[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldBeCappedAtThreeMonths()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 7);
        });

        Assert.AreEqual(3, component.FindAll(".bit-dtrp-dwp").Count);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldRenderASingleNavigationOfEachKind()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 3);
        });

        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp [title='Go to previous month']").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp [title='Go to next month']").Count);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountNavigationShouldShiftEveryRenderedMonth()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 10) });
        });

        component.Find(".bit-dtrp-dwp [title='Go to next month']").Click();

        var titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(MonthTitle(2024, 4), titles[0].TextContent.Trim());
        Assert.AreEqual(MonthTitle(2024, 5), titles[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldNotRenderDuplicateOutsideDays()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.ShowOutsideDays, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 10) });
        });

        var ids = component.FindAll(".bit-dtrp-dbt").Select(d => d.GetAttribute("id")).ToList();

        Assert.AreEqual(0, component.FindAll(".bit-dtrp-dbo").Count);
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
        // Every day of March and of April, and nothing else.
        Assert.AreEqual(61, ids.Count);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldKeepTheCalendarStillWhenPickingInTheSecondMonth()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 10));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The second grid holds April, so its first day starts the range without moving the months.
        component.FindAll(".bit-dtrp-dwp")[1].QuerySelectorAll(".bit-dtrp-dbt")[0].Click();

        var titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(4, value!.StartDate!.Value.Month);
        Assert.AreEqual(MonthTitle(2024, 3), titles[0].TextContent.Trim());
        Assert.AreEqual(MonthTitle(2024, 4), titles[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerSelectingADayOfTheNextYearShouldNotJumpAYearBack()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        BitDateRangePickerValue? value = new() { StartDate = FixedDate(2024, 12, 30) };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // December 2024 starts on a Sunday, so with a Sunday-starting week the only outside days
        // of its grid are those of January 2025.
        component.FindAll(".bit-dtrp-dbo")[0].Click();

        Assert.AreEqual(2025, value!.EndDate!.Value.Year);
        Assert.AreEqual(MonthTitle(2025, 1), component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerExcludeDisabledDatesShouldBlockRangesCoveringADisabledDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ExcludeDisabledDates, true);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 1));
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 6) });
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 4) });
        });

        var days = component.FindAll(".bit-dtrp-dbt").Where(d => d.ClassList.Contains("bit-dtrp-dbo") is false).ToList();

        Assert.IsFalse(days[4].HasAttribute("disabled"));  // March 5, its range covers nothing disabled
        Assert.IsTrue(days[5].HasAttribute("disabled"));   // March 6, disabled on its own
        Assert.IsTrue(days[6].HasAttribute("disabled"));   // March 7, its range would cover March 6
    }

    [TestMethod]
    public void BitDateRangePickerExcludeDisabledDatesShouldNotBlockAnythingWithoutAStartDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ExcludeDisabledDates, true);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 1));
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 6) });
        });

        var disabledCount = component.FindAll(".bit-dtrp-dbt").Count(d => d.HasAttribute("disabled"));

        Assert.AreEqual(1, disabledCount);
    }

    [TestMethod]
    public void BitDateRangePickerNoDateTextShouldReplaceTheMissingEndDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.NoDateText, "n/a");
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 4) });
        });

        Assert.AreEqual("04/03/2024 - n/a", component.Find(".bit-dtrp-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerNoDateTextShouldBeAcceptedBackAsAnOpenEndedRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.NoDateText, "n/a");
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-inp").Change("04/03/2024 - n/a");

        Assert.AreEqual(4, value!.StartDate!.Value.Day);
        Assert.IsFalse(value!.EndDate.HasValue);
    }

    [TestMethod]
    public void BitDateRangePickerClearButtonShouldBeLabelledAndExposedToScreenReaders()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.ClearButtonTitle, "Clear the dates");
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 4) });
        });

        var clearButton = component.Find(".bit-dtrp-clr");

        Assert.AreEqual("Clear the dates", clearButton.GetAttribute("title"));
        Assert.AreEqual("Clear the dates", clearButton.GetAttribute("aria-label"));
        Assert.IsFalse(clearButton.HasAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitDateRangePickerRelativePresetShouldStaySelectedAfterBeingApplied()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                // A provider is re-evaluated on every call, so its range never matches the stored one exactly.
                new()
                {
                    Text = "Last 7 days",
                    ValueProvider = () => new()
                    {
                        StartDate = DateTimeOffset.Now.AddDays(-7),
                        EndDate = DateTimeOffset.Now
                    }
                },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsTrue(component.Find(".bit-dtrp-prb").ClassList.Contains("bit-dtrp-prs"));
    }

    [TestMethod]
    public void BitDateRangePickerSelectingADayShouldDropThePresetSelection()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 10));
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new()
                {
                    Text = "Last 7 days",
                    ValueProvider = () => new()
                    {
                        StartDate = DateTimeOffset.Now.AddDays(-7),
                        EndDate = DateTimeOffset.Now
                    }
                },
            });
        });

        component.Find(".bit-dtrp-prb").Click();
        component.FindAll(".bit-dtrp-dbt").First(d => d.HasAttribute("disabled") is false).Click();

        Assert.IsFalse(component.Find(".bit-dtrp-prb").ClassList.Contains("bit-dtrp-prs"));
    }

    [TestMethod]
    public void BitDateRangePickerOnPresetSelectTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;
        BitDateRangePickerPreset? selected = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.OnPresetSelect, (BitDateRangePickerPreset preset) => selected = preset);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new()
                {
                    Text = "March 2024",
                    Value = new()
                    {
                        StartDate = FixedDate(2024, 3, 1),
                        EndDate = FixedDate(2024, 3, 31)
                    }
                },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNotNull(selected);
        Assert.AreEqual("March 2024", selected!.Text);
    }

    [TestMethod]
    public void BitDateRangePickerWeekDayHeaderClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowWeekNumbers, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                WeekDayHeader = "custom-week-day",
                WeekNumbersHeader = "custom-week-numbers"
            });
        });

        Assert.AreEqual(7, component.FindAll(".custom-week-day").Count);
        Assert.AreEqual(1, component.FindAll(".custom-week-numbers").Count);
    }

    [TestMethod]
    public void BitDateRangePickerAmPmShouldFollowTheTimePickerWithoutAValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        // Without a value the time picker still shows 00:00 for the start and 23:59 for the end.
        Assert.IsTrue(component.Find(".bit-dtrp-sic .bit-dtrp-bam").ClassList.Contains("bit-dtrp-bns"));
        Assert.IsTrue(component.Find(".bit-dtrp-eic .bit-dtrp-bpm").ClassList.Contains("bit-dtrp-bns"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotMutateTheBoundInstanceWhenTheEndDateIsDroppedByMinDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var boundValue = new BitDateRangePickerValue
        {
            StartDate = FixedDate(2024, 3, 1),
            EndDate = FixedDate(2024, 3, 3)
        };

        RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MinDate, FixedDate(2024, 3, 10));
            parameters.Add(p => p.Value, boundValue);
        });

        Assert.IsTrue(boundValue.EndDate.HasValue);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldKeepEveryMonthSixWeeksTall()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.FixedWeeks, false);
            // February 2021 starts on a Monday and fits in exactly four weeks on its own.
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2021, 2, 10) });
        });

        var months = component.FindAll(".bit-dtrp-dwp");

        Assert.AreEqual(6, months[0].QuerySelectorAll(".bit-dtrp-dgr").Length);
        Assert.AreEqual(6, months[1].QuerySelectorAll(".bit-dtrp-dgr").Length);
    }

    [TestMethod]
    public void BitDateRangePickerExcludeDisabledDatesShouldBlockBackwardsToo()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ExcludeDisabledDates, true);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 1));
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 10) });
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 12) });
        });

        var days = component.FindAll(".bit-dtrp-dbt").Where(d => d.ClassList.Contains("bit-dtrp-dbo") is false).ToList();

        Assert.IsFalse(days[10].HasAttribute("disabled"));  // March 11, still on this side of the blackout
        Assert.IsTrue(days[9].HasAttribute("disabled"));    // March 10, disabled on its own
        Assert.IsTrue(days[8].HasAttribute("disabled"));    // March 9, its range would cover March 10
    }

    [TestMethod]
    public void BitDateRangePickerClearButtonShouldRestoreTheStartingValueTimes()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = new()
        {
            StartDate = FixedDate(2024, 3, 1),
            EndDate = FixedDate(2024, 3, 5)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.StartingValue, new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(2024, 3, 1, 8, 30, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2024, 3, 1, 17, 45, 0, TimeSpan.Zero)
            });
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-clr").Click();

        Assert.AreEqual("8", component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[0].GetAttribute("value"));
        Assert.AreEqual("30", component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[1].GetAttribute("value"));
        Assert.AreEqual("17", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].GetAttribute("value"));
        Assert.AreEqual("45", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldSelectDatesWithAnUnboundIsOpen()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 3, 10));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-dtrp-dbt").First(d => d.HasAttribute("disabled") is false).Click();

        Assert.IsNotNull(value);
        Assert.IsTrue(value!.StartDate.HasValue);
    }

    [TestMethod]
    public void BitDateRangePickerTypedHourShouldKeepThePmPeriodInTwelveHoursFormat()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The end time shows "2" (14:00 is 2 PM); typing "3" must mean 3 PM, not 3 AM.
        component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].Input("3");

        Assert.AreEqual(15, value!.EndDate!.Value.Hour);
    }

    [TestMethod]
    public void BitDateRangePickerTypedTwelveShouldMeanMidnightInTheAmPeriod()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 14, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The start time shows "9" (9 AM); typing "12" on a 12-hour face means 12 AM, which is 00:00.
        component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[0].Input("12");

        Assert.AreEqual(0, value!.StartDate!.Value.Hour);
    }

    [TestMethod]
    public void BitDateRangePickerWholeDayMaxRangeShouldNotDisableTheTimeSpinnersWithoutAValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            // A MaxRange of at least a whole day cannot be violated by the times alone, so the
            // default 00:00 - 23:59 times must not lock the spinners while no date is picked.
            parameters.Add(p => p.MaxRange, new TimeSpan(2, 4, 30, 0));
        });

        foreach (var button in component.FindAll(".bit-dtrp-tbt"))
        {
            Assert.IsFalse(button.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void BitDateRangePickerDayPickerNavWrapperClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { DayPickerNavWrapper = "custom-nav-wrapper" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { DayPickerNavWrapper = "background-color: red;" });
        });

        var navWrapper = component.Find(".bit-dtrp-dwp .bit-dtrp-nbc");

        Assert.IsTrue(navWrapper.ClassList.Contains("custom-nav-wrapper"));
        Assert.AreEqual("background-color: red;", navWrapper.GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldRespectMaxRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(7));
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-10");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeAtExactlyMaxRangeShouldBeAccepted()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(7));
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-08");

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(new DateTime(2024, 3, 8), component.Instance.Value!.EndDate!.Value.DateTime);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldRespectMinRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MinRange, TimeSpan.FromDays(3));
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-02");

        Assert.IsNull(component.Instance.Value);

        // A range of exactly MinRange is the shortest allowed one, matching the day grid.
        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-04");

        Assert.IsNotNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldRespectMinAndMaxDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MinDate, FixedDate(2024, 3, 5));
            parameters.Add(p => p.MaxDate, FixedDate(2024, 3, 20));
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-10");

        Assert.IsNull(component.Instance.Value);

        component.Find(".bit-dtrp-inp").Change("2024-03-10 - 2024-03-25");

        Assert.IsNull(component.Instance.Value);

        component.Find(".bit-dtrp-inp").Change("2024-03-10 - 2024-03-15");

        Assert.IsNotNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldRejectABlockedEndpoint()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 5) });
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-05 - 2024-03-10");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldSpanDisabledDaysWithoutExcludeDisabledDates()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 3) });
        });

        // By default a range simply spans over the disabled days between its two ends.
        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-05");

        Assert.IsNotNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerTypedRangeShouldRespectExcludeDisabledDates()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.ExcludeDisabledDates, true);
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 3) });
        });

        component.Find(".bit-dtrp-inp").Change("2024-03-01 - 2024-03-05");

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPresetShorterThanMinRangeShouldNotBeApplied()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MinRange, TimeSpan.FromDays(5));
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Too short", Value = new() { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 3) } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPresetWithABlockedEndShouldNotBeApplied()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 5) });
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Blocked end", Value = new() { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 5) } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPresetCoveringADisabledDayShouldRespectExcludeDisabledDates()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ExcludeDisabledDates, true);
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 3) });
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Covers a blocked day", Value = new() { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 5) } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPresetClampedByMaxRangeOntoABlockedDayShouldNotBeApplied()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(5));
            // The advertised end (3/10) is fine, but the MaxRange clamp moves the applied end
            // to 3/6, which is a blocked day - so the preset must be rejected.
            parameters.Add(p => p.DisabledDates, new[] { FixedDate(2024, 3, 6) });
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Clamped onto a blocked day", Value = new() { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 10) } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerPagedNavigationShouldAdvanceByTheRenderedMonths()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.PagedNavigation, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 10) });
        });

        component.Find(".bit-dtrp-dwp [title='Go to next month']").Click();

        var titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(MonthTitle(2024, 5), titles[0].TextContent.Trim());
        Assert.AreEqual(MonthTitle(2024, 6), titles[1].TextContent.Trim());

        component.Find(".bit-dtrp-dwp [title='Go to previous month']").Click();

        titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(MonthTitle(2024, 3), titles[0].TextContent.Trim());
        Assert.AreEqual(MonthTitle(2024, 4), titles[1].TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerPagedNavigationShouldNotStepPastMaxDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
            parameters.Add(p => p.PagedNavigation, true);
            parameters.Add(p => p.MaxDate, FixedDate(2024, 4, 15));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 10) });
        });

        // The page would reach May, but the single-month navigation stops with April as the first
        // month, so the paged one must stop there too.
        component.Find(".bit-dtrp-dwp [title='Go to next month']").Click();

        var titles = component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual(MonthTitle(2024, 4), titles[0].TextContent.Trim());
    }

    [TestMethod]
    public async Task BitDateRangePickerCloseCalloutShouldCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        await component.InvokeAsync(() => component.Instance.CloseCallout());

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDateRangePickerTypedTimeAtExactlyMaxRangeShouldBeAccepted()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(2));
        });

        // With no value picked the times start at 00:00 and (clamped) 02:00. Typing the end hour
        // back to the exact MaxRange boundary is as valid as reaching it with the spinner buttons.
        component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].Input("1");
        component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].Input("2");

        Assert.AreEqual("2", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerClearButtonShouldShowForAnOpenStartRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { EndDate = FixedDate(2024, 3, 5) });
        });

        // A range holding only an end date is still a value, so it must be clearable too.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-clr").Count);
    }

    [TestMethod]
    public void BitDateRangePickerTimeSpinnerButtonsShouldBeLabelled()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        foreach (var button in component.FindAll(".bit-dtrp-tbt"))
        {
            Assert.IsFalse(string.IsNullOrEmpty(button.GetAttribute("aria-label")));
            Assert.IsFalse(string.IsNullOrEmpty(button.GetAttribute("title")));
        }

        Assert.AreEqual(1, component.FindAll("[aria-label='Increase start hour']").Count);
        Assert.AreEqual(1, component.FindAll("[aria-label='Decrease end minute']").Count);
    }

    [TestMethod]
    public void BitDateRangePickerTimeInputsShouldBeLabelled()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-dtrp-tin[aria-label='Start hour']").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-tin[aria-label='Start minute']").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-tin[aria-label='End hour']").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-tin[aria-label='End minute']").Count);
    }

    [TestMethod]
    public void BitDateRangePickerTimeSpinnerButtonTitlesShouldBeCustomizable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartTimeIncreaseHourTitle, "More start hours");
            parameters.Add(p => p.EndTimeDecreaseMinuteTitle, "Fewer end minutes");
        });

        Assert.AreEqual(1, component.FindAll("[aria-label='More start hours']").Count);
        Assert.AreEqual(1, component.FindAll("[aria-label='Fewer end minutes']").Count);
    }

    [TestMethod]
    public void BitDateRangePickerStartTimeShouldNotWalkPastMaxRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var value = new BitDateRangePickerValue
        {
            // Exactly 48 hours, sitting right on the MaxRange boundary.
            StartDate = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 6, 3, 10, 0, 0, TimeSpan.Zero)
        };
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(2));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Moving the start one hour back would stretch the range to 49 hours.
        var startHourButtons = component.FindAll(".bit-dtrp-sic .bit-dtrp-tpr")[0].QuerySelectorAll(".bit-dtrp-tbt");
        Assert.IsTrue(startHourButtons[1].HasAttribute("disabled"));

        // Typing the same violation is rejected too.
        component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[0].Input("9");

        Assert.AreEqual(new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero), component.Instance.Value!.StartDate);
    }

    [TestMethod]
    public void BitDateRangePickerEndOnlyValueInThePastShouldBePreserved()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Value, new BitDateRangePickerValue { EndDate = FixedDate(2020, 1, 5) });
        });

        // An open-ended range holding only a (past) end date has no start to precede,
        // so it must not be dropped.
        Assert.IsNotNull(component.Instance.Value);
        Assert.IsNotNull(component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerPresetShouldRespectTheTimeOfMinDateLikeTheDayGrid()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            // MinDate carries midday as its time-of-day, which disables its own day in the grid.
            parameters.Add(p => p.MinDate, FixedDate(2024, 3, 5));
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "Starts on the MinDate day", Value = new() { StartDate = FixedDate(2024, 3, 5), EndDate = FixedDate(2024, 3, 8) } },
            });
        });

        component.Find(".bit-dtrp-prb").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDateRangePickerStartingValueTimesWithinASubDayMaxRangeShouldBePreserved()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var start = new DateTime(2024, 3, 1, 0, 0, 0);
        var end = new DateTime(2024, 3, 1, 1, 45, 0);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(2));
            parameters.Add(p => p.StartingValue, new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(start, TimeZoneInfo.Local.GetUtcOffset(start)),
                EndDate = new DateTimeOffset(end, TimeZoneInfo.Local.GetUtcOffset(end))
            });
        });

        // 00:00 - 01:45 fits inside the two-hour MaxRange, so the end time must not be clamped.
        var endInputs = component.FindAll(".bit-dtrp-eic .bit-dtrp-tin");

        Assert.AreEqual("1", endInputs[0].GetAttribute("value"));
        Assert.AreEqual("45", endInputs[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerReadOnlyShouldHideTheClearButton()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 5) });
        });

        // The clear button would silently do nothing in ReadOnly, so it is not rendered at all.
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-clr").Count);
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneContainerShouldExposeItsAriaLabel()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var container = component.Find(".bit-dtrp-cac");

        // An aria-label is only exposed on an element carrying a role.
        Assert.AreEqual("group", container.GetAttribute("role"));
        Assert.AreEqual("Calendar", container.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldCarryThePublicCssVariablesOfTheStyleOntoTheCalendar()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Style, "margin:1rem;--bit-DateRangePicker-accent-color:red");
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Callout = "padding:2px" });
        });

        // A standalone calendar is never relocated, so the chain Callouts.ts carries the root's declarations through
        // is never built for it: the public variables of the Style are copied onto it ahead of its own style.
        Assert.AreEqual("--bit-DateRangePicker-accent-color:red;padding:2px", component.Find(".bit-dtrp-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldCopyNothingOntoTheCalendarUnlessItIsStandalone()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Style, "--bit-DateRangePicker-accent-color:red");
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Callout = "padding:2px" });
        });

        Assert.AreEqual("padding:2px", component.Find(".bit-dtrp-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldKeepASemicolonInsideAValueWhenItCopiesTheVariables()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Style, "--bit-DateRangePicker-accent-color:url('data:image/svg+xml;utf8,<svg></svg>');color:blue");
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Root = "--bit-DateRangePicker-x:[a;b]", Callout = "padding:2px" });
        });

        // A semicolon inside quotes or brackets is part of the value: cut there, the copy would leave a quote or a
        // bracket open that swallows everything written after it on the calendar, its own style included.
        Assert.AreEqual("--bit-DateRangePicker-accent-color:url('data:image/svg+xml;utf8,<svg></svg>');--bit-DateRangePicker-x:[a;b];padding:2px",
                        component.Find(".bit-dtrp-cal").GetAttribute("style"));
    }

    [TestMethod]
    public void BitDateRangePickerMonthNavigationButtonsShouldBeLabelled()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp [aria-label='Go to previous month']").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp [aria-label='Go to next month']").Count);
    }

    [TestMethod]
    public void BitDateRangePickerNextYearNavShouldNotBeBlockedByMaxRangeAfterACompleteRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(7));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 6, 10), EndDate = FixedDate(2024, 6, 14) });
        });

        // MaxRange only bounds the calendar while the end date is still being picked, so a complete
        // range must leave the year navigation free in both directions.
        Assert.IsFalse(component.Find("button[title^='Go to next year ']").HasAttribute("disabled"));
        Assert.IsFalse(component.Find("button[title^='Go to previous year ']").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerNextYearNavShouldBeBlockedByMaxRangeWhileTheEndDateIsBeingPicked()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(7));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 6, 10) });
        });

        // With only the start date picked, every possible end date lives in the current year,
        // so both year navigations are blocked.
        Assert.IsTrue(component.Find("button[title^='Go to next year ']").HasAttribute("disabled"));
        Assert.IsTrue(component.Find("button[title^='Go to previous year ']").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerYearPickerRangeShouldFollowTheYearNavigation()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        // The initial year picker range is 2023-2034, so 12 next-year steps land outside of it.
        for (var i = 0; i < 12; i++)
        {
            component.Find("button[title^='Go to next year ']").Click();
        }

        component.Find("button[title$='change year']").Click();

        // The year picker has to realign its range to contain the year it opens on.
        Assert.IsTrue(component.FindAll(".bit-dtrp-pkb").Any(b => b.TextContent.Trim() == "2036"));
    }

    [TestMethod]
    public void BitDateRangePickerHourStepShouldDriveTheHourSpinners()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var value = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 11, 0, 0, TimeSpan.Zero)
        };
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HourStep, 3);
            parameters.Add(p => p.ShowTimePicker, true);
            // A long delay keeps the press-and-hold spin out of the picture, so each press contributes
            // exactly one step no matter how long the test itself takes.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The step lays a grid over the day - 0, 3, 6 ... 21 - rather than adding itself to whatever the hour
        // happens to be, so the two hours the value came in on, which sit between grid points, move onto the
        // next one in the direction they were pressed instead of three past themselves.
        var increaseButton = component.Find("button[title='Increase start hour']");
        increaseButton.PointerDown();
        increaseButton.PointerUp();

        Assert.AreEqual(12, component.Instance.Value!.StartDate!.Value.Hour);

        var decreaseButton = component.Find("button[title='Decrease end hour']");
        decreaseButton.PointerDown();
        decreaseButton.PointerUp();

        Assert.AreEqual(9, component.Instance.Value!.EndDate!.Value.Hour);
    }

    [TestMethod]
    public void BitDateRangePickerHourStepShouldStayOnTheGridWhenItDoesNotDivideTheDay()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var value = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 20, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 11, 0, 0, TimeSpan.Zero)
        };
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HourStep, 5);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The grid is 0, 5, 10, 15, 20 and the four hours above 20 are not on it, so the hour wraps to the
        // top of the grid rather than to the 25th hour of a 24-hour day.
        var increaseButton = component.Find("button[title='Increase start hour']");
        increaseButton.PointerDown();
        increaseButton.PointerUp();

        Assert.AreEqual(0, component.Instance.Value!.StartDate!.Value.Hour);
    }

    [TestMethod]
    public void BitDateRangePickerMinuteStepShouldDriveTheMinuteSpinners()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var value = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 10, 30, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 11, 30, 0, TimeSpan.Zero)
        };
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MinuteStep, 15);
            parameters.Add(p => p.ShowTimePicker, true);
            // A long delay keeps the press-and-hold spin out of the picture, so each press contributes
            // exactly one step no matter how long the test itself takes.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var increaseButton = component.Find("button[title='Increase start minute']");
        increaseButton.PointerDown();
        increaseButton.PointerUp();

        Assert.AreEqual(45, component.Instance.Value!.StartDate!.Value.Minute);

        var decreaseButton = component.Find("button[title='Decrease end minute']");
        decreaseButton.PointerDown();
        decreaseButton.PointerUp();

        Assert.AreEqual(15, component.Instance.Value!.EndDate!.Value.Minute);
    }

    [TestMethod]
    public void BitDateRangePickerHomeAndEndKeysShouldMoveTheFocusInsideTheWeek()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            // June 12, 2024 is a Wednesday; with the invariant Sunday-first week its week runs June 9 to June 15.
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        var focused = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        focused.KeyDown(Key.Home);

        var focusedAfterHome = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        Assert.AreEqual("9", focusedAfterHome.TextContent.Trim());

        focusedAfterHome.KeyDown(Key.End);

        var focusedAfterEnd = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        Assert.AreEqual("15", focusedAfterEnd.TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerValidationFormTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.TestModel, new BitDateRangePickerTestModel());
        });

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.Find(".bit-dtrp-wrp").Click();
        component.Find(".bit-dtrp-dtd").Click();

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
    }

    [TestMethod]
    public void BitDateRangePickerValidationInvalidCssClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.TestModel, new BitDateRangePickerTestModel());
        });

        var root = component.Find(".bit-dtrp");
        Assert.IsFalse(root.ClassList.Contains("bit-inv"));

        component.Find("form").Submit();

        Assert.IsTrue(root.ClassList.Contains("bit-inv"));

        component.Find(".bit-dtrp-wrp").Click();
        component.Find(".bit-dtrp-dtd").Click();

        Assert.IsFalse(root.ClassList.Contains("bit-inv"));
    }

    // Midday keeps a date-only assertion safe from any time zone shifting the instant across midnight.
    private static DateTimeOffset FixedDate(int year, int month, int day)
    {
        var dateTime = new DateTime(year, month, day, 12, 0, 0);

        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    private static string MonthTitle(int year, int month)
    {
        return $"{CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)} {year}";
    }

    [TestMethod]
    public void BitDateRangePickerShouldTellAnUnreadableRangeFromAnOutOfRangeOne()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MinDate, new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.InvalidErrorMessage, "not a range");
            parameters.Add(p => p.OutOfRangeErrorMessage, "outside the range");
        });

        // Text that does not read as a range at all and text that reads fine but breaks the restrictions
        // are different mistakes, so they no longer share the one message.
        component.Find(".bit-dtrp-inp").Change("nonsense");
        component.Find("form").Submit();
        Assert.IsTrue(component.FindAll(".validation-message").Any(m => m.TextContent == "not a range"));

        component.Find(".bit-dtrp-inp").Change("2026-01-01 - 2026-01-05");
        component.Find("form").Submit();
        Assert.IsTrue(component.FindAll(".validation-message").Any(m => m.TextContent == "outside the range"));

        component.Find(".bit-dtrp-inp").Change("2026-01-12 - 2026-01-15");
        component.Find("form").Submit();
        Assert.IsEmpty(component.FindAll(".validation-message")
                                .Select(message => message.TextContent.Trim())
                                .Where(text => text.Length > 0));
    }

    [TestMethod]
    public void BitDateRangePickerShouldDisableThePastAndTheFutureLikeTheBounds()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // Today carries a time of day, the way the current instant always does, so that the bounds are
        // measured against the whole day and not against the moment the test happens to name.
        var now = new DateTime(2026, 1, 15, 14, 30, 0);
        var today = new DateTimeOffset(now, TimeZoneInfo.Local.GetUtcOffset(now));

        var past = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisablePast, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        // A day before today is out of the range exactly as it would be with a MinDate of today,
        // while today itself is the first day that can still start or end a range.
        Assert.IsTrue(past.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "14").HasAttribute("disabled"));
        Assert.IsFalse(past.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "15").HasAttribute("disabled"));
        Assert.IsFalse(past.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "16").HasAttribute("disabled"));

        var future = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.DisableFuture, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        Assert.IsFalse(future.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "14").HasAttribute("disabled"));
        Assert.IsFalse(future.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "15").HasAttribute("disabled"));
        Assert.IsTrue(future.FindAll(".bit-dtrp-dbt").First(b => b.TextContent.Trim() == "16").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldReportOpeningAndClosingTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var opened = 0;
        var closed = 0;
        var isOpen = false;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtrp-wrp").Click();
        Assert.AreEqual(1, opened);
        Assert.AreEqual(0, closed);

        component.Find(".bit-dtrp-ovl").Click();
        Assert.AreEqual(1, opened);
        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public async Task BitDateRangePickerShouldShowTheCalloutWhenIsOpenIsSetFromOutside()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>();

        var before = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // The hook does its toggling on the renderer's dispatcher rather than inline, so the queue is
        // drained before the invocations are counted.
        await component.InvokeAsync(() => Task.CompletedTask);

        var after = Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        // The callout is shown and positioned from the JS side, so an IsOpen pushed in through the
        // parameter has to reach it too - otherwise the picker reports itself open while nothing appears.
        Assert.IsTrue(after > before);
    }

    [TestMethod]
    public async Task BitDateRangePickerShouldFitTheMonthsWhenIsOpenIsSetFromOutside()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.JSInterop.Setup<decimal>("BitBlazorUI.Utils.getBodyWidth").SetResult(320m);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MonthCount, 2);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // The hook does its work on the renderer's dispatcher rather than inline, so the queue is
        // drained before the callout is read.
        await component.InvokeAsync(() => Task.CompletedTask);

        // An open pushed in from the outside measures the width available exactly as a click on the field
        // does - the width reported here being a phone's - so the extra months are dropped instead of being laid
        // out side by side past the edge of a viewport that cannot hold them.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb").Count);
    }

    [TestMethod]
    public void BitDateRangePickerShouldReportTheClearButton()
    {
        var cleared = 0;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.OnClear, () => cleared++);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-clr").Click();

        Assert.IsNull(value);
        Assert.AreEqual(1, cleared);
    }

    [TestMethod]
    public void BitDateRangePickerShouldStillReportTheClearButtonWhenFocusingTheInputFailsOnTheJsSide()
    {
        // An input taken out of the document before the focus call reaches it fails in the browser, which
        // reaches .NET as a JSException: the value is still cleared, OnClear still fires, nothing is thrown.
        Context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetException(new JSException("Unable to focus an invalid element."));

        var cleared = 0;
        BitDateRangePickerValue? value = new()
        {
            StartDate = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2024, 3, 5, 0, 0, 0, TimeSpan.Zero)
        };

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.OnClear, () => cleared++);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-clr").Click();

        Assert.IsNull(value);
        Assert.AreEqual(1, cleared);
        Assert.AreEqual(1, Context.JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
    }

    [TestMethod]
    public void BitDateRangePickerShouldFocusTheInputOnAutoFocus()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
        });

        // The autofocus attribute is not honored for an interactively rendered input, so the focus is
        // placed from the first render instead - which reaches the DOM as a focus interop call.
        Assert.IsTrue(Context.JSInterop.Invocations
                             .Any(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void BitDateRangePickerCalloutShouldBeAModalDialogOnlyWhenItFloats()
    {
        var component = RenderComponent<BitDateRangePicker>();

        var callout = component.Find(".bit-dtrp-cac");

        Assert.AreEqual("dialog", callout.GetAttribute("role"));
        Assert.AreEqual("true", callout.GetAttribute("aria-modal"));

        component.Render(parameters => parameters.Add(p => p.Standalone, true));

        callout = component.Find(".bit-dtrp-cac");

        Assert.AreEqual("group", callout.GetAttribute("role"));
        Assert.IsFalse(callout.HasAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldHandItsCalloutTheTabOrderAndTheFocusLeavingIt()
    {
        var component = RenderComponent<BitDateRangePicker>();

        component.Find(".bit-dtrp-wrp").Click();

        // The callout is relocated to the end of the document while it is open, so the tab order of the page
        // does not run through it: the JS side holds the tab order inside it and dismisses it once the focus has
        // moved on from both the field and it - which only works if the toggle is asked to.
        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(true, toggle.Arguments[28]); // trapFocus
        Assert.AreEqual(true, toggle.Arguments[29]); // dismissOnFocusLeave
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotTrapTheFocusInAStandaloneCallout()
    {
        RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        // Standalone there is no dialog and no overlay, so the focus is free to leave the way it leaves any
        // other part of the page: the callout is never toggled, and nothing is trapped.
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Callouts.toggle"));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-dtrp-sm"),
        DataRow(BitSize.Medium, "bit-dtrp-md"),
        DataRow(BitSize.Large, "bit-dtrp-lg"),
    ]
    public void BitDateRangePickerSizeShouldReachTheRootAndTheCallout(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        // The callout is rendered outside of the root, so nothing of the root's size cascades into it.
        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains(expectedClass));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDateRangePickerWithNoSizeShouldCarryNoSizeClass()
    {
        var component = RenderComponent<BitDateRangePicker>();

        var classes = component.Find(".bit-dtrp").ClassList;

        Assert.IsFalse(classes.Contains("bit-dtrp-sm"));
        Assert.IsFalse(classes.Contains("bit-dtrp-md"));
        Assert.IsFalse(classes.Contains("bit-dtrp-lg"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldPublishNoColorOrSizeWhileTheyAreUnset()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>();

        // An unset Color or Size publishes nothing - on the root or on the callout rendered outside it - so the public
        // --bit-DateRangePicker-* variables restyle the default while an explicit value, which does publish its class,
        // wins over them.
        var published = new[]
        {
            "bit-dtrp-pri", "bit-dtrp-sec", "bit-dtrp-ter", "bit-dtrp-inf", "bit-dtrp-suc", "bit-dtrp-wrn", "bit-dtrp-swr", "bit-dtrp-err",
            "bit-dtrp-pbg", "bit-dtrp-sbg", "bit-dtrp-tbg", "bit-dtrp-pfg", "bit-dtrp-sfg", "bit-dtrp-tfg", "bit-dtrp-pbr", "bit-dtrp-sbr", "bit-dtrp-tbr",
            "bit-dtrp-sm", "bit-dtrp-md", "bit-dtrp-lg",
        };

        foreach (var element in new[] { component.Find(".bit-dtrp"), component.Find(".bit-dtrp-cal") })
        {
            foreach (var cssClass in published)
            {
                Assert.IsFalse(element.ClassList.Contains(cssClass), $"An unset parameter published {cssClass}.");
            }
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitDateRangePickerHighlightTodayShouldOnlyDriveThePaint(bool highlightToday)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var today = FixedDate(2024, 6, 12);
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.HighlightToday, highlightToday);
        });

        Assert.AreEqual(highlightToday, component.FindAll(".bit-dtrp-dtd").Count > 0);

        // Whether today is painted or not, it is still the current date as far as a screen reader is concerned.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dbt[aria-current=\"date\"]").Count);
    }

    [TestMethod]
    public void BitDateRangePickerShouldReportEveryPickThroughOnSelectDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var reported = new List<BitDateRangePickerValue?>();
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.OnSelectDate, v => reported.Add(v));
        });

        var days = component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)");
        days[9].Click();
        days = component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)");
        days[14].Click();

        // The half-picked range is reported too: the callback follows the picks, not the completed value.
        Assert.AreEqual(2, reported.Count);
        Assert.IsNotNull(reported[0]?.StartDate);
        Assert.IsNull(reported[0]?.EndDate);
        Assert.IsNotNull(reported[1]?.StartDate);
        Assert.IsNotNull(reported[1]?.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerShouldTellABlockedDayFromAnOutOfRangeOne()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.MaxDate, new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDaysOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
            parameters.Add(p => p.DisabledDateErrorMessage, "not available");
            parameters.Add(p => p.OutOfRangeErrorMessage, "outside the range");
        });

        // 2026-01-10 is a Saturday, so it is blocked rather than out of bounds.
        component.Find(".bit-dtrp-inp").Change("2026-01-10 - 2026-01-13");
        component.Find("form").Submit();
        Assert.IsTrue(component.FindAll(".validation-message").Any(m => m.TextContent == "not available"));

        // 2027 is past MaxDate, and the bounds are a different mistake from a blocked day.
        component.Find(".bit-dtrp-inp").Change("2027-01-12 - 2027-01-13");
        component.Find("form").Submit();
        Assert.IsTrue(component.FindAll(".validation-message").Any(m => m.TextContent == "outside the range"));
    }

    [TestMethod]
    public void BitDateRangePickerMonthGridShouldAlwaysHaveAReachableCell()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        var months = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb");
        Assert.AreEqual(12, months.Count);
        Assert.AreEqual(1, months.Count(m => m.GetAttribute("tabindex") == "0"));
        Assert.IsTrue(months.All(m => string.IsNullOrEmpty(m.Id) is false));
    }

    [TestMethod]
    public void BitDateRangePickerMonthGridShouldAnswerTheArrowKeys()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        var focused = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb").Single(m => m.GetAttribute("tabindex") == "0");
        Assert.AreEqual("June", focused.GetAttribute("title"));

        focused.KeyDown(Key.Right);
        focused = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb").Single(m => m.GetAttribute("tabindex") == "0");
        Assert.AreEqual("July", focused.GetAttribute("title"));

        // One row of the month grid is four months wide.
        focused.KeyDown(Key.Down);
        focused = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb").Single(m => m.GetAttribute("tabindex") == "0");
        Assert.AreEqual("November", focused.GetAttribute("title"));

        focused.KeyDown(Key.Home);
        focused = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb").Single(m => m.GetAttribute("tabindex") == "0");
        Assert.AreEqual("January", focused.GetAttribute("title"));
    }

    [TestMethod]
    public void BitDateRangePickerYearGridShouldStayReachableWhenTheRangeMovesAwayFromTheCurrentYear()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        // Open the year grid, then browse to a range of years that no longer holds the displayed year.
        component.Find(".bit-dtrp-mwp .bit-dtrp-ptb").Click();
        component.FindAll(".bit-dtrp-mwp .bit-dtrp-nbt")[0].Click();

        var years = component.FindAll(".bit-dtrp-mwp .bit-dtrp-pkb");
        Assert.AreEqual(12, years.Count);
        Assert.AreEqual(1, years.Count(y => y.GetAttribute("tabindex") == "0"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldReportAnInvalidValueOnTheField()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.Disabled, false);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.InvalidErrorMessage, "not a range");
        });

        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));

        component.Find(".bit-dtrp-inp").Change("nonsense");
        component.Find("form").Submit();

        Assert.AreEqual("true", component.Find(".bit-dtrp-inp").GetAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldTakeItsUnsetParametersFromTheCascade()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePickerParamsTest>();

        var pickers = component.FindAll(".bit-dtrp");

        // The first picker sets nothing of its own, so it takes the whole cascade.
        Assert.IsTrue(pickers[0].ClassList.Contains("bit-dtrp-inf"));
        Assert.IsTrue(pickers[0].ClassList.Contains("bit-dtrp-lg"));

        // The second one writes its own Color, which the cascade may not overwrite, and still takes the Size.
        Assert.IsTrue(pickers[1].ClassList.Contains("bit-dtrp-suc"));
        Assert.IsTrue(pickers[1].ClassList.Contains("bit-dtrp-lg"));

        // The cascaded Placeholder reaches the field of the one that left it unset.
        Assert.AreEqual("from the cascade", component.FindAll(".bit-dtrp-inp")[0].GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitDateRangePickerCascadedViewParametersShouldReachTheFirstRender()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePickerParamsTest>();

        // MonthCount is one of the parameters the calendar is built from, and the calendar is built while
        // initializing - before OnParametersSet has ever run - so a cascaded one has to be in by then.
        Assert.AreEqual(2, component.FindAll(".bit-dtrp-cal")[0].QuerySelectorAll(".bit-dtrp-dwp").Length);
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotRenderTheActionsWhileItAutoApplies()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsEmpty(component.FindAll(".bit-dtrp-act"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldNotRenderTheActions()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // A standalone picker has no callout to commit or discard, so the buttons would promise something
        // there is no transaction behind.
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.AutoApply, false);
        });

        Assert.IsEmpty(component.FindAll(".bit-dtrp-act"));
    }

    [TestMethod]
    public void BitDateRangePickerApplyShouldStayDisabledUntilTheRangeIsComplete()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        component.Find(".bit-dtrp-wrp").Click();

        // Nothing is picked yet, so there is nothing half-finished to hold Apply back.
        Assert.IsFalse(component.Find(".bit-dtrp-apb").HasAttribute("disabled"));

        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[9].Click();
        Assert.IsTrue(component.Find(".bit-dtrp-apb").HasAttribute("disabled"));

        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[14].Click();
        Assert.IsFalse(component.Find(".bit-dtrp-apb").HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerApplyShouldCommitTheRangeAndCloseTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDateRangePickerValue? value = null;
        BitDateRangePickerValue? applied = null;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnApply, v => applied = v);
        });

        component.Find(".bit-dtrp-wrp").Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[9].Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[14].Click();

        // AutoClose is ignored while the callout waits for its Apply button.
        Assert.IsTrue(component.Instance.IsOpen);

        component.Find(".bit-dtrp-apb").Click();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.IsNotNull(value?.StartDate);
        Assert.IsNotNull(value?.EndDate);
        Assert.AreEqual(value?.StartDate, applied?.StartDate);
        Assert.AreEqual(value?.EndDate, applied?.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerCancelShouldPutBackTheRangeTheCalloutWasOpenedOn()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var original = new BitDateRangePickerValue
        {
            StartDate = FixedDate(2024, 6, 3),
            EndDate = FixedDate(2024, 6, 5)
        };

        var value = original;
        var cancelled = 0;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnCancel, () => cancelled++);
        });

        component.Find(".bit-dtrp-wrp").Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[9].Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[14].Click();

        // The picks reach the value as they are made, which is what the calendar itself reads.
        Assert.AreNotEqual(original.StartDate, value?.StartDate);

        component.Find(".bit-dtrp-cnb").Click();

        Assert.AreEqual(1, cancelled);
        Assert.AreEqual(original.StartDate, value?.StartDate);
        Assert.AreEqual(original.EndDate, value?.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerDismissingTheCalloutShouldRollTheRangeBackToo()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var original = new BitDateRangePickerValue
        {
            StartDate = FixedDate(2024, 6, 3),
            EndDate = FixedDate(2024, 6, 5)
        };

        var value = original;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-wrp").Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[9].Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[14].Click();

        // Escape is a dismissal like any other, so it discards the transaction rather than committing it.
        component.Find(".bit-dtrp-cac").KeyDown(Key.Escape);

        Assert.AreEqual(original.StartDate, value?.StartDate);
        Assert.AreEqual(original.EndDate, value?.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerReopeningShouldSnapshotTheRangeAgain()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        BitDateRangePickerValue? value = null;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.Today, FixedDate(2024, 6, 12));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-wrp").Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[9].Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[14].Click();
        component.Find(".bit-dtrp-apb").Click();

        var applied = value;
        Assert.IsNotNull(applied);

        // The second transaction is measured against what the first one committed, not against the empty
        // range the picker started its life with.
        component.Find(".bit-dtrp-wrp").Click();
        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")[20].Click();
        component.Find(".bit-dtrp-cnb").Click();

        Assert.AreEqual(applied.StartDate, value?.StartDate);
        Assert.AreEqual(applied.EndDate, value?.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerArrowKeysShouldPreviewTheRange()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = today });
        });

        // The start date alone is picked and the keyboard has not moved yet, so there is nothing
        // between it and the focused day to preview.
        Assert.AreEqual(0, component.FindAll(".bit-dtrp-dhr").Count);

        var focused = component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0");
        focused.KeyDown(Key.Right);
        component.FindAll(".bit-dtrp-dbt").Single(d => d.GetAttribute("tabindex") == "0").KeyDown(Key.Right);

        // Two days past the start date: the pointer is not involved, so this is the keyboard's own preview.
        Assert.AreEqual(2, component.FindAll(".bit-dtrp-dhr").Count);
    }

    [TestMethod]
    public void BitDateRangePickerShouldAnnounceOnlyTheFirstMonthTitle()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 3);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        // A strip of months would otherwise read every one of its titles out on every navigation.
        var titles = component.FindAll(".bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.IsTrue(titles.Count > 1);
        Assert.AreEqual(1, titles.Count(t => t.GetAttribute("aria-live") is not null));
    }

    [TestMethod]
    public void BitDateRangePickerSubDayMaxRangeShouldLeaveOnlyTheStartDaySelectable()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 12, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(6));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = today });
        });

        // Six hours either side of noon stay inside June 12, so a range starting then cannot end on
        // another day, and the grid must say so rather than accept the pick and silently rewrite the
        // end date back to the start day.
        var enabled = component.FindAll(".bit-dtrp-dbt").Where(d => d.HasAttribute("disabled") is false).ToList();

        Assert.AreEqual(1, enabled.Count);
        Assert.AreEqual("12", enabled[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDateRangePickerMaxRangeShouldReachEquallyFarBackAndForward()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.MaxRange, TimeSpan.FromDays(3));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = today });
        });

        // Three days either side of June 12, whatever time of day the start date carries.
        var enabled = component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")
                               .Where(d => d.HasAttribute("disabled") is false)
                               .Select(d => d.TextContent.Trim())
                               .ToList();

        CollectionAssert.AreEqual(new[] { "9", "10", "11", "12", "13", "14", "15" }, enabled);
    }

    [TestMethod]
    public void BitDateRangePickerFractionalMaxRangeShouldReachTheDayItsInstantFallsOn()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = new DateTimeOffset(2024, 1, 10, 20, 0, 0, TimeSpan.Zero);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Today, today);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(36));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = today });
        });

        // 36 hours from Jan 10 20:00 reach Jan 12 08:00 forward and Jan 9 08:00 back, so both of those
        // days stay pickable - a whole number of days counted from the start day would stop at Jan 11.
        var enabled = component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)")
                               .Where(d => d.HasAttribute("disabled") is false)
                               .Select(d => d.TextContent.Trim())
                               .ToList();

        CollectionAssert.AreEqual(new[] { "9", "10", "11", "12" }, enabled);
    }

    [TestMethod]
    public void BitDateRangePickerCalloutAndOverlayShouldCarryOnlyTheirOwnStyleWhileTheRootKeepsTheWholeStyle()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Style, "margin: 1rem; --bit-DateRangePicker-color: red;--bit-DateRangePicker-day-size:3rem");
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles
            {
                Root = "--bit-DateRangePicker-range-radius: 0; padding: 0;",
                Callout = "--bit-DateRangePicker-color: blue;"
            });
        });

        var calloutStyle = component.Find(".bit-dtrp-cal").GetAttribute("style");
        var overlayStyle = component.Find(".bit-dtrp-ovl").GetAttribute("style");

        // Nothing of the root's style is written onto the popup any more: Callouts.ts carries it into the chain
        // the callout and the overlay are relocated into, so they hold exactly what the component composes.
        Assert.AreEqual("--bit-DateRangePicker-color: blue;", calloutStyle);
        Assert.AreEqual("display:none;", overlayStyle);

        // ... and the root keeps everything it was given.
        var root = component.Find(".bit-dtrp").GetAttribute("style");
        Assert.IsNotNull(root);
        StringAssert.Contains(root, "margin: 1rem");
        StringAssert.Contains(root, "--bit-DateRangePicker-color: red");
        StringAssert.Contains(root, "--bit-DateRangePicker-day-size:3rem");
        StringAssert.Contains(root, "--bit-DateRangePicker-range-radius: 0");
    }

    [TestMethod]
    public void BitDateRangePickerShouldNameItsRootWhenItTogglesTheCallout()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Style, "--bit-DateRangePicker-color: red;");
        });

        component.Find(".bit-dtrp-wrp").Click();

        // The root is what Callouts.ts copies into the chain it relocates the callout and the overlay into,
        // which is how the public variables of the instance still reach them once they sit under the body.
        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
        var rootId = component.Find(".bit-dtrp").Id;

        Assert.IsFalse(string.IsNullOrEmpty(rootId));
        Assert.AreEqual(rootId, toggle.Arguments[^2]);
    }

    [TestMethod]
    public void BitDateRangePickerCalloutShouldKeepOnlyItsOwnStyleWithoutPublicCssVariables()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Style, "margin: 1rem;");
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Callout = "color: red;" });
        });

        Assert.AreEqual("color: red;", component.Find(".bit-dtrp-cal").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitDateRangePickerCalloutShouldCarryTheReadOnlyMarker(bool readOnly)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        Assert.AreEqual(readOnly, component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dtrp-rol"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldFocusTheInputBeforeAnInitialIsOpenPlacesTheCallout()
    {
        RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AutoFocus, true);
        });

        var identifiers = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        var focus = identifiers.IndexOf("Blazor._internal.domWrapper.focus");
        var toggle = identifiers.IndexOf("BitBlazorUI.Callouts.toggle");

        // A focus() brings the field into view, and a field it scrolled the page to after the callout was placed
        // against it would be moved out from under the callout - which the scroll then dismisses.
        Assert.IsTrue(focus >= 0);
        Assert.IsTrue(toggle >= 0);
        Assert.IsTrue(focus < toggle);
    }

    // ── The field and its chrome ──────────────────────────────────────────────

    [TestMethod]
    public void BitDateRangePickerShouldRespectUnderlinedHasBorderAndIconPlacement()
    {
        var component = RenderComponent<BitDateRangePicker>();

        var root = component.Find(".bit-dtrp");

        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-und"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-nbd"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-lic"));
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-sta"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Underlined, true);
            parameters.Add(p => p.HasBorder, false);
            parameters.Add(p => p.IconPlacement, BitPlacement.Start);
        });

        root = component.Find(".bit-dtrp");

        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-und"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-lic"));
    }

    [TestMethod]
    public void BitDateRangePickerStandaloneShouldCarryTheValueInAHiddenInput()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.Name, "stay");
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy-MM-dd");
            parameters.Add(p => p.ValueFormat, "{0} - {1}");
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 5) });
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object> { ["data-test"] = "carrier" });
        });

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-dtrp-sta"));
        // No field to click, no overlay to click away: the calendar below is the picker.
        Assert.IsEmpty(component.FindAll(".bit-dtrp-wrp"));
        Assert.IsEmpty(component.FindAll(".bit-dtrp-ovl"));

        var carrier = component.Find("input.bit-input-hidden");

        Assert.AreEqual("2024-03-01 - 2024-03-05", carrier.GetAttribute("value"));
        Assert.AreEqual("stay", carrier.GetAttribute("name"));
        Assert.AreEqual("carrier", carrier.GetAttribute("data-test"));
        Assert.IsTrue(carrier.HasAttribute("readonly"));
    }

    [TestMethod,
        DataRow(false, true),
        DataRow(true, false)
    ]
    public void BitDateRangePickerShouldMarkARequiredPickerOnlyWhileEnabled(bool disabled, bool expected)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Disabled, disabled);
        });

        // The class is what draws the asterisk after the label, which a disabled picker asks nothing for.
        Assert.AreEqual(expected, component.Find(".bit-dtrp").ClassList.Contains("bit-dtrp-req"));
        Assert.IsTrue(component.Find(".bit-dtrp-inp").HasAttribute("required"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldFollowTheFocusOfItsField()
    {
        var focus = 0;
        var focusIn = 0;
        var focusOut = 0;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => focus++);
            parameters.Add(p => p.OnFocusIn, () => focusIn++);
            parameters.Add(p => p.OnFocusOut, () => focusOut++);
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { Focused = "custom-focused" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Focused = "outline: 1px;" });
        });

        Assert.IsFalse(component.Find(".bit-dtrp").ClassList.Contains("bit-dtrp-foc"));

        component.Find(".bit-dtrp-inp").FocusIn();

        var root = component.Find(".bit-dtrp");

        Assert.IsTrue(root.ClassList.Contains("bit-dtrp-foc"));
        Assert.IsTrue(root.ClassList.Contains("custom-focused"));
        StringAssert.Contains(root.GetAttribute("style"), "outline: 1px;");
        Assert.AreEqual(1, focusIn);

        component.Find(".bit-dtrp-inp").Focus();

        Assert.AreEqual(1, focus);

        component.Find(".bit-dtrp-inp").FocusOut();

        root = component.Find(".bit-dtrp");

        Assert.AreEqual(1, focusOut);
        Assert.IsFalse(root.ClassList.Contains("bit-dtrp-foc"));
        Assert.IsFalse(root.ClassList.Contains("custom-focused"));
        Assert.IsFalse((root.GetAttribute("style") ?? string.Empty).Contains("outline: 1px;"));
    }

    [TestMethod]
    public void BitDateRangePickerDisabledShouldReportNoFocus()
    {
        var focusIn = 0;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnFocusIn, () => focusIn++);
        });

        component.Find(".bit-dtrp-inp").FocusIn();

        Assert.AreEqual(0, focusIn);
        Assert.IsFalse(component.Find(".bit-dtrp").ClassList.Contains("bit-dtrp-foc"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldRenderItsLabelAndPlaceholder()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Label, "Stay");
            parameters.Add(p => p.Placeholder, "Pick the days");
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { Label = "custom-label" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Label = "color: red;" });
        });

        var label = component.Find("label.bit-dtrp-lbl");
        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual("Stay", label.TextContent.Trim());
        Assert.AreEqual(input.Id, label.GetAttribute("for"));
        Assert.AreEqual(label.Id, input.GetAttribute("aria-labelledby"));
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.AreEqual("color: red;", label.GetAttribute("style"));
        Assert.AreEqual("Pick the days", input.GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitDateRangePickerWithoutALabelShouldNotLabelItsInput()
    {
        var component = RenderComponent<BitDateRangePicker>();

        Assert.IsEmpty(component.FindAll("label"));
        Assert.IsNull(component.Find(".bit-dtrp-inp").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldRespectTheFieldIcon()
    {
        var component = RenderComponent<BitDateRangePicker>();

        Assert.IsTrue(component.Find(".bit-dtrp-ico").ClassList.Contains("bit-icon--CalendarMirrored"));

        component.Render(parameters => parameters.Add(p => p.IconName, "Calendar"));

        Assert.IsTrue(component.Find(".bit-dtrp-ico").ClassList.Contains("bit-icon--Calendar"));

        // The BitIconInfo wins when both are set, which is how an external icon library is used.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IconName, "Calendar");
            parameters.Add(p => p.Icon, BitIconInfo.Css("fa-solid fa-calendar-days"));
        });

        Assert.IsTrue(component.Find(".bit-dtrp-ico").ClassList.Contains("fa-calendar-days"));
        Assert.IsFalse(component.Find(".bit-dtrp-ico").ClassList.Contains("bit-icon--Calendar"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldRenderTheIconTemplateInPlaceOfTheIcon()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IconTemplate, "<b class=\"custom-icon\">x</b>");
        });

        Assert.IsEmpty(component.FindAll(".bit-dtrp-ico"));
        Assert.IsNotNull(component.Find(".bit-dtrp-icn .custom-icon"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitDateRangePickerShouldRespectVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-dtrp").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility:hidden", StringComparison.Ordinal));
            Assert.IsFalse(style.Contains("display:none", StringComparison.Ordinal));
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    public void BitDateRangePickerShouldRespectClassStyleAndId()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "margin: 1px;");
            parameters.Add(p => p.Id, "stay-picker");
        });

        var root = component.Find(".bit-dtrp");

        Assert.AreEqual("stay-picker", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin: 1px;");
    }

    [TestMethod,
        DataRow(BitDir.Rtl, "rtl", true),
        DataRow(BitDir.Ltr, "ltr", false)
    ]
    public void BitDateRangePickerShouldRenderTheExplicitDirOnItsRoot(BitDir dir, string expected, bool rtl)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Dir, dir);
        });

        var root = component.Find(".bit-dtrp");

        Assert.AreEqual(expected, root.GetAttribute("dir"));
        Assert.AreEqual(rtl, root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldWriteTabIndexAriaLabelAndNameOnItsInput()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "3");
            parameters.Add(p => p.AriaLabel, "Stay");
            parameters.Add(p => p.Name, "stay");
        });

        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual("3", input.GetAttribute("tabindex"));
        Assert.AreEqual("Stay", input.GetAttribute("aria-label"));
        Assert.AreEqual("stay", input.GetAttribute("name"));
        // The root is nothing to land on or to name: the field is.
        Assert.IsFalse(component.Find(".bit-dtrp").HasAttribute("tabindex"));
        Assert.IsFalse(component.Find(".bit-dtrp").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldSplatTheInputHtmlAttributesOnTheInputAndTheHtmlAttributesOnTheRoot()
    {
        // Arbitrary HTML attributes are captured by BitComponentBase from unmatched parameters, so they are
        // supplied as raw component attributes, as real markup would.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitDateRangePicker>(0);
            builder.AddAttribute(1, "data-root", "yes");
            builder.AddAttribute(2, nameof(BitDateRangePicker.InputHtmlAttributes), new Dictionary<string, object> { ["data-input"] = "yes" });
            builder.CloseComponent();
        });

        var root = component.Find(".bit-dtrp");
        var input = component.Find(".bit-dtrp-inp");

        Assert.AreEqual("yes", root.GetAttribute("data-root"));
        Assert.IsFalse(root.HasAttribute("data-input"));
        Assert.AreEqual("yes", input.GetAttribute("data-input"));
        Assert.IsFalse(input.HasAttribute("data-root"));
    }

    [TestMethod,
        DataRow(true, false, 1),
        DataRow(false, false, 0),
        DataRow(true, true, 0)
    ]
    public void BitDateRangePickerShouldSetUpTheSwipeOnlyWhenResponsiveAndNotStandalone(bool responsive, bool standalone, int expected)
    {
        RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Responsive, responsive);
            parameters.Add(p => p.Standalone, standalone);
        });

        // The swipe dismisses the callout, and standalone there is no callout to dismiss.
        Assert.AreEqual(expected, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Swipes.setup"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldHandItsCalloutTheDropDirectionAndTheResponsiveMode()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.DropDirection, BitDropDirection.All);
            parameters.Add(p => p.Responsive, true);
        });

        component.Find(".bit-dtrp-wrp").Click();

        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(BitResponsiveMode.Top, toggle.Arguments[7]); // responsiveMode
        Assert.AreEqual(BitDropDirection.All, toggle.Arguments[8]); // dropDirection
    }

    [TestMethod]
    public void BitDateRangePickerShouldRenderTheDayMonthAndYearCellTemplates()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Today, FixedDate(2026, 1, 15));
            parameters.Add(p => p.DayCellTemplate, d => $"<span class=\"day-cell\">{d.Day}</span>");
            parameters.Add(p => p.MonthCellTemplate, m => $"<span class=\"month-cell\">{m.Month}</span>");
            parameters.Add(p => p.YearCellTemplate, y => $"<span class=\"year-cell\">{y}</span>");
        });

        Assert.AreEqual(component.FindAll(".bit-dtrp-dbt").Count, component.FindAll(".bit-dtrp-dbt .day-cell").Count);
        Assert.AreEqual("15", component.Find(".bit-dtrp-dbt[tabindex='0'] .day-cell").TextContent);
        Assert.HasCount(12, component.FindAll(".bit-dtrp-mwp .month-cell"));
        Assert.AreEqual("1", component.FindAll(".bit-dtrp-mwp .month-cell")[0].TextContent);

        component.Find(".bit-dtrp-mwp .bit-dtrp-ptb").Click();

        Assert.HasCount(12, component.FindAll(".bit-dtrp-mwp .year-cell"));
        Assert.AreEqual("2025", component.FindAll(".bit-dtrp-mwp .year-cell")[0].TextContent);
    }

    [TestMethod]
    public void BitDateRangePickerShouldNameItsNavigationButtonsWithTheGivenTitles()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Today, FixedDate(2026, 1, 15));
            parameters.Add(p => p.GoToPrevMonthTitle, "Back a month");
            parameters.Add(p => p.GoToNextMonthTitle, "On a month");
            parameters.Add(p => p.GoToPrevYearTitle, "Back to {0}");
            parameters.Add(p => p.GoToNextYearTitle, "On to {0}");
            parameters.Add(p => p.YearPickerToggleTitle, "Pick a year of {0}");
            parameters.Add(p => p.YearRangePickerToggleTitle, "Pick a month of {0} to {1}");
            parameters.Add(p => p.GoToPrevYearRangeTitle, "Back to {0}-{1}");
            parameters.Add(p => p.GoToNextYearRangeTitle, "On to {0}-{1}");
        });

        foreach (var title in new[] { "Back a month", "On a month", "Back to 2025", "On to 2027", "Pick a year of 2026" })
        {
            Assert.AreEqual(title, component.Find($"button[title='{title}']").GetAttribute("aria-label"));
        }

        component.Find(".bit-dtrp-mwp .bit-dtrp-ptb").Click();

        // The year picker opens on the range that starts one year before the displayed one: 2025 - 2036.
        foreach (var title in new[] { "Pick a month of 2025 to 2036", "Back to 2013-2024", "On to 2037-2048" })
        {
            Assert.AreEqual(title, component.Find($"button[title='{title}']").GetAttribute("aria-label"));
        }
    }

    [TestMethod]
    public void BitDateRangePickerShouldShowTheMonthPickerAsAnOverlayWhenAsked()
    {
        // Standalone, so the overlay state comes from the parameters instead of the width of the window.
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowMonthPickerAsOverlay, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Today, FixedDate(2026, 1, 15));
            parameters.Add(p => p.MonthPickerToggleTitle, "Months of {0}");
        });

        Assert.IsEmpty(component.FindAll(".bit-dtrp-mwp"));

        // The month title is the way into the overlay, and says so.
        var toggle = component.Find(".bit-dtrp-dwp .bit-dtrp-ptb");

        Assert.AreEqual("Months of January 2026", toggle.GetAttribute("title"));
        Assert.AreEqual("Months of January 2026", toggle.GetAttribute("aria-label"));

        toggle.Click();

        // The overlay takes the day picker's place rather than sitting beside it.
        Assert.HasCount(1, component.FindAll(".bit-dtrp-mwp"));
        Assert.IsEmpty(component.FindAll(".bit-dtrp-dwp"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNameTheCloseButtonWithItsTitle()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonTitle, "Shut the calendar");
        });

        var close = component.Find("button[title='Shut the calendar']");

        Assert.AreEqual("Shut the calendar", close.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNameTheTimePickerToggleWithItsTitles()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.ShowTimePickerTitle, "Show the clock");
            parameters.Add(p => p.HideTimePickerTitle, "Hide the clock");
        });

        Assert.IsEmpty(component.FindAll(".bit-dtrp-twp"));

        var show = component.Find("button[aria-label='Show the clock']");

        Assert.AreEqual("Show the clock", show.GetAttribute("title"));

        show.Click();

        Assert.HasCount(1, component.FindAll(".bit-dtrp-twp"));

        var hide = component.Find("button[aria-label='Hide the clock']");

        Assert.AreEqual("Hide the clock", hide.GetAttribute("title"));

        hide.Click();

        Assert.IsEmpty(component.FindAll(".bit-dtrp-twp"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldRenderTheCalloutHeaderAndFooterTemplates()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHeaderTemplate, "<div class=\"my-header\">header</div>");
            parameters.Add(p => p.CalloutFooterTemplate, "<div class=\"my-footer\">footer</div>");
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { CalloutHeader = "header-cls", CalloutFooter = "footer-cls" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { CalloutHeader = "color: red", CalloutFooter = "color: blue" });
        });

        var header = component.Find(".header-cls");
        var footer = component.Find(".footer-cls");

        Assert.IsNotNull(header.QuerySelector(".my-header"));
        Assert.IsNotNull(footer.QuerySelector(".my-footer"));
        Assert.AreEqual("color: red", header.GetAttribute("style"));
        Assert.AreEqual("color: blue", footer.GetAttribute("style"));

        // The header sits above the pickers of the callout, and the footer below them.
        Assert.IsTrue(header.NextElementSibling!.ClassList.Contains("bit-dtrp-grp"));
        Assert.IsTrue(footer.PreviousElementSibling!.ClassList.Contains("bit-dtrp-grp"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheClassesAndStylesOfTheDaysOfTheRange()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Today, FixedDate(2026, 1, 20));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2026, 1, 10), EndDate = FixedDate(2026, 1, 15) });
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles
            {
                StartDayButton = "custom-start",
                EndDayButton = "custom-end",
                StartAndEndSelectionDays = "custom-ends",
                SelectedDayButtons = "custom-between",
                TodayDayButton = "custom-today",
                DayButton = "custom-day"
            });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles
            {
                StartDayButton = "color: red;",
                EndDayButton = "color: blue;",
                StartAndEndSelectionDays = "font-weight: bold;",
                SelectedDayButtons = "color: green;",
                TodayDayButton = "color: gray;",
                DayButton = "padding: 1px;"
            });
        });

        var start = component.Find(".bit-dtrp-dss");
        var end = component.Find(".bit-dtrp-dse");
        var between = component.FindAll(".bit-dtrp-dsb");
        var today = component.Find(".bit-dtrp-dtd");

        Assert.IsTrue(start.ClassList.Contains("custom-start"));
        Assert.IsTrue(start.ClassList.Contains("custom-ends"));
        Assert.IsTrue(start.ClassList.Contains("custom-day"));
        StringAssert.Contains(start.GetAttribute("style"), "color: red;");
        StringAssert.Contains(start.GetAttribute("style"), "font-weight: bold;");
        StringAssert.Contains(start.GetAttribute("style"), "padding: 1px;");

        Assert.IsTrue(end.ClassList.Contains("custom-end"));
        Assert.IsTrue(end.ClassList.Contains("custom-ends"));
        StringAssert.Contains(end.GetAttribute("style"), "color: blue;");
        StringAssert.Contains(end.GetAttribute("style"), "font-weight: bold;");

        // The four days between the tenth and the fifteenth.
        Assert.HasCount(4, between);
        Assert.IsTrue(between.All(b => b.ClassList.Contains("custom-between")));
        Assert.IsTrue(between.All(b => b.GetAttribute("style")!.Contains("color: green;")));
        Assert.IsTrue(between.All(b => b.ClassList.Contains("custom-ends") is false));

        Assert.IsTrue(today.ClassList.Contains("custom-today"));
        StringAssert.Contains(today.GetAttribute("style"), "color: gray;");
        Assert.IsFalse(today.ClassList.Contains("custom-between"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheClassesAndStylesOfTheHoveredDays()
    {
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Today, FixedDate(2026, 1, 20));
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = FixedDate(2026, 1, 10) });
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { HoveredDayButtons = "custom-hovered" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { HoveredDayButtons = "color: orange;" });
        });

        component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)").First(d => d.TextContent.Trim() == "13").PointerEnter();

        // The three days after the start date up to and including the hovered one.
        var hovered = component.FindAll(".bit-dtrp-dhr");

        Assert.HasCount(3, hovered);
        Assert.IsTrue(hovered.All(d => d.ClassList.Contains("custom-hovered")));
        Assert.IsTrue(hovered.All(d => d.GetAttribute("style")!.Contains("color: orange;")));
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheClassesAndStylesOfThePresets()
    {
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.Presets, new BitDateRangePickerPreset[]
            {
                new() { Text = "March 1-5", Class = "custom-march", Style = "color: red;", Value = new() { StartDate = FixedDate(2024, 3, 1), EndDate = FixedDate(2024, 3, 5) } },
            });
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { PresetsContainer = "custom-presets", PresetButton = "custom-preset", SelectedPresetButton = "custom-selected-preset" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { PresetsContainer = "gap: 1px;", PresetButton = "padding: 1px;" });
        });

        var container = component.Find(".bit-dtrp-prc");
        var preset = component.Find(".bit-dtrp-prb");

        Assert.IsTrue(container.ClassList.Contains("custom-presets"));
        Assert.AreEqual("gap: 1px;", container.GetAttribute("style"));
        Assert.IsTrue(preset.ClassList.Contains("custom-preset"));
        Assert.IsTrue(preset.ClassList.Contains("custom-march"));
        Assert.IsFalse(preset.ClassList.Contains("custom-selected-preset"));
        // The style of every preset comes first, the preset's own after it.
        StringAssert.Contains(preset.GetAttribute("style"), "padding: 1px;");
        StringAssert.Contains(preset.GetAttribute("style"), "color: red");

        preset.Click();

        preset = component.Find(".bit-dtrp-prb");

        Assert.IsTrue(preset.ClassList.Contains("bit-dtrp-prs"));
        Assert.IsTrue(preset.ClassList.Contains("custom-selected-preset"));
        Assert.IsTrue(preset.ClassList.Contains("custom-preset"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheTextsClassesAndStylesOfTheActions()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.AutoApply, false);
            parameters.Add(p => p.ApplyButtonText, "Keep");
            parameters.Add(p => p.CancelButtonText, "Drop");
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { ActionsContainer = "custom-actions", ApplyButton = "custom-apply", CancelButton = "custom-cancel" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { ActionsContainer = "gap: 1px;", ApplyButton = "color: green;", CancelButton = "color: red;" });
        });

        component.Find(".bit-dtrp-wrp").Click();

        var actions = component.Find(".bit-dtrp-act");
        var apply = component.Find(".bit-dtrp-apb");
        var cancel = component.Find(".bit-dtrp-cnb");

        Assert.IsTrue(actions.ClassList.Contains("custom-actions"));
        Assert.AreEqual("gap: 1px;", actions.GetAttribute("style"));
        Assert.AreEqual("Keep", apply.TextContent.Trim());
        Assert.IsTrue(apply.ClassList.Contains("custom-apply"));
        Assert.AreEqual("color: green;", apply.GetAttribute("style"));
        Assert.AreEqual("Drop", cancel.TextContent.Trim());
        Assert.IsTrue(cancel.ClassList.Contains("custom-cancel"));
        Assert.AreEqual("color: red;", cancel.GetAttribute("style"));

        // The actions are the footer of the callout, which the JS side keeps out of the scrolling content.
        var toggle = Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");

        Assert.AreEqual(actions.Id, toggle.Arguments[13]); // footerId
    }

    [TestMethod]
    public void BitDateRangePickerShouldApplyTheClassesAndStylesOfTheOverlayTheGroupAndTheDivider()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitDateRangePickerClassStyles { Overlay = "custom-overlay", Group = "custom-group", Divider = "custom-divider" });
            parameters.Add(p => p.Styles, new BitDateRangePickerClassStyles { Group = "padding: 1px;", Divider = "width: 2px;" });
        });

        Assert.IsTrue(component.Find(".bit-dtrp-ovl").ClassList.Contains("custom-overlay"));
        Assert.IsTrue(component.Find(".bit-dtrp-grp").ClassList.Contains("custom-group"));
        Assert.AreEqual("padding: 1px;", component.Find(".bit-dtrp-grp").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-dtrp-dvd").ClassList.Contains("custom-divider"));
        Assert.AreEqual("width: 2px;", component.Find(".bit-dtrp-dvd").GetAttribute("style"));
    }

    // The messages a typed text is refused with name the field - by its DisplayName ahead of the name of the bound
    // property - and tell an unreadable text from a blocked day and from the bounds.
    [TestMethod]
    public void BitDateRangePickerShouldNameTheFieldInItsParsingMessages()
    {
        var model = new BitDateRangePickerTestModel();
        var editContext = new EditContext(model);

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitDateRangePicker>(0);
                builder.AddAttribute(1, nameof(BitDateRangePicker.AllowTextInput), true);
                builder.AddAttribute(2, nameof(BitDateRangePicker.Culture), CultureInfo.InvariantCulture);
                builder.AddAttribute(3, nameof(BitDateRangePicker.DateFormat), "yyyy-MM-dd");
                builder.AddAttribute(4, nameof(BitDateRangePicker.ValueFormat), "{0} - {1}");
                builder.AddAttribute(5, nameof(BitDateRangePicker.DisplayName), "Stay");
                builder.AddAttribute(6, nameof(BitDateRangePicker.DisabledDaysOfWeek), new[] { DayOfWeek.Saturday });
                builder.AddAttribute(7, nameof(BitDateRangePicker.MaxDate), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero));
                builder.AddAttribute(8, nameof(BitDateRangePicker.Value), model.Value);
                builder.AddAttribute(9, nameof(BitDateRangePicker.ValueChanged), EventCallback.Factory.Create<BitDateRangePickerValue?>(this, v => model.Value = v));
                builder.AddAttribute(10, nameof(BitDateRangePicker.ValueExpression), (Expression<Func<BitDateRangePickerValue?>>)(() => model.Value));
                builder.CloseComponent();
            }));
        });

        component.Find(".bit-dtrp-inp").Change("nonsense");

        CollectionAssert.AreEqual(new[] { "The Stay field is not valid." }, editContext.GetValidationMessages().ToArray());

        // 2026-01-10 is a Saturday: readable, but a day the calendar blocks.
        component.Find(".bit-dtrp-inp").Change("2026-01-10 - 2026-01-13");

        CollectionAssert.AreEqual(new[] { "The Stay field contains a date that is not available." }, editContext.GetValidationMessages().ToArray());

        component.Find(".bit-dtrp-inp").Change("2027-01-12 - 2027-01-13");

        CollectionAssert.AreEqual(new[] { "The Stay field is out of the allowed range." }, editContext.GetValidationMessages().ToArray());
        Assert.IsNull(model.Value);

        component.Find(".bit-dtrp-inp").Change("2026-01-12 - 2026-01-13");

        Assert.IsEmpty(editContext.GetValidationMessages());
        Assert.AreEqual(new DateTime(2026, 1, 12), model.Value!.StartDate!.Value.DateTime);
    }

    [TestMethod]
    public async Task BitDateRangePickerDisposeShouldReleaseItsJsResources()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
        });

        var calloutId = component.Find(".bit-dtrp-cal").Id;

        // Disposal goes through bUnit so the component is disposed once, by the framework that owns it.
        await Context.DisposeComponentsAsync();

        // The keys, the swipe and the relocated callout are all registered under the callout's id, and released by it.
        foreach (var identifier in new[] { "BitBlazorUI.Calendars.dispose", "BitBlazorUI.Callouts.clear", "BitBlazorUI.Swipes.dispose" })
        {
            var call = Context.JSInterop.Invocations.Single(i => i.Identifier == identifier);

            Assert.AreEqual(calloutId, call.Arguments[0]);
        }
    }

    [TestMethod]
    public void BitDateRangePickerShouldKeepAnEndTimeSetBeforeTheEndDayIsPicked()
    {
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
            // A long delay keeps the press-and-hold spin out of the picture, so each press is exactly one step.
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        string EndHour() => component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].GetAttribute("value")!;

        void DecreaseEndHour()
        {
            var button = component.Find("button[title='Decrease end hour']");
            button.PointerDown();
            button.PointerUp();
        }

        DecreaseEndHour();
        Assert.AreEqual("22", EndHour());

        // The start day changes the value, which re-derives the time fields from it - and it has no end date yet to
        // read the end time from, so the one set for the end day still to be picked has to be kept.
        component.Find("[id$='-day-2024-06-10']").Click();
        Assert.AreEqual("22", EndHour());

        // A change of the end time is a change of the value as well, once it has a start date.
        DecreaseEndHour();
        Assert.AreEqual("21", EndHour());

        component.Find("[id$='-day-2024-06-14']").Click();

        Assert.AreEqual(new DateTimeOffset(2024, 6, 10, 0, 0, 0, TimeSpan.Zero), value!.StartDate);
        Assert.AreEqual(new DateTimeOffset(2024, 6, 14, 21, 59, 0, TimeSpan.Zero), value.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerShouldPutTheEndTimeBackOnceTheRangeIsCleared()
    {
        BitDateRangePickerValue? value = null;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 10, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.ContinuousSpinDelay, 60_000);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find("[id$='-day-2024-06-10']").Click();

        var button = component.Find("button[title='Decrease end hour']");
        button.PointerDown();
        button.PointerUp();

        Assert.AreEqual("22", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].GetAttribute("value"));

        // A time set for a range that is gone has no end day left to wait for.
        component.Find(".bit-dtrp-clr").Click();

        Assert.AreEqual("23", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[0].GetAttribute("value"));
        Assert.AreEqual("59", component.FindAll(".bit-dtrp-eic .bit-dtrp-tin")[1].GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerStartingOpenShouldShowTheDayPickerBesideTheTimePicker()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        // An initial IsOpen opens onto the state a click opens onto: the day grid with the time picker beside it,
        // rather than the time picker alone.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp").Count);
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-twp").Count);
        Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Utils.getBodyWidth"));
    }

    [TestMethod]
    public void BitDateRangePickerStartingOpenShouldFitTheMonthsToTheViewport()
    {
        Context.JSInterop.Setup<decimal>("BitBlazorUI.Utils.getBodyWidth").SetResult(320m);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MonthCount, 2);
        });

        // The extra months are dropped instead of being laid out side by side past the edge of the viewport.
        Assert.AreEqual(1, component.FindAll(".bit-dtrp-dwp").Count);
    }

    [TestMethod]
    public void BitDateRangePickerShouldLeaveSelectingTheTimeInputsToTheScript()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        // The script selects the text as the focus lands (see Calendars.ts): a select() from .NET a round trip later
        // would pull the focus back onto an input a quick Tab has already left.
        foreach (var input in component.FindAll(".bit-dtrp-tin"))
        {
            Assert.IsFalse(input.HasAttribute("blazor:onfocus"));
        }

        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Utils.selectText"));
    }
}
