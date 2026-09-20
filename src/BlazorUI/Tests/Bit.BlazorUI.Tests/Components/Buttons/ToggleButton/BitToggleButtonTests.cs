using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
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

        // The pointer is taken away from a loading toggle button whose clicks are being swallowed, and left on a
        // Reclickable one - which is the loading state that still takes them.
        Assert.AreEqual(isLoading && reclickable is false, bitToggleButton.ClassList.Contains("bit-tgb-lod"));

        bitToggleButton.Click();

        Assert.AreEqual(isLoading is false || reclickable, isChecked);
    }

    [TestMethod]
    public void BitToggleButtonShouldTakeThePointerAwayFromTheFirstClickOfALoadingDelay()
    {
        // The delay holds the spinner back, not the click guard, so the pointer affordance has to go with the
        // guard rather than with the spinner - a toggle button that already ignores clicks must not still
        // invite them.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingDelay, 5000);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-tgb-lda"));
        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-lod"));
    }

    [TestMethod]
    public async Task BitToggleButtonAutoLoadingShouldCoverTheOnClickCallbackToo()
    {
        // An async OnClick handler is part of what the click is waiting for, so the loading state has to already
        // be open while it runs - otherwise the guard leaves open exactly the window it exists to close, and the
        // toggle button shows neither a spinner nor a busy state for the whole of it.
        var clickCount = 0;
        var tcs = new TaskCompletionSource();

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.Text, "Microphone");
            parameters.Add(p => p.OnClick, (MouseEventArgs _) =>
            {
                clickCount++;
                return tcs.Task;
            });
        });

        var bitToggleButton = component.Find(".bit-tgb");

        var click = bitToggleButton.ClickAsync(new MouseEventArgs());

        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-lda"));
            Assert.AreEqual("true", component.Find(".bit-tgb").GetAttribute("aria-busy"));
            Assert.HasCount(1, component.FindAll(".bit-tgb-spn"));
        });

        // and the click guard is up with it, so the second click never reaches the handler
        _ = bitToggleButton.ClickAsync(new MouseEventArgs());
        Assert.AreEqual(1, clickCount);

        tcs.SetResult();
        await click;

        component.WaitForAssertion(() =>
        {
            Assert.IsFalse(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-lda"));
            Assert.IsEmpty(component.FindAll(".bit-tgb-spn"));
        });

        Assert.IsTrue(component.Instance.IsChecked);
    }

    [TestMethod]
    public async Task BitToggleButtonAutoLoadingShouldClearWhenTheChangeIsCancelled()
    {
        // A refused toggle is still a finished one: the loading state the click opened has to close with it, or
        // the toggle button is left spinning over a state that never moved and swallowing every further click.
        var tcs = new TaskCompletionSource();

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AutoLoading, true);
            parameters.Add(p => p.Text, "Microphone");
            parameters.Add(p => p.OnChanging, async (BitToggleButtonChangeArgs args) =>
            {
                await tcs.Task;
                args.Cancel = true;
            });
        });

        var click = component.Find(".bit-tgb").ClickAsync(new MouseEventArgs());

        component.WaitForAssertion(() => Assert.HasCount(1, component.FindAll(".bit-tgb-spn")));

        tcs.SetResult();
        await click;

        component.WaitForAssertion(() =>
        {
            Assert.IsEmpty(component.FindAll(".bit-tgb-spn"));
            Assert.IsFalse(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-lda"));
            Assert.IsFalse(component.Find(".bit-tgb").HasAttribute("aria-busy"));
        });

        Assert.IsFalse(component.Instance.IsChecked);
        Assert.IsFalse(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-chk"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitToggleButtonToggleAsyncShouldFollowTheClickGuardOfTheLoadingState(bool reclickable)
    {
        // The programmatic path is the click's path, so the guard that stops a loading toggle button from being
        // clicked again stops it from being toggled from code as well - unless Reclickable lifted it.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Text, "Microphone");
            parameters.Add(p => p.Reclickable, reclickable);
        });

        await component.Instance.ToggleAsync();

        Assert.AreEqual(reclickable, component.Instance.IsChecked);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonExpandedAriaModeShouldRenderAriaExpandedAlone(bool isChecked)
    {
        // The disclosure pattern: the checked state is another part of the page being shown, which a screen
        // reader announces as collapsed or expanded rather than as pressed. The two readings are mutually
        // exclusive, so the pressed state has to go with it, and the button keeps its implicit role.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaMode, BitToggleButtonAriaMode.Expanded);
            parameters.Add(p => p.AriaControls, "details-panel");
            parameters.Add(p => p.IsChecked, isChecked);
            parameters.Add(p => p.Text, "Details");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(isChecked ? "true" : "false", bitToggleButton.GetAttribute("aria-expanded"));
        Assert.AreEqual("details-panel", bitToggleButton.GetAttribute("aria-controls"));
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-checked"));
        Assert.IsFalse(bitToggleButton.HasAttribute("role"));
    }

    [TestMethod]
    public void BitToggleButtonShouldNotRenderAriaPressedWhenOnlyTheTitleChanges()
    {
        // With nothing else to name it, an icon-only toggle button is named by its title - so a title that
        // differs per state changes the accessible name as surely as a per-state text does.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Microphone");
            parameters.Add(p => p.OnTitle, "Click to unmute");
            parameters.Add(p => p.OffTitle, "Click to mute");
        });

        Assert.IsFalse(component.Find(".bit-tgb").HasAttribute("aria-pressed"));

        // and an aria-label pins the name down over the varying tooltip, which brings the state attribute back
        component.Render(parameters =>
        {
            parameters.Add(p => p.IconOnly, true);
            parameters.Add(p => p.IconName, "Microphone");
            parameters.Add(p => p.AriaLabel, "Mute");
            parameters.Add(p => p.OnTitle, "Click to unmute");
            parameters.Add(p => p.OffTitle, "Click to mute");
        });

        Assert.AreEqual("false", component.Find(".bit-tgb").GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonShouldKeepAriaPressedWhenTheTitleIsBehindAVisibleText()
    {
        // The title only names a toggle button that has no content of its own; a visible text outranks it in the
        // accessible name computation, so a varying tooltip behind a stable text changes nothing.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Text, "Microphone");
            parameters.Add(p => p.OnTitle, "Click to unmute");
            parameters.Add(p => p.OffTitle, "Click to mute");
        });

        Assert.AreEqual("false", component.Find(".bit-tgb").GetAttribute("aria-pressed"));
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

    [TestMethod]
    public void BitToggleButtonShouldKeepItsAccessibleNameWhileLoading()
    {
        // The content is faded out rather than hidden from assistive technologies: a name that disappears while the
        // toggle button is busy leaves a screen reader with nothing to announce it as.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Text, "Microphone");
        });

        var hiddenContent = component.Find(".bit-tgb-hcn");

        Assert.IsFalse(hiddenContent.HasAttribute("aria-hidden"));
        Assert.AreEqual("Microphone", hiddenContent.TextContent.Trim());

        // while the spinner and the loading label, which say nothing a reader needs, are the ones taken out
        Assert.AreEqual("true", component.Find(".bit-tgb-ldg").GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitToggleButtonShouldRenderALoadingTemplateAsItWasWritten()
    {
        // A template of the page's own replaces the spinner and the label rather than joining them, so it is neither
        // laid out by the loading container nor hidden from assistive technologies: what it carries may be the only
        // thing saying the toggle button is busy, and the live region below announces the LoadingLabel alone.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.Text, "Microphone");
            parameters.Add(p => p.LoadingTemplate, "<span class=\"custom-loading\">Saving...</span>");
        });

        Assert.AreEqual(0, component.FindAll(".bit-tgb-ldg").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tgb-spn").Count);

        var template = component.Find(".custom-loading");

        Assert.IsFalse(template.HasAttribute("aria-hidden"));
        Assert.AreEqual("Microphone", component.Find(".bit-tgb-hcn").TextContent.Trim());
    }

    [TestMethod]
    public void BitToggleButtonShouldAnnounceTheLoadingLabelThroughALiveRegion()
    {
        const string loadingLabel = "Saving...";

        // A live region only announces what changes inside one already in the document, so the region is rendered
        // from the first render - and left out altogether for a toggle button whose loading has nothing to announce.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.LoadingLabel, loadingLabel);
            parameters.Add(p => p.Text, "Microphone");
        });

        var status = component.Find(".bit-tgb-sts");

        Assert.AreEqual("status", status.GetAttribute("role"));
        Assert.AreEqual(string.Empty, status.TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, loadingLabel);
            parameters.Add(p => p.Text, "Microphone");
        });

        Assert.AreEqual(loadingLabel, component.Find(".bit-tgb-sts").TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, false);
            parameters.Add(p => p.LoadingLabel, null);
            parameters.Add(p => p.Text, "Microphone");
        });

        Assert.IsEmpty(component.FindAll(".bit-tgb-sts"));
    }

    [TestMethod]
    public void BitToggleButtonLoadingDelayShouldDeferTheSpinnerButGuardImmediately()
    {
        var isChecked = false;

        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingDelay, 200);
            parameters.Bind(p => p.IsChecked, isChecked, v => isChecked = v);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        // The click guard and the busy state apply immediately; only the spinner waits out the delay.
        Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-busy"));
        Assert.IsFalse(bitToggleButton.ClassList.Contains("bit-tgb-lda"));
        Assert.IsEmpty(component.FindAll(".bit-tgb-spn"));

        bitToggleButton.Click();
        Assert.IsFalse(isChecked);

        component.WaitForAssertion(() =>
        {
            Assert.HasCount(1, component.FindAll(".bit-tgb-spn"));
            Assert.IsTrue(component.Find(".bit-tgb").ClassList.Contains("bit-tgb-lda"));
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void BitToggleButtonLoadingDelayShouldNeverShowTheSpinnerForAFastLoad()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingDelay, 5000);
        });

        Assert.IsEmpty(component.FindAll(".bit-tgb-spn"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsLoading, false);
            parameters.Add(p => p.LoadingDelay, 5000);
        });

        Assert.IsEmpty(component.FindAll(".bit-tgb-spn"));
        Assert.IsFalse(component.Find(".bit-tgb").HasAttribute("aria-busy"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonNoWrapTest(bool noWrap)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.NoWrap, noWrap);
            parameters.Add(p => p.Text, "A rather long toggle button label");
        });

        Assert.AreEqual(noWrap, component.Find(".bit-tgb").ClassList.Contains("bit-tgb-nwr"));
    }

    [TestMethod]
    public void BitToggleButtonShouldLeaveTheTabOrderWhenItIsHiddenFromAssistiveTechnologies()
    {
        // A control a screen reader is told to ignore must not be reachable by Tab either, or a keyboard user
        // lands on something the reader has nothing to say about. The same holds for an aria-hidden written by
        // hand, which is what the splatted test below covers.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AriaHidden, true);
            parameters.Add(p => p.AutoFocus, true);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("-1", bitToggleButton.GetAttribute("tabindex"));
        Assert.IsFalse(bitToggleButton.HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitToggleButtonShouldNotAutoFocusADisabledButtonThatIsOutOfTheTabOrder()
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.AllowDisabledFocus, false);
        });

        Assert.IsFalse(component.Find(".bit-tgb").HasAttribute("autofocus"));
    }

    /// <summary>
    /// Renders a toggle button with the given attributes splatted onto it, the way real markup does.
    /// The builder rejects unmatched parameters on a component without [Parameter(CaptureUnmatchedValues)],
    /// so an attribute BitComponentBase captures has to arrive as a raw component attribute.
    /// </summary>
    private IRenderedComponent<BitToggleButton> RenderSplatted(Dictionary<string, object> attributes)
    {
        return Context.Render<BitToggleButton>(builder =>
        {
            builder.OpenComponent<BitToggleButton>(0);
            builder.AddMultipleAttributes(1, attributes);
            builder.CloseComponent();
        });
    }

    [TestMethod]
    public void BitToggleButtonShouldKeepTheAttributesWrittenByHandBesideItsOwn()
    {
        // A hyphenated aria-* name never reaches the parameter that mirrors it, so it arrives as a splatted
        // attribute - which the null of the component would silently remove rather than leave alone, taking the
        // name or the state of the control with it.
        var component = RenderSplatted(new()
        {
            ["AriaDescription"] = "Turns the microphone off.",
            ["aria-label"] = "Mute",
            ["aria-describedby"] = "external-help",
            ["aria-controls"] = "microphone-panel",
            ["role"] = "menuitemcheckbox"
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("Mute", bitToggleButton.GetAttribute("aria-label"));
        Assert.AreEqual("menuitemcheckbox", bitToggleButton.GetAttribute("role"));
        Assert.AreEqual("microphone-panel", bitToggleButton.GetAttribute("aria-controls"));

        // aria-describedby is a list of ids, so the one written by hand keeps its place beside the description
        Assert.AreEqual($"external-help {component.Find(".bit-tgb-dsc").Id}", bitToggleButton.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleButtonOwnParametersShouldWinOverTheAttributesWrittenByHand()
    {
        var component = RenderSplatted(new()
        {
            ["AriaLabel"] = "Mute",
            ["OnAriaLabel"] = "Unmute",
            ["IsChecked"] = true,
            ["aria-label"] = "Something else",
            ["aria-pressed"] = "false",
            ["role"] = "button"
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual("Unmute", bitToggleButton.GetAttribute("aria-label"));

        // the two a page is left in charge of: the role, which places the toggle button in a pattern of its own,
        // and a state attribute for the states where AriaMode has the component render none. Here the name changes
        // with the state, so the component writes no aria-pressed and the one written by hand is what is left.
        Assert.AreEqual("button", bitToggleButton.GetAttribute("role"));
        Assert.AreEqual("false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonAnAriaLabelWrittenByHandShouldKeepAriaPressed()
    {
        // The accessible name is what is rendered rather than what the parameters alone say: a hand-written
        // aria-label outranks the two texts and reads the same in both states, so the name does not change with
        // the state and the state attribute stays.
        var bitToggleButton = RenderSplatted(new()
        {
            ["OnText"] = "Muted",
            ["OffText"] = "Unmuted",
            ["aria-label"] = "Mute"
        }).Find(".bit-tgb");

        Assert.AreEqual("Mute", bitToggleButton.GetAttribute("aria-label"));
        Assert.AreEqual("false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonAnAriaLabelledByWrittenByHandShouldKeepAriaPressed()
    {
        // aria-labelledby wins the name computation outright, so wherever it points the name is pinned for both states
        var bitToggleButton = RenderSplatted(new()
        {
            ["OnText"] = "Muted",
            ["OffText"] = "Unmuted",
            ["aria-labelledby"] = "microphone-label"
        }).Find(".bit-tgb");

        Assert.AreEqual("microphone-label", bitToggleButton.GetAttribute("aria-labelledby"));
        Assert.AreEqual("false", bitToggleButton.GetAttribute("aria-pressed"));
    }

    [TestMethod]
    public void BitToggleButtonARoleWrittenByHandShouldTakeTheStateAttributeWithIt()
    {
        // aria-pressed is a state of the button role alone, so a page that named a pattern of its own writes the
        // state that pattern reads - the aria-checked of a menu item here - and the component writes none of its own.
        var bitToggleButton = RenderSplatted(new()
        {
            ["Text"] = "Status bar",
            ["role"] = "menuitemcheckbox",
            ["aria-checked"] = "true"
        }).Find(".bit-tgb");

        Assert.AreEqual("menuitemcheckbox", bitToggleButton.GetAttribute("role"));
        Assert.AreEqual("true", bitToggleButton.GetAttribute("aria-checked"));
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow("menuitemcheckbox", true),
        DataRow("menuitemradio", false),
        DataRow("option", true),
        DataRow("checkbox", false),
        DataRow("treeitem", true),
        DataRow("menuitem", false)
    ]
    public void BitToggleButtonAHandWrittenRoleThatReadsAriaCheckedShouldBeGivenTheState(string role, bool isChecked)
    {
        // Naming one of these roles describes the pattern, and the state it reads is aria-checked rather than the
        // aria-pressed of the button role - so the toggle button keeps it in step by itself, instead of leaving the
        // page with a second copy of IsChecked to maintain. A menuitem, which has no checked state, is given none.
        var bitToggleButton = RenderSplatted(new()
        {
            ["Text"] = "Status bar",
            ["IsChecked"] = isChecked,
            ["role"] = role
        }).Find(".bit-tgb");

        var expected = role is "menuitem" ? null : isChecked.ToString().ToLower();

        Assert.AreEqual(expected, bitToggleButton.GetAttribute("aria-checked"));
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow(BitToggleButtonAriaMode.None),
        DataRow(BitToggleButtonAriaMode.Expanded)
    ]
    public void BitToggleButtonAnExplicitAriaModeShouldWinOverTheRoleWrittenByHand(BitToggleButtonAriaMode ariaMode)
    {
        // Both of these named the state attribute themselves - none at all, and aria-expanded - so the state the
        // role would otherwise be given is not added beside what was asked for.
        var bitToggleButton = RenderSplatted(new()
        {
            ["Text"] = "Status bar",
            ["IsChecked"] = true,
            ["AriaMode"] = ariaMode,
            ["role"] = "menuitemcheckbox"
        }).Find(".bit-tgb");

        Assert.IsFalse(bitToggleButton.HasAttribute("aria-checked"));
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-pressed"));
        Assert.AreEqual(ariaMode is BitToggleButtonAriaMode.Expanded ? "true" : null, bitToggleButton.GetAttribute("aria-expanded"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonFixedColorTest(bool fixedColor)
    {
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Text, "Mute");
            parameters.Add(p => p.FixedColor, fixedColor);
            parameters.Add(p => p.Variant, BitVariant.Outline);
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.AreEqual(fixedColor, bitToggleButton.ClassList.Contains("bit-tgb-fxc"));
    }

    [TestMethod]
    public void BitToggleButtonSplattedAriaHiddenShouldRemoveItFromTheTabOrder()
    {
        var bitToggleButton = RenderSplatted(new()
        {
            ["AutoFocus"] = true,
            ["aria-hidden"] = "true"
        }).Find(".bit-tgb");

        Assert.AreEqual("-1", bitToggleButton.GetAttribute("tabindex"));
        Assert.IsFalse(bitToggleButton.HasAttribute("autofocus"));
    }

    [TestMethod,
        DataRow(true, "true"),
        DataRow("true", "true"),
        DataRow("TRUE", "true"),
        DataRow("false", "false"),
        DataRow(false, (string?)null)
    ]
    public void BitToggleButtonShouldKeepASplattedAriaHiddenWhateverItWasWrittenAs(object ariaHidden, string? expected)
    {
        // The component writes this attribute itself, so the value the page wrote has to be resolved against it like
        // every other one: a null written over a splatted attribute removes it rather than leaving it alone. Neither
        // the name nor the value is read as text alone - the renderer writes a splatted true with no value at all,
        // and the value is case insensitive - so a hidden button is hidden however it was said.
        var bitToggleButton = RenderSplatted(new()
        {
            ["AutoFocus"] = true,
            ["aria-hidden"] = ariaHidden
        }).Find(".bit-tgb");

        Assert.AreEqual(expected, bitToggleButton.GetAttribute("aria-hidden"));

        // and what it means is what the tab order and the autofocus are decided by
        var isHidden = expected is "true";
        Assert.AreEqual(isHidden ? "-1" : null, bitToggleButton.GetAttribute("tabindex"));
        Assert.AreEqual(isHidden is false, bitToggleButton.HasAttribute("autofocus"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleButtonIconOnlyShouldTakeTheNoTextClassOfItsSizeClass(bool iconOnly)
    {
        // The size class carries the control height of its size as a floor, which is what lines a toggle button up
        // with the other controls beside it and keeps the smallest one above the 24px pointer target of WCAG 2.2.
        // An icon-only one takes that same height as a minimum width through the no-text class, so it stays square.
        var component = RenderComponent<BitToggleButton>(parameters =>
        {
            parameters.Add(p => p.Size, BitSize.Small);
            parameters.Add(p => p.IconOnly, iconOnly);
            parameters.Add(p => p.IconName, "Microphone");
            parameters.Add(p => p.Text, "Microphone");
        });

        var bitToggleButton = component.Find(".bit-tgb");

        Assert.IsTrue(bitToggleButton.ClassList.Contains("bit-tgb-sm"));
        Assert.AreEqual(iconOnly, bitToggleButton.ClassList.Contains("bit-tgb-ntx"));
    }
}
