using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Links;

[TestClass]
public class BitLinkTests : BunitTestContext
{
    [DataTestMethod,
        DataRow("fakelink.com"), 
        DataRow("")
    ]
    public void BitLinkShouldRenderExpectedElementBaseOnHref(string href)
    {
        var component = RenderComponent<BitLinkTest>(parameters => 
        {
            parameters.Add(p => p.Href, href);
        });

        var bitLink = component.Find($".bit-lnk");

        var tagName = href.HasValue() ? "a" : "button";

        Assert.AreEqual(tagName, bitLink.TagName, ignoreCase: true);
    }

    [TestMethod]
    public void BitLinkButtonOnClickTest()
    {
        var component = RenderComponent<BitLinkButtonTest>();

        var bitLinkButton = component.Find(".bit-lnk");

        bitLinkButton.Click();

        Assert.AreEqual(1, component.Instance.CurrentCount);
    }
}
