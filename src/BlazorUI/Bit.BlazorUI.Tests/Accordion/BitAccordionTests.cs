using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Accordion;

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
        string title = "title-value";
        string description = "description-value";
        string text = "text-value";

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

        Assert.IsFalse(root.ClassName.Contains("bit-acd-exp"));

        header.Click();

        Assert.IsTrue(root.ClassName.Contains("bit-acd-exp"));
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

        var root = com.Find(".bit-acd");

        Assert.AreEqual(defaultIsExpanded, root.ClassName.Contains("bit-acd-exp"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitAccordionShouldBeOnClickAndOnChange(bool isClick)
    {
        bool isClicked = !isClick;
        bool isChanged = !isClick;

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
        string expandedHeaderHtml = "<h1>Expanded</h1>";
        string collapsedHeaderHtml = "<h1>Collapsed</h1>";

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
        string contentHtml = "<h1>ContentTemplate</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, contentHtml);
        });

        var content = com.Find(".bit-acd-con");

        Assert.IsTrue(content.InnerHtml.Equals(contentHtml));
    }
}
