using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProPanel;

[TestClass]
public class BitProPanelTests : BunitTestContext
{
    [TestMethod]
    public void BitProPanelContentTest()
    {
        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-ppl-bdy");

        elementContent.MarkupMatches("<div class=\"bit-ppl-bdy\"><div>Test Content</div></div>");
    }

    [TestMethod]
    public void BitProPanelFooterContentTest()
    {
        var footerContent = "<div>Test Footer Content</div>";

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Footer, footerContent);
        });

        var elementContent = com.Find(".bit-ppl-fcn :first-child");

        elementContent.MarkupMatches(footerContent);
    }

    [TestMethod]
    public void BitProPanelHeaderContentTest()
    {
        const string headerContent = "<div>Test Header Content</div>";

        var com = RenderComponent<BitProPanel>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Header, headerContent);
        });

        var elementContent = com.Find(".bit-ppl-hcn :first-child :first-child");

        elementContent.MarkupMatches(headerContent);
    }
}
