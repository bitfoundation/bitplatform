using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
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

        // None of these is supported on the img role, where there is nothing to require, disable or correct.
        Assert.IsFalse(bitRating.HasAttribute("aria-readonly"));
        Assert.IsFalse(bitRating.HasAttribute("aria-required"));
        Assert.IsFalse(bitRating.HasAttribute("aria-disabled"));
        Assert.IsFalse(bitRating.HasAttribute("aria-invalid"));
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

    [TestMethod,
     DataRow(1d, 1d),
     DataRow(0.5d, 0.5d),
     DataRow(0.25d, 0.25d),
     DataRow(0.1d, 0.1d)]
    public void BitRatingFloorShouldBeASingleStep(double precision, double expected)
    {
        // Without AllowZeroStars or AllowClear the smallest rating that can be given is one step, so a
        // half-star scale reaches 0.5 instead of being held at a whole item.
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, precision);
        });

        Assert.AreEqual(expected, component.Instance.Value);
        Assert.AreEqual(expected.ToString(CultureInfo.InvariantCulture), component.Find("input").GetAttribute("min"));
    }

    [TestMethod]
    public void BitRatingShouldCommitTheFirstFractionOfAFractionalScale()
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The first slice of the first item is half a star, which the floor of a whole item would have
        // pulled back up to 1.
        component.FindAll(".bit-rtg-seg")[0].Click();

        Assert.AreEqual(0.5d, value);
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

        // Only the preview the component paints is off: the hovered value is still reported, which is
        // what a page drawing a preview of its own needs.
        Assert.AreEqual(4d, hovered);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[3].GetAttribute("style"), "width:0%");

        // The class turns off the CSS half of the preview, which shades the filled part on hover.
        Assert.IsTrue(component.Find(".bit-rtg").ClassList.Contains("bit-rtg-nhp"));

        component.Find(".bit-rtg").MouseLeave();

        Assert.IsNull(hovered);
    }

    [TestMethod]
    public void BitRatingNoHoverPreviewShouldKeepPaintingTheCommittedValue()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 2);
            parameters.Add(p => p.NoHoverPreview, true);
            parameters.Add(p => p.OnHoverChange, (double? v) => { });
        });

        component.FindAll(".bit-rtg-btn")[4].MouseOver();

        var fills = component.FindAll(".bit-rtg-ifl");
        StringAssert.Contains(fills[1].GetAttribute("style"), "width:100%");
        StringAssert.Contains(fills[2].GetAttribute("style"), "width:0%");
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
    public void BitRatingShouldEndThePreviewWhenAValueIsCommitted()
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

        component.FindAll(".bit-rtg-btn")[3].Click();

        // The preview has served its purpose once the value lands: the committed value is rendered and
        // OnHoverChange reports the end of the preview without waiting for the pointer to leave.
        Assert.AreEqual(4d, value);
        Assert.IsNull(hovered);
    }

    [TestMethod]
    public void BitRatingSegmentShouldCommitACleanTenth()
    {
        double value = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.1);
            parameters.Add(p => p.AllowZeroStars, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The seventh segment of the first item is 0.7, which naive floating point arithmetic would
        // produce as 0.7000000000000001. (AllowZeroStars opens up the values below 1, which would
        // otherwise be clamped up to the minimum of 1.)
        component.FindAll(".bit-rtg-seg")[6].Click();

        Assert.AreEqual(0.7, value);
        Assert.AreEqual("0.7", component.Find(".bit-input-hidden").GetAttribute("value"));
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

    [TestMethod,
     DataRow("ArrowUp", 4.5d),
     DataRow("ArrowDown", 4d)]
    public void BitRatingKeyboardShouldMoveAnOffGridValueOntoTheGrid(string key, double expected)
    {
        // A value the Precision never snapped - one bound from elsewhere - has to move onto the grid the
        // Precision lays over the scale rather than carry its own remainder up and down it.
        double value = 4.3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    public void BitRatingKeyboardShouldWalkAFractionalScaleOntoWholeItems()
    {
        double value = 1;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        var root = component.Find(".bit-rtg");

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(1.5d, value);

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.AreEqual(2d, value);

        root.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.AreEqual(1.5d, value);
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
    public void BitRatingShouldMarkTheCurrentItemApartFromTheCheckedOne()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3.5);
        });

        var items = component.FindAll(".bit-rtg-btn");

        // A radio cannot be half checked, so a fractional value checks none of them - but the item the
        // value lands in is still the one being picked, which is what the styling hook marks.
        Assert.IsTrue(items.All(i => i.GetAttribute("aria-checked") == "false"));
        Assert.AreEqual("true", items[3].GetAttribute("data-is-current"));
        Assert.AreEqual(1, items.Count(i => i.GetAttribute("data-is-current") == "true"));
    }

    [TestMethod]
    public void BitRatingCurrentItemShouldFollowTheHoverPreview()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 2);
        });

        component.FindAll(".bit-rtg-btn")[4].MouseOver();

        var items = component.FindAll(".bit-rtg-btn");

        Assert.AreEqual("true", items[4].GetAttribute("data-is-current"));
        // The committed value is what the radio reports, whatever the preview is showing.
        Assert.AreEqual("true", items[1].GetAttribute("aria-checked"));
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
    public void BitRatingItemContextShouldReportTheCurrentItem()
    {
        var contexts = new List<BitRatingItemContext>();
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3.5);
            parameters.Add(p => p.ItemTemplate, (BitRatingItemContext context) =>
                (builder) =>
                {
                    contexts.Add(context);
                    builder.AddContent(0, context.Index);
                });
        });

        var current = contexts.Where(c => c.IsCurrent).ToList();

        Assert.AreEqual(1, current.Count);
        Assert.AreEqual(4, current[0].Index);
        // The run behind it is filled, but only the fourth is the one the value lands in.
        Assert.AreEqual(3, contexts.Count(c => c.IsFull));
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
    public void BitRatingShouldEndThePreviewWhenAChangeIsRefused()
    {
        double value = 2;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnChanging, (BitRatingChangeArgs args) => args.Cancel = true);
        });

        component.FindAll(".bit-rtg-btn")[4].MouseOver();
        component.FindAll(".bit-rtg-btn")[4].Click();

        // A refused value must not go on being previewed: on a touch device no mouseleave ever arrives
        // to end the preview, so the rating would keep showing the value it just refused.
        Assert.AreEqual(2d, value);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[4].GetAttribute("style"), "width:0%");
    }

    [TestMethod]
    public void BitRatingShouldEndThePreviewWhenAOneWayBindingRefusesTheValue()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Value, 2d);
        });

        component.FindAll(".bit-rtg-btn")[4].MouseOver();
        component.FindAll(".bit-rtg-btn")[4].Click();

        Assert.AreEqual(2d, component.Instance.Value);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[4].GetAttribute("style"), "width:0%");
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

        // Announced as the one string it is, rather than as whichever part of it changed.
        Assert.AreEqual("polite", live.GetAttribute("aria-live"));
        Assert.AreEqual("true", live.GetAttribute("aria-atomic"));
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

    [TestMethod,
        DataRow("ArrowLeft", true, false, false),
        DataRow("ArrowRight", false, true, false),
        DataRow("Home", false, false, true),
        DataRow("End", true, false, false),
        DataRow("3", false, true, false)
    ]
    public void BitRatingKeyboardShouldLeaveAModifiedKeyToTheBrowser(string key, bool alt, bool ctrl, bool meta)
    {
        var value = 3d;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Alt+ArrowLeft goes back, Ctrl+Home reaches the top of the page, Ctrl+digit switches tabs: a held
        // modifier makes the key a shortcut of the page rather than a move inside the rating.
        component.Find(".bit-rtg").KeyDown(new KeyboardEventArgs
        {
            Key = key,
            AltKey = alt,
            CtrlKey = ctrl,
            MetaKey = meta
        });

        Assert.AreEqual(3d, value);
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
    public void BitRatingReadOnlyVerticalShouldNotClaimAnOrientation()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Vertical, true);
            parameters.Add(p => p.ReadOnly, true);
        });

        // aria-orientation belongs to the radiogroup pattern; the img role a read-only rating takes has
        // no use for it, even though the items are still stacked visually.
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-orientation"));
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
    public void BitRatingShouldRespectFocusEvents()
    {
        var focusedIn = 0;
        var focusedOut = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.OnFocusIn, () => focusedIn++);
            parameters.Add(p => p.OnFocusOut, () => focusedOut++);
        });

        component.Find(".bit-rtg").FocusIn();
        Assert.AreEqual(1, focusedIn);
        Assert.AreEqual(0, focusedOut);

        component.Find(".bit-rtg").FocusOut();
        Assert.AreEqual(1, focusedIn);
        Assert.AreEqual(1, focusedOut);
    }

    [TestMethod]
    public void BitRatingShouldNotRaiseFocusEventsWhenDisabled()
    {
        var focusedIn = 0;
        var focusedOut = 0;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, false);
            parameters.Add(p => p.OnFocusIn, () => focusedIn++);
            parameters.Add(p => p.OnFocusOut, () => focusedOut++);
        });

        component.Find(".bit-rtg").FocusIn();
        component.Find(".bit-rtg").FocusOut();

        Assert.AreEqual(0, focusedIn);
        Assert.AreEqual(0, focusedOut);
    }

    [TestMethod]
    public void BitRatingShouldKeepASplattedAriaLabel()
    {
        // Every aria attribute the component computes is rendered after the HtmlAttributes splat, so one
        // it leaves empty must hand back what the page wrote rather than erase it.
        var component = Context.Render<BitRating>(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddMultipleAttributes(1, new Dictionary<string, object> { ["aria-label"] = "Rate this product" });
            builder.CloseComponent();
        });

        Assert.AreEqual("Rate this product", component.Find(".bit-rtg").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingShouldKeepASplattedAriaLabelledBy()
    {
        var component = Context.Render<BitRating>(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddMultipleAttributes(1, new Dictionary<string, object> { ["aria-labelledby"] = "external-label" });
            builder.CloseComponent();
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("external-label", root.GetAttribute("aria-labelledby"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingLabelShouldWinOverASplattedAriaLabel()
    {
        var component = Context.Render<BitRating>(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddMultipleAttributes(1, new Dictionary<string, object>
            {
                [nameof(BitRating.Label)] = "Quality",
                ["aria-label"] = "ignored"
            });
            builder.CloseComponent();
        });

        var root = component.Find(".bit-rtg");

        // A name given by reference wins, so the inline one is not rendered beside it.
        StringAssert.Contains(root.GetAttribute("aria-labelledby"), "-label");
        Assert.IsFalse(root.HasAttribute("aria-label"));
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
    public void BitRatingShouldRenderAndBeNamedByItsLabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Rate this product");
        });

        var root = component.Find(".bit-rtg");
        var label = component.Find(".bit-rtg-lbl");

        Assert.AreEqual("Rate this product", label.TextContent);

        // A row of stars carries no text of its own, so the visible label is what names the group.
        Assert.AreEqual(component.Find(".bit-rtg-lbc").Id, root.GetAttribute("aria-labelledby"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingShouldRenderALabelTemplate()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-label");
                builder.AddContent(2, "How was it?");
                builder.CloseElement();
            }));
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("How was it?", component.Find(".custom-label").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-rtg-lbl").Count);
        Assert.AreEqual(component.Find(".bit-rtg-lbc").Id, root.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitRatingShouldRenderNoLabelContainerWithoutALabel()
    {
        var component = RenderComponent<BitRating>();

        Assert.AreEqual(0, component.FindAll(".bit-rtg-lbc").Count);
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-labelledby"));
    }

    [TestMethod,
        DataRow(null, ""),
        DataRow(BitLabelPosition.Top, ""),
        DataRow(BitLabelPosition.Bottom, "bit-rtg-lbm"),
        DataRow(BitLabelPosition.Start, "bit-rtg-lst"),
        DataRow(BitLabelPosition.End, "bit-rtg-led")
    ]
    public void BitRatingShouldRespectLabelPosition(BitLabelPosition? position, string expectedClass)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.LabelPosition, position);
        });

        var root = component.Find(".bit-rtg");

        if (string.IsNullOrEmpty(expectedClass))
        {
            Assert.IsFalse(root.ClassList.Any(c => c is "bit-rtg-lbm" or "bit-rtg-lst" or "bit-rtg-led"));
        }
        else
        {
            Assert.IsTrue(root.ClassList.Contains(expectedClass));
        }
    }

    [TestMethod]
    public void BitRatingAriaLabelShouldWinOverTheLabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.AriaLabel, "Rate the build quality");
        });

        var root = component.Find(".bit-rtg");

        // aria-labelledby would silently discard the explicit string, so only one of the two is rendered.
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
        Assert.AreEqual("Rate the build quality", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingShouldRespectAriaLabelledBy()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.AriaLabelledBy, "external-heading");
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual("external-heading", root.GetAttribute("aria-labelledby"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingReadOnlyWithALabelShouldKeepTheValueInItsName()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.Label, "Average rating");
            parameters.Add(p => p.DefaultValue, 4.2);
        });

        var root = component.Find(".bit-rtg");
        var valueText = component.Find(".bit-rtg-alb[id]");

        // Naming the picture of a value by its label alone would lose the value it exists to show.
        Assert.AreEqual($"{component.Find(".bit-rtg-lbc").Id} {valueText.Id}", root.GetAttribute("aria-labelledby"));
        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", 4.2, 5), valueText.TextContent);
        Assert.IsFalse(root.HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitRatingShouldRenderNoHiddenValueTextWithoutALabel()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.DefaultValue, 4.2);
        });

        // Without a label there is nothing for the value to join, so it stays in the aria-label instead.
        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, "{0} of {1}", 4.2, 5),
                        component.Find(".bit-rtg").GetAttribute("aria-label"));
        Assert.AreEqual(0, component.FindAll(".bit-rtg-alb").Count);
    }

    [TestMethod]
    public void BitRatingShouldRenderAndBeDescribedByItsDescription()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.Description, "Half a star is selectable.");
        });

        var root = component.Find(".bit-rtg");
        var description = component.Find(".bit-rtg-dsc");

        Assert.AreEqual("Half a star is selectable.", description.TextContent.Trim());

        // The description describes the group rather than naming it, so it joins aria-describedby and the
        // name still comes from the label.
        Assert.AreEqual(description.Id, root.GetAttribute("aria-describedby"));
        Assert.AreEqual(component.Find(".bit-rtg-lbc").Id, root.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitRatingShouldRenderADescriptionTemplate()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DescriptionTemplate, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "custom-description");
                builder.AddContent(2, "Tap a star to rate");
                builder.CloseElement();
            }));
        });

        Assert.AreEqual("Tap a star to rate", component.Find(".custom-description").TextContent);
        Assert.AreEqual(component.Find(".bit-rtg-dsc").Id, component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingShouldKeepASplattedAriaDescribedBy()
    {
        // A hyphenated aria-* name never reaches a parameter, so it arrives as a splatted attribute - which the
        // component's own value would otherwise replace. aria-describedby is a space separated list of IDREFs,
        // so the description of the rating joins the page's rather than taking its place.
        var component = Context.Render<BitRating>(builder =>
        {
            builder.OpenComponent<BitRating>(0);
            builder.AddMultipleAttributes(1, new Dictionary<string, object>
            {
                [nameof(BitRating.Description)] = "Half a star is selectable.",
                ["aria-describedby"] = "external-hint"
            });
            builder.CloseComponent();
        });

        Assert.AreEqual($"external-hint {component.Find(".bit-rtg-dsc").Id}",
                        component.Find(".bit-rtg").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public void BitRatingShouldRespectAllowClearOnAFractionalValue()
    {
        double value = 2.5;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // Committing the fraction that is already committed clears the rating, the same way a whole item
        // does - the slices are the choices of a fractional scale.
        component.FindAll(".bit-rtg-seg")[4].Click();

        Assert.AreEqual(0d, value);
    }

    [TestMethod]
    public void BitRatingAllowClearShouldPreviewTheClear()
    {
        double value = 3;
        double? hovered = null;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Bind(p => p.Value, value, v => value = v);
            parameters.Add(p => p.OnHoverChange, (double? v) => hovered = v);
        });

        // The preview is the value a click would commit, and a click on the committed value clears it, so
        // the items empty rather than showing the value the pointer happens to be over.
        component.FindAll(".bit-rtg-btn")[2].MouseOver();

        Assert.AreEqual(0d, hovered);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[0].GetAttribute("style"), "width:0%");
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[2].GetAttribute("style"), "width:0%");

        // Any other item previews itself as usual, and the committed value is untouched throughout.
        component.FindAll(".bit-rtg-btn")[4].MouseOver();

        Assert.AreEqual(5d, hovered);
        Assert.AreEqual(3d, value);
    }

    [TestMethod]
    public void BitRatingAllowClearShouldPreviewTheClearOfAFractionalValue()
    {
        double value = 2.5;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowClear, true);
            parameters.Add(p => p.Precision, 0.5);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // The leading half of the third item is the 2.5 that is already committed, so hovering it previews
        // the clear; the trailing half is a 3 like any other step.
        component.FindAll(".bit-rtg-seg")[4].MouseOver();

        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[1].GetAttribute("style"), "width:0%");

        component.FindAll(".bit-rtg-seg")[5].MouseOver();

        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[2].GetAttribute("style"), "width:100%");
    }

    [TestMethod]
    public void BitRatingShouldPreviewTheCommittedValueWithoutAllowClear()
    {
        double? hovered = null;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, 3);
            parameters.Add(p => p.OnHoverChange, (double? v) => hovered = v);
        });

        // Nothing is cleared without AllowClear, so the committed value previews as itself.
        component.FindAll(".bit-rtg-btn")[2].MouseOver();

        Assert.AreEqual(3d, hovered);
        StringAssert.Contains(component.FindAll(".bit-rtg-ifl")[2].GetAttribute("style"), "width:100%");
    }

    [TestMethod,
        DataRow("Home"),
        DataRow("0")
    ]
    public void BitRatingAllowZeroStarsShouldPutZeroWithinReachOfTheRangeKeys(string key)
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowZeroStars, true);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        // AllowZeroStars puts 0 in the range, and the keys that reach the ends of the range reach it -
        // unlike the pointer, which always commits at least one step, and unlike Delete, which is the
        // clearing key and stays behind AllowClear.
        component.Find(".bit-rtg").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(0d, value);
    }

    [TestMethod,
        DataRow("Home"),
        DataRow("0")
    ]
    public void BitRatingWithoutAllowZeroStarsShouldHoldTheRangeKeysAtTheFloor(string key)
    {
        double value = 3;
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-rtg").KeyDown(new KeyboardEventArgs { Key = key });

        // Without it the floor is a single step, so the same keys stop there instead of clearing.
        Assert.AreEqual(1d, value);
    }

    [TestMethod]
    public void BitRatingShouldHideTheDrawingOfEveryItemFromAssistiveTechnologies()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 3);
        });

        // Every item names itself, so its drawing is presentation only - which is what keeps an ItemTemplate
        // that renders a number or a face from being appended to that name.
        foreach (var iconContainer in component.FindAll(".bit-rtg-ict"))
        {
            Assert.AreEqual("true", iconContainer.GetAttribute("aria-hidden"));
        }
    }

    [TestMethod]
    public void BitRatingShouldHideTheDrawingOfAnItemTemplateToo()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, 3);
            parameters.Add(p => p.ItemTemplate, (BitRatingItemContext context) =>
                (builder) =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, context.Index);
                    builder.CloseElement();
                });
        });

        var containers = component.FindAll(".bit-rtg-ict");

        Assert.AreEqual(3, containers.Count);
        Assert.AreEqual("true", containers[0].GetAttribute("aria-hidden"));

        // The name of the item is the hidden label alone, and not that label plus the number the template drew.
        Assert.AreEqual("1 of 3", component.FindAll(".bit-rtg-btn > .bit-rtg-alb")[0].TextContent.Trim());
    }

    [TestMethod,
        DataRow(-1d),
        DataRow(double.NaN)
    ]
    public void BitRatingShouldFallBackToWholeItemsForAnUnusablePrecision(double precision)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Precision, precision);
        });

        // A precision that asks for no steps, or for infinitely many, leaves the items whole.
        Assert.AreEqual(0, component.FindAll(".bit-rtg-seg").Count);
        Assert.AreEqual("1", component.Find(".bit-input-hidden").GetAttribute("step"));
    }

    [TestMethod]
    public void BitRatingShouldRenderNoDescriptionContainerWithoutADescription()
    {
        var component = RenderComponent<BitRating>();

        Assert.AreEqual(0, component.FindAll(".bit-rtg-dsc").Count);
        Assert.IsFalse(component.Find(".bit-rtg").HasAttribute("aria-describedby"));
    }

    [TestMethod,
        DataRow(true, false, true),
        DataRow(false, false, false),
        DataRow(true, true, false)
    ]
    public void BitRatingShouldMarkARequiredLabel(bool required, bool readOnly, bool expected)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.Required, required);
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        Assert.AreEqual(expected, component.Find(".bit-rtg").ClassList.Contains("bit-rtg-req"));
    }

    [TestMethod]
    public void BitRatingParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitRating)}", BitRatingParams.ParamName);
    }

    [TestMethod]
    public void BitRatingParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitRatingParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.IsInstanceOfType<BitInputBaseParams>(@params);
        Assert.AreEqual(BitRatingParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitRatingShouldApplyCascadingParametersFromBitParams()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitRatingParams
            {
                Max = 3,
                Color = BitColor.Success,
                Size = BitSize.Large,
                Vertical = true,
                ReadOnly = true,
                Label = "Cascaded label",
                LabelPosition = BitLabelPosition.End,
                SelectedIconName = "HeartFill",
                UnselectedIconName = "Heart"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRating>(0);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual(3, component.FindAll(".bit-rtg-btn").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-suc"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-lg"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-vrt"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-rdl"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-led"));
        Assert.AreEqual("Cascaded label", component.Find(".bit-rtg-lbl").TextContent);
        Assert.IsTrue(component.FindAll(".bit-rtg-iem")[2].ClassList.Contains("bit-icon--Heart"));
        Assert.IsTrue(component.FindAll(".bit-rtg-ifl")[0].ClassList.Contains("bit-icon--HeartFill"));
    }

    [TestMethod]
    public void BitRatingDirectParametersShouldOverrideCascadingParameters()
    {
        var paramsList = new List<IBitComponentParams>
        {
            new BitRatingParams
            {
                Max = 3,
                Color = BitColor.Success,
                ReadOnly = true,
                Label = "Cascaded label"
            }
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, paramsList);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitRating>(0);
                builder.AddAttribute(1, nameof(BitRating.Max), 6);
                builder.AddAttribute(2, nameof(BitRating.Color), BitColor.Error);
                builder.AddAttribute(3, nameof(BitRating.ReadOnly), false);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-rtg");

        Assert.AreEqual(6, component.FindAll(".bit-rtg-btn").Count);
        Assert.IsTrue(root.ClassList.Contains("bit-rtg-err"));

        // An explicit false is a written parameter like any other, so the cascade does not fill it in.
        Assert.IsFalse(root.ClassList.Contains("bit-rtg-rdl"));

        // What the markup left unwritten still comes from the cascade.
        Assert.AreEqual("Cascaded label", component.Find(".bit-rtg-lbl").TextContent);
    }

    [TestMethod]
    public void BitRatingShouldRespectTheLabelClassesAndStyles()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Label, "Quality");
            parameters.Add(p => p.Classes, new BitRatingClassStyles
            {
                Label = "custom-label",
                LabelContainer = "custom-label-container",
                Container = "custom-container"
            });
            parameters.Add(p => p.Styles, new BitRatingClassStyles
            {
                Label = "color: red;",
                LabelContainer = "padding: 1rem;",
                Container = "gap: 1rem;"
            });
        });

        var labelContainer = component.Find(".bit-rtg-lbc");
        var label = component.Find(".bit-rtg-lbl");
        var container = component.Find(".bit-rtg-cnt");

        Assert.IsTrue(labelContainer.ClassList.Contains("custom-label-container"));
        Assert.IsTrue(label.ClassList.Contains("custom-label"));
        Assert.IsTrue(container.ClassList.Contains("custom-container"));
        StringAssert.Contains(labelContainer.GetAttribute("style"), "padding: 1rem;");
        StringAssert.Contains(label.GetAttribute("style"), "color: red;");
        StringAssert.Contains(container.GetAttribute("style"), "gap: 1rem;");
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
