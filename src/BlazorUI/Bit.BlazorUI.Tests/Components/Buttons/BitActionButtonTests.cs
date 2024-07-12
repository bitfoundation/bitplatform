using System;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitActionButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, "AddFriend", "title"),
        DataRow(false, "AddFriend", "title")
    ]
    public void BitActionButtonTest(bool isEnabled, string iconName, string title)
    {
        var clicked = false;
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.Title, title);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-acb");
        var bitIconITag = com.Find(".bit-icon");

        if (isEnabled)
        {
            Assert.IsFalse(bitButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitButton.ClassList.Contains("bit-dis"));
        }

        Assert.IsTrue(bitIconITag.ClassList.Contains($"bit-icon--{iconName}"));

        Assert.AreEqual(bitButton.GetAttribute("title"), title);

        bitButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
       DataRow(true, false, false),
       DataRow(true, true, false),
       DataRow(false, false, true),
       DataRow(false, true, false),
   ]
    public void BitActionButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-acb");

        var hasTabIndexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(hasTabIndexAttr, expectedResult);

        if (hasTabIndexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }

    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitActionButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = com.Find(".bit-acb");

        Assert.IsTrue(bitButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitActionButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = com.Find(".bit-acb");

        Assert.IsTrue(bitButton.GetAttribute("aria-label").Contains(ariaLabel));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitActionButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitButton = com.Find(".bit-acb");

        Assert.AreEqual(bitButton.HasAttribute("aria-hidden"), expectedResult);
    }

    [DataTestMethod,
        DataRow("", true),
        DataRow("bing.com", true),
        DataRow("bing.com", false)
    ]
    public void BitActionButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitActionButton = component.Find(".bit-acb");
        var tagName = bitActionButton.TagName;
        var expectedElement = href.HasValue() ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(BitButtonType.Button),
        DataRow(BitButtonType.Submit),
        DataRow(BitButtonType.Reset)
    ]
    public void BitActionButtonTypeOfButtonTest(BitButtonType buttonType)
    {
        var component = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitActionButton = component.Find(".bit-acb");

        var buttonTypeName = buttonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => throw new NotSupportedException(),
        };

        Assert.AreEqual(buttonTypeName, bitActionButton.GetAttribute("type"));
    }

    [TestMethod]
    public void BitActionButtonSubmitStateInEditContextTest()
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
        });

        var bitButton = com.Find(".bit-acb");

        Assert.AreEqual("submit", bitButton.GetAttribute("type"));
    }

    [TestMethod]
    public void BitActionButtonButtonStateNotOverriddenInEditContextTest()
    {
        var com = RenderComponent<BitActionButton>(parameters =>
        {
            parameters.Add(p => p.EditContext, new EditContext(this));
            parameters.Add(p => p.ButtonType, BitButtonType.Button);
        });

        var bitButton = com.Find(".bit-acb");

        Assert.AreEqual("button", bitButton.GetAttribute("type"));
    }
}
