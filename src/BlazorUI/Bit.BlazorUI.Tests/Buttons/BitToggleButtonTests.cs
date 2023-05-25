using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitToggleButtonTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(true, true, "Button label", BitIconName.Volume0, "title"),
       DataRow(true, false, "Button label", BitIconName.Volume1, "title"),
       DataRow(false, true, "Button label", BitIconName.Volume2, "title"),
       DataRow(false, false, "Button label", BitIconName.Volume3, "title")
    ]
    public void BitToggleButtonShouldHaveCorrectLabelAndIconAndTitle(bool isChecked, bool isEnabled, string label, BitIconName? iconName, string title)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.Label, label);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Title, title);
        });

        var bitToggleButton = component.Find(".bit-tgb");
        var bitIconTag = component.Find(".bit-tgb > span > i");
        var bitLabelTag = component.Find(".bit-tgb > span > span");

        if (isEnabled)
        {
            Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(bitLabelTag.TextContent, label);

        Assert.AreEqual(bitToggleButton.GetAttribute("title"), title);

        Assert.IsTrue(bitIconTag.ClassList.Contains($"bit-icon--{iconName.GetName()}"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitToggleButtonClickEvent(bool isEnabled)
    {
        var clicked = false;
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clicked = true);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        bitToggleButton.Click();

        Assert.AreEqual(isEnabled, clicked);
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitToggleButtonShouldChangeIsCheckedParameterAfterClickWhenIsEnable(bool isEnabled, bool isChecked)
    {
        bool isCheckedBindingValue = isChecked;
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Bind(p => p.IsChecked, isCheckedBindingValue, newValue => isCheckedBindingValue = newValue);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        bitToggleButton.Click();

        Assert.AreEqual(isEnabled ? !isChecked : isChecked, component.Instance.IsChecked);
        Assert.AreEqual(isEnabled ? !isChecked : isChecked, isCheckedBindingValue);
    }

    [DataTestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitToggleButtonShouldAddRemoveCheckedClassAfterClickWhenIsEnable(bool isEnabled, bool isChecked)
    {
        bool isCheckedBindingValue = isChecked;
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Bind(p => p.IsChecked, isCheckedBindingValue, newValue => isCheckedBindingValue = newValue);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        bitToggleButton.Click();

        Assert.AreEqual(isEnabled ? !isChecked : isChecked, bitToggleButton.ClassList.Contains("bit-tgb-chk"));
        Assert.AreEqual(isEnabled ? !isChecked : isChecked, isCheckedBindingValue);
    }

    [DataTestMethod,
      DataRow(true, false),
      DataRow(true, true),
      DataRow(false, false),
      DataRow(false, true),
    ]
    public void BitToggleButtonDisabledFocusTest(bool isEnabled, bool allowDisabledFocus)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitButton = component.Find(".bit-tgb");
        var hasTabindexAttr = bitButton.HasAttribute("tabindex");

        Assert.AreEqual(!isEnabled && !allowDisabledFocus, hasTabindexAttr);

        if (hasTabindexAttr)
        {
            Assert.IsTrue(bitButton.GetAttribute("tabindex").Equals("-1"));
        }
    }

    [DataTestMethod, DataRow("Detailed description")]
    public void BitToggleButtonAriaDescriptionTest(string ariaDescription)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));

        Assert.AreEqual(bitButton.GetAttribute("aria-describedby"), ariaDescription);
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitToggleButtonAriaLabelTest(string ariaLabel)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitButton.HasAttribute("aria-label"));

        Assert.AreEqual(bitButton.GetAttribute("aria-label"), ariaLabel);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
        DataRow(null)
    ]
    public void BitToggleButtonAriaHiddenTest(bool ariaHidden)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, ariaHidden);
        });

        var bitButton = component.Find(".bit-tgb");

        Assert.AreEqual(ariaHidden, bitButton.HasAttribute("aria-hidden"));
    }

    [DataTestMethod,
        DataRow("", true),
        DataRow("bing.com", true),
        DataRow("bing.com", false)
    ]
    public void BitToggleButtonShouldRenderExpectedElementBasedOnHref(string href, bool isEnabled)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Href, href);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitToggleButton = component.Find(".bit-tgb");
        var tagName = bitToggleButton.TagName;
        var expectedElement = href.HasValue() && isEnabled ? "a" : "button";

        Assert.AreEqual(expectedElement, tagName, ignoreCase: true);
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
        DataRow(null)
    ]
    public void BitToggleButtonDefaultIsCheckedTest(bool? defaultIsChecked)
    {
        bool isCheckedAfterOnChange = false;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.DefaultIsChecked, defaultIsChecked);
            parameters.Add(p => p.OnChange, (e) => isCheckedAfterOnChange = e);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        bitToggleButton.Click();

        if (defaultIsChecked is null)
        {
            Assert.IsTrue(isCheckedAfterOnChange);
        }
        else
        {
            Assert.AreNotEqual(defaultIsChecked, isCheckedAfterOnChange);
        }
    }

    [DataTestMethod,
        DataRow(BitButtonSize.Small),
        DataRow(BitButtonSize.Medium),
        DataRow(BitButtonSize.Large),
        DataRow(null)
    ]
    public void BitToggleButtonSizeTest(BitButtonSize? size)
    {
        var com = RenderComponent<BitToggleButton>(parameters =>
        {
            if (size.HasValue)
            {
                parameters.Add(p => p.ButtonSize, size.Value);
            }
        });

        var sizeClass = size switch
        {
            BitButtonSize.Small => "bit-tgb-sm",
            BitButtonSize.Medium => "bit-tgb-md",
            BitButtonSize.Large => "bit-tgb-lg",
            _ => "bit-tgb-md",
        };

        var bitToggleButton = com.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains(sizeClass));
    }
}
