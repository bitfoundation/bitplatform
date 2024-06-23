using System;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Links;

[TestClass]
public class BitLinkTests : BunitTestContext
{
    [DataTestMethod,
        DataRow("bitplatform.dev"),
        DataRow("")
    ]
    public void BitLinkShouldRenderExpectedElementBaseOnHref(string href)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.Href, href);
        });

        var bitLink = component.Find(".bit-lnk");

        var tagName = href.HasValue() ? "a" : "button";

        Assert.AreEqual(tagName, bitLink.TagName, ignoreCase: true);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLinkButtonOnClickTest(bool isEnabled)
    {
        int currentCount = 0;
        var component = RenderComponent<BitLink>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => currentCount++);
        });

        var bitLinkButton = component.Find(".bit-lnk");

        bitLinkButton.Click();

        Assert.AreEqual(isEnabled ? 1 : 0, currentCount);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
        DataRow(null)
    ]
    public void BitLinkShouldRespectUnderLineStyle(bool? hasUnderline)
    {
        var component = RenderComponent<BitLink>(parameters =>
        {
            if (hasUnderline.HasValue)
            {
                parameters.Add(p => p.HasUnderline, hasUnderline.Value);
            }
        });

        var bitLink = component.Find(".bit-lnk");

        Assert.AreEqual(hasUnderline ?? false, bitLink.ClassList.Contains("bit-lnk-und"));
    }
}
