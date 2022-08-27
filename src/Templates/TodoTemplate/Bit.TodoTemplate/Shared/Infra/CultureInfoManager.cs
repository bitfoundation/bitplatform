using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TodoTemplate.Shared.Infra;
public class CultureInfoManager
{
    public static (string name, string code) DefaultCulture { get; } = ("English", "en");

    public static (string name, string code)[] SupportedCultures { get; } = new (string name, string code)[]
    {
        ("English", "en"),
        ("Française", "fr"),
        // ("فارسی", "fa"), // To add more languages, you've to provide resx files. You might also put some efforts to change your app flow direction based on CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
    };

    public static CultureInfo CreateCultureInfo(string cultureInfoId)
    {
        var cultureInfo = RuntimeInformation.ProcessArchitecture == Architecture.Wasm ? CultureInfo.CreateSpecificCulture(cultureInfoId) : new CultureInfo(cultureInfoId);

        if (cultureInfoId == "fa")
        {
            CustomizeCultureInfoForFaCulture(cultureInfo);
        }

        return cultureInfo;
    }

    public static void SetCurrentCulture(string? cultureInfoCookie)
    {
        var currentCulture = GetCurrentCulture(cultureInfoCookie);

        var cultureInfo = CreateCultureInfo(currentCulture);

        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }

    public static string GetCurrentCulture(string? preferredCultureCookie)
    {
        string culture = CultureInfo.CurrentUICulture.Name[..2];
        if (preferredCultureCookie is not null)
        {
            culture = preferredCultureCookie[(preferredCultureCookie.IndexOf("|uic=") + 5)..];
        }
        if (SupportedCultures.Any(sc => sc.code == culture) is false)
        {
            culture = DefaultCulture.code;
        }
        return culture;
    }

    /// <summary>
    /// This is an example to demonstrate the way you can customize application culture
    /// </summary>
    public static CultureInfo CustomizeCultureInfoForFaCulture(CultureInfo cultureInfo)
    {
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

    private static readonly FieldInfo _cultureDataField = typeof(TextInfo).GetField("_cultureData", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo _iReadingLayoutField = Type.GetType("System.Globalization.CultureData, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")!.GetField("_iReadingLayout", BindingFlags.NonPublic | BindingFlags.Instance)!;
}
