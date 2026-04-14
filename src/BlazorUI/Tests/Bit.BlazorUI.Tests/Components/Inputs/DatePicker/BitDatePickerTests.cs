using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
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

    [Ignore]
    [TestMethod,
         DataRow("ChevronLeft", "bit-icon--ChevronLeft")
     ]
    public void BitDatePickerPrevYearRangeNavIconNameTest(string iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearRangeNavIconName, iconName);
        });
        
        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));
        
        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on PrevYearRangeNavIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDatePickerPrevYearRangeNavIconTest()
    {
        var expectedClass = "bit-icon--ChevronLeft";

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.PrevYearRangeNavIcon, BitIconInfo.Bit("ChevronLeft"));
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));
        
        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on PrevYearRangeNavIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod,
        DataRow("ChevronRight", "bit-icon--ChevronRight")
    ]
    public void BitDatePickerNextYearRangeNavIconNameTest(string iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearRangeNavIconName, iconName);
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));
        
        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on NextYearRangeNavIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDatePickerNextYearRangeNavIconTest()
    {
        var expectedClass = "bit-icon--ChevronRight";

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NextYearRangeNavIcon, BitIconInfo.Bit("ChevronRight"));
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));
        
        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on NextYearRangeNavIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod,
        DataRow("Clock", "bit-icon--Clock")
    ]
    public void BitDatePickerShowTimePickerIconNameTest(string iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePickerIconName, iconName);
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on ShowTimePickerIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDatePickerShowTimePickerIconTest()
    {
        var expectedClass = "bit-icon--Clock";

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePickerIcon, BitIconInfo.Bit("Clock"));
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on ShowTimePickerIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod,
        DataRow("Cancel", "bit-icon--Cancel")
    ]
    public void BitDatePickerHideTimePickerIconNameTest(string iconName, string expectedClass)
    {
        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowTimePicker, true);
            parameters.Add(p => p.ShowMonthPickerAsOverlay, true);
            parameters.Add(p => p.HideTimePickerIconName, iconName);
            parameters.Add(p => p.Classes, new BitDatePickerClassStyles { ShowTimePickerButton = "picker-button" });
        });

        var btn = component.Find(".picker-button");

        btn.Click();

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on HideTimePickerIcon but no matching icon element was found.");
    }

    [Ignore]
    [TestMethod]
    public void BitDatePickerHideTimePickerIconTest()
    {
        var expectedClass = "bit-icon--Cancel";

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.HideTimePickerIcon, BitIconInfo.Bit("Cancel"));
        });

        var icon = component.FindAll("i")
                            .FirstOrDefault(i => i.ClassList.Contains(expectedClass));

        Assert.IsNotNull(icon,
            $"Expected class '{expectedClass}' on HideTimePickerIcon but no matching icon element was found.");
    }
}
