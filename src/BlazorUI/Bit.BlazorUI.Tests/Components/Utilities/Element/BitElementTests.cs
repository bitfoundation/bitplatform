using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Utilities.Element;

[TestClass]
public class BitElementTests : BunitTestContext
{
    [DataTestMethod,
     DataRow("div"),
     DataRow("button"),
     DataRow("input"),
     DataRow("a"),
     DataRow(null)
    ]
    [TestMethod]
    public void BitElementTagTest(string element)
    {
        var com = RenderComponent<BitElement>(parameters =>
        {
            if (element.HasValue())
            {
                parameters.Add(p => p.Element, element);
            }
        });

        var expectedElement = element ?? "div";

        var expectedHtml = $"<{expectedElement} diff:ignore></{expectedElement}>";

        com.MarkupMatches(expectedHtml);
    }

    [DataTestMethod]
    [DataRow("input", "placeholder", "Enter text")]
    [DataRow("a", "href", "/")]
    public void BitElementTagWithAttributesTest(string element, string attribute, string attributeValue)
    {
        var com = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, element);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object>
            {
                { attribute, attributeValue }
            });
        });

        var expectedHtml = $"<{element} {attribute}=\"{attributeValue}\" diff:ignore></{element}>";
        com.MarkupMatches(expectedHtml);
    }
}
