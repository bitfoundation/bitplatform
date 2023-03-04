using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Pickers;

[TestClass]
public class BitDatePickerTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitDatePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitDatePicker = component.Find(".bit-dtp");

        if (isEnabled)
        {
            Assert.IsFalse(bitDatePicker.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitDatePicker.ClassList.Contains("disabled"));
        }
    }

    [DataTestMethod, DataRow("<div>This is labelTemplate</div>")]
    public void BitDatePickerShouldRenderLabelTemplate(string labelTemplate)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var bitDatePickerLabelChild = component.Find(".bit-dtp > label").ChildNodes;
        bitDatePickerLabelChild.MarkupMatches(labelTemplate);
    }

    [DataTestMethod, DataRow("go to today text")]
    public void BitDatePickerShouldGiveValueToGoToToday(string goToToday)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.GoToToday, goToToday);
            parameters.Add(p => p.IsOpen, true);
        });

        var goToTodayButton = component.Find(".go-today-btn");

        Assert.AreEqual(goToTodayButton.TextContent, goToToday);
    }

    [DataTestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitDatePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        int clickedValue = 0;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var bitDatePickerInput = component.Find(".wrapper");
        bitDatePickerInput.Click();

        Assert.AreEqual(count, clickedValue);
    }

    [DataTestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitDatePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled, int count)
    {
        int selectedDateValue = 0;
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnSelectDate, () => selectedDateValue++);
        });

        var dateItems = component.FindAll(".day-btn");

        Random random = new();
        int randomNumber = random.Next(0, dateItems.Count - 1);
        dateItems[randomNumber].Click();
        Assert.AreEqual(count, selectedDateValue);
    }

    [DataTestMethod]
    public void BitDatePickerCalendarSelectTodayDate()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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
    public void BitDatePickerCalendarWithCustomCultureInfo()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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

    [DataTestMethod]
    public void BitDatePickerValidationFormTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
        });

        var form = component.Find("form");
        form.Submit();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);

        //open date picker
        var datePicker = component.Find(".wrapper");
        datePicker.Click();

        //select today
        var today = component.Find(".date-cell--today button.day-btn");
        today.Click();

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [DataTestMethod]
    public void BitDatePickerValidationInvalidHtmlAttributeTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
        });

        var inputDate = component.Find("input[type='text']");
        Assert.IsFalse(inputDate.HasAttribute("aria-invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.IsTrue(inputDate.HasAttribute("aria-invalid"));
        Assert.AreEqual("true", inputDate.GetAttribute("aria-invalid"));

        //open date picker
        var datePicker = component.Find(".wrapper");
        datePicker.Click();

        //select today
        var today = component.Find(".date-cell--today button.day-btn");
        today.Click();

        form.Submit();

        Assert.IsFalse(inputDate.HasAttribute("aria-invalid"));
    }

    [DataTestMethod]
    public void BitDatePickerValidationInvalidCssClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
        });

        var bitDatePicker = component.Find(".bit-dtp");

        Assert.IsFalse(bitDatePicker.ClassList.Contains("invalid"));

        var form = component.Find("form");
        form.Submit();

        Assert.IsTrue(bitDatePicker.ClassList.Contains("invalid"));

        //open date picker
        var datePicker = component.Find(".wrapper");
        datePicker.Click();

        //select today
        var today = component.Find(".date-cell--today button.day-btn");
        today.Click();

        Assert.IsFalse(bitDatePicker.ClassList.Contains("invalid"));
    }

    [DataTestMethod, DataRow("DatePicker")]
    public void BitDatePickerAriaLabelTest(string pickerAriaLabel)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.PickerAriaLabel, pickerAriaLabel);
        });

        var bitDatePickerCallout = component.Find(".callout-main");
        var calloutAriaLabel = bitDatePickerCallout.GetAttribute("aria-label");

        Assert.AreEqual(pickerAriaLabel, calloutAriaLabel);
    }

    [DataTestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitDatePickerShowGoToTodayTest(bool showGoToToday)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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
    public void BitDatePickerShowCloseButtonTest(bool showCloseButton)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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
    public void BitDatePickerHighlightCurrentMonthTest(bool highlightCurrentMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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
    public void BitDatePickerHighlightSelectedMonthTest(bool highlightSelectedMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var component = RenderComponent<BitDatePicker>(parameters =>
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
    public void BitDatePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            {"style", "color: blue" }
        };

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var bitDatePickerCallout = component.Find(".callout-main");
        var calloutStyle = bitDatePickerCallout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }
}
