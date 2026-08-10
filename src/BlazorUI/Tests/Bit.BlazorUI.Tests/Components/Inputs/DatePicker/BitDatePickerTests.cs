using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

[TestClass]
public class BitDatePickerTests : BunitTestContext
{
    [TestMethod,
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
            Assert.IsFalse(bitDatePicker.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitDatePicker.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod, DataRow("<div>This is labelTemplate</div>")]
    public void BitDatePickerShouldRenderLabelTemplate(string labelTemplate)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var bitDatePickerLabelChild = component.Find(".bit-dtp > label").ChildNodes;

        bitDatePickerLabelChild.MarkupMatches(labelTemplate);
    }

    [TestMethod, DataRow("go to today text")]
    public void BitDatePickerShouldGiveValueToGoToToday(string goToToday)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.GoToTodayTitle, goToToday);
            parameters.Add(p => p.IsOpen, true);
        });

        var goToTodayButton = component.Find(".bit-dtp-gtb");

        Assert.AreEqual(goToTodayButton.GetAttribute("title"), goToToday);
    }

    [TestMethod,
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

        var bitDatePickerInput = component.Find(".bit-dtp-wrp");

        bitDatePickerInput.Click();

        Assert.AreEqual(count, clickedValue);
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitDatePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled, int count)
    {
        var isOpen = true;
        var changedDateValue = 0;

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnChange, () => changedDateValue++);
        });

        var dateItems = component.FindAll(".bit-dtp-dbt");

        Random random = new();
        int randomNumber = random.Next(0, dateItems.Count - 1);

        dateItems[randomNumber].Click();

        Assert.AreEqual(count, changedDateValue);
    }

    [TestMethod]
    public void BitDatePickerCalendarSelectTodayDate()
    {
        var isOpen = true;

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.IsEnabled, true);
        });

        Assert.IsNull(component.Instance.Value);

        var today = component.Find(".bit-dtp-dtd");

        today.Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(component.Instance.Value.Value.Date, DateTimeOffset.Now.Date);
        Assert.AreEqual(component.Instance.Value.Value.Offset, DateTimeOffset.Now.Offset);
    }

    [TestMethod]
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
        var datePicker = component.Find(".bit-dtp-wrp");

        datePicker.Click();

        //select today
        var today = component.Find(".bit-dtp-dtd");

        today.Click();

        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(component.Instance.ValidCount, component.Instance.InvalidCount);
    }

    [TestMethod]
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
        var datePicker = component.Find(".bit-dtp-wrp");

        datePicker.Click();

        //select today
        var today = component.Find(".bit-dtp-dtd");

        today.Click();

        form.Submit();

        Assert.IsFalse(inputDate.HasAttribute("aria-invalid"));
    }

    [TestMethod]
    public void BitDatePickerValidationInvalidCssClassTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, true);
            parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
        });

        var bitDatePicker = component.Find(".bit-dtp");

        Assert.IsFalse(bitDatePicker.ClassList.Contains("bit-inv"));

        var form = component.Find("form");

        form.Submit();

        Assert.IsTrue(bitDatePicker.ClassList.Contains("bit-inv"));

        //open date picker
        var datePicker = component.Find(".bit-dtp-wrp");

        datePicker.Click();

        //select today
        var today = component.Find(".bit-dtp-dtd");

        today.Click();

        Assert.IsFalse(bitDatePicker.ClassList.Contains("bit-inv"));
    }

    [TestMethod, DataRow("DatePicker")]
    public void BitDatePickerAriaLabelTest(string pickerAriaLabel)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutAriaLabel, pickerAriaLabel);
        });

        var bitDatePickerCallout = component.Find(".bit-dtp-cac");
        var calloutAriaLabel = bitDatePickerCallout.GetAttribute("aria-label");

        Assert.AreEqual(pickerAriaLabel, calloutAriaLabel);
    }

    [TestMethod,
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

        var goToTodayBtnElms = component.FindAll(".bit-dtp-gtb");

        if (showGoToToday)
        {
            Assert.HasCount(1, goToTodayBtnElms);
        }
        else
        {
            Assert.IsEmpty(goToTodayBtnElms);
        }
    }

    //[TestMethod,
    //    DataRow(false),
    //    DataRow(true)
    //]
    //public void BitDatePickerShowCloseButtonTest(bool showCloseButton)
    //{
    //    Context.JSInterop.Mode = JSRuntimeMode.Loose;
    //    var component = RenderComponent<BitDatePicker>(parameters =>
    //    {
    //        parameters.Add(p => p.ShowCloseButton, showCloseButton);
    //    });

    //    var closeBtnElms = component.FindAll(".bit-dtp-cbtn");

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
    public void BitDatePickerHighlightCurrentMonthTest(bool highlightCurrentMonth)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.HighlightCurrentMonth, highlightCurrentMonth);
        });

        var currentMonthCells = component.FindAll(".bit-dtp-pcm");

        if (highlightCurrentMonth)
        {
            Assert.HasCount(1, currentMonthCells);
        }
        else
        {
            Assert.IsEmpty(currentMonthCells);
        }
    }

    [TestMethod,
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


        var selectedMonthCells = component.FindAll(".bit-dtp-psm");

        if (highlightSelectedMonth)
        {
            Assert.HasCount(1, selectedMonthCells);
        }
        else
        {
            Assert.IsEmpty(selectedMonthCells);
        }
    }

    [TestMethod]
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

        var bitDatePickerCallout = component.Find(".bit-dtp-cac");
        var calloutStyle = bitDatePickerCallout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }

    [TestMethod,
        DataRow("ChevronLeft", "bit-icon--ChevronLeft"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerPrevMonthNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevMonthNavIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-pkh .bit-dtp-nbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerPrevMonthNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-left"));
        });

        var icon = component.Find(".bit-dtp-pkh .bit-dtp-nbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-left"),
            $"Expected 'fa-chevron-left' on PrevMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerNextMonthNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.NextMonthNavIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-pkh .bit-dtp-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerNextMonthNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextMonthNavIcon, BitIconInfo.Css("fa-solid fa-chevron-right"));
        });

        var icon = component.Find(".bit-dtp-pkh .bit-dtp-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-right"),
            $"Expected 'fa-chevron-right' on NextMonthNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("CalendarDay", "bit-icon--CalendarDay"),
        DataRow(null, "bit-icon--GotoToday")
    ]
    public void BitDatePickerGoToTodayIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowGoToToday, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { GoToTodayIcon = "gtt-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.GoToTodayIconName, iconName);
            }
        });

        var icon = component.Find(".gtt-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerGoToTodayIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowGoToToday, true);
            parameters.Add(p => p.GoToTodayIcon, BitIconInfo.Css("fa-solid fa-calendar-day"));
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { GoToTodayIcon = "gtt-icon" });
        });

        var icon = component.Find(".gtt-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-calendar-day"),
            $"Expected 'fa-calendar-day' on GoToTodayIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("X", "bit-icon--X"),
        DataRow(null, "bit-icon--Cancel")
    ]
    public void BitDatePickerCloseButtonIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { CloseButtonIcon = "close-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.CloseButtonIconName, iconName);
            }
        });

        var icon = component.Find(".close-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerCloseButtonIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowCloseButton, true);
            parameters.Add(p => p.CloseButtonIcon, BitIconInfo.Css("fa-solid fa-xmark"));
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { CloseButtonIcon = "close-icon" });
        });

        var icon = component.Find(".close-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"),
            $"Expected 'fa-xmark' on CloseButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("XmarkCircle", "bit-icon--XmarkCircle"),
        DataRow(null, "bit-icon--Cancel")
    ]
    public void BitDatePickerClearButtonIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, DateTimeOffset.Now);

            if (iconName is not null)
            {
                parameters.Add(p => p.ClearButtonIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-clr i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerClearButtonIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, DateTimeOffset.Now);
            parameters.Add(p => p.ClearButtonIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        var icon = component.Find(".bit-dtp-clr i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-xmark"),
            $"Expected 'fa-xmark' on ClearButtonIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ClockRegular", "bit-icon--ClockRegular"),
        DataRow(null, "bit-icon--Clock")
    ]
    public void BitDatePickerGoToNowIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowGoToNow, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.GoToNowIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-gtn i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on GoToNowIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerGoToNowIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowGoToNow, true);
            parameters.Add(p => p.GoToNowIcon, BitIconInfo.Css("fa-solid fa-clock"));
        });

        var icon = component.Find(".bit-dtp-gtn i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on GoToNowIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-clock"),
            $"Expected 'fa-clock' on GoToNowIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUpSmall", "bit-icon--ChevronUpSmall"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDatePickerTimePickerIncreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.TimePickerIncreaseHourIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-tbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on TimePickerIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerTimePickerIncreaseHourIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
        });

        var icon = component.Find(".bit-dtp-tbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on TimePickerIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on TimePickerIncreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUpSmall", "bit-icon--ChevronUpSmall"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDatePickerTimePickerDecreaseHourIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.TimePickerDecreaseHourIconName, iconName);
            }
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[1].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on TimePickerDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerTimePickerDecreaseHourIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseHourIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[1].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on TimePickerDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on TimePickerDecreaseHourIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUpSmall", "bit-icon--ChevronUpSmall"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDatePickerTimePickerIncreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.TimePickerIncreaseMinuteIconName, iconName);
            }
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[2].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on TimePickerIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerTimePickerIncreaseMinuteIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerIncreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-up"));
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[2].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on TimePickerIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-up"),
            $"Expected 'fa-chevron-up' on TimePickerIncreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronUpSmall", "bit-icon--ChevronUpSmall"),
        DataRow(null, "bit-icon--ChevronDownSmall")
    ]
    public void BitDatePickerTimePickerDecreaseMinuteIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.TimePickerDecreaseMinuteIconName, iconName);
            }
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[3].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on TimePickerDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerTimePickerDecreaseMinuteIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerDecreaseMinuteIcon, BitIconInfo.Css("fa-solid fa-chevron-down"));
        });

        var tbtButtons = component.FindAll(".bit-dtp-tbt");
        var icon = tbtButtons[3].QuerySelector("i")!;

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on TimePickerDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-chevron-down"),
            $"Expected 'fa-chevron-down' on TimePickerDecreaseMinuteIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronLeft", "bit-icon--ChevronLeft"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerPrevYearNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevYearNavIconName, iconName);
            }
        });

        // PrevYearNavIcon is in the month-picker header (year-month-picker wrapper)
        var icon = component.Find(".bit-dtp-mwp .bit-dtp-pkh .bit-dtp-nbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerPrevYearNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-left"));
        });

        var icon = component.Find(".bit-dtp-mwp .bit-dtp-pkh .bit-dtp-nbt:first-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-left"),
            $"Expected 'fa-angles-left' on PrevYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerNextYearNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);

            if (iconName is not null)
            {
                parameters.Add(p => p.NextYearNavIconName, iconName);
            }
        });

        var icon = component.Find(".bit-dtp-mwp .bit-dtp-pkh .bit-dtp-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerNextYearNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearNavIcon, BitIconInfo.Css("fa-solid fa-angles-right"));
        });

        var icon = component.Find(".bit-dtp-mwp .bit-dtp-pkh .bit-dtp-nbt:last-child i");

        Assert.IsTrue(icon.ClassList.Contains("fa-solid"),
            $"Expected 'fa-solid' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
        Assert.IsTrue(icon.ClassList.Contains("fa-angles-right"),
            $"Expected 'fa-angles-right' on NextYearNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod,
        DataRow("ChevronLeft", "bit-icon--ChevronLeft"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerPrevYearRangeNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { PrevYearRangeNavIcon = "prev-range-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.PrevYearRangeNavIconName, iconName);
            }
        });

        // the year range picker is reached through the year toggle of the month picker header
        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        var icon = component.Find(".prev-range-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on PrevYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerPrevYearRangeNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-backward"));
            parameters.Add(p => p.PrevYearRangeNavIconName, "ShouldNotRender");
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { PrevYearRangeNavIcon = "prev-range-icon" });
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        var icon = component.Find(".prev-range-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-backward"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ShouldNotRender"));
    }

    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight"),
        DataRow(null, "bit-icon--Up")
    ]
    public void BitDatePickerNextYearRangeNavIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { NextYearRangeNavIcon = "next-range-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.NextYearRangeNavIconName, iconName);
            }
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        var icon = component.Find(".next-range-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on NextYearRangeNavIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerNextYearRangeNavIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearRangeNavIcon, BitIconInfo.Css("fa-solid fa-forward"));
            parameters.Add(p => p.NextYearRangeNavIconName, "ShouldNotRender");
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { NextYearRangeNavIcon = "next-range-icon" });
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        var icon = component.Find(".next-range-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-forward"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ShouldNotRender"));
    }

    [TestMethod,
        DataRow("ClockRegular", "bit-icon--ClockRegular"),
        DataRow(null, "bit-icon--Clock")
    ]
    public void BitDatePickerShowTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        // Standalone, so the overlay state comes from the parameters instead of the width of the window
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { ShowTimePickerIcon = "show-time-icon" });

            if (iconName is not null)
            {
                parameters.Add(p => p.ShowTimePickerIconName, iconName);
            }
        });

        var icon = component.Find(".show-time-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on ShowTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerShowTimePickerIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.ShowTimePickerIcon, BitIconInfo.Css("fa-solid fa-clock"));
            parameters.Add(p => p.ShowTimePickerIconName, "ShouldNotRender");
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { ShowTimePickerIcon = "show-time-icon" });
        });

        var icon = component.Find(".show-time-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-clock"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ShouldNotRender"));
    }

    [TestMethod,
        DataRow("Cancel", "bit-icon--Cancel"),
        DataRow(null, "bit-icon--CalendarMirrored")
    ]
    public void BitDatePickerHideTimePickerIconNameTest(string? iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles
            {
                ShowTimePickerButton = "show-time-button",
                HideTimePickerIcon = "hide-time-icon"
            });

            if (iconName is not null)
            {
                parameters.Add(p => p.HideTimePickerIconName, iconName);
            }
        });

        // the hide button only exists once the time picker overlay is on top
        component.Find(".show-time-button").Click();

        var icon = component.Find(".hide-time-icon");

        Assert.IsTrue(icon.ClassList.Contains(expectedClass),
            $"Expected class '{expectedClass}' on HideTimePickerIcon but got: {string.Join(' ', icon.ClassList)}");
    }

    [TestMethod]
    public void BitDatePickerHideTimePickerIconTest()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.HideTimePickerIcon, BitIconInfo.Css("fa-solid fa-calendar"));
            parameters.Add(p => p.HideTimePickerIconName, "ShouldNotRender");
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles
            {
                ShowTimePickerButton = "show-time-button",
                HideTimePickerIcon = "hide-time-icon"
            });
        });

        component.Find(".show-time-button").Click();

        var icon = component.Find(".hide-time-icon");

        Assert.IsTrue(icon.ClassList.Contains("fa-calendar"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--ShouldNotRender"));
    }

    [TestMethod]
    public void BitDatePickerShouldRespectDefaultValue()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var defaultValue = new DateTimeOffset(2020, 1, 15, 0, 0, 0, DateTimeOffset.Now.Offset);

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, component.Instance.Value);
    }

    // ── Disabled dates ────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRespectDisabledDates()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.DisabledDates, [GetLocalDate(2026, 1, 15)]);
        });

        var disabledButtons = component.FindAll(".bit-dtp-dbt[disabled]");

        Assert.HasCount(1, disabledButtons);
        Assert.AreEqual("15", disabledButtons[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerShouldRespectDisabledDaysOfWeek()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        Assert.IsEmpty(component.FindAll(".bit-dtp-dbt[disabled]"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.DisabledDaysOfWeek, [DayOfWeek.Saturday, DayOfWeek.Sunday]);
        });

        // two disabled days per rendered week
        Assert.AreEqual(component.FindAll(".bit-dtp-dgr").Count * 2, component.FindAll(".bit-dtp-dbt[disabled]").Count);
    }

    [TestMethod]
    public void BitDatePickerShouldRespectIsDateDisabled()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.IsDateDisabled, d => d.Day % 2 == 1);
        });

        var disabledButtons = component.FindAll(".bit-dtp-dbt[disabled]");

        Assert.IsNotEmpty(disabledButtons);
        Assert.IsTrue(disabledButtons.All(b => int.Parse(b.TextContent.Trim()) % 2 == 1));
    }

    [TestMethod]
    public void BitDatePickerDisabledDayShouldReportAValidAriaDisabled()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.DisabledDates, [GetLocalDate(2026, 1, 15)]);
        });

        var disabled = component.Find(".bit-dtp-dbt[disabled]");
        var enabled = component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "16");

        // "true"/"false" are the only values aria-disabled accepts; an empty one is ignored by AT
        Assert.AreEqual("true", disabled.GetAttribute("aria-disabled"));
        Assert.IsFalse(enabled.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitDatePickerShouldNotSelectDisabledDate()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.DisabledDates, [GetLocalDate(2026, 1, 15)]);
        });

        component.Find(".bit-dtp-dbt[disabled]").Click();

        Assert.IsNull(component.Instance.Value);
    }

    // ── Highlighted dates ─────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRespectHighlightedDates()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.HighlightedDates, [GetLocalDate(2026, 1, 15)]);
        });

        var highlightedButton = component.Find(".bit-dtp-dhl");

        Assert.AreEqual("15", highlightedButton.TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerShouldRespectGetDayClass()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.GetDayClass, d => d.Day == 15 ? "custom-day-class" : null);
        });

        var customButtons = component.FindAll(".custom-day-class");

        Assert.HasCount(1, customButtons);
        Assert.AreEqual("15", customButtons[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerShouldRespectHighlightedDayButtonClass()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.HighlightedDates, [GetLocalDate(2026, 1, 15)]);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { HighlightedDayButton = "custom-highlighted" });
        });

        Assert.HasCount(1, component.FindAll(".custom-highlighted"));
    }

    [TestMethod]
    public void BitDatePickerShouldRespectDaysGridClassAndStyle()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { DaysGrid = "custom-days-grid" });
            parameters.Add(p => p.Styles, new BitDatePickerClassStyles { DaysGrid = "padding:2px" });
        });

        var daysGrid = component.Find(".bit-dtp-grd");

        StringAssert.Contains(daysGrid.ClassName, "custom-days-grid");
        StringAssert.Contains(daysGrid.GetAttribute("style"), "padding:2px");
    }

    [TestMethod]
    public void BitDatePickerShouldRespectDayNameHeaderClassAndStyle()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { DayNameHeader = "custom-day-name" });
            parameters.Add(p => p.Styles, new BitDatePickerClassStyles { DayNameHeader = "color:red" });
        });

        var dayNameHeaders = component.FindAll(".bit-dtp-wlb");

        Assert.HasCount(7, dayNameHeaders);
        Assert.IsTrue(dayNameHeaders.All(h => h.ClassList.Contains("custom-day-name")));
        Assert.IsTrue(dayNameHeaders.All(h => h.GetAttribute("style")!.Contains("color:red")));
    }

    // ── Week configuration ────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRespectFirstDayOfWeek()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Monday);
        });

        var firstDayHeader = component.Find(".bit-dtp-dgh .bit-dtp-wlb");

        Assert.AreEqual(CultureInfo.CurrentUICulture.DateTimeFormat.GetShortestDayName(DayOfWeek.Monday), firstDayHeader.GetAttribute("title"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitDatePickerShouldRespectFixedWeeks(bool fixedWeeks)
    {
        // February 2026 fits in exactly 4 weeks when the week starts on Sunday
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 2, 15));
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Add(p => p.FixedWeeks, fixedWeeks);
        });

        Assert.HasCount(fixedWeeks ? 6 : 4, component.FindAll(".bit-dtp-dgr"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitDatePickerShouldRespectShowOutsideDays(bool showOutsideDays)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
            parameters.Add(p => p.ShowOutsideDays, showOutsideDays);
        });

        if (showOutsideDays)
        {
            Assert.IsNotEmpty(component.FindAll(".bit-dtp-dbo"));
            Assert.IsEmpty(component.FindAll(".bit-dtp-dbe"));
        }
        else
        {
            Assert.IsEmpty(component.FindAll(".bit-dtp-dbo"));
            Assert.IsNotEmpty(component.FindAll(".bit-dtp-dbe"));
        }
    }

    [TestMethod,
        DataRow(CalendarWeekRule.FirstDay),
        DataRow(CalendarWeekRule.FirstFullWeek),
        DataRow(CalendarWeekRule.FirstFourDayWeek)]
    public void BitDatePickerShouldRespectWeekNumberRule(CalendarWeekRule rule)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
            parameters.Add(p => p.ShowWeekNumbers, true);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Monday);
            parameters.Add(p => p.WeekNumberRule, rule);
        });

        // The first rendered row of March 2026 with Monday as the first day of the week runs from
        // Monday, February 23rd to Sunday, March 1st (March 1st is a Sunday, so it closes that row).
        var calendar = CultureInfo.CurrentUICulture.Calendar;
        var expected = calendar.GetWeekOfYear(new DateTime(2026, 2, 23), rule, DayOfWeek.Monday);

        Assert.AreEqual(expected.ToString(), component.FindAll(".bit-dtp-wnm")[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerWeekNumbersShouldDefaultToTheFirstFullWeekRule()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
            parameters.Add(p => p.ShowWeekNumbers, true);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Monday);
        });

        var calendar = CultureInfo.CurrentUICulture.Calendar;
        var expected = calendar.GetWeekOfYear(new DateTime(2026, 2, 23), CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);

        Assert.AreEqual(expected.ToString(), component.FindAll(".bit-dtp-wnm")[0].TextContent.Trim());
    }

    // ── Culture & TimeZone ────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRenderTheValueInTheGivenTimeZone()
    {
        // 2026-01-20 23:30 UTC is 2026-01-21 04:00 at UTC+04:30, so the input has to read the 21st, which
        // is the very day the calendar marks as selected.
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("bit-test-tz", TimeSpan.FromMinutes(270), "bit-test-tz", "bit-test-tz");

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.TimeZone, timeZone);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "yyyy/MM/dd HH:mm");
            parameters.Add(p => p.Value, new DateTimeOffset(2026, 1, 20, 23, 30, 0, TimeSpan.Zero));
        });

        Assert.AreEqual("2026/01/21 04:00", component.Find(".bit-dtp-inp").GetAttribute("value"));
        Assert.AreEqual("21", component.Find(".bit-dtp-dbs").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerShouldRenderEveryMonthOfANonGregorianCalendar()
    {
        // A leap year of the Hebrew calendar has thirteen months, so a grid of twelve would leave one of
        // them out and the days of the last one out of reach altogether.
        var calendar = new HebrewCalendar();
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        // The component counts months with CultureInfo.Calendar, which has no public setter and is not
        // touched by DateTimeFormat.Calendar, so the private backing field is the only way in.
        var calendarField = culture.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(calendarField, "CultureInfo._calendar is a runtime implementation detail; " +
                                        "update this test if the field is renamed or removed.");
        calendarField.SetValue(culture, calendar);

        // 2024-03-15 falls in the Hebrew year 5784, a leap year of thirteen months.
        var startingValue = GetLocalDate(2024, 3, 15);

        Assert.AreEqual(5784, calendar.GetYear(startingValue.DateTime));
        Assert.AreEqual(13, calendar.GetMonthsInYear(5784));

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.StartingValue, startingValue);
        });

        Assert.HasCount(13, component.FindAll(".bit-dtp-mwp .bit-dtp-pkb"));
    }

    // ── Today ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRespectToday()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Today, GetLocalDate(2021, 3, 15));
        });

        var todayButton = component.Find(".bit-dtp-dtd");

        Assert.AreEqual("15", todayButton.TextContent.Trim());
        Assert.AreEqual("date", todayButton.GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitDatePickerGoToTodayShouldUseTheTodayParameter()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Today, GetLocalDate(2021, 3, 15));
            parameters.Add(p => p.StartingValue, GetLocalDate(2024, 8, 10));
        });

        Assert.Contains("2024", component.Find(".bit-dtp-pkt, .bit-dtp-ptb").TextContent);

        component.Find(".bit-dtp-gtb").Click();

        Assert.Contains("2021", component.Find(".bit-dtp-pkt, .bit-dtp-ptb").TextContent);
    }

    // ── Color & Size ──────────────────────────────────────────────────────────

    [TestMethod,
        DataRow(null, "bit-dtp-pri"),
        DataRow(BitColor.Primary, "bit-dtp-pri"),
        DataRow(BitColor.Secondary, "bit-dtp-sec"),
        DataRow(BitColor.Success, "bit-dtp-suc"),
        DataRow(BitColor.Error, "bit-dtp-err"),
        DataRow(BitColor.TertiaryBorder, "bit-dtp-tbr")]
    public void BitDatePickerShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            if (color.HasValue)
            {
                parameters.Add(p => p.Color, color.Value);
            }
        });

        Assert.IsTrue(component.Find(".bit-dtp").ClassList.Contains(expectedClass));
        // The callout is rendered outside of the root, so it carries the color class itself.
        Assert.IsTrue(component.Find(".bit-dtp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-dtp-sm"),
        DataRow(BitSize.Medium, "bit-dtp-md"),
        DataRow(BitSize.Large, "bit-dtp-lg")]
    public void BitDatePickerShouldRespectSize(BitSize size, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(component.Find(".bit-dtp").ClassList.Contains(expectedClass));
        Assert.IsTrue(component.Find(".bit-dtp-cal").ClassList.Contains(expectedClass));
    }

    [TestMethod]
    public void BitDatePickerShouldNotRenderSizeClassByDefault()
    {
        var component = RenderComponent<BitDatePicker>();

        var classList = component.Find(".bit-dtp").ClassList;

        Assert.IsFalse(classList.Contains("bit-dtp-sm"));
        Assert.IsFalse(classList.Contains("bit-dtp-md"));
        Assert.IsFalse(classList.Contains("bit-dtp-lg"));
    }

    // ── MonthPicker visibility ────────────────────────────────────────────────

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitDatePickerShouldRespectIsMonthPickerVisible(bool isMonthPickerVisible)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsMonthPickerVisible, isMonthPickerVisible);
        });

        Assert.HasCount(isMonthPickerVisible ? 1 : 0, component.FindAll(".bit-dtp-mwp"));
        // The day picker stays visible either way.
        Assert.HasCount(1, component.FindAll(".bit-dtp-dwp"));
    }

    [TestMethod]
    public void BitDatePickerHiddenMonthPickerShouldNotTakeOverAsOverlay()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsMonthPickerVisible, false);
            parameters.Add(p => p.ShowMonthPickerAsOverlay, true);
        });

        Assert.IsEmpty(component.FindAll(".bit-dtp-mwp"));
        Assert.HasCount(1, component.FindAll(".bit-dtp-dwp"));
    }

    [TestMethod]
    public void BitDatePickerHiddenMonthPickerShouldKeepTheGoToTodayButton()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsMonthPickerVisible, false);
        });

        Assert.HasCount(1, component.FindAll(".bit-dtp-gtb"));
    }

    [TestMethod]
    public void BitDatePickerHiddenMonthPickerShouldKeepTheTimePickerReachable()
    {
        // Standalone, so the overlay state is resolved from the parameters instead of from the width of
        // the window the callout would be measured against.
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.IsMonthPickerVisible, false);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowTimePickerAsOverlay, true);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { ShowTimePickerButton = "show-time-picker" });
        });

        var toggle = component.Find(".show-time-picker");

        Assert.IsEmpty(component.FindAll(".bit-dtp-twp"));

        toggle.Click();

        Assert.HasCount(1, component.FindAll(".bit-dtp-twp"));
    }

    [TestMethod]
    public void BitDatePickerHiddenMonthPickerShouldNotRenderTheMonthTitleAsAToggle()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsMonthPickerVisible, false);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        // the day picker stays on screen instead of toggling to a month picker that is turned off
        Assert.IsEmpty(component.FindAll(".bit-dtp-dwp .bit-dtp-ptb"));
        Assert.HasCount(1, component.FindAll(".bit-dtp-dwp"));
    }

    [TestMethod]
    public void BitDatePickerMonthPickerModeShouldIgnoreIsMonthPickerVisible()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Mode, BitDatePickerMode.MonthPicker);
            parameters.Add(p => p.IsMonthPickerVisible, false);
        });

        Assert.HasCount(1, component.FindAll(".bit-dtp-mwp"));
        Assert.IsEmpty(component.FindAll(".bit-dtp-dwp"));
    }

    // ── Events ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldRespectOnSelectDate()
    {
        DateTimeOffset? selectedDate = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.OnSelectDate, (DateTimeOffset? date) => selectedDate = date);
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsNotNull(selectedDate);
        Assert.AreEqual(new DateTime(2026, 1, 20), selectedDate!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerShouldRespectOnMonthChange()
    {
        DateTimeOffset? changedMonth = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.OnMonthChange, (DateTimeOffset month) => changedMonth = month);
        });

        // the day picker's nav buttons: previous month, go to today, next month
        component.FindAll(".bit-dtp-dwp .bit-dtp-nbt").Last().Click();

        Assert.IsNotNull(changedMonth);
        Assert.AreEqual(new DateTime(2026, 2, 1), changedMonth!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerShouldNotFireOnMonthChangeWhenTheMonthStays()
    {
        var count = 0;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.OnMonthChange, (DateTimeOffset _) => count++);
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.AreEqual(0, count);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerSelectingADayOfTheNextYearShouldKeepTheCalendarInSync()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2025, 12, 15));
        });

        // the trailing days of the December 2025 grid belong to January 2026
        var januaryDay = component.FindAll(".bit-dtp-dbt.bit-dtp-dbo").Last();

        januaryDay.Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(2026, component.Instance.Value!.Value.Year);
        Assert.Contains("January 2026", component.Find(".bit-dtp-pkt, .bit-dtp-ptb").TextContent);
    }

    [TestMethod]
    public void BitDatePickerMonthPickerModeShouldClampTheSelectionToMinDate()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Mode, BitDatePickerMode.MonthPicker);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.MinDate, GetLocalDate(2026, 1, 10));
        });

        component.FindAll(".bit-dtp-pkb").First().Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(new DateTime(2026, 1, 10), component.Instance.Value!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerReadOnlyShouldNotAllowSelectingADay()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDatePickerStandaloneShouldSelectEvenWithAOneWayBoundIsOpen()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsNotNull(component.Instance.Value);
        Assert.AreEqual(new DateTime(2026, 1, 20), component.Instance.Value!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerReadOnlyShouldReportTheDaysAsAriaDisabled()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        var day = component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20");

        Assert.AreEqual("true", day.GetAttribute("aria-disabled"));
        // still focusable, since a read-only picker can be browsed
        Assert.IsFalse(day.HasAttribute("disabled"));

        Assert.AreEqual("true", component.Find(".bit-dtp-gtn").GetAttribute("aria-disabled"));
        Assert.IsTrue(component.FindAll(".bit-dtp-tbt").All(b => b.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitDatePickerTypingADateOfAnotherYearShouldMoveTheYearRange()
    {
        DateTimeOffset? value = null;
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input("20/01/2030");

        // the year picker toggle of the month picker header shows the year of the typed date
        Assert.Contains("2030", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent);
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerDayGridShouldFollowTheAriaGridPattern()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.ShowWeekNumbers, true);
        });

        Assert.IsNotNull(component.Find(".bit-dtp-grd[role='grid']"));
        Assert.IsNotNull(component.Find(".bit-dtp-dgh[role='row']"));
        Assert.IsNotEmpty(component.FindAll(".bit-dtp-dgr[role='row']"));
        Assert.IsNotEmpty(component.FindAll(".bit-dtp-wlb[role='columnheader']"));
        Assert.IsNotEmpty(component.FindAll(".bit-dtp-wnm[role='rowheader']"));
    }

    [TestMethod]
    public void BitDatePickerDayButtonsShouldHaveAnAccessibleName()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        var day = component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "15");
        var expected = new DateTime(2026, 1, 15).ToString(CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, day.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDatePickerCalloutShouldBeAModalDialogOnlyWhenItFloats()
    {
        var component = RenderComponent<BitDatePicker>();

        var callout = component.Find(".bit-dtp-cac");

        Assert.AreEqual("dialog", callout.GetAttribute("role"));
        Assert.AreEqual("true", callout.GetAttribute("aria-modal"));

        component.Render(parameters => parameters.Add(p => p.Standalone, true));

        callout = component.Find(".bit-dtp-cac");

        Assert.IsFalse(callout.HasAttribute("role"));
        Assert.IsFalse(callout.HasAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitDatePickerClearButtonShouldBeAccessible()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowClearButton, true);
            parameters.Add(p => p.Value, DateTimeOffset.Now);
            parameters.Add(p => p.ClearButtonTitle, "Remove the date");
        });

        var clearButton = component.Find(".bit-dtp-clr");

        Assert.IsFalse(clearButton.HasAttribute("aria-hidden"));
        Assert.AreEqual("Remove the date", clearButton.GetAttribute("aria-label"));
        Assert.AreEqual("Remove the date", clearButton.GetAttribute("title"));
    }

    [TestMethod]
    public void BitDatePickerTimePickerInputsShouldHaveAccessibleNames()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimePickerHourTitle, "Saat");
            parameters.Add(p => p.TimePickerMinuteTitle, "Daghighe");
        });

        var inputs = component.FindAll(".bit-dtp-tin");

        Assert.AreEqual("Saat", inputs[0].GetAttribute("aria-label"));
        Assert.AreEqual("Daghighe", inputs[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitDatePickerLiveRegionShouldBeEmptyWithoutAValue()
    {
        var component = RenderComponent<BitDatePicker>();

        Assert.AreEqual(string.Empty, component.Find(".bit-dtp-sdt").TextContent.Trim());
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldMakeExactlyOneDayFocusable()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        Assert.HasCount(1, component.FindAll(".bit-dtp-dbt[tabindex='0']"));
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldMoveFocusToNextDay()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
        });

        var focusedButton = component.Find(".bit-dtp-dbt[tabindex='0']");

        Assert.AreEqual("15", focusedButton.TextContent.Trim());

        focusedButton.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("16", component.Find(".bit-dtp-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod,
        DataRow("ArrowLeft", "14"),
        DataRow("ArrowUp", "8"),
        DataRow("ArrowDown", "22"),
        DataRow("Home", "11"),
        DataRow("End", "17")]
    public void BitDatePickerKeyboardNavigationShouldMoveFocusWithinMonth(string key, string expectedDay)
    {
        // January 15, 2026 is a Thursday; the week starts on Sunday, January 11 and ends on Saturday, January 17
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.FirstDayOfWeek, DayOfWeek.Sunday);
        });

        component.Find(".bit-dtp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedDay, component.Find(".bit-dtp-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldSkipDisabledDays()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.DisabledDates, [GetLocalDate(2026, 1, 16)]);
        });

        component.Find(".bit-dtp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("17", component.Find(".bit-dtp-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldChangeMonthOnPageDown()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        component.Find(".bit-dtp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.Contains("February", component.Find(".bit-dtp-pkt, .bit-dtp-ptb").TextContent);
        Assert.AreEqual("15", component.Find(".bit-dtp-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod,
        DataRow("PageUp", "January 2025"),
        DataRow("PageDown", "January 2027")]
    public void BitDatePickerKeyboardNavigationShouldChangeYearOnShiftPage(string key, string expectedTitle)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
        });

        component.Find(".bit-dtp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = true });

        Assert.Contains(expectedTitle, component.Find(".bit-dtp-pkt, .bit-dtp-ptb").TextContent);
        Assert.AreEqual("15", component.Find(".bit-dtp-dbt[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldReportTheMonthChange()
    {
        DateTimeOffset? changedMonth = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15));
            parameters.Add(p => p.OnMonthChange, (DateTimeOffset month) => changedMonth = month);
        });

        component.Find(".bit-dtp-dbt[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.IsNotNull(changedMonth);
        Assert.AreEqual(new DateTime(2026, 2, 1), changedMonth!.Value.Date);
    }

    // ── Keyboard navigation of the month and year grids ───────────────────────

    [TestMethod]
    public void BitDatePickerShouldMakeExactlyOneMonthFocusable()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        var focusableMonths = component.FindAll(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']");

        Assert.HasCount(1, focusableMonths);
        Assert.AreEqual("Mar", focusableMonths[0].TextContent.Trim());
    }

    [TestMethod,
        DataRow(3, "ArrowRight", "Apr"),
        DataRow(3, "ArrowLeft", "Feb"),
        DataRow(3, "ArrowDown", "Jul"),
        DataRow(7, "ArrowUp", "Mar"),
        DataRow(3, "Home", "Jan"),
        DataRow(3, "End", "Dec")]
    public void BitDatePickerKeyboardNavigationShouldMoveFocusWithinTheMonthGrid(int startingMonth, string key, string expectedMonth)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, startingMonth, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedMonth, component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldNotLeaveTheMonthGrid()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual("Jan", component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerKeyboardNavigationShouldSkipDisabledMonths()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
            parameters.Add(p => p.MaxDate, GetLocalDate(2026, 5, 31));
        });

        // April is next, May is the last month the range allows, and June onwards is disabled.
        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual("May", component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerPageKeysInTheMonthGridShouldChangeTheYear()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.InvariantCulture);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.Contains("2027", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent);

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageUp" });

        Assert.Contains("2026", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent);
    }

    [TestMethod]
    public void BitDatePickerShouldMakeExactlyOneYearFocusable()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        var focusableYears = component.FindAll(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']");

        Assert.HasCount(1, focusableYears);
        Assert.AreEqual("2026", focusableYears[0].TextContent.Trim());
    }

    [TestMethod,
        DataRow("ArrowRight", "2027"),
        DataRow("ArrowLeft", "2025"),
        DataRow("ArrowDown", "2030"),
        DataRow("Home", "2025"),
        DataRow("End", "2036")]
    public void BitDatePickerKeyboardNavigationShouldMoveFocusWithinTheYearGrid(string key, string expectedYear)
    {
        // The year picker opens on the range that starts one year before the displayed one: 2025 - 2036.
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expectedYear, component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerPageKeysInTheYearGridShouldChangeTheYearRange()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        Assert.Contains("2037", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent);
        Assert.AreEqual("2037", component.Find(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']").TextContent.Trim());
    }

    [TestMethod]
    public void BitDatePickerYearGridShouldStayReachableOutsideTheDisplayedYear()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 3, 15));
        });

        component.Find(".bit-dtp-mwp .bit-dtp-ptb").Click();

        // The next range holds no displayed year at all, so the fallback of the roving tabindex is what
        // keeps the grid in the tab sequence.
        component.FindAll(".bit-dtp-mwp .bit-dtp-nbt").Last().Click();

        Assert.HasCount(1, component.FindAll(".bit-dtp-mwp .bit-dtp-pkb[tabindex='0']"));
    }

    [TestMethod]
    public void BitDatePickerArrowKeysOnTheInputShouldOpenTheCallout()
    {
        var isOpen = false;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtp-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitDatePickerEscapeOnTheInputShouldCloseTheCallout()
    {
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtp-inp").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDatePickerEscapeInTheCalloutShouldCloseIt()
    {
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtp-cal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDatePickerDisabledArrowKeysShouldNotOpenTheCallout()
    {
        var isOpen = false;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
        });

        component.Find(".bit-dtp-inp").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.IsFalse(isOpen);
    }

    // ── AutoClose ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerAutoCloseShouldCloseTheCalloutOnSelection()
    {
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitDatePickerAutoCloseShouldKeepTheCalloutOpenWithTheTimePicker()
    {
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsTrue(isOpen);
        Assert.IsNotNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitDatePickerWithoutAutoCloseShouldKeepTheCalloutOpen()
    {
        var isOpen = true;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, v => isOpen = v);
            parameters.Add(p => p.AutoClose, false);
            parameters.Add(p => p.StartingValue, GetLocalDate(2026, 1, 15));
        });

        component.FindAll(".bit-dtp-dbt").First(b => b.TextContent.Trim() == "20").Click();

        Assert.IsTrue(isOpen);
    }

    // ── Time picker ───────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerTwelveHourClockShouldRenderTheHourOfTheMeridiem()
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, GetLocalDate(2026, 1, 15, 15, 30));
        });

        var hourInput = component.FindAll(".bit-dtp-tin")[0];

        Assert.AreEqual("3", hourInput.GetAttribute("value"));
        Assert.AreEqual("1", hourInput.GetAttribute("min"));
        Assert.AreEqual("12", hourInput.GetAttribute("max"));
    }

    [TestMethod,
        DataRow(15, 30, "5", 17),
        DataRow(15, 30, "12", 12),
        DataRow(9, 30, "5", 5),
        DataRow(9, 30, "12", 0)]
    public void BitDatePickerTwelveHourClockShouldKeepTheMeridiemOfTheTypedHour(int hour, int minute, string typed, int expectedHour)
    {
        DateTimeOffset? value = GetLocalDate(2026, 1, 15, hour, minute);

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-dtp-tin")[0].Input(typed);

        Assert.IsNotNull(value);
        Assert.AreEqual(expectedHour, value!.Value.Hour);
        Assert.AreEqual(minute, value!.Value.Minute);
    }

    [TestMethod]
    public void BitDatePickerTwentyFourHourClockShouldTakeTheTypedHourAsItIs()
    {
        DateTimeOffset? value = GetLocalDate(2026, 1, 15, 15, 30);

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var hourInput = component.FindAll(".bit-dtp-tin")[0];

        Assert.AreEqual("0", hourInput.GetAttribute("min"));
        Assert.AreEqual("23", hourInput.GetAttribute("max"));

        hourInput.Input("5");

        Assert.AreEqual(5, value!.Value.Hour);
    }

    // ── Text input ────────────────────────────────────────────────────────────

    [TestMethod]
    public void BitDatePickerShouldParseTheTypedDate()
    {
        DateTimeOffset? value = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input("20/01/2026");

        Assert.IsNotNull(value);
        Assert.AreEqual(new DateTime(2026, 1, 20), value!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerShouldRejectATypedDateOutOfRange()
    {
        DateTimeOffset? value = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Add(p => p.MinDate, GetLocalDate(2026, 1, 10));
            parameters.Add(p => p.MaxDate, GetLocalDate(2026, 1, 20));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input("25/01/2026");

        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitDatePickerMonthPickerModeShouldClampTheTypedMonthToMinDate()
    {
        DateTimeOffset? value = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Mode, BitDatePickerMode.MonthPicker);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "MM/yyyy");
            parameters.Add(p => p.MinDate, GetLocalDate(2026, 1, 10));
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // January is selectable in the calendar even though its first day is before MinDate, so typing
        // it has to be clamped the same way a click on it is rather than rejected as out of range.
        component.Find(".bit-dtp-inp").Input("01/2026");

        Assert.IsNotNull(value);
        Assert.AreEqual(new DateTime(2026, 1, 10), value!.Value.Date);
    }

    [TestMethod]
    public void BitDatePickerReadOnlyShouldIgnoreTheTypedDate()
    {
        DateTimeOffset? value = null;

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.DateFormat, "dd/MM/yyyy");
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input("20/01/2026");

        Assert.IsNull(value);
    }

    [TestMethod]
    public async Task BitDatePickerDisposeShouldNotThrow()
    {
        RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Value, DateTimeOffset.Now);
            parameters.Add(p => p.ShowTimePicker, true);
        });

        // Disposal goes through bUnit so the component is disposed once, by the framework that owns it.
        await Context.DisposeComponentsAsync();
    }

    private static DateTimeOffset GetLocalDate(int year, int month, int day, int hour = 0, int minute = 0)
    {
        var dateTime = new DateTime(year, month, day, hour, minute, 0);

        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }
}
