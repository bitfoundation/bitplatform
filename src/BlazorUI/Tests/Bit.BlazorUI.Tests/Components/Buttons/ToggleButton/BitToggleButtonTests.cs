using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Buttons.ToggleButton;

[TestClass]
public class BitToggleButtonTests : BunitTestContext
{
    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
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
            Assert.IsTrue(bitButton?.GetAttribute("tabindex")?.Equals("-1"));
        }
    }

    [TestMethod, DataRow("Detailed description")]
    public void BitToggleButtonAriaDescriptionTest(string ariaDescription)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var bitButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitButton.HasAttribute("aria-describedby"));

        // aria-describedby takes an id reference, so the description itself is rendered into a hidden element
        var descriptionId = bitButton.GetAttribute("aria-describedby");
        var description = component.Find($"[id='{descriptionId}']");

        Assert.AreEqual(ariaDescription, description.TextContent);
    }

    [TestMethod, DataRow("Detailed label")]
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

    [TestMethod,
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

    [TestMethod,
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

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonShouldRenderAriaPressedWhenTheAccessibleNameIsStable(bool isChecked)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.Text, "Microphone");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(isChecked ? "true" : "false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonShouldNotRenderAriaPressedWhenTheAccessibleNameChanges()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.OnText, "Muted");
            parameters.Add(p => p.OffText, "Unmuted");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        // a name that changes with the state already conveys it, so announcing aria-pressed on top of it is ambiguous
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonShouldRenderAriaPressedWhenAStableAriaLabelIsProvided()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Mute");
            parameters.Add(p => p.OnText, "Muted");
            parameters.Add(p => p.OffText, "Unmuted");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow(BitToggleButtonAriaMode.Pressed),
        DataRow(BitToggleButtonAriaMode.Switch),
        DataRow(BitToggleButtonAriaMode.None)
    ]
    public void BitToggleButtonAriaModeTest(BitToggleButtonAriaMode ariaMode)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaMode, ariaMode);
            parameters.Add(p => p.IsChecked, true);
            parameters.Add(p => p.OnText, "Muted");
            parameters.Add(p => p.OffText, "Unmuted");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        switch (ariaMode)
        {
            case BitToggleButtonAriaMode.Pressed:
                Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-pressed"));
                Assert.IsFalse(bitToggleButton.HasAttribute("aria-checked"));
                Assert.IsFalse(bitToggleButton.HasAttribute("role"));
                break;

            case BitToggleButtonAriaMode.Switch:
                Assert.AreEqual("switch", bitToggleButton.GetAttribute("role"));
                Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-checked"));
                Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
                break;

            case BitToggleButtonAriaMode.None:
                Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
                Assert.IsFalse(bitToggleButton.HasAttribute("aria-checked"));
                break;
        }
    }

    [TestMethod,
        DataRow(true, true),
        DataRow(true, false),
        DataRow(false, true),
        DataRow(false, false)
    ]
    public void BitToggleButtonDisabledAttributesTest(bool isEnabled, bool allowDisabledFocus)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.AllowDisabledFocus, allowDisabledFocus);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(isEnabled is false, bitToggleButton.HasAttribute("aria-disabled"));

        // the native disabled attribute takes the button out of the accessibility tree, so it is only used when focus is not allowed
        Assert.AreEqual(isEnabled is false && allowDisabledFocus is false, bitToggleButton.HasAttribute("disabled"));
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(true, true),
        DataRow(false, false)
    ]
    public void BitToggleButtonLoadingTest(bool isLoading, bool reclickable)
    {
        var isChecked = false;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, isLoading);
            parameters.Add(p => p.Reclickable, reclickable);
            parameters.Bind(p => p.IsChecked, isChecked, v => isChecked = v);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(isLoading, bitToggleButton.ClassList.Contains("bit-tgb-lda"));
        Assert.AreEqual(isLoading, bitToggleButton.HasAttribute("aria-busy"));
        Assert.AreEqual(isLoading ? 1 : 0, component.FindAll(".bit-tgb-spn").Count);

        bitToggleButton.Click();

        Assert.AreEqual(isLoading is false || reclickable, isChecked);
    }

    [TestMethod]
    public void BitToggleButtonOnChangingShouldCancelTheChange()
    {
        var isChecked = false;
        var changeCount = 0;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.OnChanging, (BitToggleButtonChangeArgs args) => { args.Cancel = true; });
            parameters.Add(p => p.OnChange, _ => { changeCount++; });
            parameters.Bind(p => p.IsChecked, isChecked, v => isChecked = v);
        });

        component.Find(".bit-tgb").Click();

        Assert.IsFalse(isChecked);
        Assert.AreEqual(0, changeCount);
        Assert.IsFalse(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-chk"));
    }

    [TestMethod]
    public void BitToggleButtonOnChangingShouldAllowTheChange()
    {
        var isChecked = false;
        var changeCount = 0;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.OnChanging, (BitToggleButtonChangeArgs args) => { });
            parameters.Add(p => p.OnChange, _ => { changeCount++; });
            parameters.Bind(p => p.IsChecked, isChecked, v => isChecked = v);
        });

        component.Find(".bit-tgb").Click();

        Assert.IsTrue(isChecked);
        Assert.AreEqual(1, changeCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonCheckMarkTest(bool fixedCheckMark)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.ShowCheckMark, true);
            parameters.Add(p => p.FixedCheckMark, fixedCheckMark);
            parameters.Add(p => p.Text, "Bold");
        });

        // in the unchecked state the check mark only stays in the DOM to reserve its space
        Assert.AreEqual(fixedCheckMark ? 1 : 0, component.FindAll(".bit-tgb-chm").Count);

        if (fixedCheckMark)
        {
            Assert.IsTrue(component.Find(".bit-tgb-chm").ClassList.Contains("bit-tgb-chmh"));
        }

        component.Find(".bit-tgb").Click();

        var checkMark = component.Find(".bit-tgb-chm");

        Assert.IsFalse(checkMark.ClassList.Contains("bit-tgb-chmh"));
        Assert.IsTrue(checkMark.ClassList.Contains("bit-icon--Accept"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonShouldApplyThePerStateColorAndVariant(bool isChecked)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.OnColor, BitColor.Success);
            parameters.Add(p => p.OffColor, BitColor.Error);
            parameters.Add(p => p.OnVariant, BitVariant.Fill);
            parameters.Add(p => p.OffVariant, BitVariant.Outline);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains(isChecked ? "bit-tgb-suc" : "bit-tgb-err"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains(isChecked ? "bit-tgb-fil" : "bit-tgb-otl"));
    }

    [TestMethod]
    public void BitToggleButtonPerStateColorAndVariantShouldFallBackToTheGeneralOnes()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Color, BitColor.Info);
            parameters.Add(p => p.Variant, BitVariant.Text);
            parameters.Add(p => p.OnColor, BitColor.Warning);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-inf"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-txt"));

        bitToggleButton.Click();

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-wrn"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-txt"));
    }

    [TestMethod]
    public void BitToggleButtonShouldRenderThePerStateTemplate()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.OnTemplate, "<span class=\"on-template\">On</span>");
            parameters.Add(p => p.OffTemplate, "<span class=\"off-template\">Off</span>");
        });

        Assert.AreEqual(1, component.FindAll(".off-template").Count);
        Assert.AreEqual(0, component.FindAll(".on-template").Count);

        component.Find(".bit-tgb").Click();

        Assert.AreEqual(1, component.FindAll(".on-template").Count);
        Assert.AreEqual(0, component.FindAll(".off-template").Count);
    }

    [TestMethod]
    public async Task BitToggleButtonToggleAsyncShouldChangeTheCheckedState()
    {
        var isChecked = false;
        var changeCount = 0;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.OnChange, _ => { changeCount++; });
            parameters.Bind(p => p.IsChecked, isChecked, v => isChecked = v);
        });

        await component.InvokeAsync(() => component.Instance.ToggleAsync());

        Assert.IsTrue(isChecked);
        Assert.AreEqual(1, changeCount);
        Assert.IsTrue(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-chk"));
    }

    [TestMethod,
        DataRow(BitIconPosition.Start),
        DataRow(BitIconPosition.End)
    ]
    public void BitToggleButtonIconPositionTest(BitIconPosition iconPosition)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IconPosition, iconPosition);
            parameters.Add(p => p.IconName, "Microphone");
            parameters.Add(p => p.Text, "Microphone");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(iconPosition is BitIconPosition.End, bitToggleButton.ClassList.Contains("bit-tgb-eni"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonFullWidthTest(bool fullWidth)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        Assert.AreEqual(fullWidth, component.Find(".bit-tgb").ClassList.Contains("bit-tgb-flw"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonShouldSwitchTheAriaLabelPerState(bool isChecked)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.OnAriaLabel, "Unmute");
            parameters.Add(p => p.OffAriaLabel, "Mute");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(isChecked ? "Unmute" : "Mute", bitToggleButton.GetAttribute("aria-label"));
    }
}
