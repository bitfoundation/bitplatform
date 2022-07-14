using System.Globalization;
using System.Reflection;

namespace Bit.BlazorUI;

public static class CultureInfoHelper
{
    private static readonly FieldInfo _cultureDataField = typeof(TextInfo).GetField("_cultureData", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo _iReadingLayoutField = Type.GetType("System.Globalization.CultureData, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")!.GetField("_iReadingLayout", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static CultureInfo GetFaIrCultureByFarsiNames()
    {
        var cultureInfo = CultureInfo.CreateSpecificCulture("fa-IR");

        cultureInfo.DateTimeFormat.MonthNames = new[]
        {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", ""
        };

        cultureInfo.DateTimeFormat.AbbreviatedMonthNames = new[]
        {
            "فرور", "ارد", "خرد", "تیر", "مرد", "شهر", "مهر", "آبا", "آذر", "دی", "بهم", "اسف", ""
        };

        cultureInfo.DateTimeFormat.MonthGenitiveNames = cultureInfo.DateTimeFormat.MonthNames;
        cultureInfo.DateTimeFormat.AbbreviatedMonthGenitiveNames = cultureInfo.DateTimeFormat.AbbreviatedMonthNames;
        cultureInfo.DateTimeFormat.DayNames = new[]
        {
            "یکشنبه", "دوشنبه", "ﺳﻪشنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"
        };

        cultureInfo.DateTimeFormat.AbbreviatedDayNames = new[]
        {
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        };

        cultureInfo.DateTimeFormat.ShortestDayNames = new[]
        {
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        };

        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        cultureInfo.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Saturday;

        var cultureData = _cultureDataField.GetValue(cultureInfo.TextInfo);

        _iReadingLayoutField.SetValue(cultureData, 1 /*rtl*/); // this affects cultureInfo.TextInfo.IsRightToLeft

        if (cultureInfo.DateTimeFormat.Calendar is not PersianCalendar)
        {
            cultureInfo.DateTimeFormat.Calendar = new PersianCalendar();
        }

        return cultureInfo;
    }

    public static CultureInfo GetFaIrCultureByFingilishNames()
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

        cultureInfo.DateTimeFormat.AMDesignator = "G.Z";
        cultureInfo.DateTimeFormat.PMDesignator = "B.Z";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        cultureInfo.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Saturday;

        var cultureData = _cultureDataField.GetValue(cultureInfo.TextInfo);

        _iReadingLayoutField.SetValue(cultureData, 1 /*rtl*/); // this affects cultureInfo.TextInfo.IsRightToLeft

        if (cultureInfo.DateTimeFormat.Calendar is not PersianCalendar)
        {
            cultureInfo.DateTimeFormat.Calendar = new PersianCalendar();
        }

        return cultureInfo;
    }
}
