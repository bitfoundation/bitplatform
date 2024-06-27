using System;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitIconButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, "Emoji2", null),
        DataRow(false, "Emoji2", null),
        DataRow(true, "Emoji2", "I'm Happy"),
        DataRow(false, "Emoji2", "I'm Happy")
    ]
    public void BitIconButtonTest(bool isEnabled, string iconName, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitIconButton = com.Find(".bit-icb");

        if (isEnabled)
        {
            Assert.IsFalse(bitIconButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitIconButton.ClassList.Contains("bit-dis"));
        }

        var bitIconITag = com.Find(".bit-icb > span.bit-icb-ict > i.bit-icon");
        Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName}"));

        if (title.HasValue())
        {
            Assert.IsTrue(bitIconButton.GetAttribute("title").Contains(title));
        }

        bitIconButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
      DataRow(true, false),
      DataRow(true, true),
      DataRow(false, false),
      DataRow(false, true)
    ]
    public void BitIconButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus)
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-icb");

        var hasTabindexAttr = bitButton.HasAttribute("tabindex");

        var expectedResult = isEnabled ? false : allowDisabledFocus ? false : true;

        Assert.AreEqual(hasTabindexAttr, expectedResult);

        if (hasTabindexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }
    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitIconButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitIconButton = com.Find(".bit-icb");

        Assert.IsTrue(bitIconButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitIconButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitIconButton = com.Find(".bit-icb");

        Assert.IsTrue(bitIconButton.GetAttribute("aria-label").Contains(ariaLabel));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
        DataRow(null)
    ]
    public void BitIconButtonAriaHiddenTest(bool expectedAriaHidden)
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, expectedAriaHidden);
        });

        var bitIconButton = com.Find(".bit-icb");

        Assert.AreEqual(expectedAriaHidden, bitIconButton.HasAttribute("aria-hidden"));
    }

    [DataTestMethod,
        DataRow("", true),
        DataRow("bing.com", true),
        DataRow("bing.com", false)
    ]
    public void BitIconButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitIconButton = component.Find(".bit-icb");
        var tagName = bitIconButton.TagName;
        var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(BitButtonType.Button),
        DataRow(BitButtonType.Submit),
        DataRow(BitButtonType.Reset)
    ]
    public void BitIconButtonTypeOfButtonTest(BitButtonType buttonType)
    {
        var component = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitIconButton = component.Find(".bit-icb");

        var buttonTypeName = buttonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => throw new NotSupportedException(),
        };
        Assert.AreEqual(bitIconButton.GetAttribute("type"), buttonTypeName);
    }
    
    [TestMethod]
    public void BitIconButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });
        
        var bitButton = com.Find(".bit-icb");

        Assert.AreEqual("submit", bitButton.GetAttribute("type"));
    }
    
    [TestMethod]
    public void BitIconButtonButtonStateNotOverridenInEditContextTest()
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });
        
        var bitButton = com.Find(".bit-icb");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}
