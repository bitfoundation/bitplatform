using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Element;

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
    public void BitElementTagTest(string tag)
    {
        var com = RenderComponent<BitElement>(parameters =>
        {
            if (tag.HasValue())
            {
                parameters.Add(p => p.Element, tag);
            }
        });

        var expectedElement = tag ?? "div";

        var expectedHtml = $"<{expectedElement} diff:ignore></{expectedElement}>";

        com.MarkupMatches(expectedHtml);
    }

    [DataTestMethod]
    [DataRow("input", "placeholder", "Enter text")]
    [DataRow("a", "href", "/")]
    public void BitElementTagWithAttributesTest(string tag, string attribute, string attributeValue)
    {
        var com = RenderComponent<BitElement>(parameters =>
        {
            parameters.Add(p => p.Element, tag);
            parameters.Add(p => p.HtmlAttributes, new Dictionary<string, object>
            {
                { attribute, attributeValue }
            });
        });

        var element = com.Find(tag);

        var expectedHtml = $"<{tag} {attribute}=\"{attributeValue}\" diff:ignore></{tag}>";
        com.MarkupMatches(expectedHtml);
    }
}
