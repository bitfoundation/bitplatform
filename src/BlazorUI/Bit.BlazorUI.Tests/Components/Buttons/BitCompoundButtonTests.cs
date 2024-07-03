using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitCompoundButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, BitVariant.Fill),
        DataRow(true, BitVariant.Outline),
        DataRow(false, BitVariant.Fill),
        DataRow(false, BitVariant.Outline)
    ]
    public void BitCompoundButtonTest(bool isEnabled, BitVariant variant)
    {
        var clicked = false;
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
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
        DataRow(true, BitVariant.Fill, false, false),
        DataRow(true, BitVariant.Outline, true, false),
        DataRow(false, BitVariant.Fill, false, true),
        DataRow(false, BitVariant.Outline, true, false),
    ]
    public void BitCompoundButtonDisabledFocusTest(bool isEnabled, BitVariant variant, bool allowDisabledFocus, bool expectedResult)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Variant, variant);
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
        DataRow(BitVariant.Fill),
        DataRow(BitVariant.Outline),
        DataRow(null),
    ]
    public void BitCompoundButtonTypeOfVariantTest(BitVariant? variant)
    {
        var component = RenderComponent<BitCompoundButton>(parameters =>
        {
            if (variant.HasValue)
            {
                parameters.Add(p => p.Variant, variant.Value);
            }
        });

        var bitCompoundButton = component.Find(".bit-cmb");

        if (variant == BitVariant.Fill)
        {
            Assert.IsTrue(bitCompoundButton.ClassList.Contains("bit-cmb-fil"));
            Assert.IsFalse(bitCompoundButton.ClassList.Contains("bit-cmb-otl"));
        }

        if (variant == BitVariant.Outline)
        {
            Assert.IsFalse(bitCompoundButton.ClassList.Contains("bit-cmb-fil"));
            Assert.IsTrue(bitCompoundButton.ClassList.Contains("bit-cmb-otl"));
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
