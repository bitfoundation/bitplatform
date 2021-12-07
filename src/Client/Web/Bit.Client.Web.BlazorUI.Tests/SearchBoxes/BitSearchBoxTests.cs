using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.SearchBoxes
{
    [TestClass]
    public class BitSearchBoxTests : BunitTestContext
    {
        [DataTestMethod, DataRow("Search"), DataRow("Filter")]
        public void SearchBox_Placeholder_MeetEnteredValue(string componentPlaceholder)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.Placeholder, componentPlaceholder));
            var input = component.Find(".srch-box-input");

            var inputPlaceholder = input.GetAttribute("placeholder");

            Assert.AreEqual(componentPlaceholder, inputPlaceholder);
        }

        [DataTestMethod, DataRow("Search"), DataRow("Closed Issue"), DataRow("fake value")]
        public void SearchBox_DefaultValue_MeetEnteredValue(string value)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.Value, value));
            var input = component.Find(".srch-box-input");

            var inputValue = input.GetAttribute("value");

            Assert.AreEqual(value, inputValue);
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_NoAnimation_ShouldHaveClassName(bool disableAnimation)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.DisableAnimation, disableAnimation));
            var searchBox = component.Find(".bit-srch-box-fluent");

            Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("bit-srch-box-no-animation-fluent"));
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_Underlined_ShouldHaveClassName(bool isUnderlined)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.IsUnderlined, isUnderlined));
            var searchBox = component.Find(".bit-srch-box-fluent");

            Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("bit-srch-box-underlined-fluent"));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitSearchBoxAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitSearchBoxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitSearchBox = com.Find(".srch-box-input");

            Assert.IsTrue(bitSearchBox.GetAttribute("aria-label").Equals(ariaLabel));
        }

        [DataTestMethod, DataRow("hello world")]
        public void BitSearchBoxShouldTakeDefaultValue(string defaultValue)
        {
            var component = RenderComponent<BitSearchBoxTest>(
                parameters =>
                {
                    parameters.Add(p => p.DefaultValue, defaultValue);
                });

            var input = component.Find(".srch-box-input");
            Assert.AreEqual(defaultValue, input.GetAttribute("value"));
        }

        [DataTestMethod,
            DataRow("hello world", true),
            DataRow("hello world", false)
        ]
        public void BitSearchBoxdMustShowSearchIconEvenHasValueWhenShowIconTrue(string value, bool showIcon)
        {
            var component = RenderComponent<BitSearchBoxTest>(
                parameters =>
                {
                    parameters.Add(p => p.Value, value);
                    parameters.Add(p => p.ShowIcon, showIcon);
                });

            var bitSearchBox = component.Find(".bit-srch-box");
            Assert.AreEqual(showIcon, bitSearchBox.ClassList.Contains("bit-srch-box-fixed-icon-has-value-fluent"));
        }

        [DataTestMethod,
            DataRow(true),
            DataRow(false)
        ]
        public void BitSearchBoxMustRespondToTheChangeEvent(bool isEnabled)
        {
            var component = RenderComponent<BitSearchBoxTest>(
                parameters =>
                {
                    parameters.Add(p => p.IsEnabled, isEnabled);                    
                });
            var input = component.Find(".srch-box-input");
            //TODO: bypassed - BUnit oninput event issue
            //input.KeyDown("a");
            //Assert.AreEqual(isEnabled ? 1 : 0, component.Instance.CurrentCount);
        }
    }
}
