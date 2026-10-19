using System.Globalization;

namespace Bit.BlazorUI.Tests;

internal static class TestCultures
{
    public static CultureInfo ArSaUmAlQura { get; } = WithCalendar("ar-SA", new UmAlQuraCalendar());

    private static CultureInfo WithCalendar(string name, Calendar calendar)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        culture.DateTimeFormat.Calendar = calendar;
        return CultureInfo.ReadOnly(culture);
    }
}
