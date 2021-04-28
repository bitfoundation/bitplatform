using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Rating
{
    [TestClass]
    public class BitRatingTests : BunitTestContext
    {
        [DataTestMethod, DataRow(true, true), DataRow(false, false)]
        public void BitRatingShouldRespectIsReadonly(bool isReadonly, bool expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.IsReadonly, isReadonly));
            var bitRating = component.FindAll("button").FirstOrDefault();

            Assert.AreEqual(expectedResult, bitRating.ClassList.Contains("readonly"));
        }

        [DataTestMethod, DataRow(true, false), DataRow(false, true)]
        public void BitRatingShouldRespectIsEnable(bool isEnable, bool expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.IsEnabled, isEnable));
            var bitRating = component.FindAll("button").FirstOrDefault();

            Assert.AreEqual(expectedResult, bitRating.ClassList.Contains("disabled"));
        }

        [DataTestMethod, DataRow(null, true), DataRow(RatingSize.Small, true), DataRow(RatingSize.Large, false)]
        public void BitRatingShouldRespectSize(RatingSize size, bool expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.Size, size));
            var bitRating = component.FindAll("button").FirstOrDefault();

            Assert.AreEqual(expectedResult, bitRating.ClassList.Contains("rating-star-small"));
        }

        [DataTestMethod, DataRow("Starburst", true, 0, true)]
        public void BitRatingShouldRespectCustomIcon(string icon, bool allowZeroStars, int defaultRating, bool expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters =>
            {
                parameters.Add(p => p.Icon, icon);
                parameters.Add(p => p.AllowZeroStars, allowZeroStars);
                parameters.Add(p => p.DefaultRating, defaultRating);
            });
            var bitRating = component.FindAll("i").FirstOrDefault();

            Assert.AreEqual(expectedResult, bitRating.ClassList.Contains("bit-icon--Starburst"));
        }

        [DataTestMethod, DataRow(null, true, 0, false), DataRow(null, false, 1, true),
            DataRow("FavoriteStarFill", false, 4, true)]
        public void BitRatingShouldRespectIcon(string icon, bool allowZeroStars, int defaultRating, bool expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters =>
            {
                parameters.Add(p => p.Icon, icon);
                parameters.Add(p => p.AllowZeroStars, allowZeroStars);
                parameters.Add(p => p.DefaultRating, defaultRating);
            });
            var bitRating = component.FindAll("i").FirstOrDefault();

            Assert.AreEqual(expectedResult, bitRating.ClassList.Contains("bit-icon--FavoriteStarFill"));
        }

        [DataTestMethod, DataRow(8, 8)]
        public void BitRatingShouldMeetMaxCount(int max, int expectedResult)
        {
            var component = RenderComponent<BitRating>(parameters => parameters.Add(p => p.Max, max));
            var bitRating = component.FindAll("button");

            Assert.AreEqual(expectedResult, bitRating.Count());
        }
    }
}
