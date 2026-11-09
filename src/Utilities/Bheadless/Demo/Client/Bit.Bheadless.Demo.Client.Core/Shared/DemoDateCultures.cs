using System.Globalization;

namespace Bit.Bheadless.Demo.Client.Core.Shared;

/// <summary>
/// The cultures the date demos (calendar, date field, date picker) are written in. The WebAssembly demo runs with
/// InvariantGlobalization, where only the invariant culture exists and <see cref="CultureInfo.GetCultureInfo(string)"/>
/// throws: there, en-US and de-DE are built from the invariant culture with their date formats and names, and the
/// Persian culture - whose calendar needs ICU - is not available (<see cref="FaIr"/> is null). An app with ICU loaded
/// just uses <c>CultureInfo.GetCultureInfo("de-DE")</c>.
/// </summary>
public static class DemoDateCultures
{
    public static readonly CultureInfo EnUs = Get("en-US") ?? Build(dtf =>
    {
        dtf.ShortDatePattern = "M/d/yyyy";
        dtf.LongDatePattern = "dddd, MMMM d, yyyy";
        dtf.YearMonthPattern = "MMMM yyyy";
        dtf.ShortTimePattern = "h:mm tt";
        dtf.LongTimePattern = "h:mm:ss tt";
        dtf.FirstDayOfWeek = DayOfWeek.Sunday;
        dtf.ShortestDayNames = ["S", "M", "T", "W", "T", "F", "S"];
    });

    public static readonly CultureInfo DeDe = Get("de-DE") ?? Build(dtf =>
    {
        string[] months = ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember", ""];
        dtf.MonthNames = months;
        dtf.MonthGenitiveNames = months;
        dtf.AbbreviatedMonthNames = ["Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", ""];
        dtf.AbbreviatedMonthGenitiveNames = dtf.AbbreviatedMonthNames;
        dtf.DayNames = ["Sonntag", "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag"];
        dtf.AbbreviatedDayNames = ["So.", "Mo.", "Di.", "Mi.", "Do.", "Fr.", "Sa."];
        dtf.ShortestDayNames = ["S", "M", "D", "M", "D", "F", "S"];
        dtf.ShortDatePattern = "dd.MM.yyyy";
        dtf.LongDatePattern = "dddd, d. MMMM yyyy";
        dtf.YearMonthPattern = "MMMM yyyy";
        dtf.ShortTimePattern = "HH:mm";
        dtf.LongTimePattern = "HH:mm:ss";
        dtf.FirstDayOfWeek = DayOfWeek.Monday;
    });

    /// <summary>Persian (Iran): the Persian calendar, right to left. Null where ICU isn't loaded.</summary>
    public static readonly CultureInfo? FaIr = Get("fa-IR") is { DateTimeFormat.Calendar: PersianCalendar } fa ? fa : null;

    private static CultureInfo? Get(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static CultureInfo Build(Action<DateTimeFormatInfo> configure)
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        configure(culture.DateTimeFormat);
        return CultureInfo.ReadOnly(culture);
    }
}
