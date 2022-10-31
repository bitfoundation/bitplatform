using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.DateRangePicker;

[TestClass]
public class BitDateRangePickerTests : BunitTestContext
{
    [DataTestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitDateRangePickerShouldRenderLabelFragment(string labelFragment)
    {
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.LabelFragment, labelFragment);
        });

        var bitDateRangePickerLabelChild = component.Find(".bit-dtrp > label").ChildNodes;
        bitDateRangePickerLabelChild.MarkupMatches(labelFragment);
    }

    [DataTestMethod,
      DataRow(Visual.Fluent, true),
      DataRow(Visual.Fluent, false),

      DataRow(Visual.Cupertino, true),
      DataRow(Visual.Cupertino, false),

      DataRow(Visual.Material, true),
      DataRow(Visual.Material, false)
    ]
    public void BitDateRangePickerShouldTakeCorrectVisual(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitDateRangePickerTest>(
            parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

        var bitDateRangePicker = component.Find(".bit-dtrp");
        var dateRangePickerIsEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitDateRangePicker.ClassList.Contains($"bit-dtrp-{dateRangePickerIsEnabledClass}-{visualClass}"));
    }

    [DataTestMethod, DataRow("go to today text")]
    public void BitDateRangePickerShouldGiveValueToGoToToday(string goToToday)
    {
        var component = RenderComponent<BitDateRangePickerTest>(
            parameters =>
            {
                parameters.Add(p => p.GoToToday, goToToday);
                parameters.Add(p => p.IsOpen, true);
            });

        var goToTodayButton = component.Find(".go-today-btn");

        Assert.AreEqual(goToToday, goToTodayButton.TextContent);
    }

    [DataTestMethod,
      DataRow(true, 1),
      DataRow(false, 0)
    ]
    public void BitDateRangePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(
            parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

        var bitDateRangePickerInput = component.Find(".dtrp-wrapper");
        bitDateRangePickerInput.Click();

        Assert.AreEqual(component.Instance.ClickedValue, count);
    }

    [DataTestMethod,
      DataRow(true, 1),
      DataRow(false, 0)
    ]
    public void BitDateRangePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(
           parameters =>
           {
               parameters.Add(p => p.IsOpen, true);
               parameters.Add(p => p.IsEnabled, isEnabled);
           });

        var dateItems = component.FindAll(".day-btn");

        Random random = new();
        int randomNumber = random.Next(0, dateItems.Count - 1);
        dateItems[randomNumber].Click();
        Assert.AreEqual(component.Instance.SelectedDateValue, count);
    }

    [DataTestMethod]
    public void BitDateRangePickerCalendarSelectTodayDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(
           parameters =>
           {
               parameters.Add(p => p.IsOpen, true);
               parameters.Add(p => p.IsEnabled, true);
           });

        Assert.IsNull(component.Instance.Value);

        var today = component.Find(".date-cell--today button.day-btn");
        today.Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(component.Instance.Value.Value.Date, DateTimeOffset.Now.Date);
        Assert.AreEqual(component.Instance.Value.Value.Offset, DateTimeOffset.Now.Offset);
    }

    [DataTestMethod]
    public void BitDateRangePickerCalendarWithCustomCultureInfo()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(
           parameters =>
           {
               parameters.Add(p => p.IsOpen, true);
               parameters.Add(p => p.Culture, CultureInfoHelper.GetFaIrCultureByFingilishNames());
           });

        var monthButtons = component.FindAll(".month-picker-wrapper .grid-container .btn-row button");

        var index = 0;
        foreach (var button in monthButtons)
        {
            Assert.AreEqual(button.FirstElementChild.TextContent, component.Instance.Culture.DateTimeFormat.AbbreviatedMonthNames[index++]);
        }
    }

    [DataTestMethod,
        DataRow("DateRangePicker")
    ]
    public void BitDateRangePickerAriaLabelTest(string pickerAriaLabel)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.PickerAriaLabel, pickerAriaLabel);
        });

        var bitDateRangePickerCallout = component.Find(".dtrp-callout-main");
        var calloutAriaLabel = bitDateRangePickerCallout.GetAttribute("aria-label");

        Assert.AreEqual(pickerAriaLabel, calloutAriaLabel);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerShowGoToTodayTest(bool showGoToToday)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.ShowGoToToday, showGoToToday);
        });

        var goToTodayBtnElms = component.FindAll(".go-today-btn");

        if (showGoToToday)
        {
            Assert.AreEqual(1, goToTodayBtnElms.Count);
        }
        else
        {
            Assert.AreEqual(0, goToTodayBtnElms.Count);
        }
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerShowCloseButtonTest(bool showCloseButton)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.ShowCloseButton, showCloseButton);
        });

        var closeBtnElms = component.FindAll(".header-icon-btn");

        if (showCloseButton)
        {
            Assert.AreEqual(1, closeBtnElms.Count);
        }
        else
        {
            Assert.AreEqual(0, closeBtnElms.Count);
        }
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerHighlightCurrentMonthTest(bool highlightCurrentMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.HighlightCurrentMonth, highlightCurrentMonth);
        });

        var currentMonthCells = component.FindAll(".current-month");

        if (highlightCurrentMonth)
        {
            Assert.AreEqual(1, currentMonthCells.Count);
        }
        else
        {
            Assert.AreEqual(0, currentMonthCells.Count);
        }
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDateRangePickerHighlightSelectedMonthTest(bool highlightSelectedMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.HighlightSelectedMonth, highlightSelectedMonth);
        });


        var selectedMonthCells = component.FindAll(".selected-month");

        if (highlightSelectedMonth)
        {
            Assert.AreEqual(1, selectedMonthCells.Count);
        }
        else
        {
            Assert.AreEqual(0, selectedMonthCells.Count);
        }
    }

    [DataTestMethod]
    public void BitDateRangePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            {"style", "color: blue" }
        };

        var component = RenderComponent<BitDateRangePickerTest>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var bitDateRangePickerCallout = component.Find(".dtrp-callout-main");
        var calloutStyle = bitDateRangePickerCallout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }
}
