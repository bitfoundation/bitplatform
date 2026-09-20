using System;
using System.Collections.Generic;
using System.Globalization;
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
        DataRow(null, "bit-dtrp-pri"),
        DataRow(BitColor.Primary, "bit-dtrp-pri"),
        DataRow(BitColor.Secondary, "bit-dtrp-sec"),
        DataRow(BitColor.Tertiary, "bit-dtrp-ter"),
        DataRow(BitColor.Info, "bit-dtrp-inf"),
        DataRow(BitColor.Success, "bit-dtrp-suc"),
        DataRow(BitColor.Warning, "bit-dtrp-wrn"),
        DataRow(BitColor.SevereWarning, "bit-dtrp-swr"),
        DataRow(BitColor.Error, "bit-dtrp-err")
    ]
    public void BitDateRangePickerColorTest(BitColor? color, string expectedClass)
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
        var monthStart = FixedDate(DateTime.Now.Year, DateTime.Now.Month, 1);
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
            parameters.Bind(p => p.Value, currentValue, v => currentValue = v);
        });

        // A third click restarts the range, which used to null out the caller's own instance.
        component.Find(".bit-dtrp-dtd").Click();

        Assert.AreEqual(originalStart, boundValue.StartDate);
        Assert.AreEqual(originalEnd, boundValue.EndDate);
        Assert.AreNotSame(boundValue, component.Instance.Value);
        Assert.AreEqual(DateTimeOffset.Now.Date, component.Instance.Value!.StartDate!.Value.Date);
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
                new() { Text = "Disabled one", IsEnabled = false, Title = "Not yet" },
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
                new() { Text = "Today", IsEnabled = false, Value = new() { StartDate = DateTimeOffset.Now.Date } },
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
            parameters.Add(p => p.IsEnabled, isEnabled);
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
            parameters.Add(p => p.IsEnabled, true);
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
            parameters.Add(p => p.IsEnabled, true);
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
            parameters.Add(p => p.IsEnabled, true);
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
        Assert.AreEqual(0, component.FindAll(".validation-message").Count);
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
        // does - the width reported here being zero - so the extra months are dropped instead of being laid
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
    public void BitDateRangePickerShouldTrapTheFocusInAFloatingCallout()
    {
        var component = RenderComponent<BitDateRangePicker>();

        // A callout that reports itself a modal dialog has to hold the tab order, which happens on the JS
        // side - so it only works if the setup is actually told to trap.
        var setup = Context.JSInterop.Invocations["BitBlazorUI.Calendars.setup"].Single();

        Assert.AreEqual(true, setup.Arguments[1]);
    }

    [TestMethod]
    public void BitDateRangePickerShouldNotTrapTheFocusInAStandaloneCallout()
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var setup = Context.JSInterop.Invocations["BitBlazorUI.Calendars.setup"].Single();

        Assert.AreEqual(false, setup.Arguments[1]);
    }
}
