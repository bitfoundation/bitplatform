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
            var component = RenderComponent<BitCheckboxTest>(parameters => parameters
            .Add(p => p.IsEnabled, isEnabled).Add(p => p.IsChecked, isChecked));

            var bitCheckbox = component.Find(".bit-checkbox-item");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-checkbox-container");
            Assert.AreEqual(afterClickHasCheckedClass, bitCheckboxContainer.ClassList.Contains("checked"));
        }

        [DataTestMethod, DataRow(true, true, false), DataRow(true, false, true)]
        public void IndeterminatedCheckbox_OnClick_MeetExpectedValue(bool isIndeterminate, bool isEnabled, bool afterClickHasIndeterminateClass)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters => parameters
            .Add(p => p.IsIndeterminate, isIndeterminate)
            .Add(p => p.IsEnabled, isEnabled));

            var bitCheckbox = component.Find(".bit-checkbox-item");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-checkbox-container");
            Assert.AreEqual(afterClickHasIndeterminateClass, bitCheckboxContainer.ClassList.Contains("indeterminate"));
        }

        [DataTestMethod, DataRow(true, true, true, true), DataRow(true, false, true, false)]
        public void IndeterminatedCheckboxWidthIsCheckedValue_OnClick_MeetExpectedValue(bool isIndeterminate, bool isChecked, bool isEnabled, bool afterClickHasCheckedClass)
        {
            var component = RenderComponent<BitCheckboxTest>(parameters => parameters
            .Add(p => p.IsIndeterminate, isIndeterminate)
            .Add(p => p.IsChecked, isChecked));

            var bitCheckbox = component.Find(".bit-checkbox-item");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-checkbox-container");
            Assert.AreEqual(afterClickHasCheckedClass, bitCheckboxContainer.ClassList.Contains("checked"));
        }
    }
}
