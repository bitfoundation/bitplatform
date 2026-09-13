//-:cnd:noEmit
using System.Runtime.CompilerServices;

namespace Boilerplate.Shared.Infrastructure.Services;

public partial class CultureInfoManager
{
    /// <summary>
    /// To enable/disable multilingual support, navigate to Directory.Build.props and modify the InvariantGlobalization flag.
    /// </summary>
    public static bool InvariantGlobalization { get; } =
#if InvariantGlobalization
    true;
#else
    false;
#endif

    public static CultureInfo DefaultCulture => field ??= CreateCultureInfo("en-US");

    public static (string DisplayName, CultureInfo Culture)[] SupportedCultures => field ??=
    [
        ("English US", CreateCultureInfo("en-US")),
        ("English UK", CreateCultureInfo("en-GB")),
        ("Nederlands", CreateCultureInfo("nl-NL")),
        ("فارسی", CreateCultureInfo("fa-IR")),
        ("svenska", CreateCultureInfo("sv-SE")),
        ("हिन्दी", CreateCultureInfo("hi-IN")),
        ("中文", CreateCultureInfo("zh-CN")),
        ("español", CreateCultureInfo("es-ES")),
        ("français", CreateCultureInfo("fr-FR")),
        ("العربية", CreateCultureInfo("ar-SA")),
        ("Deutsch", CreateCultureInfo("de-DE"))
        // Adding new cultures requires changing MainActivity's DataPathPrefixes.
    ];

    public static CultureInfo CreateCultureInfo(string name)
    {
        var cultureInfo = AppPlatform.IsBrowser ? CultureInfo.CreateSpecificCulture(name) : new CultureInfo(name);

        if (name == "fa-IR")
        {
            CustomizeCultureInfoForFaCulture(cultureInfo);
        }

        return cultureInfo;
    }

    public static CultureInfo? GetCultureInfo(string? cultureName)
    {
        return SupportedCultures.Select(sc => sc.Culture)
                                .FirstOrDefault(c => string.Equals(c.Name, cultureName, StringComparison.InvariantCultureIgnoreCase));
    }

    public static void SetCurrentCulture(string? cultureName)
    {
        var cultureInfo = GetCultureInfo(cultureName) ?? DefaultCulture;

        if (AppPlatform.IsBlazorHybridOrBrowser)
        {
            // Single-user process: the process-wide defaults ONLY
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }
        else
        {
            // Blazor Server
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
        }
    }

    public static string? FindRegionIso2(string? cultureName)
    {
        var cultureInfo = GetCultureInfo(cultureName);

        if (cultureInfo is null) return null;

        return new RegionInfo(cultureInfo.LCID).TwoLetterISORegionName;
    }

    /// <summary>
    /// This is an example to demonstrate the way you can customize application culture
    /// </summary>
    private static CultureInfo CustomizeCultureInfoForFaCulture(CultureInfo cultureInfo)
    {
        Get_CalendarField(cultureInfo) = new PersianCalendar();

        cultureInfo.DateTimeFormat.AMDesignator = "ق.ظ";
        cultureInfo.DateTimeFormat.PMDesignator = "ب.ظ";
        cultureInfo.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        cultureInfo.DateTimeFormat.LongDatePattern = "yyyy MMMM d, dddd";
        cultureInfo.DateTimeFormat.AbbreviatedDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];
        cultureInfo.DateTimeFormat.ShortestDayNames =
        [
            "ی", "د", "س", "چ", "پ", "ج", "ش"
        ];

        string[] monthNames =
        [
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند",
            "" // 13-month calendars reserve a 13th slot; it must exist even when unused.
        ];
        cultureInfo.DateTimeFormat.MonthNames = monthNames;
        cultureInfo.DateTimeFormat.MonthGenitiveNames = monthNames;
        cultureInfo.DateTimeFormat.AbbreviatedMonthNames = monthNames;
        cultureInfo.DateTimeFormat.AbbreviatedMonthGenitiveNames = monthNames;
        cultureInfo.DateTimeFormat.DayNames =
        [
            "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"
        ];

        return cultureInfo;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_calendar")]
    static extern ref Calendar Get_CalendarField(CultureInfo cultureInfo);
}
