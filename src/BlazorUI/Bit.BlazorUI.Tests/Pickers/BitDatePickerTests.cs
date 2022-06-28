using System;
using Bit.Client.Web.BlazorUI.Utils;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Pickers
{
    [TestClass]
    public class BitDatePickerTests : BunitTestContext
    {
        [DataTestMethod,
          DataRow(Visual.Fluent, true),
          DataRow(Visual.Fluent, false),

          DataRow(Visual.Cupertino, true),
          DataRow(Visual.Cupertino, false),

          DataRow(Visual.Material, true),
          DataRow(Visual.Material, false)
        ]
        public void BitDatePickerShouldTakeCorrectVisual(Visual visual, bool isEnabled)
        {
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitDatePicker = component.Find(".bit-dtp");
            var datePickerIsEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitDatePicker.ClassList.Contains($"bit-dtp-{datePickerIsEnabledClass}-{visualClass}"));
        }

        [DataTestMethod, DataRow("go to today text")]
        public void BitDatePickerShouldGiveValueToGoToToday(string goToToday)
        {
            var component = RenderComponent<BitDatePickerTest>(
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
        public void BitDatePickerShouldHandleOnClickEvent(bool isEnabled, int count)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);
                });

            var bitDatePickerInput = component.Find(".bit-dtp-wrapper");
            bitDatePickerInput.Click();

            Assert.AreEqual(component.Instance.ClickedValue, count);
        }

        [DataTestMethod,
          DataRow(true, 1),
          DataRow(false, 0)
        ]
        public void BitDatePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled, int count)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = RenderComponent<BitDatePickerTest>(
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
        public void BitDatePickerCalendarSelectTodayDate()
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
        public void BitDatePickerCalendarWithCustomCultureInfo()
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = RenderComponent<BitDatePickerTest>(
               parameters =>
               {
                   parameters.Add(p => p.IsOpen, true);
                   parameters.Add(p => p.Culture, CultureInfoHelper.GetPersianCultureByFinglishNames());
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

            Assert.AreEqual(component.Instance.ValidCount, 0);
            Assert.AreEqual(component.Instance.InvalidCount, 1);

            //open date picker
            var datepicker = component.Find(".bit-dtp-wrapper");
            datepicker.Click();

            //select today
            var today = component.Find(".date-cell--today button.day-btn");
            today.Click();

            form.Submit();

            Assert.AreEqual(component.Instance.ValidCount, 1);
            Assert.AreEqual(component.Instance.InvalidCount, 1);
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
            Assert.AreEqual(inputDate.GetAttribute("aria-invalid"), "true");

            //open date picker
            var datepicker = component.Find(".bit-dtp-wrapper");
            datepicker.Click();

            //select today
            var today = component.Find(".date-cell--today button.day-btn");
            today.Click();

            form.Submit();

            Assert.IsFalse(inputDate.HasAttribute("aria-invalid"));
        }

        [DataTestMethod,
            DataRow(Visual.Fluent),
            DataRow(Visual.Cupertino),
            DataRow(Visual.Material),
        ]
        public void BitDatePickerValidationInvalidCssClassTest(Visual visual)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = RenderComponent<BitDatePickerValidationTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, true);
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.TestModel, new BitDatePickerTestModel());
            });

            var bitDatePicker = component.Find(".bit-dtp");
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsFalse(bitDatePicker.ClassList.Contains($"bit-dtp-invalid-{visualClass}"));

            var form = component.Find("form");
            form.Submit();

            Assert.IsTrue(bitDatePicker.ClassList.Contains($"bit-dtp-invalid-{visualClass}"));

            //open date picker
            var datepicker = component.Find(".bit-dtp-wrapper");
            datepicker.Click();

            //select today
            var today = component.Find(".date-cell--today button.day-btn");
            today.Click();

            Assert.IsFalse(bitDatePicker.ClassList.Contains($"bit-dtp-invalid-{visualClass}"));
        }
    }
}
