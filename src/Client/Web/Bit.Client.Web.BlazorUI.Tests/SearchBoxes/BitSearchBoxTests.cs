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
            var input = component.Find("input.bit-searchbox-item__input");

            var inputPlaceholder = input.GetAttribute("placeholder");

            Assert.AreEqual(componentPlaceholder, inputPlaceholder);
        }

        [DataTestMethod, DataRow("Search"), DataRow("Closed Issue"), DataRow("fake value")]
        public void SearchBox_DefaultValue_MeetEnteredValue(string value)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.Value, value));
            var input = component.Find("input.bit-searchbox-item__input");

            var inputValue = input.GetAttribute("value");

            Assert.AreEqual(value, inputValue);
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_NoAnimation_ShouldHaveClassName(bool disableAnimation)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.DisableAnimation, disableAnimation));
            var searchBox = component.Find(".bit-searchbox");

            Assert.AreEqual(disableAnimation, searchBox.ClassList.Contains("no-animation"));
        }

        [DataTestMethod, DataRow(true), DataRow(false)]
        public void SearchBox_Underlined_ShouldHaveClassName(bool isUnderlined)
        {
            var component = RenderComponent<BitSearchBoxTest>(parameter =>
            parameter.Add(p => p.IsUnderlined, isUnderlined));
            var searchBox = component.Find(".bit-searchbox");

            Assert.AreEqual(isUnderlined, searchBox.ClassList.Contains("underlined"));
        }

        //[DataTestMethod,DataRow("a"),DataRow("G"),DataRow(".")]
        //public void SearchBox_onTypeInSearchInput_MeetEnteredValue(string key) 
        //{  
        //    var component = RenderComponent<BitSearchBoxTest>();
        //    var input = component.Find("input.bit-searchbox-item__input");
        //    _context.JSInterop.Setup<string>("getElementProperty",input,"vale").SetResult(key);
        //    input.KeyDown(key);


        //    Assert.AreEqual(key, component.Instance.SearchBoxValue);
        //}

        //[TestMethod]
        //public void SearchBox_onTypeAlexInSearchInput_True()
        //{
        //    var component = RenderComponent<BitSearchBoxTest>();

        //    var input = component.Find("input.bit-searchbox-item__input");
        //    input.KeyDown("A");
        //    input.KeyDown("l");
        //    input.KeyDown("e");
        //    input.KeyDown("x");

        //    Assert.AreEqual("Alex", component.Instance.SearchBoxValue);
        //}


        //[TestMethod]
        //public void SearchBox_OnEscape_ClearInput() 
        //{
        //    var component = RenderComponent<BitSearchBoxTest>();
        //    var input = component.Find("input.bit-searchbox-item__input");

        //    input.KeyDown("A");
        //    Assert.AreEqual("A", component.Instance.SearchBoxValue);

        //    input.KeyDown(Key.Escape);
        //    Assert.AreEqual(string.Empty, component.Instance.SearchBoxValue);
        //}

        //[TestMethod]
        //public void SearchBox_OnClickClearButton_ClearInput()
        //{
        //    var component = RenderComponent<BitSearchBoxTest>();
        //    var input = component.Find("input.bit-searchbox-item__input");
        //    var clearButton = component.Find("button.bit-searchbox-item__clear-button");

        //    input.KeyDown("A");
        //    Assert.AreEqual("A", component.Instance.SearchBoxValue);

        //    clearButton.Click();
        //    Assert.AreEqual(string.Empty, component.Instance.SearchBoxValue);
        //}
    }
}
