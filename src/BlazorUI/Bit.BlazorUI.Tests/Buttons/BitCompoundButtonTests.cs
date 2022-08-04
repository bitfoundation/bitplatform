using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitCompoundButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true, BitButtonStyle.Primary),
        DataRow(Visual.Fluent, true, BitButtonStyle.Standard),
        DataRow(Visual.Fluent, false, BitButtonStyle.Primary),
        DataRow(Visual.Fluent, false, BitButtonStyle.Standard),

        DataRow(Visual.Cupertino, true, BitButtonStyle.Primary),
        DataRow(Visual.Cupertino, true, BitButtonStyle.Standard),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Primary),
        DataRow(Visual.Cupertino, false, BitButtonStyle.Standard),

        DataRow(Visual.Material, true, BitButtonStyle.Primary),
        DataRow(Visual.Material, true, BitButtonStyle.Standard),
        DataRow(Visual.Material, false, BitButtonStyle.Primary),
        DataRow(Visual.Material, false, BitButtonStyle.Standard)
    ]
    public void BitCompoundButton(Visual visual, bool isEnabled, BitButtonStyle style)
    {
        var clicked = false;
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitButton = com.Find(".bit-cmp-btn");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        Assert.IsTrue(bitButton.ClassList.Contains($"bit-cmp-btn-{isEnabledClass}-{visualClass}"));

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

        var bitButton = com.Find(".bit-cmp-btn");

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

        var bitCompoundButton = com.Find(".bit-cmp-btn");

        Assert.IsTrue(bitCompoundButton.GetAttribute("aria-describedby").Contains(ariaDescription));
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitCompoundButtonAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitCompoundButton = com.Find(".bit-cmp-btn");

        Assert.IsTrue(bitCompoundButton.GetAttribute("aria-label").Contains(ariaLabel));
    }

    [DataTestMethod, DataRow(true, true), DataRow(false, false), DataRow(null, false)]
    public void BitCompoundButtonAriaHiddenTest(bool ariaHidden, bool expectedResult)
    {
        var com = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitCompoundButton = com.Find(".bit-cmp-btn");

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

        var bitCompoundButton = component.Find(".bit-cmp-btn");
        var tagName = bitCompoundButton.TagName;
        var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, BitButtonStyle.Primary, false),
        DataRow(Visual.Fluent, BitButtonStyle.Standard, false),
        DataRow(Visual.Cupertino, BitButtonStyle.Primary, false),
        DataRow(Visual.Cupertino, BitButtonStyle.Standard, false),
        DataRow(Visual.Material, BitButtonStyle.Primary, false),
        DataRow(Visual.Material, BitButtonStyle.Standard, false)
    ]
    public void BitCompoundButtonShouldHaveCorrectDisabledClassBasedOnButtonStyle(Visual visual, BitButtonStyle buttonStyle, bool isEnabled)
    {
        var component = RenderComponent<BitCompoundButton>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.ButtonStyle, buttonStyle);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitCompoundButton = component.Find(".bit-cmp-btn");

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var buttonStyleStr = buttonStyle == BitButtonStyle.Primary ? "primary" : "standard";
        Assert.IsTrue(bitCompoundButton.ClassList.Contains($"bit-cmp-btn-{buttonStyleStr}-disabled-{visualClass}"));
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

        var bitCompoundButton = component.Find(".bit-cmp-btn");

        var buttonTypeName = buttonType == BitButtonType.Button ? "button" : buttonType == BitButtonType.Submit ? "submit" : "reset";
        Assert.AreEqual(bitCompoundButton.GetAttribute("type"), buttonTypeName);
    }
}