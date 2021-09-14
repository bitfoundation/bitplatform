using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Checkboxes
{
    [TestClass]
    public class BitCheckboxTests : BunitTestContext
    {
        [DataTestMethod,
            DataRow(true, true),
            DataRow(false, true),
            DataRow(true, false),
            DataRow(false, false)
        ]
        public void BitCheckboxOnClickShouldWorkIfIsEnabled(bool defaultIsChecked, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsEnabled, isEnabled);
                parameters.Add(p => p.DefaultIsChecked, defaultIsChecked);
            });

            var chb = component.Find(".bit-chb");
            var chbCheckbox = component.Find(".bit-chb-checkbox");
            chbCheckbox.Click();

            Assert.AreEqual(isEnabled, chb.ClassList.Contains("bit-chb-checked-fluent"));
            if (isEnabled)
            {
                Assert.AreEqual(1, component.Instance.ClickCounter);
                Assert.IsTrue(component.Instance.IsCheckedChanged);
            }
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void IndeterminatedBitCheckboxShouldHaveCorrectClassNameIfIsEnabled(bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckbox>(parameters =>
            {
                parameters.Add(p => p.DefaultIsIndeterminate, true);
                parameters.Add(p => p.IsEnabled, isEnabled);
            });

            var chb = component.Find(".bit-chb");
            var chbCheckbox = component.Find(".bit-chb-checkbox");
            chbCheckbox.Click();

            if (isEnabled)
            {
                Assert.IsTrue(!chb.ClassList.Contains("bit-chb-indeterminate-fluent"));
            }
        }

        [DataTestMethod,
            DataRow(true, true, true),
            DataRow(true, false, true)
        ]
        public void IndeterminatedCheckboxWidthIsCheckedValue_OnClick_MeetExpectedValue(bool isIndeterminate, bool isChecked, bool isEnabled)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.IsIndeterminate, isIndeterminate);
                parameters.Add(p => p.IsChecked, isChecked);
            });

            var bitCheckbox = component.Find(".bit-chb-checkbox");
            bitCheckbox.Click();

            var bitCheckboxContainer = component.Find(".bit-chb-fluent");
            //Assert.AreEqual(afterClickHasCheckedClass, bitCheckboxContainer.ClassList.Contains("bit-chb-checked-fluent"));
        }

        [DataTestMethod,
            DataRow("Detailed label")
        ]
        public void BitCheckboxAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitCheckbox = com.Find("input");

            Assert.IsTrue(bitCheckbox.GetAttribute("aria-label").Equals(ariaLabel));
        }

        [DataTestMethod, DataRow("Emoji2")]
        public void BitCheckboxCustomCheckmarkIcon(string checkmarkIconName)
        {
            var com = RenderComponent<BitCheckboxTest>(parameters =>
            {
                parameters.Add(p => p.CheckmarkIconName, checkmarkIconName);
            });

            var icon = com.Find(".bit-chb-checkmark");

            Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{checkmarkIconName}"));
        }
    }
}
