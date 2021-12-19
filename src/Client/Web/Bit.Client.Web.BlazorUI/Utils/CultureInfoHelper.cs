using System.Globalization;

namespace Bit.Client.Web.BlazorUI
{
    public static class CultureInfoHelper
    {
        public static CultureInfo GetPersianCultureByFinglishNames()
        {
            var cultureInfo = CultureInfo.CreateSpecificCulture("fa-IR");

            cultureInfo.DateTimeFormat.MonthNames = new[]
            {
                "Farvardin",
                "Ordibehesht",
                "Khordad",
                "Tir",
                "Mordad",
                "Shahrivar",
                "Mehr",
                "Aban",
                "Azar",
                "Dey",
                "Bahman",
                "Esfand",
                ""
            };

            cultureInfo.DateTimeFormat.AbbreviatedMonthNames = new[]
            {
                "Far",
                "Ord",
                "Khr",
                "Tir",
                "Mrd",
                "Shr",
                "Mhr",
                "Abn",
                "Azr",
                "Dey",
                "Bah",
                "Esf",
                ""
            };

            cultureInfo.DateTimeFormat.MonthGenitiveNames = cultureInfo.DateTimeFormat.MonthNames;
            cultureInfo.DateTimeFormat.AbbreviatedMonthGenitiveNames = cultureInfo.DateTimeFormat.AbbreviatedMonthNames;

            cultureInfo.DateTimeFormat.DayNames = new[]
            {
                "YekShanbe",
                "DoShanbe",
                "SeShanbe",
                "ChaharShanbe",
                "PanjShanbe",
                "Jome",
                "Shanbe"
            };

            cultureInfo.DateTimeFormat.AbbreviatedDayNames = new[]
            {
                "Yek",
                "Do",
                "Se",
                "Ch",
                "Pj",
                "Jom",
                "Shn"
            };

            cultureInfo.DateTimeFormat.ShortestDayNames = new[]
            {
                "Y",
                "D",
                "S",
                "C",
                "P",
                "J",
                "S"
            };

            return cultureInfo;
        }
    }
}
