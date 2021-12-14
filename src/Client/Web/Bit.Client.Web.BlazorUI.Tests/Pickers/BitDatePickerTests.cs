﻿using System;
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

            var bitDatePickerInput = component.Find(".bit-txt-fluent>div input");
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
                   parameters.Add(p => p.Culture, CultureInfoProvider.GetPersianCaltureByFinglishNames());
               });

            var monthButtons = component.FindAll(".month-picker-wrapper .grid-container .btn-row button");

            var index = 0;
            foreach (var button in monthButtons)
            {
                Assert.AreEqual(button.FirstElementChild.TextContent, component.Instance.Culture.DateTimeFormat.AbbreviatedMonthNames[index++]);
            }
        }
    }
}
