using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Checkboxes
{
    [TestClass]
    public class BitCheckboxTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, true, false),
            DataRow(false, true, true),
            DataRow(true, false, true),
            DataRow(false, false, false)]
        public void BasicCheckbox_OnClick_MeetExpectedValue(bool isChecked, bool isEnabled, bool afterClickHasCheckedClass)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.IsChecked, isChecked);
            });

            var bitCheckbox = component.Find(".bit-chb-fluent > div > div");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-chb-fluent");
            Assert.AreEqual(afterClickHasCheckedClass, bitCheckboxContainer.ClassList.Contains("bit-chb-checked-fluent"));
        }

        [DataTestMethod, DataRow(true, true, false), DataRow(true, false, true)]
        public void IndeterminatedCheckbox_OnClick_MeetExpectedValue(bool isIndeterminate, bool isEnabled, bool afterClickHasIndeterminateClass)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsIndeterminate, isIndeterminate);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var bitCheckbox = component.Find(".bit-chb-fluent > div > div");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-chb-fluent");
            Assert.AreEqual(afterClickHasIndeterminateClass, bitCheckboxContainer.ClassList.Contains("bit-chb-indeterminate-fluent"));
        }

        [DataTestMethod, DataRow(true, true, true, true), DataRow(true, false, true, false)]
        public void IndeterminatedCheckboxWidthIsCheckedValue_OnClick_MeetExpectedValue(bool isIndeterminate, bool isChecked, bool isEnabled, bool afterClickHasCheckedClass)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsIndeterminate, isIndeterminate);
                parameters.Add(p => p.IsChecked, isChecked);
            });

            var bitCheckbox = component.Find(".bit-chb-fluent > div > div");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-chb-fluent");
            Assert.AreEqual(afterClickHasCheckedClass, bitCheckboxContainer.ClassList.Contains("bit-chb-checked-fluent"));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitCheckboxAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitCheckbox = com.Find(".bit-chb > input");

            Assert.IsTrue(bitCheckbox.GetAttribute("aria-label").Equals(ariaLabel));
        }
    }
}
