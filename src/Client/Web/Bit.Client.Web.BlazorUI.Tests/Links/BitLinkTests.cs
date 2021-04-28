using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Links
{
    [TestClass]
    public class BitLinkTests : BunitTestContext
    {
        [DataTestMethod, DataRow("fakelink.com", "a"), DataRow("", "button")]
        public async Task BitLinkShouldRenderExpectedElementBaseOnHref(string href, string expectedElement)
        {
            var component = RenderComponent<BitLinkTest>(parameters => parameters.Add(p => p.Href, href));

            var bitLink = component.Find(".bit-lnk");
            var tagName = bitLink.TagName;

            Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
        }

        [TestMethod]
        public async Task BitLinkButton_OnClick_CounterValueEqualOne()
        {
            var component = RenderComponent<BitLinkButtonTest>();

            var bitLinkButton = component.Find("button.bit-lnk");
            bitLinkButton.Click();

            Assert.AreEqual(1, component.Instance.CurrentCount);
        }
    }
}
