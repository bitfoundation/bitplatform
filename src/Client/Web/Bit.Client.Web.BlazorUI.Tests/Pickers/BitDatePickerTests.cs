using System;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Pickers
{
    [TestClass]
    public class BitDatePickerTests : BunitTestContext
    {
        [DataTestMethod,
          DataRow(Visual.Fluent, true, true),
          DataRow(Visual.Fluent, false, false),

          DataRow(Visual.Cupertino, true, true),
          DataRow(Visual.Cupertino, false, false),

          DataRow(Visual.Material, true, true),
          DataRow(Visual.Material, false, false)
      ]
        public void BitDatePickerShouldTakeCorrectVisual(Visual visual, bool isEnabled, bool isCalloutOpen)
        {
            var component = RenderComponent<BitDatePickerTest>(
                parameters =>
                {
                    parameters.Add(p => p.Visual, visual);
                    parameters.Add(p => p.IsEnabled, isEnabled);
                    parameters.Add(p => p.IsOpen, isCalloutOpen);
                });

            var bitDatePicker = component.Find(".bit-dtp");
            var datePickerIsEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitDatePicker.ClassList.Contains($"bit-dtp-{datePickerIsEnabledClass}-{visualClass}"));
            Assert.IsTrue(bitDatePicker.Children.Count().Equals(isCalloutOpen ? 2 : 1));
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

            var goToTodayButton = component.Find(".bit-dtp>div:nth-child(2)>div>div>div:nth-child(2)>div:nth-child(4)>button");

            goToTodayButton.MarkupMatches($"<button class=\"bit-dtp-cal\" type=\"button\">{goToToday}</button>");
        }

        [DataTestMethod,
          DataRow(true, 1),
          DataRow(false, 0)
      ]
        public void BitDatePickerShouldHandleOnClickEvent(bool isEnabled, int count)
        {
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
        public void BitDatePickerCalendarItemsShouldRespectIsEnabled(bool isEnabled,int count)
        {
            var component = RenderComponent<BitDatePickerTest>(
               parameters =>
               {
                   parameters.Add(p => p.IsOpen, true);
                   parameters.Add(p => p.IsEnabled, isEnabled);
               });
          
            var dateItems = component.FindAll(".bit-dtp>div:nth-child(2)>div>div>div:nth-child(2)>div:nth-child(2)>div:nth-child(2)>table>tbody tr:not([role]):nth-child(n+1) td");
            
            Random random = new();
            int randomNumber = random.Next(0, dateItems.Count -1);
            dateItems[randomNumber].Click();
            Assert.AreEqual(component.Instance.SelectedDateValue, count);
        }
    }
}
