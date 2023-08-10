using System;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitCompoundButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary),
        DataRow(true, BitButtonStyle.Standard),
        DataRow(false, BitButtonStyle.Primary),
        DataRow(false, BitButtonStyle.Standard)
    ]
    public void BitCompoundButtonTest(bool isEnabled, BitButtonStyle style)
    {
        var clicked = false;
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-cmb");

        if (isEnabled)
        {
            Assert.IsFalse(bitButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitButton.ClassList.Contains("bit-dis"));
        }

        bitButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
        DataRow(true, BitButtonStyle.Primary, false, false),
        DataRow(true, BitButtonStyle.Standard, true, false),
        DataRow(false, BitButtonStyle.Primary, false, true),
        DataRow(false, BitButtonStyle.Standard, true, false),
    ]
    public void BitCompoundButtonDisabledFocusTest(bool isEnabled, BitButtonStyle style, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = com.Find(".bit-cmb");

        var hasTabindexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(hasTabindexAttr, expectedResult);

        if (hasTabindexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }
    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitCompoundButtonAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitCompoundButton = com.Find(".bit-cmb");

        Assert.IsTrue(bitCompoundButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitCompoundButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitCompoundButton = com.Find(".bit-cmb");

        Assert.IsTrue(bitCompoundButton.GetAttribute("aria-label").Contains(ariaLabel));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitCompoundButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitCompoundButton = com.Find(".bit-cmb");

        Assert.AreEqual(bitCompoundButton.HasAttribute("aria-hidden"), expectedResult);
    }

    [DataTestMethod,
        DataRow("", true),
        DataRow("bing.com", true),
        DataRow("bing.com", false)
    ]
    public void BitCompoundButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitCompoundButton = component.Find(".bit-cmb");
        var tagName = bitCompoundButton.TagName;
        var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(BitButtonStyle.Primary),
        DataRow(BitButtonStyle.Standard),
        DataRow(null),
    ]
    public void BitCompoundButtonTypeOfButtonStyleTest(BitButtonStyle? buttonStyle)
    {
        var component = RenderComponent<BitCompoundButton>(parameters =>
        {
            if (buttonStyle.HasValue)
            {
                parameters.Add(p => p.ButtonStyle, buttonStyle.Value);
            }
        });

        var bitCompoundButton = component.Find(".bit-cmb");

        if (buttonStyle == BitButtonStyle.Standard)
        {
            Assert.IsFalse(bitCompoundButton.ClassList.Contains("bit-cmb-pri"));
            Assert.IsTrue(bitCompoundButton.ClassList.Contains("bit-cmb-std"));
        }
        else
        {
            Assert.IsTrue(bitCompoundButton.ClassList.Contains("bit-cmb-pri"));
            Assert.IsFalse(bitCompoundButton.ClassList.Contains("bit-cmb-std"));
        }
    }

    [DataTestMethod,
        DataRow(BitButtonType.Button),
        DataRow(BitButtonType.Submit),
        DataRow(BitButtonType.Reset)
    ]
    public void BitCompoundButtonTypeOfButtonTest(BitButtonType buttonType)
    {
        var component = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.ButtonType, buttonType);
        });

        var bitCompoundButton = component.Find(".bit-cmb");

        var buttonTypeName = buttonType switch
        {
            BitButtonType.Button => "button",
            BitButtonType.Submit => "submit",
            BitButtonType.Reset => "reset",
            _ => throw new NotSupportedException(),
        };
        Assert.AreEqual(bitCompoundButton.GetAttribute("type"), buttonTypeName);
    }
}
