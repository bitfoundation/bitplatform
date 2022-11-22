﻿using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Rating;

[TestClass]
public class BitRatingTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true),
        DataRow(Visual.Fluent, false),

        DataRow(Visual.Cupertino, true),
        DataRow(Visual.Cupertino, false),

        DataRow(Visual.Material, true),
        DataRow(Visual.Material, false)
    ]
    public void BitRatingShouldTakeCorrectVisualandEnabledStyle(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Visual, visual);
        });
        var bitRating = component.Find(".bit-rtg");

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(bitRating.ClassList.Contains($"bit-rtg-{isEnabledClass}-{visualClass}"));
        Assert.IsTrue(bitRating.HasAttribute("role"));
        Assert.AreEqual("radiogroup", bitRating.GetAttribute("role"));

        var button = component.Find(".rate-btn");

        Assert.AreEqual(!isEnabled, button.HasAttribute("disabled"));
    }

    [DataTestMethod]
    public void BitRatingShouldRespectIsReadonly()
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.IsReadOnly, true);
            parameters.Add(p => p.AllowZeroStars, true);
        });

        var bitRating = component.Find(".bit-rtg");

        Assert.IsTrue(bitRating.ClassList.Contains($"bit-rtg-readonly-fluent"));
        Assert.IsTrue(bitRating.HasAttribute("aria-readonly"));

        var buttons = component.FindAll(".rate-btn");

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
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.Max, max);
        });

        var button = component.FindAll(".rate-btn");

        Assert.AreEqual(max, button.Count);
    }

    [DataTestMethod,
        DataRow("Select {0} of {1} stars")]
    public void BitRatingShouldTakeCorrectAriaLabelFormat(string ariaLabelFormat)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabelFormat, ariaLabelFormat);
        });
        var span = component.Find(".aria-reader");
        Assert.AreEqual(string.Format(ariaLabelFormat, 1, 5), span.TextContent);
    }

    [DataTestMethod,
        DataRow(null),
        DataRow(BitRatingSize.Small),
        DataRow(BitRatingSize.Large)
    ]
    public void BitRatingShouldRespectSize(BitRatingSize size)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.Size, size);
        });
        var bitRating = component.Find(".bit-rtg");

        var sizeClass = size == BitRatingSize.Large ? "large" : "small";

        Assert.IsTrue(bitRating.ClassList.Contains($"bit-rtg-{sizeClass}-fluent"));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitRatingShouldRespectAllowZeroStars(bool allowZeroStars)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.AllowZeroStars, allowZeroStars);
        });

        var firstButton = component.Find(".rate-btn");

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.AreEqual(!allowZeroStars, bool.Parse(firstButton.GetAttribute("aria-checked")));
    }

    [DataTestMethod,
        DataRow(BitIconName.HeartFill, BitIconName.Heart),
        DataRow(BitIconName.HeartFill, BitIconName.Heart),
        ]
    public void BitRatingShouldTakeCustomIcon(BitIconName icon, BitIconName unselectedIcon)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.Icon, icon);
            parameters.Add(p => p.UnselectedIcon, unselectedIcon);
        });

        var bitRating = component.FindAll(".rate-btn");
        var ratingIcon = component.Find(".rate-btn i");
        var ratingUnselectedIcon = component.Find(".rate-btn:nth-child(2) i");

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.IsTrue( ratingIcon.ClassList.Contains($"bit-icon--{icon}"));
        //Assert.IsTrue(ratingUnselectedIcon.ClassList.Contains($"bit-icon--{unselectedIcon}"));
    }

    [DataTestMethod,
        DataRow(10, 3, true, false, 3),
        DataRow(10, 2, false, false, 1),
        DataRow(10, 0, true, true, 1),
        DataRow(10, 4, false, true, 1),
        DataRow(10, 0, true, false, 1)]
    public void BitRatingShouldRespectClickIndex(int max, int clickedIndex, bool isEnabled, bool isReadonly, int expectedResult)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.Max, max);
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.IsReadOnly, isReadonly);
        });

        var bitRatingButtons = component.FindAll(".rate-btn");

        if (clickedIndex <= 0)
        {
            clickedIndex = 1;
        }

        bitRatingButtons[clickedIndex - 1].Click();

        var bitRatingIcons = component.FindAll("i");

        var filledBitRatingIconCount = bitRatingIcons.Where(r => r.ClassList.Contains("bit-icon--FavoriteStarFill")).Count();
        var unselectedBitRatingIconCount = bitRatingIcons.Where(r => r.ClassList.Contains("bit-icon--FavoriteStar")).Count();

        Assert.AreEqual(bitRatingButtons.Count(), max);

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.AreEqual((!isEnabled || isReadonly) ? 1 : clickedIndex, filledBitRatingIconCount);
        //Assert.AreEqual((!isEnabled || isReadonly) ? max - 1 : max - clickedIndex, unselectedBitRatingIconCount);
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitRatingAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, ariaLabel);
        });

        var bitRating = com.Find(".bit-rtg");

        Assert.IsTrue(bitRating.HasAttribute("aria-label"));
        Assert.AreEqual(ariaLabel, bitRating.GetAttribute("aria-label"));
    }


    [DataTestMethod,
          DataRow(5, 2, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 3, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 1.25, BitIconName.HeartFill, BitIconName.Heart),
          DataRow(5, 2.5, BitIconName.HeartFill, BitIconName.Heart)
    ]
    public void BitRatingShouldRespectRatingValue(int max, double value, BitIconName icon, BitIconName unselectedIcon)
    {
        var component = RenderComponent<BitRatingTest>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.Max, max);
        });

        var buttons = component.FindAll(".bit-rating button i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Where(s => s.ClassList.Contains($"bit-icon--{icon.GetName()}")).Count();
        var unselectedBitRatingIconCount = buttons.Where(s => s.ClassList.Contains($"bit-icon--{unselectedIcon.GetName()}")).Count();

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.AreEqual(filledBitRatingIconCount, value);
        //Assert.AreEqual(unselectedBitRatingIconCount, max - value);
    }

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

        var buttons = component.FindAll(".rate-btn i:nth-child(2)");

        var filledBitRatingIconCount = buttons.Where(s => s.ClassList.Contains($"bit-icon--{icon.GetName()}")).Count();
        var unselectedBitRatingIconCount = buttons.Where(s => s.ClassList.Contains($"bit-icon--{unselectedIcon.GetName()}")).Count();

        var actualValue = value == default ? defaultValue : value;

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.AreEqual(filledBitRatingIconCount, actualValue);
        //Assert.AreEqual(unselectedBitRatingIconCount, max - actualValue);

        var input = component.Find(".bit-input-hidden");

        Assert.AreEqual(input.GetAttribute("value"), actualValue.ToString());
    }
}
