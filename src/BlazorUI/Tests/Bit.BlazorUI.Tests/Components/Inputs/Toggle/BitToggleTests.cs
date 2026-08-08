using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Toggle;

[TestClass]
public class BitToggleTests : BunitTestContext
{
    [TestMethod,
       DataRow(true, true),
       DataRow(true, false),
       DataRow(false, true),
       DataRow(false, false)
   ]
    public void BitToggleTest(bool isEnabled, bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Value, value);
        });

        var bitToggle = com.Find(".bit-tgl");
        var button = com.Find("button");

        Assert.AreEqual(isEnabled is false, bitToggle.ClassList.Contains("bit-dis"));
        Assert.AreEqual(value, bitToggle.ClassList.Contains("bit-tgl-chk"));

        // a disabled toggle is really disabled, so it leaves the tab order instead of only looking greyed out
        Assert.AreEqual(isEnabled is false, button.HasAttribute("disabled"));
        Assert.AreEqual(isEnabled ? null : "true", button.GetAttribute("aria-disabled"));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitToggleInlineLabelShouldHaveClassName(bool isInlineLabel)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Inline, isInlineLabel);
        });

        var bitToggle = com.Find(".bit-tgl");

        Assert.AreEqual(isInlineLabel, bitToggle.ClassList.Contains("bit-tgl-inl"));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitToggleReversedShouldHaveClassName(bool reversed)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        Assert.AreEqual(reversed, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-rvs"));
    }

    [TestMethod,
      DataRow(true),
      DataRow(false)
    ]
    public void BitToggleFullWidthShouldHaveClassName(bool fullWidth)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.FullWidth, fullWidth);
        });

        Assert.AreEqual(fullWidth, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-fwi"));
    }

    [TestMethod, DataRow("Detailed AriaLabel")]
    public void BitToggleAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitToggleButton = com.Find("button");
        Assert.AreEqual(ariaLabel, bitToggleButton.GetAttribute("aria-label"));

        // an explicit label wins outright, so nothing else is left pointing at the switch
        Assert.IsFalse(bitToggleButton.HasAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow(true, "on", "off", "This is the first defaultText", "This is the first label"),
        DataRow(false, "on", "off", "This is the second defaultText", "This is the second label"),
        DataRow(true, "on", "off", null, "This is the Third label"),
        DataRow(false, "on", "off", "This is the fourth defaultText", null),
        DataRow(false, "on", "off", null, null),
        DataRow(false, null, "off", "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, "on", null, "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, null, null, "This is the fourth defaultText", "This is the fourth label"),
        DataRow(false, null, null, null, null),
    ]
    public void BitToggleAriaLabelledbyTest(bool value, string onText, string offText, string defaultText, string label)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnText, onText);
            parameters.Add(p => p.OffText, offText);
            parameters.Add(p => p.Text, defaultText);
            parameters.Add(p => p.Label, label);
        });

        var button = com.Find("button");
        var labelId = button.Id!.Replace("button", "label");
        var stateTextId = button.Id!.Replace("button", "state-text");
        var effectiveOnText = onText ?? defaultText;
        var effectiveOffText = offText ?? defaultText;
        var stateText = value ? effectiveOnText : effectiveOffText;

        // The accessible name of a switch must not change with its state, so it comes from the label, and
        // falls back to the state text only when there is no label at all and that text says the same thing
        // in both states.
        var expectedLabelledBy = label.HasValue()
                               ? labelId
                               : (stateText.HasValue() && effectiveOnText == effectiveOffText) ? stateTextId : null;

        Assert.AreEqual(expectedLabelledBy, button.GetAttribute("aria-labelledby"));

        // whatever the name points at has to exist in the document, or the switch has no name at all
        if (expectedLabelledBy is not null)
        {
            Assert.IsNotNull(com.Find($"#{expectedLabelledBy}"));
        }

        // the state text that is not part of the name is announced as a description instead
        var expectedDescribedBy = stateText.HasValue() && expectedLabelledBy != stateTextId ? stateTextId : null;
        Assert.AreEqual(expectedDescribedBy, button.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleAriaLabelledbyShouldOutrankEveryOtherSourceOfTheName()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.AriaLabelledby, "an-external-heading");
            parameters.Add(p => p.AriaLabel, "An aria label");
            parameters.Add(p => p.Label, "A label");
            parameters.Add(p => p.Text, "A state text");
        });

        var button = com.Find("button");

        Assert.AreEqual("an-external-heading", button.GetAttribute("aria-labelledby"));

        // what the explicit id displaced is announced after the name instead of being dropped
        Assert.AreEqual(com.Find(".bit-tgl-stx").Id, button.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleAStateTextThatChangesShouldNotBecomeTheAccessibleName()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.OnText, "On");
            parameters.Add(p => p.OffText, "Off");
        });

        var button = com.Find("button");
        var stateTextId = com.Find(".bit-tgl-stx").Id;

        // naming the switch after a text that reads "On" one moment and "Off" the next would rename it on
        // every flip, so it is only ever described by it
        Assert.IsFalse(button.HasAttribute("aria-labelledby"));
        Assert.AreEqual(stateTextId, button.GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitToggleAStateTextThatStaysTheSameShouldBecomeTheAccessibleName()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Text, "Dark mode");
        });

        var button = com.Find("button");

        Assert.AreEqual(com.Find(".bit-tgl-stx").Id, button.GetAttribute("aria-labelledby"));
        Assert.IsFalse(button.HasAttribute("aria-describedby"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleAriaCheckedTest(bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
        });

        var ariaChecked = value ? "true" : "false";
        var bitToggleButton = com.Find("button");
        Assert.AreEqual(ariaChecked, bitToggleButton.GetAttribute("aria-checked"));
    }

    [TestMethod,
        DataRow(null),
        DataRow("Foo"),
        DataRow("Bar")
        ]
    public void BitToggleRoleTest(string role)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            if (role is not null)
            {
                parameters.Add(p => p.Role, role);
            }
        });

        var bitToggleButton = com.Find("button");
        Assert.AreEqual(role ?? "switch", bitToggleButton.GetAttribute("role"));
    }

    [TestMethod, DataRow("This is label")]
    public void BitToggleLabel(string label)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Label, label);
        });

        var bitToggleLabel = com.Find("label");
        Assert.AreEqual(label, bitToggleLabel.TextContent.Trim());

        // the label points at the button, which is what makes a click on it flip the toggle
        Assert.AreEqual(com.Find("button").Id, bitToggleLabel.GetAttribute("for"));
    }

    [TestMethod, DataRow("<div>This is labelTemplate</div>")]
    public void BitToggleMarkupLabelTest(string labelTemplate)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, labelTemplate);
        });

        var label = com.Find("label");
        label.ChildNodes.MarkupMatches(labelTemplate);

        // the templated label names the switch exactly like a plain one does
        Assert.AreEqual(label.Id, com.Find("button").GetAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow(true, "On!", "Off!", "Fallback"),
        DataRow(false, "On!", "Off!", "Fallback"),
        DataRow(true, null, "Off!", "Fallback"),
        DataRow(false, "On!", null, "Fallback"),
        DataRow(true, null, null, "Fallback"),
        DataRow(true, null, null, null)
    ]
    public void BitToggleStateTextTest(bool value, string onText, string offText, string text)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnText, onText);
            parameters.Add(p => p.OffText, offText);
            parameters.Add(p => p.Text, text);
        });

        var expected = (value ? onText : offText) ?? text;

        if (expected.HasValue())
        {
            Assert.AreEqual(expected, com.Find(".bit-tgl-stx").TextContent.Trim());
        }
        else
        {
            Assert.AreEqual(0, com.FindAll(".bit-tgl-stx").Count);
        }
    }

    [TestMethod]
    public void BitToggleStateTextShouldFollowTheChangedParameters()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.OffText, "Off");
        });

        Assert.AreEqual("Off", com.Find(".bit-tgl-stx").TextContent.Trim());

        com.Render(parameters =>
        {
            parameters.Add(p => p.OffText, "Disabled");
        });

        Assert.AreEqual("Disabled", com.Find(".bit-tgl-stx").TextContent.Trim());
    }

    [TestMethod]
    public void BitToggleClickShouldToggleTheValue()
    {
        var currentValue = false;
        var changedCount = 0;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, currentValue);
            parameters.Add(p => p.ValueChanged, v => currentValue = v);
            parameters.Add(p => p.OnChange, _ => changedCount++);
        });

        com.Find("button").Click();

        Assert.IsTrue(currentValue);
        Assert.AreEqual(1, changedCount);

        com.Render(parameters => parameters.Add(p => p.Value, currentValue));
        com.Find("button").Click();

        Assert.IsFalse(currentValue);
        Assert.AreEqual(2, changedCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleDefaultValueTest(bool defaultValue)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));

        // an uncontrolled toggle keeps its own state, so the click lands without any binding at all
        com.Find("button").Click();

        Assert.AreEqual(defaultValue is false, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod]
    public void BitToggleOneWayBoundValueShouldNotChangeOnClick()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, true);
        });

        com.Find("button").Click();

        Assert.IsTrue(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod]
    public void BitToggleDisabledShouldNotChangeOnClick()
    {
        var changed = false;
        var clicked = false;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnClick, () => clicked = true);
            parameters.Add(p => p.OnChange, _ => changed = true);
        });

        com.Find("button").Click();

        Assert.IsFalse(clicked);
        Assert.IsFalse(changed);
        Assert.IsFalse(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleReadOnlyTest(bool readOnly)
    {
        var changed = false;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChange, _ => changed = true);
        });

        var toggle = com.Find(".bit-tgl");
        var button = com.Find("button");

        Assert.AreEqual(readOnly, toggle.ClassList.Contains("bit-tgl-rdl"));
        Assert.AreEqual(readOnly ? "true" : null, button.GetAttribute("aria-readonly"));

        // a read-only toggle stays focusable, which is exactly what separates it from a disabled one
        Assert.IsFalse(button.HasAttribute("disabled"));

        button.Click();

        Assert.AreEqual(readOnly is false, changed);
        Assert.AreEqual(readOnly is false, toggle.ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleLoadingTest(bool loading)
    {
        var changed = false;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Loading, loading);
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChange, _ => changed = true);
        });

        var toggle = com.Find(".bit-tgl");
        var button = com.Find("button");

        Assert.AreEqual(loading, toggle.ClassList.Contains("bit-tgl-ldg"));
        Assert.AreEqual(loading ? "true" : null, button.GetAttribute("aria-busy"));
        Assert.AreEqual(loading ? 1 : 0, com.FindAll(".bit-tgl-spn").Count);

        // the enlarged geometry the spinner needs is the same one the knob icons get
        Assert.AreEqual(loading, toggle.ClassList.Contains("bit-tgl-tic"));

        button.Click();

        Assert.AreEqual(loading is false, changed);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleRequiredTest(bool required)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.Label, "A label");
        });

        Assert.AreEqual(required, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-req"));
        Assert.AreEqual(required ? "true" : null, com.Find("button").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitToggleRequiredWithoutALabelShouldNotGetTheAsteriskClass()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Required, true);
        });

        Assert.IsFalse(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-req"));
        Assert.AreEqual("true", com.Find("button").GetAttribute("aria-required"));
    }

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tgl-pri"),
        DataRow(BitColor.Secondary, "bit-tgl-sec"),
        DataRow(BitColor.Tertiary, "bit-tgl-ter"),
        DataRow(BitColor.Info, "bit-tgl-inf"),
        DataRow(BitColor.Success, "bit-tgl-suc"),
        DataRow(BitColor.Warning, "bit-tgl-wrn"),
        DataRow(BitColor.SevereWarning, "bit-tgl-swr"),
        DataRow(BitColor.Error, "bit-tgl-err"),
        DataRow(BitColor.PrimaryBackground, "bit-tgl-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-tgl-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-tgl-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-tgl-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-tgl-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-tgl-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-tgl-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-tgl-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-tgl-tbr"),
        DataRow(null, "bit-tgl-pri")
    ]
    public void BitToggleColorTest(BitColor? color, string expectedClass)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(com.Find(".bit-tgl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-tgl-sm"),
        DataRow(BitSize.Medium, "bit-tgl-md"),
        DataRow(BitSize.Large, "bit-tgl-lg"),
        DataRow(null, "bit-tgl-md")
    ]
    public void BitToggleSizeTest(BitSize? size, string expectedClass)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });

        Assert.IsTrue(com.Find(".bit-tgl").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(BitLabelPosition.Top, "bit-tgl-ltp"),
        DataRow(BitLabelPosition.Bottom, "bit-tgl-lbt"),
        DataRow(BitLabelPosition.Start, "bit-tgl-lst"),
        DataRow(BitLabelPosition.End, "bit-tgl-lnd"),
        DataRow(null, null)
    ]
    public void BitToggleLabelPositionTest(BitLabelPosition? labelPosition, string expectedClass)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.LabelPosition, labelPosition);
            parameters.Add(p => p.Label, "A label");
        });

        var toggle = com.Find(".bit-tgl");

        if (expectedClass is null)
        {
            Assert.IsFalse(toggle.ClassList.Contains("bit-tgl-ltp"));
            Assert.IsFalse(toggle.ClassList.Contains("bit-tgl-lbt"));
            Assert.IsFalse(toggle.ClassList.Contains("bit-tgl-lst"));
            Assert.IsFalse(toggle.ClassList.Contains("bit-tgl-lnd"));
        }
        else
        {
            Assert.IsTrue(toggle.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod,
        DataRow(true, "Accept", "Cancel"),
        DataRow(false, "Accept", "Cancel"),
        DataRow(true, "Accept", null),
        DataRow(false, "Accept", null),
        DataRow(false, null, null)
    ]
    public void BitToggleIconTest(bool value, string onIconName, string offIconName)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnIconName, onIconName);
            parameters.Add(p => p.OffIconName, offIconName);
        });

        var expectedIcon = value ? onIconName : offIconName;
        var icons = com.FindAll(".bit-tgl-ico");

        if (expectedIcon is null)
        {
            Assert.AreEqual(0, icons.Count);
        }
        else
        {
            Assert.AreEqual(1, icons.Count);
            Assert.IsTrue(icons[0].ClassList.Contains($"bit-icon--{expectedIcon}"));
        }

        // the roomier geometry is decided by the parameters, not by the state, so flipping never resizes it
        var hasAnyIcon = onIconName is not null || offIconName is not null;
        Assert.AreEqual(hasAnyIcon, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-tic"));
    }

    [TestMethod]
    public void BitToggleExternalIconTest()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, true);
            parameters.Add(p => p.OnIcon, BitIconInfo.Fa("solid check"));
            parameters.Add(p => p.OnIconName, "Accept");
        });

        var icon = com.Find(".bit-tgl-ico");

        // OnIcon takes precedence over OnIconName when both are set
        Assert.IsTrue(icon.ClassList.Contains("fa-solid"));
        Assert.IsTrue(icon.ClassList.Contains("fa-check"));
        Assert.IsFalse(icon.ClassList.Contains("bit-icon--Accept"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleTrackContentTest(bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.OnContent, "ON");
            parameters.Add(p => p.OffContent, "OFF");
        });

        var content = com.Find(".bit-tgl-cnn");

        Assert.AreEqual(value ? "ON" : "OFF", content.TextContent.Trim());

        // the state is already carried by aria-checked, so the track content stays out of the name
        Assert.AreEqual("true", content.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitToggleKnobShouldPrecedeTheTrackContent()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.OffContent, "OFF");
        });

        var children = com.Find(".bit-tgl-btn").Children;

        // The knob is positioned rather than laid out, so it has to come before the content it slides past for
        // the content to paint underneath it rather than over it.
        Assert.AreEqual(2, children.Length);
        Assert.IsTrue(children[0].ClassList.Contains("bit-tgl-sta"));
        Assert.IsTrue(children[1].ClassList.Contains("bit-tgl-cnn"));
    }

    [TestMethod]
    public void BitToggleTrackContentShouldOnlyRenderForTheStateItBelongsTo()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.OffContent, "OFF");
        });

        Assert.AreEqual("OFF", com.Find(".bit-tgl-cnn").TextContent.Trim());

        com.Render(parameters => parameters.Add(p => p.Value, true));

        Assert.AreEqual(0, com.FindAll(".bit-tgl-cnn").Count);
    }

    [TestMethod]
    public void BitToggleEventsShouldFireInOrder()
    {
        var log = string.Empty;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnClick, () => log += "click;");
            parameters.Add(p => p.OnChanging, (BitToggleChangeArgs args) => log += $"changing({args.Value});");
            parameters.Add(p => p.OnChange, (bool v) => log += $"change({v});");
        });

        com.Find("button").Click();

        Assert.AreEqual("click;changing(True);change(True);", log);
    }

    [TestMethod]
    public void BitToggleOnChangingShouldBeAbleToCancelTheChange()
    {
        var changed = false;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChanging, (BitToggleChangeArgs args) => args.Cancel = true);
            parameters.Add(p => p.OnChange, _ => changed = true);
        });

        com.Find("button").Click();

        Assert.IsFalse(changed);
        Assert.IsFalse(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod]
    public void BitToggleShouldIgnoreAClickWhileAChangeIsStillInFlight()
    {
        var gate = new TaskCompletionSource();
        var changingCount = 0;
        var changedCount = 0;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChanging, async (BitToggleChangeArgs _) =>
            {
                changingCount++;
                await gate.Task;
            });
            parameters.Add(p => p.OnChange, _ => changedCount++);
        });

        com.Find("button").Click();
        com.Find("button").Click();

        // the second click lands while the first change is still being decided, and is dropped rather than
        // queued behind it - two clicks on a switch that has only moved once would flip it back
        Assert.AreEqual(1, changingCount);
        Assert.AreEqual(0, changedCount);

        gate.SetResult();

        com.WaitForAssertion(() => Assert.AreEqual(1, changedCount));
        Assert.IsTrue(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));

        // the toggle opens back up once the change it was holding is done
        com.Find("button").Click();

        com.WaitForAssertion(() => Assert.AreEqual(2, changingCount));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitToggleToggleAsyncShouldFlipTheValue(bool value)
    {
        var changedCount = 0;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, value);
            parameters.Add(p => p.OnChange, _ => changedCount++);
        });

        await com.InvokeAsync(() => com.Instance.ToggleAsync());

        Assert.AreEqual(value is false, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
        Assert.AreEqual(1, changedCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitToggleToggleAsyncWithAStateShouldOnlyChangeWhatIsNotAlreadyThere(bool value)
    {
        var changedCount = 0;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChange, _ => changedCount++);
        });

        await com.InvokeAsync(() => com.Instance.ToggleAsync(value));
        await com.InvokeAsync(() => com.Instance.ToggleAsync(value));

        Assert.AreEqual(value, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));

        // moving to the state it is already in is not a change, so it is never reported as one
        Assert.AreEqual(value ? 1 : 0, changedCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public async Task BitToggleToggleAsyncShouldRunThroughOnChanging(bool cancel)
    {
        var seenValue = (bool?)null;

        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, false);
            parameters.Add(p => p.OnChanging, (BitToggleChangeArgs args) =>
            {
                seenValue = args.Value;
                args.Cancel = cancel;
            });
        });

        await com.InvokeAsync(() => com.Instance.ToggleAsync());

        // a change from code is still a change, so the veto that guards the toggle applies to it too
        Assert.IsTrue(seenValue);
        Assert.AreEqual(cancel is false, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod,
        DataRow(true, false, true),
        DataRow(false, true, true),
        DataRow(false, false, true)
    ]
    public async Task BitToggleToggleAsyncShouldOnlyBeStoppedByDisabling(bool readOnly, bool loading, bool expected)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.Loading, loading);
            parameters.Add(p => p.DefaultValue, false);
        });

        await com.InvokeAsync(() => com.Instance.ToggleAsync());

        // read-only and loading close the toggle to the user, not to the code behind it
        Assert.AreEqual(expected, com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod]
    public async Task BitToggleToggleAsyncOnADisabledToggleShouldDoNothing()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.DefaultValue, false);
        });

        await com.InvokeAsync(() => com.Instance.ToggleAsync());

        Assert.IsFalse(com.Find(".bit-tgl").ClassList.Contains("bit-tgl-chk"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleAutoFocusTest(bool autoFocus)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, autoFocus);
        });

        Assert.AreEqual(autoFocus, com.Find("button").HasAttribute("autofocus"));
    }

    [TestMethod, DataRow("The native tooltip")]
    public void BitToggleTitleTest(string title)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Title, title);
        });

        Assert.AreEqual(title, com.Find("button").GetAttribute("title"));
    }

    [TestMethod, DataRow("A detailed description")]
    public void BitToggleAriaDescriptionTest(string ariaDescription)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var describedById = com.Find("button").GetAttribute("aria-describedby");

        Assert.IsFalse(string.IsNullOrEmpty(describedById));
        Assert.AreEqual(ariaDescription, com.Find($"#{describedById}").TextContent.Trim());
    }

    [TestMethod, DataRow("A label", "A description")]
    public void BitToggleAriaDescriptionShouldFollowTheStateText(string label, string ariaDescription)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Label, label);
            parameters.Add(p => p.Text, "The state");
            parameters.Add(p => p.AriaDescription, ariaDescription);
        });

        var button = com.Find("button");
        var describedBy = button.GetAttribute("aria-describedby")!.Split(' ');

        Assert.AreEqual(2, describedBy.Length);
        Assert.AreEqual("The state", com.Find($"#{describedBy[0]}").TextContent.Trim());
        Assert.AreEqual(ariaDescription, com.Find($"#{describedBy[1]}").TextContent.Trim());
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleHiddenInputShouldCarryTheFormValue(bool value)
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Name, "the-toggle");
        });

        var input = com.Find("input[type='checkbox']");

        Assert.AreEqual("the-toggle", input.GetAttribute("name"));
        Assert.AreEqual(value ? "true" : "false", input.GetAttribute("value"));

        // an unchecked checkbox is not submitted at all, which is what makes the checked attribute the
        // difference between a toggle that posts its value and one that never does
        Assert.AreEqual(value, input.HasAttribute("checked"));
    }

    [TestMethod]
    public void BitToggleStylesAndClassesTest()
    {
        var com = RenderComponent<BitToggle>(parameters =>
        {
            parameters.Add(p => p.Value, true);
            parameters.Add(p => p.Label, "A label");
            parameters.Add(p => p.Text, "A state text");
            parameters.Add(p => p.OnIconName, "Accept");
            parameters.Add(p => p.OnContent, "ON");
            parameters.Add(p => p.Styles, new BitToggleClassStyles
            {
                Root = "color: red;",
                Label = "color: green;",
                Container = "color: blue;",
                Button = "color: yellow;",
                Checked = "color: purple;",
                Content = "color: orange;",
                Thumb = "color: pink;",
                Icon = "color: brown;",
                Text = "color: gray;"
            });
            parameters.Add(p => p.Classes, new BitToggleClassStyles
            {
                Root = "root-class",
                Label = "label-class",
                Container = "container-class",
                Button = "button-class",
                Checked = "checked-class",
                Content = "content-class",
                Thumb = "thumb-class",
                Icon = "icon-class",
                Text = "text-class"
            });
        });

        var root = com.Find(".bit-tgl");

        Assert.IsTrue(root.ClassList.Contains("root-class"));
        Assert.IsTrue(root.ClassList.Contains("checked-class"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: red;"));
        Assert.IsTrue(root.GetAttribute("style")!.Contains("color: purple;"));

        Assert.IsTrue(com.Find(".bit-tgl-lbl").ClassList.Contains("label-class"));
        Assert.IsTrue(com.Find(".bit-tgl-cnt").ClassList.Contains("container-class"));
        Assert.IsTrue(com.Find(".bit-tgl-btn").ClassList.Contains("button-class"));
        Assert.IsTrue(com.Find(".bit-tgl-cnn").ClassList.Contains("content-class"));
        Assert.IsTrue(com.Find(".bit-tgl-sta").ClassList.Contains("thumb-class"));
        Assert.IsTrue(com.Find(".bit-tgl-ico").ClassList.Contains("icon-class"));
        Assert.IsTrue(com.Find(".bit-tgl-stx").ClassList.Contains("text-class"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationFormTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value ? 0 : 1, com.Instance.ValidCount);
        Assert.AreEqual(value ? 1 : 0, com.Instance.InvalidCount);

        var button = com.Find("button");
        button.Click();
        form.Submit();

        Assert.AreEqual(1, com.Instance.ValidCount);
        Assert.AreEqual(1, com.Instance.InvalidCount);
        Assert.AreEqual(com.Instance.ValidCount, com.Instance.InvalidCount);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationInvalidHtmlAttributeTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var checkBox = com.Find("input[type='checkbox']");
        Assert.IsFalse(checkBox.HasAttribute("aria-invalid"));

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value, checkBox.HasAttribute("aria-invalid"));
        if (checkBox.HasAttribute("aria-invalid"))
        {
            Assert.AreEqual("true", checkBox.GetAttribute("aria-invalid"));
        }

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(value is false, checkBox.HasAttribute("aria-invalid"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationShouldReportInvalidOnTheSwitchItself(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var button = com.Find("button");
        Assert.IsFalse(button.HasAttribute("aria-invalid"));

        com.Find("form").Submit();

        // the hidden input is out of reach of assistive technologies, so the failure has to be reported
        // on the element that actually carries the switch role
        Assert.AreEqual(value ? "true" : null, com.Find("button").GetAttribute("aria-invalid"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitToggleValidationInvalidCssClassTest(bool value)
    {
        var com = RenderComponent<BitToggleValidationTest>(parameters =>
        {
            parameters.Add(p => p.TestModel, new BitToggleTestModel { Value = value });
            parameters.Add(p => p.IsEnabled, true);
        });

        var bitToggle = com.Find(".bit-tgl");

        Assert.IsFalse(bitToggle.ClassList.Contains("bit-inv"));

        var form = com.Find("form");
        form.Submit();

        Assert.AreEqual(value, bitToggle.ClassList.Contains("bit-inv"));

        var button = com.Find("button");
        button.Click();

        Assert.AreEqual(value is false, bitToggle.ClassList.Contains("bit-inv"));
    }
}
