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
            Assert.IsFalse(bitAccordion.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitAccordion.ClassList.Contains("disabled"));
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

        var bitAccordionTitle = com.Find(".title");
        var bitAccordionDescription = com.Find(".description");
        var bitAccordionText = com.Find(".content");

        Assert.AreEqual(bitAccordionTitle.TextContent, title);
        Assert.AreEqual(bitAccordionDescription.TextContent, description);
        Assert.AreEqual(bitAccordionText.TextContent, text);
    }

    [DataTestMethod]
    public void BitAccordionShouldBeExpandWhenClicked()
    {
        var com = RenderComponent<BitAccordion>();

        var bitAccordionHeader = com.Find(".header");
        var bitAccordionContent = com.Find(".content");
        bitAccordionHeader.Click();

        Assert.IsTrue(bitAccordionHeader.ClassName.Contains("expanded"));
        Assert.IsTrue(bitAccordionContent.ClassName.Contains("expanded"));
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

        var bitAccordionHeader = com.Find(".header");
        var bitAccordionContent = com.Find(".content");

        Assert.AreEqual(defaultIsExpanded, bitAccordionHeader.ClassName.Contains("expanded"));
        Assert.AreEqual(defaultIsExpanded, bitAccordionContent.ClassName.Contains("expanded"));
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

        var bitAccordionHeader = com.Find(".header");
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

        var bitAccordionHeader = com.Find(".header");

        Assert.AreEqual(defaultIsExpanded, bitAccordionHeader.ClassName.Contains("expanded"));
        Assert.AreEqual(defaultIsExpanded, bitAccordionHeader.InnerHtml.Equals(expandedHeaderHtml));
    }

    [DataTestMethod]
    public void BitAccordionShouldBeSetContentTemplate()
    {
        string contentHtml = "<h1>ContentTemplate</h1>";

        var com = RenderComponent<BitAccordion>(parameters =>
        {
            parameters.Add(p => p.ChildContent, contentHtml);
        });

        var bitAccordionContent = com.Find(".content");

        Assert.IsTrue(bitAccordionContent.InnerHtml.Equals(contentHtml));
    }
}
