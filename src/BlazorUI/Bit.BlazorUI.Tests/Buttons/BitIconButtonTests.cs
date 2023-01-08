using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitIconButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, BitIconName.Emoji2, null),
        DataRow(false, BitIconName.Emoji2, null),
        DataRow(true, BitIconName.Emoji2, "I'm Happy"),
        DataRow(false, BitIconName.Emoji2, "I'm Happy")
    ]
    public void BitIconButtonTest(bool isEnabled, BitIconName iconName, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitIconButton = com.Find(".bit-icob");

        if (isEnabled)
        {
            Assert.IsFalse(bitIconButton.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitIconButton.ClassList.Contains("disabled"));
        }

        var bitIconITag = com.Find(".bit-icob > span.icon-container > i.bit-icon");
        Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName.GetName()}"));

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

        var bitButton = com.Find(".bit-icob");

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

        var bitIconButton = com.Find(".bit-icob");

        Assert.IsTrue(bitIconButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitIconButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitIconButton = com.Find(".bit-icob");

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

        var bitIconButton = com.Find(".bit-icob");

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

        var bitIconButton = component.Find(".bit-icob");
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

        var bitIconButton = component.Find(".bit-icob");

        var buttonTypeName = buttonType == BitButtonType.Button ? "button" : buttonType == BitButtonType.Submit ? "submit" : "reset";
        Assert.AreEqual(bitIconButton.GetAttribute("type"), buttonTypeName);
    }
    
    [TestMethod]
    public void BitIconButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitIconButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });
        
        var bitButton = com.Find(".bit-icob");

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
        
        var bitButton = com.Find(".bit-icob");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}
