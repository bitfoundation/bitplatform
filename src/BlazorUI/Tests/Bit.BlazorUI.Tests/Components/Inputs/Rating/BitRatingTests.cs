using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

[TestClass]
public class BitRatingTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitRatingShouldTakeCorrectVisualAndEnabledStyle(bool isEnabled)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });
        var bitRating = component.Find(".bit-rtg");

        if (isEnabled)
        {
            Assert.IsFalse(bitRating.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitRating.ClassList.Contains("bit-dis"));
        }

        Assert.IsTrue(bitRating.HasAttribute("role"));
        Assert.AreEqual("radiogroup", bitRating.GetAttribute("role"));
        Assert.AreEqual(isEnabled ? null : "true", bitRating.GetAttribute("aria-disabled"));

        var button = component.Find(".bit-rtg-btn");

        Assert.AreEqual(isEnabled is false, button.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRatingShouldRespectReadOnly()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.AllowZeroStars, true);
        });

        var bitRating = component.Find(".bit-rtg");

        Assert.IsTrue(bitRating.ClassList.Contains("bit-rtg-rdl"));
        Assert.AreEqual("true", bitRating.GetAttribute("aria-readonly"));

        // A read-only rating is a picture of a value rather than a set of choices, so it is announced
        // as a single labelled image instead of a group of unreachable radios.
        Assert.AreEqual("img", bitRating.GetAttribute("role"));

        var buttons = component.FindAll(".bit-rtg-btn");

        foreach (var button in buttons)
        {
            Assert.IsTrue(button.HasAttribute("disabled"));
            Assert.AreEqual("-1", button.GetAttribute("tabindex"));
            Assert.AreEqual("true", button.GetAttribute("aria-hidden"));
            Assert.IsFalse(button.HasAttribute("role"));
        }
    }

    [TestMethod]
    public void BitRatingReadOnlyShouldDropTheAriaOfAnInteractiveGroup()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.IsEnabled, false);
        });

        var bitRating = component.Find(".bit-rtg");

        // Neither attribute is supported on the img role, where there is nothing to require or disable.
        Assert.IsFalse(bitRating.HasAttribute("aria-required"));
        Assert.IsFalse(bitRating.HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitRatingReadOnlyShouldFallBackToAValueAriaLabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 3.5);
        });

        var bitRating = component.Find(".bit-rtg");

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", 3.5, 5), bitRating.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingShouldRespectGetAriaLabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 4);
            parameters.Add(p => p.GetAriaLabel, (double value, double max) => $"rated {value} out of {max}");
        });

        Assert.AreEqual("rated 4 out of 5", component.Find(".bit-rtg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingAriaLabelShouldWinOverGetAriaLabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "explicit label");
            parameters.Add(p => p.GetAriaLabel, (double value, double max) => "callback label");
        });

        Assert.AreEqual("explicit label", component.Find(".bit-rtg").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(5),
        DataRow(8)
    ]
    public void BitRatingShouldShowCorrectMax(int max)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, max);
        });

        var button = component.FindAll(".bit-rtg-btn");

        Assert.AreEqual(max, button.Count);
    }

    [TestMethod,
        DataRow(0),
        DataRow(-3)
    ]
    public void BitRatingShouldRenderAtLeastOneItemForAnInvalidMax(int max)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, max);
        });

        Assert.AreEqual(1, component.FindAll(".bit-rtg-btn").Count);
    }

    [TestMethod,
        DataRow("Select {0} of {1} stars")]
    public void BitRatingShouldTakeCorrectAriaLabelFormat(string ariaLabelFormat)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AriaLabelFormat, ariaLabelFormat);
        });
        var span = component.Find(".bit-rtg-alb");
        Assert.AreEqual(string.Format(ariaLabelFormat, 1, 5), span.TextContent.Trim());
    }

    [TestMethod,
        DataRow(null),
        DataRow(BitSize.Small),
        DataRow(BitSize.Medium),
        DataRow(BitSize.Large)
    ]
    public void BitRatingShouldRespectSize(BitSize size)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });
        var bitRating = component.Find(".bit-rtg");

        var sizeClass = size switch
        {
            BitSize.Small => "bit-rtg-sm",
            BitSize.Large => "bit-rtg-lg",
            _ => "bit-rtg-md"
        };

        Assert.IsTrue(bitRating.ClassList.Contains(sizeClass));
    }

    [TestMethod,
        DataRow(null, "bit-rtg-pri"),
        DataRow(BitColor.Primary, "bit-rtg-pri"),
        DataRow(BitColor.Secondary, "bit-rtg-sec"),
        DataRow(BitColor.Success, "bit-rtg-suc"),
        DataRow(BitColor.Error, "bit-rtg-err"),
        DataRow(BitColor.TertiaryBorder, "bit-rtg-tbr")
    ]
    public void BitRatingShouldRespectColor(BitColor? color, string expectedClass)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Color, color);
        });

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains(expectedClass));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitRatingShouldRespectAllowZeroStars(bool allowZeroStars)
    {
        double value = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowZeroStars, allowZeroStars);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Without AllowZeroStars the value is pulled up into the range, which selects the first item.
        Assert.AreEqual(allowZeroStars ? 0 : 1, value);

        var firstButton = component.Find(".bit-rtg-btn");

        Assert.AreEqual(allowZeroStars ? "false" : "true", firstButton.GetAttribute("aria-checked"));
    }

    [TestMethod]
    public void BitRatingShouldClampAValueAboveTheMax()
    {
        double value = 9;
        RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        Assert.AreEqual(5, value);
    }

    [TestMethod]
    public void BitRatingShouldTakeCustomIcon()
    {
        var icon = "HeartFill";
        var unselectedIcon = "Heart";
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 2);
            parameters.Add(p => p.SelectedIconName, icon);
            parameters.Add(p => p.UnselectedIconName, unselectedIcon);
        });

        var ratingIcon1 = component.Find(".bit-rtg-btn:nth-child(1) i");
        var ratingIcon2 = component.Find(".bit-rtg-btn:nth-child(2) i");
        var ratingUnselectedIcon3 = component.Find(".bit-rtg-btn:nth-child(3) i");
        var ratingUnselectedIcon4 = component.Find(".bit-rtg-btn:nth-child(4) i");
        var ratingUnselectedIcon5 = component.Find(".bit-rtg-btn:nth-child(5) i");

        Assert.IsTrue(ratingIcon1.ClassList.Contains($"bit-icon--{icon}"));
        Assert.IsTrue(ratingIcon2.ClassList.Contains($"bit-icon--{icon}"));
        Assert.IsTrue(ratingUnselectedIcon3.ClassList.Contains($"bit-icon--{unselectedIcon}"));
        Assert.IsTrue(ratingUnselectedIcon4.ClassList.Contains($"bit-icon--{unselectedIcon}"));
        Assert.IsTrue(ratingUnselectedIcon5.ClassList.Contains($"bit-icon--{unselectedIcon}"));
    }

    [TestMethod]
    public void BitRatingShouldTakeExternalIcons()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.SelectedIcon, BitIconInfo.Bi("star-fill"));
            parameters.Add(p => p.UnselectedIcon, BitIconInfo.Fa("regular star"));
        });

        var selected = component.Find(".bit-rtg-btn:nth-child(1) .bit-rtg-ifl");
        var unselected = component.Find(".bit-rtg-btn:nth-child(2) .bit-rtg-iem");

        Assert.IsTrue(selected.ClassList.Contains("bi"));
        Assert.IsTrue(selected.ClassList.Contains("bi-star-fill"));
        Assert.IsTrue(unselected.ClassList.Contains("fa-regular"));
        Assert.IsTrue(unselected.ClassList.Contains("fa-star"));
    }

    [TestMethod,
        DataRow(10, 3, true, false, 3),
        DataRow(10, 2, false, false, 1),
        DataRow(10, 0, true, true, 1),
        DataRow(10, 4, false, true, 1),
        DataRow(10, 0, true, false, 1)
    ]
    public void BitRatingShouldRespectClickIndex(int max, int clickedIndex, bool isEnabled, bool readOnly, int expectedResult)
    {
        double value = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var bitRatingButtons = component.FindAll(".bit-rtg-btn");

        if (clickedIndex <= 0)
        {
            clickedIndex = 1;
        }

        bitRatingButtons[clickedIndex - 1].Click();

        Assert.AreEqual(max, bitRatingButtons.Count);
        Assert.AreEqual(expectedResult, value);
    }

    [TestMethod, DataRow("Detailed label")]
    public void BitRatingAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitRating = com.Find(".bit-rtg");

        Assert.IsTrue(bitRating.HasAttribute("aria-label"));
        Assert.AreEqual(ariaLabel, bitRating.GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(5, 2d),
        DataRow(5, 3d),
        DataRow(5, 1.25),
        DataRow(5, 2.5)
    ]
    public void BitRatingShouldRespectRatingValue(int max, double value)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, value);
            parameters.Add(p => p.Max, max);
        });

        var fills = component.FindAll(".bit-rtg-ifl");

        for (var index = 1; index <= max; index++)
        {
            var expected = System.Math.Clamp(value - (index - 1), 0, 1) * 100;

            StringAssert.Contains(fills[index - 1].GetAttribute("style"),
                                  FormattableString.Invariant($"width:{expected}%"));
        }

        var input = component.Find(".bit-input-hidden");

        Assert.AreEqual(value.ToString(CultureInfo.InvariantCulture), input.GetAttribute("value"));
    }

    [TestMethod,
        DataRow(5, 2d),
        DataRow(5, 1.25),
        DataRow(5, 0d),
        DataRow(5, 7d)
    ]
    public void BitRatingShouldRespectDefaultRatingValue(int max, double defaultValue)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Max, max);
        });

        // The DefaultValue starts the uncontrolled mode, and is still held to the range of the rating.
        var expected = System.Math.Clamp(defaultValue, 1, max);

        Assert.AreEqual(expected.ToString(CultureInfo.InvariantCulture), component.Find(".bit-input-hidden").GetAttribute("value"));
    }

    [TestMethod,
        DataRow(5, 0d, 2d, 1d),
        DataRow(5, 3d, 2d, 3d),
        DataRow(5, 2.25, 1.25, 2.25)
    ]
    public void BitRatingValueShouldWinOverDefaultValue(int max, double value, double defaultValue, double expected)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        // The DefaultValue only seeds the uncontrolled mode, so once a Value is supplied - even the
        // unrated 0, which is then pulled up into the range - the DefaultValue is out of the picture.
        Assert.AreEqual(expected.ToString(CultureInfo.InvariantCulture), component.Find(".bit-input-hidden").GetAttribute("value"));
    }

    [TestMethod]
    public void BitRatingShouldRenderNoSegmentAtTheDefaultPrecision()
    {
        var component = RenderComponent<BitRating>();

        Assert.AreEqual(0, component.FindAll(".bit-rtg-seg").Count);
    }

    [TestMethod,
        DataRow(0.5, 2),
        DataRow(0.25, 4),
        DataRow(0.1, 10),
        DataRow(1d, 1),
        DataRow(2d, 1),
        DataRow(0d, 1)
    ]
    public void BitRatingShouldSplitEachItemIntoPrecisionSteps(double precision, int expectedSteps)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 5);
            parameters.Add(p => p.Precision, precision);
        });

        var expectedSegments = expectedSteps == 1 ? 0 : 5 * expectedSteps;

        Assert.AreEqual(expectedSegments, component.FindAll(".bit-rtg-seg").Count);
    }

    [TestMethod]
    public void BitRatingShouldCommitAFractionalValueFromASegment()
    {
        double value = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The first segment of the third item is the leading half of it, which is 2.5.
        component.FindAll(".bit-rtg-seg")[4].Click();

        Assert.AreEqual(2.5, value);
    }

    [TestMethod]
    public void BitRatingShouldNotRenderSegmentsWhenNotInteractive()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.ReadOnly, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-rtg-seg").Count);
    }

    [TestMethod]
    public void BitRatingShouldRespectHighlightSelectedOnly()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3);
            parameters.Add(p => p.HighlightSelectedOnly, true);
        });

        var fills = component.FindAll(".bit-rtg-ifl");

        for (var index = 1; index <= 5; index++)
        {
            var expected = index == 3 ? "width:100%" : "width:0%";

            StringAssert.Contains(fills[index - 1].GetAttribute("style"), expected);
        }
    }

    [TestMethod]
    public void BitRatingShouldPreviewTheHoveredValue()
    {
        double value = 1;
        double? hovered = null;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnHoverChange, (double? v) => hovered = v);
        });

        component.FindAll(".bit-rtg-btn")[3].MouseOver();

        Assert.AreEqual(4d, hovered);

        // The preview only paints the items; the committed value stays where it was.
        Assert.AreEqual(1d, value);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:100%");

        component.Find(".bit-rtg").MouseLeave();

        Assert.IsNull(hovered);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:0%");
    }

    [TestMethod]
    public void BitRatingShouldRespectNoHoverPreview()
    {
        double? hovered = null;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.NoHoverPreview, true);
            parameters.Add(p => p.OnHoverChange, (double? v) => hovered = v);
        });

        component.FindAll(".bit-rtg-btn")[3].MouseOver();

        Assert.IsNull(hovered);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:0%");

        // The class turns off the CSS half of the preview, which shades the filled part on hover.
        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-rtg-nhp"));
    }

    [TestMethod]
    public void BitRatingShouldDropAStalePreviewWhenItStopsBeingInteractive()
    {
        double value = 1;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-rtg-btn")[3].MouseOver();

        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:100%");

        // Going read-only under the pointer means no mouseleave ever arrives to end the preview.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, true));

        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:0%");
    }

    [TestMethod]
    public void BitRatingShouldRespectAllowClear()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-rtg-btn")[2].Click();

        Assert.AreEqual(0d, value);
    }

    [TestMethod]
    public void BitRatingShouldNotClearWithoutAllowClear()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.FindAll(".bit-rtg-btn")[2].Click();

        Assert.AreEqual(3d, value);
    }

    [TestMethod,
        DataRow("ArrowRight", 4d),
        DataRow("ArrowUp", 4d),
        DataRow("ArrowLeft", 2d),
        DataRow("ArrowDown", 2d),
        DataRow("Home", 1d),
        DataRow("End", 5d)
    ]
    public void BitRatingShouldBeOperableFromTheKeyboard(string key, double expected)
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown(key);

        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldStepByThePrecision()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown("ArrowRight");

        Assert.AreEqual(3.5, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldReverseTheHorizontalArrowsInRtl()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown("ArrowRight");

        Assert.AreEqual(2d, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldClearWithAllowClear()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown("Delete");

        Assert.AreEqual(0d, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldDoNothingWhenReadOnly()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown("ArrowRight");

        Assert.AreEqual(3d, value);
    }

    [TestMethod]
    public void BitRatingShouldKeepASingleReachableTabStop()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowZeroStars, true);
            parameters.Add(p => p.DefaultValue, 0);
        });

        var buttons = component.FindAll(".bit-rtg-btn");

        // With no item selected the tab stop falls back to the first one, so the rating stays reachable.
        Assert.AreEqual(1, buttons.Count(b => b.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("0", buttons[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitRatingTabStopShouldFollowTheValue()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 4);
        });

        var buttons = component.FindAll(".bit-rtg-btn");

        Assert.AreEqual(1, buttons.Count(b => b.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("0", buttons[3].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitRatingShouldReportThePositionOfEachItem()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 4);
        });

        var buttons = component.FindAll(".bit-rtg-btn");

        for (var index = 1; index <= 4; index++)
        {
            Assert.AreEqual("radio", buttons[index - 1].GetAttribute("role"));
            Assert.AreEqual(index.ToString(), buttons[index - 1].GetAttribute("aria-posinset"));
            Assert.AreEqual("4", buttons[index - 1].GetAttribute("aria-setsize"));
        }
    }

    [TestMethod]
    public void BitRatingShouldRespectItemTitles()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ItemTitles, new List<string> { "Terrible", "Bad" });
        });

        var buttons = component.FindAll(".bit-rtg-btn");

        Assert.AreEqual("Terrible", buttons[0].GetAttribute("title"));
        Assert.AreEqual("Bad", buttons[1].GetAttribute("title"));

        // A list shorter than the rating simply leaves the remaining items without a tooltip.
        Assert.IsFalse(buttons[2].HasAttribute("title"));
    }

    [TestMethod]
    public void BitRatingShouldRespectItemTemplate()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 3);
            parameters.Add(p => p.DefaultValue, 2);
            parameters.Add(p => p.ItemTemplate, (BitRatingItemContext context) =>
                (builder) =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "custom-item");
                    builder.AddContent(2, $"{context.Index}:{context.Percentage}");
                    builder.CloseElement();
                });
        });

        var items = component.FindAll(".custom-item");

        Assert.AreEqual(3, items.Count);
        Assert.AreEqual("1:100", items[0].TextContent);
        Assert.AreEqual("2:100", items[1].TextContent);
        Assert.AreEqual("3:0", items[2].TextContent);

        // The template replaces the built-in icons rather than adding to them.
        Assert.AreEqual(0, component.FindAll(".bit-rtg-iem").Count);
    }

    [TestMethod]
    public void BitRatingShouldRespectOnChangingCancel()
    {
        double value = 1;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnChanging, (BitRatingChangeArgs args) => args.Cancel = true);
        });

        component.FindAll(".bit-rtg-btn")[3].Click();

        Assert.AreEqual(1d, value);
    }

    [TestMethod]
    public void BitRatingShouldReportBothValuesToOnChanging()
    {
        double value = 1;
        BitRatingChangeArgs? received = null;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnChanging, (BitRatingChangeArgs args) => received = args);
        });

        component.FindAll(".bit-rtg-btn")[3].Click();

        Assert.IsNotNull(received);
        Assert.AreEqual(4d, received!.Value);
        Assert.AreEqual(1d, received.OldValue);
        Assert.AreEqual(4d, value);
    }

    [TestMethod]
    public void BitRatingShouldRespectOnChange()
    {
        double changed = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.OnChange, (double v) => changed = v);
        });

        component.FindAll(".bit-rtg-btn")[2].Click();

        Assert.AreEqual(3d, changed);
    }

    [TestMethod]
    public void BitRatingShouldSetTheRangeOfTheHiddenInput()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 8);
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.AllowZeroStars, true);
            parameters.Add(p => p.Required, true);
        });

        var input = component.Find(".bit-input-hidden");

        Assert.AreEqual("0", input.GetAttribute("min"));
        Assert.AreEqual("8", input.GetAttribute("max"));

        // Below a whole item the stepping is the component's job, so the browser is told not to enforce one.
        Assert.AreEqual("any", input.GetAttribute("step"));
        Assert.IsTrue(input.HasAttribute("required"));
        Assert.AreEqual("-1", input.GetAttribute("tabindex"));
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-required"));
    }

    [TestMethod]
    public void BitRatingShouldAnnounceAFractionalValueThroughALiveRegion()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.DefaultValue, 3.5);
        });

        // The region is the last visually hidden span, after the per-item ones.
        var live = component.Find("[aria-live]");

        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", 3.5, 5), live.TextContent);
    }

    [TestMethod]
    public void BitRatingLiveRegionShouldStaySilentForAValueARadioCarries()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.DefaultValue, 3);
        });

        // The region is still rendered - one inserted at the moment of the change would be announced too
        // late - but a whole value is already carried by the radio it checks, so it stays empty.
        var live = component.Find("[aria-live]");

        Assert.AreEqual(string.Empty, live.TextContent);
    }

    [TestMethod]
    public void BitRatingShouldNotRenderALiveRegionWhenReadOnly()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 3.5);
        });

        Assert.AreEqual(0, component.FindAll("[aria-live]").Count);
    }

    [TestMethod]
    public void BitRatingShouldRenderCleanPercentagesForAnExactPrecision()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.1);
            parameters.Add(p => p.DefaultValue, 3.7);
        });

        // 3.7 - 3 lands on 0.7000000000000002 in binary floating point, which must not reach the markup.
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:70%");
    }

    [TestMethod]
    public void BitRatingShouldRespectClassesAndStyles()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Classes, new BitRatingClassStyles
            {
                Root = "custom-root",
                Button = "custom-button",
                IconContainer = "custom-container",
                SelectedIcon = "custom-selected",
                UnselectedIcon = "custom-unselected"
            });
            parameters.Add(p => p.Styles, new BitRatingClassStyles
            {
                Root = "color:red;",
                Button = "color:blue;",
                IconContainer = "color:green;",
                SelectedIcon = "color:purple;",
                UnselectedIcon = "color:orange;"
            });
        });

        var root = component.Find(".bit-rtg");
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red;");

        var button = component.Find(".bit-rtg-btn");
        Assert.IsTrue(button.ClassList.Contains("custom-button"));
        StringAssert.Contains(button.GetAttribute("style"), "color:blue;");

        var container = component.Find(".bit-rtg-ict");
        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        StringAssert.Contains(container.GetAttribute("style"), "color:green;");

        var selected = component.Find(".bit-rtg-ifl");
        Assert.IsTrue(selected.ClassList.Contains("custom-selected"));
        StringAssert.Contains(selected.GetAttribute("style"), "color:purple;");

        var unselected = component.Find(".bit-rtg-iem");
        Assert.IsTrue(unselected.ClassList.Contains("custom-unselected"));
        StringAssert.Contains(unselected.GetAttribute("style"), "color:orange;");
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, ""),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")
    ]
    public void BitRatingShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
        });

        var style = component.Find(".bit-rtg").GetAttribute("style") ?? string.Empty;

        if (expectedStyle.HasValue())
        {
            StringAssert.Contains(style, expectedStyle);
        }
        else
        {
            Assert.AreEqual(string.Empty, style);
        }
    }

    [TestMethod]
    public void BitRatingShouldRespectDir()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitRatingShouldNameEveryItemEvenWithoutAnAriaLabelFormat()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 3);
        });

        // A radio whose only content is a decorative glyph would otherwise reach a screen reader nameless.
        var labels = component.FindAll(".bit-rtg-btn .bit-rtg-alb");

        Assert.AreEqual(3, labels.Count);

        for (var index = 1; index <= 3; index++)
        {
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", index, 3), labels[index - 1].TextContent.Trim());
        }
    }

    [TestMethod]
    public void BitRatingItemNameShouldFallBackToTheItemTitle()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ItemTitles, new List<string> { "Terrible", "Bad" });
        });

        var labels = component.FindAll(".bit-rtg-btn .bit-rtg-alb");

        // A title says more about the item than its position does, so it names the item where there is one.
        Assert.AreEqual("Terrible", labels[0].TextContent.Trim());
        Assert.AreEqual("Bad", labels[1].TextContent.Trim());
        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", 3, 5), labels[2].TextContent.Trim());
    }

    [TestMethod]
    public void BitRatingAriaLabelFormatShouldWinOverTheItemTitle()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AriaLabelFormat, "Select {0} of {1} stars");
            parameters.Add(p => p.ItemTitles, new List<string> { "Terrible" });
        });

        Assert.AreEqual("Select 1 of 5 stars", component.Find(".bit-rtg-btn .bit-rtg-alb").TextContent.Trim());
    }

    [TestMethod]
    public void BitRatingShouldNotNameTheItemsOfAReadOnlyRating()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.AriaLabelFormat, "Select {0} of {1} stars");
        });

        // The items are aria-hidden behind the single label of the group, which leaves them nothing to name.
        Assert.AreEqual(0, component.FindAll(".bit-rtg-alb").Count);
    }

    [TestMethod]
    public void BitRatingShouldRespectValueTextFormat()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.DefaultValue, 3.5);
            parameters.Add(p => p.ValueTextFormat, "{0} out of {1} stars");
        });

        Assert.AreEqual("3.5 out of 5 stars", component.Find("[aria-live]").TextContent);

        // The same text is what a read-only rating falls back to when it is given no other label.
        var readOnly = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 3.5);
            parameters.Add(p => p.ValueTextFormat, "{0} out of {1} stars");
        });

        Assert.AreEqual("3.5 out of 5 stars", readOnly.Find(".bit-rtg").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow("4", 4d),
        DataRow("0", 1d),
        DataRow("9", 5d)
    ]
    public void BitRatingKeyboardShouldJumpToADigit(string key, double expected)
    {
        double value = 2;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // A digit beyond the ends of the scale is held to them, so "9 of 5" lands on the max and the
        // unreachable 0 lands on the min.
        component.Find(".bit-rtg").KeyDown(key);

        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldJumpToANonAsciiDigit()
    {
        double value = 2;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The shortcut has to work on the keyboard layouts a right-to-left rating is used with, where the
        // digit keys produce Eastern Arabic-Indic numerals rather than ASCII ones.
        component.Find(".bit-rtg").KeyDown("۴");

        Assert.AreEqual(4d, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldClearWithADigitZeroWhenAllowClear()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown("0");

        Assert.AreEqual(0d, value);
    }

    [TestMethod,
        DataRow("ArrowRight", true, 4d),
        DataRow("ArrowLeft", true, 3d),
        DataRow("PageUp", false, 4d),
        DataRow("PageDown", false, 3d)
    ]
    public void BitRatingKeyboardShouldStepByAWholeItem(string key, bool shift, double expected)
    {
        double value = 3.4;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.1);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // A coarse step moves to the next or previous whole item rather than adding one to a fraction,
        // which keeps a rating split into tenths five presses wide instead of fifty.
        component.Find(".bit-rtg").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldLeaveEscapeToTheContainer()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // A rating inside a modal or a panel would otherwise clear itself on the way to dismissing its
        // container, which is one press doing two things.
        component.Find(".bit-rtg").KeyDown("Escape");

        Assert.AreEqual(3d, value);
    }

    [TestMethod]
    public void BitRatingShouldRespectVertical()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.Precision, 0.5);
            parameters.Add(p => p.DefaultValue, 3.5);
        });

        var root = component.Find(".bit-rtg");

        Assert.IsTrue(root.ClassList.Contains("bit-rtg-vrt"));
        Assert.AreEqual("vertical", root.GetAttribute("aria-orientation"));

        // The fill of a vertical rating grows along the block axis, from the bottom edge of its item.
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "height:50%");

        // ... and so do the slices that commit the fractions of the item.
        var segment = component.FindAll(".bit-rtg-seg")[0].GetAttribute("style");

        StringAssert.Contains(segment, "height:50%");
        StringAssert.Contains(segment, "bottom:0%");
    }

    [TestMethod]
    public void BitRatingHorizontalShouldNotClaimAnOrientation()
    {
        // A radiogroup is horizontal by default, so only the vertical case has anything to announce.
        Assert.IsFalse(RenderComponent<BitRating>().Find(".bit-rtg").HasAttribute("aria-orientation"));
    }

    [TestMethod]
    public void BitRatingShouldRespectPerItemIcons()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 3);
            parameters.Add(p => p.DefaultValue, 1);
            parameters.Add(p => p.SelectedIconName, "HeartFill");
            parameters.Add(p => p.UnselectedIconName, "Heart");
            parameters.Add(p => p.GetSelectedIcon, (int index) => index == 1 ? BitIconInfo.Bit("DislikeSolid") : null);
            parameters.Add(p => p.GetUnselectedIcon, (int index) => index == 3 ? BitIconInfo.Bit("Like") : null);
        });

        var fills = component.FindAll(".bit-rtg-ifl");
        var empties = component.FindAll(".bit-rtg-iem");

        // The first item answers with its own glyph, the rest fall back to the shared pair.
        Assert.IsTrue(fills[0].ClassList.Contains("bit-icon--DislikeSolid"));
        Assert.IsTrue(empties[1].ClassList.Contains("bit-icon--Heart"));
        Assert.IsTrue(empties[2].ClassList.Contains("bit-icon--Like"));
    }

    [TestMethod]
    public void BitRatingHighlightSelectedOnlyShouldFillAFractionalItemPartially()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3.5);
            parameters.Add(p => p.HighlightSelectedOnly, true);
        });

        var fills = component.FindAll(".bit-rtg-ifl");

        // Only the item the value ends inside is filled, and only by as much of it as the value covers.
        StringAssert.Contains(fills[2].GetAttribute("style"), "width:0%");
        StringAssert.Contains(fills[3].GetAttribute("style"), "width:50%");
        StringAssert.Contains(fills[4].GetAttribute("style"), "width:0%");
    }

    [TestMethod]
    public void BitRatingShouldRespectAutoFocus()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AutoFocus, true);
            parameters.Add(p => p.DefaultValue, 3);
        });

        var buttons = component.FindAll(".bit-rtg-btn");

        // The attribute lands on the single tab stop, which is the item the value points at.
        Assert.AreEqual(1, buttons.Count(b => b.HasAttribute("autofocus")));
        Assert.IsTrue(buttons[2].HasAttribute("autofocus"));
    }

    [TestMethod]
    public void BitRatingShouldRespectName()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Name, "product-rate");
        });

        Assert.AreEqual("product-rate", component.Find(".bit-input-hidden").GetAttribute("name"));
    }

    [TestMethod]
    public void BitRatingValidationFormTest()
    {
        var component = RenderComponent<BitRatingValidationTest>();

        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(0, component.Instance.InvalidCount);

        var form = component.Find("form");
        form.Submit();

        // The form starts unrated, which the Range annotation rejects.
        Assert.AreEqual(0, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);

        component.FindAll(".bit-rtg-btn")[3].Click();
        form.Submit();

        Assert.AreEqual(1, component.Instance.ValidCount);
        Assert.AreEqual(1, component.Instance.InvalidCount);
        Assert.AreEqual(4d, component.Instance.TestModel.Value);
    }

    [TestMethod]
    public void BitRatingValidationShouldMarkTheInvalidValue()
    {
        var component = RenderComponent<BitRatingValidationTest>();

        Assert.IsFalse(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));

        component.Find("form").Submit();

        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find(".bit-rtg").GetAttribute("aria-invalid"));
        Assert.AreEqual("true", component.Find(".bit-input-hidden").GetAttribute("aria-invalid"));

        component.FindAll(".bit-rtg-btn")[3].Click();

        Assert.IsFalse(component.Find(".bit-rtg").ClassList.Contains("bit-inv"));
    }
}
