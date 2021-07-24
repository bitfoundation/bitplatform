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
            var input = component.Find("input.search-input");

            var inputPlaceholder = input.GetAttribute("placeholder");

            Assert.AreEqual(componentPlaceholder, inputPlaceholder);
        }

        [DataTestMethod, DataRow("Search"), DataRow("Closed Issue"), DataRow("fake value")]
        public void SearchBox_DefaultValue_MeetEnteredValue(string value)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.Value, value));
            var input = component.Find("input.search-input");

            var inputValue = input.GetAttribute("value");

            Assert.AreEqual(value, inputValue);
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_NoAnimation_ShouldHaveClassName(bool disableAnimation)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.DisableAnimation, disableAnimation));
            var searchBox = component.Find(".bit-sch-box-fluent");

            Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("bit-sch-box-no-animation-fluent"));
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_Underlined_ShouldHaveClassName(bool isUnderlined)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.IsUnderlined, isUnderlined));
            var searchBox = component.Find(".bit-sch-box-fluent");

            Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("bit-sch-box-underlined-fluent"));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitSearchBoxAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitSearchBoxTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitSearchBox = com.Find(".search-input");

            Assert.IsTrue(bitSearchBox.GetAttribute("aria-label").Equals(ariaLabel));
        }
    }
}
