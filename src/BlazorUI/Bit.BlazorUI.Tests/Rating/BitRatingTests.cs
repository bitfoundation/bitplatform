using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Rating;

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

        var button = component.Find(".button");

        Assert.AreEqual(isEnabled is false, button.HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitRatingShouldRespectIsReadonly()
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.IsReadOnly, true);
            parameters.Add(p => p.AllowZeroStars, true);
        });

        var bitRating = component.Find(".bit-rtg");

        Assert.IsTrue(bitRating.ClassList.Contains("readonly"));
        Assert.IsTrue(bitRating.HasAttribute("aria-readonly"));

        var buttons = component.FindAll(".button");

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

        var button = component.FindAll(".button");

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
        var span = component.Find(".aria-label");
        Assert.AreEqual(string.Format(ariaLabelFormat, 1, 5), span.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(BitRatingSize.Small),
        DataRow(BitRatingSize.Large)
    ]
    public void BitRatingShouldRespectSize(BitRatingSize size)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });
        var bitRating = component.Find(".bit-rtg");

        var sizeClass = size == BitRatingSize.Large ? "large" : "small";

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

        var firstButton = component.Find(".button");

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(allowZeroStars is false, bool.Parse(firstButton.GetAttribute("aria-checked")));
    }

    [DataTestMethod,
        DataRow(BitIconName.HeartFill, BitIconName.Heart),
        DataRow(BitIconName.HeartFill, BitIconName.Heart),
    ]
    public void BitRatingShouldTakeCustomIcon(BitIconName icon, BitIconName unselectedIcon)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Icon, icon);
            parameters.Add(p => p.UnselectedIcon, unselectedIcon);
        });

        var ratingIcon = component.Find(".button i");
        var ratingUnselectedIcon = component.Find(".button:nth-child(2) i");

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.IsTrue(ratingIcon.ClassList.Contains($"bit-icon--{icon}"));
        Assert.IsTrue(ratingUnselectedIcon.ClassList.Contains($"bit-icon--{unselectedIcon}"));
    }

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
        DataRow(10, 3, true, false, 3),
        DataRow(10, 2, false, false, 1),
        DataRow(10, 0, true, true, 1),
        DataRow(10, 4, false, true, 1),
        DataRow(10, 0, true, false, 1)
    ]
    public void BitRatingShouldRespectClickIndex(int max, int clickedIndex, bool isEnabled, bool isReadonly, int expectedResult)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsReadOnly, isReadonly);
        });

        var bitRatingButtons = component.FindAll(".button");

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
        Assert.AreEqual((!isEnabled || isReadonly) ? 1 : clickedIndex, filledBitRatingIconCount);
        Assert.AreEqual((!isEnabled || isReadonly) ? max - 1 : max - clickedIndex, unselectedBitRatingIconCount);
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
          DataRow(5, 2, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 3, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 1.25, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 2.5, BitIconName.HeartFill, BitIconName.Heart)
    ]
    public void BitRatingShouldRespectRatingValue(int max, double value, BitIconName icon, BitIconName unselectedIcon)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        var buttons = component.FindAll(".bit-rating button i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{icon.GetName()}"));
        var unselectedBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{unselectedIcon.GetName()}"));

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(filledBitRatingIconCount, value);
        Assert.AreEqual(unselectedBitRatingIconCount, max - value);
    }

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
         DataRow(5, 0, 2, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 0, 3, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 0, 1.25, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 0, 2.5, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 3, 2, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 5, 3, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 2.25, 1.25, BitIconName.HeartFill, BitIconName.Heart),
         DataRow(5, 1.5, 2.5, BitIconName.HeartFill, BitIconName.Heart)
    ]
    public void BitRatingShouldRespectDefaultRatingValue(int max, double value, double defaultValue, BitIconName icon, BitIconName unselectedIcon)
    {
        var component = RenderComponent<BitRating>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, defaultValue);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        var buttons = component.FindAll(".button i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{icon.GetName()}"));
        var unselectedBitRatingIconCount = buttons.Count(s => s.ClassList.Contains($"bit-icon--{unselectedIcon.GetName()}"));

        var actualValue = value == default ? defaultValue : value;

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(filledBitRatingIconCount, actualValue);
        Assert.AreEqual(unselectedBitRatingIconCount, max - actualValue);

        var input = component.Find(".bit-input-hidden");

        Assert.AreEqual(input.GetAttribute("value"), actualValue.ToString());
    }
}
