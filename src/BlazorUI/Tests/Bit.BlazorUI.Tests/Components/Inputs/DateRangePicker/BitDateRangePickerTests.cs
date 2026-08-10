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
            parameters.Add(p => p.Today, new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero));
            parameters.Add(p => p.DisabledDaysOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        });

        var days = component.FindAll(".bit-dtrp-dbt");

        // June 2024 starts on a Saturday, so the very first rendered cell is a disabled weekend day.
        Assert.IsTrue(days.Count(d => d.HasAttribute("disabled")) > 0);
        Assert.IsTrue(days[0].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitDateRangePickerDisabledDatesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var today = DateTimeOffset.Now.Date;
        var disabledDate = new DateTimeOffset(today.AddDays(1), DateTimeOffset.Now.Offset);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
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
        var highlightedDate = new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(1), DateTimeOffset.Now.Offset);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
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
        var startDate = new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(-15), DateTimeOffset.Now.Offset);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
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
        var boundValue = new BitDateRangePickerValue
        {
            StartDate = new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(-5), DateTimeOffset.Now.Offset),
            EndDate = new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(-1), DateTimeOffset.Now.Offset)
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
    public void BitDateRangePickerDisabledPresetShouldNotBeSelectable()
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
    public void BitDateRangePickerPresetShouldNavigateTheCalendarToItsStartDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var isOpen = true;

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
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

        Assert.IsTrue(component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Contains("2024"));
        Assert.IsTrue(component.Find(".bit-dtrp-pkt, .bit-dtrp-ptb").TextContent.Contains("March"));
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
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.MaxRange, TimeSpan.FromHours(2));
            parameters.Add(p => p.Value, new BitDateRangePickerValue
            {
                StartDate = new DateTimeOffset(2024, 3, 1, 10, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2024, 3, 1, 11, 0, 0, TimeSpan.Zero)
            });
        });

        // Pushing the start time past the end time of the same day is not allowed.
        component.FindAll(".bit-dtrp-sic .bit-dtrp-tin")[1].Input("59");

        Assert.IsTrue(component.Instance.Value!.StartDate <= component.Instance.Value!.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerMonthCountShouldRenderConsecutiveMonths()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
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
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // December 2024 starts on a Sunday, so the only outside days of its grid are those of January 2025.
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

    // Midday keeps a date-only assertion safe from any time zone shifting the instant across midnight.
    private static DateTimeOffset FixedDate(int year, int month, int day)
    {
        var dateTime = new DateTime(year, month, day, 12, 0, 0);

        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    private static string MonthTitle(int year, int month)
    {
        return $"{System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(month)} {year}";
    }
}
