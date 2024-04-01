using System.Reflection;

namespace Bit.BlazorUI.Demo.Client.Core.Helpers;

public static class CultureInfoHelper
{
    public static CultureInfo GetFaIrCultureWithFarsiNames()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("fa-IR");
        cultureInfo.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cultureInfo, new PersianCalendar());

        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        cultureInfo.DateTimeFormat.AbbreviatedDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];
        cultureInfo.DateTimeFormat.ShortestDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];

        return cultureInfo;
    }

    public static CultureInfo GetFaIrCultureWithFingilishNames()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("fa-IR");
        cultureInfo.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cultureInfo, new PersianCalendar());

        cultureInfo.DateTimeFormat.MonthNames =
        [
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
        ];

        cultureInfo.DateTimeFormat.AbbreviatedMonthNames =
        [
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
        ];

        cultureInfo.DateTimeFormat.MonthGenitiveNames = cultureInfo.DateTimeFormat.MonthNames;
        cultureInfo.DateTimeFormat.AbbreviatedMonthGenitiveNames = cultureInfo.DateTimeFormat.AbbreviatedMonthNames;
        cultureInfo.DateTimeFormat.DayNames =
        [
            "YekShanbe",
            "DoShanbe",
            "SeShanbe",
            "ChaharShanbe",
            "PanjShanbe",
            "Jome",
            "Shanbe"
        ];

        cultureInfo.DateTimeFormat.AbbreviatedDayNames =
        [
            "Yek",
            "Do",
            "Se",
            "Ch",
            "Pj",
            "Jom",
            "Shn"
        ];

        cultureInfo.DateTimeFormat.ShortestDayNames =
        [
            "Y",
            "D",
            "S",
            "C",
            "P",
            "J",
            "S"
        ];

        cultureInfo.DateTimeFormat.AMDesignator = "G.Z";
        cultureInfo.DateTimeFormat.PMDesignator = "B.Z";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";

        return cultureInfo;
    }
}
