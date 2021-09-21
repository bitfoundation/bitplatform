using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Rating
{
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
            var bitRating = component.Find(".bit-rating");

            var isEnabledClass = isEnabled ? "enabled" : "disabled";
            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

            Assert.IsTrue(bitRating.ClassList.Contains($"bit-rating-{isEnabledClass}-{visualClass}"));

            Assert.IsTrue(bitRating.HasAttribute("role"));
            Assert.AreEqual("radiogroup", bitRating.GetAttribute("role"));
        }

        [DataTestMethod]
        public void BitRatingShouldRespectIsReadonly()
        {
            var component = RenderComponent<BitRatingTest>(parameters =>
            {
                parameters.Add(p => p.IsReadOnly, true);
            });

            var bitRating = component.Find(".bit-rating");

            Assert.IsTrue(bitRating.ClassList.Contains($"bit-rating-readonly-fluent"));
            Assert.IsTrue(bitRating.HasAttribute("aria-readonly"));
            Assert.AreEqual("true", bitRating.GetAttribute("aria-readonly"));

            var buttons = component.FindAll(".bit-rating button");

            foreach (var button in buttons)
            {
                Assert.IsTrue(button.HasAttribute("aria-hidden"));
                Assert.AreEqual("true", button.GetAttribute("aria-hidden"));
                Assert.IsTrue(button.HasAttribute("disabled"));
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

            var buttons = component.FindAll(".bit-rating button");

            Assert.AreEqual(max, buttons.Count);
        }


        [DataTestMethod,
            DataRow(null),
            DataRow(RatingSize.Small),
            DataRow(RatingSize.Large)
        ]
        public void BitRatingShouldRespectSize(RatingSize size)
        {
            var component = RenderComponent<BitRatingTest>(parameters =>
            {
                parameters.Add(p => p.Size, size);
            });
            var bitRating = component.Find(".bit-rating");

            var sizeClass = size == RatingSize.Large ? "large" : "small";

            Assert.IsTrue(bitRating.ClassList.Contains($"bit-rating-{sizeClass}-fluent"));
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

            var firstButton = component.Find(".bit-rating button");
            Assert.AreEqual(!allowZeroStars, bool.Parse(firstButton.GetAttribute("aria-checked")));
        }

        [DataTestMethod,
            DataRow("HeartFill", "Heart"),
            DataRow("HeartFill", "Heart"),
            ]
        public void BitRatingShouldTakeCustomIcon(string icon, string unselectedIcon)
        {
            var component = RenderComponent<BitRatingTest>(parameters =>
            {
                parameters.Add(p => p.Icon, icon);
                parameters.Add(p => p.UnselectedIcon, unselectedIcon);
            });

            var bitRating = component.FindAll(".bit-rating button");
            var ratingIcon = component.Find(".bit-rating button i");
            var ratingUnselectedIcon = component.Find(".bit-rating button:nth-child(2) i");

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

            var bitRatingButtons = component.FindAll(".bit-rating button");

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
            //Assert.AreEqual(filledBitRatingIconCount, expectedResult);
            //Assert.AreEqual(unselectedBitRatingIconCount, (max - expectedResult));
        }

        [DataTestMethod, DataRow("Detailed label")]
        public void BitRatingAriaLabelTest(string ariaLabel)
        {
            var com = RenderComponent<BitRatingTest>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, ariaLabel);
            });

            var bitRating = com.Find(".bit-rating");

            Assert.IsTrue(bitRating.HasAttribute("aria-label"));
            Assert.AreEqual(ariaLabel, bitRating.GetAttribute("aria-label"));
        }
    }
}
