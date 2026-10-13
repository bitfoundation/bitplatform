using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bit.BlazorUI.Tests.Components.Inputs.Calendar;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DateRangePicker;

// The picker's calendar is laid out in the calendar of its culture and its input writes and reads the culture's own
// dates, while the range it hands back is made of the same instants whatever calendar they were picked or typed in.
// Every test runs in UTC so the value of a picked day is that day's midnight, whatever machine runs it.
[TestClass]
public class BitDateRangePickerCultureTests : BunitTestContext
{
    private const string NEXT_MONTH = "button[aria-label='Go to next month']";

    private static readonly TimeSpan EndOfDay = new(23, 59, 0);

    #region fa-IR (PersianCalendar)

    [TestMethod,
        DataRow(2025, 3, 15, 1403, 30), // 1403 is a leap year of the Persian calendar: Esfand has thirty days
        DataRow(2024, 3, 10, 1402, 29)  // 1402 is a common one: Esfand has twenty-nine
    ]
    public void BitDateRangePickerShouldShowEveryDayOfEsfandInAFaIrCulture(int year, int month, int day, int persianYear, int daysInEsfand)
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(year, month, day));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(12)} {persianYear}", GetMonthTitle(component));
        Assert.AreEqual(persianYear.ToString(CultureInfo.InvariantCulture), component.Find(".bit-dtrp-mwp .bit-dtrp-ptb").TextContent.Trim());
        CollectionAssert.AreEqual(Enumerable.Range(1, daysInEsfand).Select(d => d.ToString(CultureInfo.InvariantCulture)).ToArray(), GetInMonthDays(component));
    }

    [TestMethod]
    public void BitDateRangePickerShouldStartTheWeekOnSaturdayInAFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        var headers = component.FindAll(".bit-dtrp-dgh .bit-dtrp-wlb");

        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Saturday), headers[0].GetAttribute("aria-label"));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Friday), headers[6].GetAttribute("aria-label"));
        // The first of Esfand 1403 is a Wednesday, the fifth day of a week that starts on Saturday.
        Assert.AreEqual(4, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNavigateAcrossThePersianNewYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        DateTimeOffset? changedMonth = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.OnMonthChange, (DateTimeOffset m) => changedMonth = m));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
        Assert.AreEqual(6, GetColumnOfFirstDay(component));
        Assert.AreEqual(Utc(2025, 3, 21), changedMonth);
    }

    [TestMethod]
    public void BitDateRangePickerShouldSelectARangeAcrossThePersianNewYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        BitDateRangePickerValue? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        // The last day of 1403 and, among the days trailing it in the grid, the first day of 1404.
        GetDayButton(component, "30").Click();
        component.FindAll(".bit-dtrp-dbt.bit-dtrp-dbo").First(b => b.TextContent.Trim() == "1").Click();

        Assert.IsNotNull(value);
        Assert.AreEqual(Utc(2025, 3, 20), value.StartDate);
        // A picked end day ends at the last minute of the day unless the end time says otherwise.
        Assert.AreEqual(Utc(2025, 3, 21).Add(EndOfDay), value.EndDate);
        Assert.AreEqual("Start: 1403/12/30 - End: 1404/1/1", component.Find(".bit-dtrp-inp").GetAttribute("value"));
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
    }

    [TestMethod]
    public void BitDateRangePickerShouldReadATypedPersianRange()
    {
        BitDateRangePickerValue? value = null;

        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p =>
        {
            p.Add(x => x.AllowTextInput, true);
            p.Bind(x => x.Value, value, v => value = v);
        });

        component.Find(".bit-dtrp-inp").Change("Start: 1403/12/30 - End: 1404/1/13");

        Assert.IsNotNull(value);
        Assert.AreEqual(Utc(2025, 3, 20), value.StartDate);
        Assert.AreEqual(Utc(2025, 4, 2), value.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerShouldFollowTheDirectionOfAFaIrCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p => p.Add(x => x.IsOpen, true));

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dtrp-rtl"));
    }

    #endregion

    #region ar-SA (UmAlQuraCalendar)

    [TestMethod]
    public void BitDateRangePickerShouldShowTheUmAlQuraMonthOfAnArSaCulture()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        var component = Render(culture, Utc(2025, 3, 15));

        // Ramadan 1446 of the Umm al-Qura calendar starts on Saturday 1 March 2025 and has twenty-nine days.
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(9)} 1446", GetMonthTitle(component));
        Assert.HasCount(29, GetInMonthDays(component));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(firstDayOfWeek), component.FindAll(".bit-dtrp-dgh .bit-dtrp-wlb")[0].GetAttribute("aria-label"));
        Assert.AreEqual(((int)DayOfWeek.Saturday - (int)firstDayOfWeek + 7) % 7, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitDateRangePickerShouldSelectTheGregorianInstantsOfAnUmAlQuraRange()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        BitDateRangePickerValue? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, "1").Click();
        GetDayButton(component, "29").Click();

        Assert.IsNotNull(value);
        Assert.AreEqual(Utc(2025, 3, 1), value.StartDate);
        Assert.AreEqual(Utc(2025, 3, 29).Add(EndOfDay), value.EndDate);

        var pattern = culture.DateTimeFormat.ShortDatePattern;
        Assert.AreEqual($"Start: {new DateTime(2025, 3, 1).ToString(pattern, culture)} - End: {new DateTime(2025, 3, 29).ToString(pattern, culture)}",
                        component.Find(".bit-dtrp-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitDateRangePickerShouldNavigateAcrossTheHijriNewYearOfAnArSaCulture()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var umAlQura = new UmAlQuraCalendar();

        var component = Render(culture, new DateTimeOffset(new DateTime(1446, 12, 10, umAlQura), TimeSpan.Zero));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1447", GetMonthTitle(component));
        Assert.HasCount(umAlQura.GetDaysInMonth(1447, 1), GetInMonthDays(component));
    }

    [TestMethod]
    public void BitDateRangePickerShouldFollowTheDirectionOfAnArSaCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("ar-SA"), Utc(2025, 3, 15), p => p.Add(x => x.IsOpen, true));

        Assert.IsTrue(component.Find(".bit-dtrp").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-dtrp-cal").ClassList.Contains("bit-dtrp-rtl"));
        Assert.IsNull(component.Find(".bit-dtrp-cal").GetAttribute("dir"));
    }

    #endregion

    #region HijriCalendar

    [TestMethod]
    public void BitDateRangePickerShouldSelectTheGregorianInstantsOfAHijriRange()
    {
        var culture = BitCalendarCultureTests.CreateHijriCulture();
        var hijri = culture.Calendar;
        var march15 = new DateTime(2025, 3, 15);
        var hijriYear = hijri.GetYear(march15);
        var hijriMonth = hijri.GetMonth(march15);
        var daysInMonth = hijri.GetDaysInMonth(hijriYear, hijriMonth);
        BitDateRangePickerValue? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(hijriMonth)} {hijriYear}", GetMonthTitle(component));
        Assert.HasCount(daysInMonth, GetInMonthDays(component));

        GetDayButton(component, "1").Click();
        GetDayButton(component, daysInMonth.ToString(CultureInfo.InvariantCulture)).Click();

        Assert.IsNotNull(value);
        Assert.AreEqual(new DateTimeOffset(new DateTime(hijriYear, hijriMonth, 1, hijri), TimeSpan.Zero), value.StartDate);
        Assert.AreEqual(new DateTimeOffset(new DateTime(hijriYear, hijriMonth, daysInMonth, hijri), TimeSpan.Zero).Add(EndOfDay), value.EndDate);
    }

    [TestMethod]
    public void BitDateRangePickerShouldFollowACalendarSetThroughTheDateTimeFormat()
    {
        // The documented way of switching the calendar of a culture is DateTimeFormat.Calendar: CultureInfo.Calendar is
        // read-only, which is why the demo's CultureInfoHelper has to reach for its private field instead.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
        culture.DateTimeFormat.Calendar = new HijriCalendar();
        var hijri = culture.DateTimeFormat.Calendar;
        var march15 = new DateTime(2025, 3, 15);

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.Value, new BitDateRangePickerValue { StartDate = Utc(2025, 3, 15) }));

        var selected = component.Find(".bit-dtrp-dbt[aria-selected='true']");

        Assert.AreEqual(march15.ToString(culture.DateTimeFormat.LongDatePattern, culture), selected.GetAttribute("aria-label"));
        Assert.AreEqual(hijri.GetDayOfMonth(march15).ToString(CultureInfo.InvariantCulture), selected.TextContent.Trim());
    }

    #endregion

    private IRenderedComponent<BitDateRangePicker> Render(CultureInfo culture, DateTimeOffset startingDate, Action<ComponentParameterCollectionBuilder<BitDateRangePicker>>? more = null)
    {
        return RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, new BitDateRangePickerValue { StartDate = startingDate });
            more?.Invoke(parameters);
        });
    }

    private static string GetMonthTitle(IRenderedComponent<BitDateRangePicker> component)
    {
        return component.Find(".bit-dtrp-dwp .bit-dtrp-pkt, .bit-dtrp-dwp .bit-dtrp-ptb").TextContent.Trim();
    }

    private static string[] GetInMonthDays(IRenderedComponent<BitDateRangePicker> component)
    {
        return component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)").Select(b => b.TextContent.Trim()).ToArray();
    }

    private static IElement GetDayButton(IRenderedComponent<BitDateRangePicker> component, string day)
    {
        return component.FindAll(".bit-dtrp-dbt:not(.bit-dtrp-dbo)").First(b => b.TextContent.Trim() == day);
    }

    private static int GetColumnOfFirstDay(IRenderedComponent<BitDateRangePicker> component)
    {
        var cells = component.FindAll(".bit-dtrp-dgr")[0].Children.Where(c => c.ClassList.Contains("bit-dtrp-dbt") || c.ClassList.Contains("bit-dtrp-dbe")).ToList();

        return cells.FindIndex(c => c.ClassList.Contains("bit-dtrp-dbo") is false && c.TextContent.Trim() == "1");
    }

    [TestMethod]
    public void BitDateRangePickerShouldNameTheMonthsOfACommonYearOfAThirteenMonthCalendar()
    {
        // The month names of a Hebrew culture are the thirteen of a leap year, so a common year names its seventh
        // month onwards one place further along that table - as .NET's own formatter and BitCalendar do. 5785 is
        // a common year, and the tenth of April 2025 falls in its seventh month, which is Nisan. The calendar is
        // set the documented way, through DateTimeFormat.Calendar.
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        culture.DateTimeFormat.Calendar = new HebrewCalendar();

        var inNisan = new DateTime(2025, 4, 10);
        var nisan = inNisan.ToString("MMMM", culture);

        var component = RenderComponent<BitDateRangePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Value, new BitDateRangePickerValue { StartDate = new DateTimeOffset(inNisan, TimeSpan.Zero), EndDate = new DateTimeOffset(inNisan, TimeSpan.Zero) });
        });

        // The seventh name of the table is the second Adar, which a common year does not have at all.
        Assert.AreNotEqual(nisan, culture.DateTimeFormat.GetMonthName(7));
        Assert.AreEqual(nisan, component.FindAll(".bit-dtrp-pkb")[6].GetAttribute("title"));
        Assert.Contains(nisan, component.Find(".bit-dtrp-ptb, .bit-dtrp-pkt").TextContent);
    }

    private static DateTimeOffset Utc(int year, int month, int day) => new(new DateTime(year, month, day), TimeSpan.Zero);
}
