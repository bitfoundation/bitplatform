using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Names a month of a year the way the culture's own formatter spells it, for the pickers that list the months of
/// a year (BitCalendar, BitDatePicker, BitDateRangePicker) - one rule, since a month name read off the culture's
/// table directly can differ from the one the very date being shown is formatted with.
/// </summary>
internal static class BitMonthNames
{
    /// <summary>
    /// The full name of the month, as the culture's formatter spells a date of that month.
    /// </summary>
    public static string GetMonthName(CultureInfo culture, int year, int month)
        => culture.DateTimeFormat.GetMonthName(GetMonthNameIndex(culture, year, month));

    /// <summary>
    /// The abbreviated name of the month, as the culture's formatter spells a date of that month.
    /// </summary>
    public static string GetAbbreviatedMonthName(CultureInfo culture, int year, int month)
        => culture.DateTimeFormat.GetAbbreviatedMonthName(GetMonthNameIndex(culture, year, month));

    /// <summary>
    /// The index into the culture's month-name table of the given month of the given year. A culture whose month
    /// names are the thirteen of a Hebrew leap year names the months of a common year one place further along
    /// that table from the seventh on - the thirteenth name is the second Adar, which a common year does not have -
    /// and .NET's own formatter makes the same shift.
    /// </summary>
    public static int GetMonthNameIndex(CultureInfo culture, int year, int month)
    {
        if (month < 7) return month;

        // Only a culture whose table really does hold thirteen names shifts: everywhere else the thirteenth entry
        // is the empty string every DateTimeFormatInfo pads its twelve names with. It is read through
        // GetMonthName(13) rather than MonthNames, whose getter clones the whole table on every call.
        if (culture.DateTimeFormat.GetMonthName(13).HasNoValue()) return month;

        return culture.DateTimeFormat.Calendar.GetMonthsInYear(year) == 13 ? month : month + 1;
    }
}
