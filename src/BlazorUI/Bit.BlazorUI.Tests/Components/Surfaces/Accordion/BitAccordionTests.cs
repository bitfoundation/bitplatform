using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Accordion;

[TestClass]
public class BitAccordionTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitAccordionIsEnabledTest(bool isEnabled)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitAccordion = com.Find(".bit-acd");

        if (isEnabled)
        {
            Assert.IsFalse(bitAccordion.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitAccordion.ClassList.Contains("bit-dis"));
        }
    }

    [DataTestMethod]
    public void BitAccordionShouldBeSetTitleAndDescriptionAndText()
    {
        var title = "title-value";
        var description = "description-value";
        var text = "text-value";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.Description, description);
            parameters.Add(p => p.ChildContent, text);
        });

        var bitAccordionTitle = com.Find(".bit-acd-ttl");
        var bitAccordionDescription = com.Find(".bit-acd-des");
        var bitAccordionText = com.Find(".bit-acd-con");

        Assert.AreEqual(bitAccordionTitle.TextContent, title);
        Assert.AreEqual(bitAccordionDescription.TextContent, description);
        Assert.AreEqual(bitAccordionText.TextContent, text);
    }

    [DataTestMethod]
    public void BitAccordionShouldBeExpandWhenClicked()
    {
        var com = RenderComponent<BitAccordion>();

        var root = com.Find(".bit-acd");
        var header = com.Find(".bit-acd-hdr");

        var icon = com.Find(".bit-acd-hdr > i");
        var content = com.Find(".bit-acd-con");
        header.Click();

        Assert.IsTrue(icon.ClassName.Contains("bit-acd-hex"));
        Assert.IsTrue(content.ClassName.Contains("bit-acd-cex"));
    }


    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeSetDefaultIsExpanded(bool defaultIsExpanded)
    {
        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, defaultIsExpanded);
        });

        var header = com.Find(".bit-acd-hdr > i");
        var content = com.Find(".bit-acd-con");

        Assert.AreEqual(defaultIsExpanded, header.ClassName.Contains("bit-acd-hex"));
        Assert.AreEqual(defaultIsExpanded, content.ClassName.Contains("bit-acd-cex"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeOnClickAndOnChange(bool isClick)
    {
        var isClicked = !isClick;
        var isChanged = !isClick;

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.OnClick, () => isClicked = isClick);
            parameters.Add(p => p.OnChange, () => isChanged = isClick);
        });

        var bitAccordionHeader = com.Find(".bit-acd-hdr");
        bitAccordionHeader.Click();

        Assert.AreEqual(isClick, isClicked);
        Assert.AreEqual(isClick, isChanged);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeSetHeaderTemplate(bool defaultIsExpanded)
    {
        var expandedHeaderHtml = "<h1>Expanded</h1>";
        var collapsedHeaderHtml = "<h1>Collapsed</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.DefaultIsExpanded, defaultIsExpanded);
            parameters.Add(p => p.HeaderTemplate, (isExpanded) => isExpanded ? expandedHeaderHtml : collapsedHeaderHtml);
        });

        var header = com.Find(".bit-acd-hdr");

        Assert.AreEqual(defaultIsExpanded, header.InnerHtml.Equals(expandedHeaderHtml));
        Assert.AreEqual(!defaultIsExpanded, header.InnerHtml.Equals(collapsedHeaderHtml));
    }

    [DataTestMethod]
    public void BitAccordionShouldBeSetContentTemplate()
    {
        var contentHtml = "<h1>ContentTemplate</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, contentHtml);
        });

        var content = com.Find(".bit-acd-con");

        Assert.IsTrue(content.InnerHtml.Equals(contentHtml));
    }
}
