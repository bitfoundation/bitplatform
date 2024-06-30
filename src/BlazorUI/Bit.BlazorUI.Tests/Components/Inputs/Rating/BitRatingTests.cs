using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

[TestClass]
public class BitRatingTests : BunitTestContext
{
    [DataTestMethod,
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
        Assert.IsTrue(bitRating.HasAttribute("aria-readonly"));

        var buttons = component.FindAll(".bit-rtg-btn");

        foreach (var button in buttons)
        {
            Assert.IsTrue(button.HasAttribute("disabled"));
            Assert.AreEqual("-1", button.GetAttribute("tabindex"));
        }

    }

    [DataTestMethod,
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

    [DataTestMethod,
        DataRow("Select {0} of {1} stars")]
    public void BitRatingShouldTakeCorrectAriaLabelFormat(string ariaLabelFormat)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AriaLabelFormat, ariaLabelFormat);
        });
        var span = component.Find(".bit-rtg-alb");
        Assert.AreEqual(string.Format(ariaLabelFormat, 1, 5), span.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(BitSize.Small),
        DataRow(BitSize.Large)
    ]
    public void BitRatingShouldRespectSize(BitSize size)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });
        var bitRating = component.Find(".bit-rtg");

        var sizeClass = size == BitSize.Large ? "bit-rtg-lg" : "bit-rtg-sm";

        Assert.IsTrue(bitRating.ClassList.Contains(sizeClass));
    }

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitRatingShouldRespectAllowZeroStars(bool allowZeroStars)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.AllowZeroStars, allowZeroStars);
        });

        var firstButton = component.Find(".bit-rtg-btn");

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(allowZeroStars is false, bool.Parse(firstButton.GetAttribute("aria-checked")));
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

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
        DataRow(10, 3, true, false, 3),
        DataRow(10, 2, false, false, 1),
        DataRow(10, 0, true, true, 1),
        DataRow(10, 4, false, true, 1),
        DataRow(10, 0, true, false, 1)
    ]
    public void BitRatingShouldRespectClickIndex(int max, int clickedIndex, bool isEnabled, bool readOnly, int expectedResult)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.ReadOnly, readOnly);
        });

        var bitRatingButtons = component.FindAll(".bit-rtg-btn");

        if (clickedIndex <= 0)
        {
            clickedIndex = 1;
        }

        bitRatingButtons[clickedIndex - 1].Click();

        var bitRatingIcons = component.FindAll("i");

        var filledBitRatingIconCount = bitRatingIcons.Count(r => r.ClassList.Contains("bit-icon--FavoriteStarFill"));
        var unselectedBitRatingIconCount = bitRatingIcons.Count(r => r.ClassList.Contains("bit-icon--FavoriteStar"));

        Assert.AreEqual(bitRatingButtons.Count(), max);

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual((!isEnabled || readOnly) ? 1 : clickedIndex, filledBitRatingIconCount);
        Assert.AreEqual((!isEnabled || readOnly) ? max - 1 : max - clickedIndex, unselectedBitRatingIconCount);
    }

    [DataTestMethod, DataRow("Detailed label")]
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


    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
          DataRow(5, 2, "HeartFill", "Heart"),
          DataRow(5, 3, "HeartFill", "Heart"),
          DataRow(5, 1.25, "HeartFill", "Heart"),
          DataRow(5, 2.5, "HeartFill", "Heart")
    ]
    public void BitRatingShouldRespectRatingValue(int max, double value, string icon, string unselectedIcon)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        var buttons = component.FindAll(".bit-rating button i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{icon}"));
        var unselectedBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{unselectedIcon}"));

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(filledBitRatingIconCount, value);
        Assert.AreEqual(unselectedBitRatingIconCount, max - value);
    }

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
         DataRow(5, 0, 2, "HeartFill", "Heart"),
         DataRow(5, 0, 3, "HeartFill", "Heart"),
         DataRow(5, 0, 1.25, "HeartFill", "Heart"),
         DataRow(5, 0, 2.5, "HeartFill", "Heart"),
         DataRow(5, 3, 2, "HeartFill", "Heart"),
         DataRow(5, 5, 3, "HeartFill", "Heart"),
         DataRow(5, 2.25, 1.25, "HeartFill", "Heart"),
         DataRow(5, 1.5, 2.5, "HeartFill", "Heart")
    ]
    public void BitRatingShouldRespectDefaultRatingValue(int max, double value, double defaultValue, string icon, string unselectedIcon)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        var buttons = component.FindAll(".bit-rtg-btn i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{icon}"));
        var unselectedBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{unselectedIcon}"));

        var actualValue = value == default ? defaultValue : value;

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(filledBitRatingIconCount, actualValue);
        Assert.AreEqual(unselectedBitRatingIconCount, max - actualValue);

        var input = component.Find(".bit-input-hidden");

        Assert.AreEqual(input.GetAttribute("value"), actualValue.ToString());
    }
}
