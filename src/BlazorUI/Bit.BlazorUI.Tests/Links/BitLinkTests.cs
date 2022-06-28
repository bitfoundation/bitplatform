using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Links
{
    [TestClass]
    public class BitLinkTests : BunitTestContext
    {
        [DataTestMethod, DataRow("fakelink.com", "a"), DataRow("", "button")]
        public void BitLinkShouldRenderExpectedElementBaseOnHref(string href, string expectedElement)
        {
            var component = RenderComponent<BitLinkTest>(parameters => parameters.Add(p => p.Href, href));

            var bitLink = component.Find($".bit-lnk > {expectedElement}");
            var tagName = bitLink.TagName;

            Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
        }

        [TestMethod]
        public void BitLinkButton_OnClick_CounterValueEqualOne()
        {
            var component = RenderComponent<BitLinkButtonTest>();

            var bitLinkButton = component.Find(".bit-lnk > button");
            bitLinkButton.Click();

            Assert.AreEqual(1, component.Instance.CurrentCount);
        }
    }
}
