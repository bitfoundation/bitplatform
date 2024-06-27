using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

[TestClass]
public class BitDateRangePickerTests : BunitTestContext
{
    [DataTestMethod,
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

    [DataTestMethod, DataRow("<div>This is labelFragment</div>")]
    public void BitDateRangePickerShouldRenderLabelFragment(string labelTemplate)
    {
        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var bitDateRangePickerLabelChild = component.Find(".bit-dtrp > label").ChildNodes;
        bitDateRangePickerLabelChild.MarkupMatches(labelTemplate);
    }

    [DataTestMethod, DataRow("go to today text")]
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod]
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    //[DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod]
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
}
