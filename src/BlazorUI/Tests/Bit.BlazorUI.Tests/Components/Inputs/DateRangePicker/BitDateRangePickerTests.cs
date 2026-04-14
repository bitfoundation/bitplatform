using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

[TestClass]
public class BitDateRangePickerTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitDateRangePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
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
            parameters.Add(p => p.IsEnabled, isEnabled);
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
    public void BitDateRangePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled)
    {
        var isOpen = true;
        var changeValue = 0;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnChange, () => changeValue++);
        });

        var dateItems = component.FindAll(".bit-dtrp-dbt");

        Random random = new();
        int randomNumber = random.Next(0, dateItems.Count - 1);
        dateItems[randomNumber].Click();
        Assert.AreEqual(isEnabled ? 1 : 0, changeValue);
    }

    [TestMethod]
    public void BitDateRangePickerCalendarSelectTodayDate()
    {
        var isOpen = true;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
        });

        Assert.IsNull(component.Instance.Value);

        var today = component.Find(".bit-dtrp-dtd");
        today.Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.IsNotNull(component.Instance.Value.StartDate);
        Assert.IsNull(component.Instance.Value.EndDate);
        Assert.AreEqual(component.Instance.Value.StartDate.Value.Date, DateTimeOffset.Now.Date);
        Assert.AreEqual(component.Instance.Value.StartDate.Value.Offset, DateTimeOffset.Now.Offset);
        today.Click();

        Assert.IsNotNull(component.Instance.Value.StartDate);
        Assert.AreEqual(component.Instance.Value.StartDate.Value.Date, DateTimeOffset.Now.Date);
        Assert.AreEqual(component.Instance.Value.StartDate.Value.Offset, DateTimeOffset.Now.Offset);

        Assert.IsNotNull(component.Instance.Value.EndDate);
        Assert.AreEqual(component.Instance.Value.EndDate.Value.Date, DateTimeOffset.Now.Date);
        Assert.AreEqual(component.Instance.Value.EndDate.Value.Offset, DateTimeOffset.Now.Offset);
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

    //[TestMethod,
    //    DataRow(false),
    //    DataRow(true)
    //]
    //public void BitDateRangePickerShowCloseButtonTest(bool showCloseButton)
    //{
    //    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    //    var component = RenderComponent<BitDateRangePicker>(parameters =>
    //    {
    //        parameters.Add(p => p.ShowCloseButton, showCloseButton);
    //    });

    //    var closeBtnElms = component.FindAll(".bit-dtrp-cbtn");

    //    if (showCloseButton)
    //    {
    //        Assert.AreEqual(1, closeBtnElms.Count);
    //    }
    //    else
    //    {
    //        Assert.AreEqual(0, closeBtnElms.Count);
    //    }
    //}

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

    [Ignore]
    [TestMethod,
        DataRow("Clock", "bit-icon--Clock"),
        DataRow(null, "bit-icon--Clock")
    ]
    public void BitDateRangePickerShowTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.ShowTimePickerIconName, iconName);
            }
        });

        var icon = component.FindAll("i").FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on ShowTimePickerIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDateRangePickerShowTimePickerIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerIcon, BitIconInfo.Css("fa-solid fa-clock"));
        });

        var icon = component.FindAll("i").FirstOrDefault(i => i.ClassList.Contains("fa-clock"));

        Assert.IsNotNull(icon,
            $"Expected 'fa-clock' on ShowTimePickerIcon but no matching icon element was found.");
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on ShowTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [Ignore]
    [TestMethod,
        DataRow("CalendarSolid", "bit-icon--CalendarSolid"),
        DataRow(null, "bit-icon--CalendarMirrored")
    ]
    public void BitDateRangePickerHideTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.HideTimePickerIconName, iconName);
            }
        });

        var icon = component.FindAll("i").FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on HideTimePickerIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDateRangePickerHideTimePickerIconTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.HideTimePickerIcon, BitIconInfo.Css("fa-solid fa-calendar"));
        });

        var icon = component.FindAll("i").FirstOrDefault(i => i.ClassList.Contains("fa-calendar"));

        Assert.IsNotNull(icon,
            $"Expected 'fa-calendar' on HideTimePickerIcon but no matching icon element was found.");
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on HideTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
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
}
