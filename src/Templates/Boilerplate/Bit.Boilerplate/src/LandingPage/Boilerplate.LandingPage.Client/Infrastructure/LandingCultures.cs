//-:cnd:noEmit
using System.Runtime.CompilerServices;

namespace Boilerplate.LandingPage.Client.Infrastructure;

public static class LandingCultures
{
    public static bool InvariantGlobalization { get; } =
#if InvariantGlobalization
        true;
#else
        false;
#endif

    public static CultureInfo Default => field ??= new CultureInfo("en-US");

    public static (string DisplayName, CultureInfo Culture)[] Supported => field ??= InvariantGlobalization
        ? [("English", Default)]
        : [("English", Default), ("فارسی", CreatePersian())];

    // Written here, not in resx: each one shows in the other language, whose resources the visitor hasn't downloaded
    public static IReadOnlyDictionary<string, (string Suggestion, string Switch)> Suggestions { get; } = new Dictionary<string, (string, string)>
    {
        ["en-US"] = ("This page is also available in English.", "Read it in English"),
        ["fa-IR"] = ("این صفحه به فارسی هم هست.", "خواندن به فارسی")
    };

    public static CultureInfo? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        return Supported.Select(s => s.Culture).FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    // Only the exact spelling of a non-default culture counts; the server redirects every other form to the canonical URL
    public static bool IsPrefix(string? segment)
    {
        return Find(segment) is { } culture && culture.Name != Default.Name && culture.Name == segment;
    }

    public static (CultureInfo Culture, string Path) Split(string absolutePath)
    {
        var path = string.IsNullOrEmpty(absolutePath) ? "/" : absolutePath;
        var segments = path.Split('/', 3);

        if (segments.Length > 1 && IsPrefix(segments[1]))
            return (Find(segments[1])!, segments.Length > 2 ? $"/{segments[2]}" : "/");

        return (Default, path);
    }

    public static string Localize(string path, CultureInfo culture)
    {
        if (culture.Name == Default.Name) return path;

        return path == "/" ? $"/{culture.Name}" : $"/{culture.Name}{path}";
    }

    // [mirror] the fa-IR culture customization - keep in sync with:
    // - src/Shared/Infrastructure/Services/CultureInfoManager.cs (CustomizeCultureInfoForFaCulture)
    private static CultureInfo CreatePersian()
    {
        var culture = OperatingSystem.IsBrowser() ? CultureInfo.CreateSpecificCulture("fa-IR") : new CultureInfo("fa-IR");

        Get_CalendarField(culture) = new PersianCalendar();

        // The empty 13th name fills the slot 13-month calendars reserve; it must exist even when unused
        string[] monthNames = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", ""];

        culture.DateTimeFormat.AMDesignator = "ق.ظ";
        culture.DateTimeFormat.PMDesignator = "ب.ظ";
        culture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
        culture.DateTimeFormat.LongDatePattern = "d MMMM yyyy";
        culture.DateTimeFormat.MonthNames = monthNames;
        culture.DateTimeFormat.MonthGenitiveNames = monthNames;
        culture.DateTimeFormat.AbbreviatedMonthNames = monthNames;
        culture.DateTimeFormat.AbbreviatedMonthGenitiveNames = monthNames;
        culture.DateTimeFormat.AbbreviatedDayNames = ["ی", "د", "س", "چ", "پ", "ج", "ش"];
        culture.DateTimeFormat.ShortestDayNames = ["ی", "د", "س", "چ", "پ", "ج", "ش"];
        culture.DateTimeFormat.DayNames = ["یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه"];

        return culture;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_calendar")]
    private static extern ref Calendar Get_CalendarField(CultureInfo cultureInfo);

    public static void Apply(CultureInfo culture)
    {
        // The defaults are process-wide, so only the browser sets them; on the server they would leak into other requests
        if (OperatingSystem.IsBrowser())
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
