using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons;

[TestClass]
public class BitToggleButtonTests : BunitTestContext
{
    [DataTestMethod,
       DataRow(true, true, "Button label", "Volume0", "title"),
       DataRow(true, false, "Button label", "Volume1", "title"),
       DataRow(false, true, "Button label", "Volume2", "title"),
       DataRow(false, false, "Button label", "Volume3", "title")
    ]
    public void BitToggleButtonShouldHaveCorrectLabelAndIconAndTitle(bool isChecked, bool isEnabled, string text, string? iconName, string title)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.Text, text);
            parameters.Add(p => p.IconName, iconName);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Title, title);
        });

        var bitToggleButton = component.Find(".bit-tgb");
        var bitIconTag = component.Find(".bit-tgb > i");
        var bitLabelTag = component.Find(".bit-tgb > span");

        if (isEnabled)
        {
            Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-dis"));
        }

        Assert.AreEqual(bitLabelTag.TextContent, text);

        Assert.AreEqual(bitToggleButton.GetAttribute("title"), title);

        Assert.IsTrue(bitIconTag.ClassList.Contains($"bit-icon--{iconName}"));
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
}
