using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.Calendar;

// The calendar is laid out in the calendar of its culture - the months, their lengths, the year and the first day of the
// week are all that calendar's - while the value it hands back is the same instant whatever calendar it was picked in.
// Every test runs in UTC so the value of a picked day is that day's midnight, whatever machine runs it.
[TestClass]
public class BitCalendarCultureTests : BunitTestContext
{
    private const string NEXT_MONTH = "button[aria-label='Go to next month']";
    private const string PREV_MONTH = "button[aria-label='Go to previous month']";

    #region fa-IR (PersianCalendar)

    [TestMethod]
    public void BitCalendarShouldShowThePersianMonthAndYearOfAFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        // 15 March 2025 is the 25th of Esfand 1403.
        var component = Render(culture, Utc(2025, 3, 15));

        Assert.IsInstanceOfType<PersianCalendar>(culture.Calendar);
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(12)} 1403", GetMonthTitle(component));
        Assert.AreEqual(GetMonthTitle(component), component.Find(".bit-cal-dwp .bit-cal-grd").GetAttribute("aria-label"));
        Assert.AreEqual("1403", component.Find(".bit-cal-mwp .bit-cal-ptb").TextContent.Trim());
    }

    [TestMethod,
        DataRow(2025, 3, 15, 1403, 30), // 1403 is a leap year of the Persian calendar: Esfand has thirty days
        DataRow(2024, 3, 10, 1402, 29), // 1402 is a common one: Esfand has twenty-nine
        DataRow(2026, 3, 10, 1404, 29)
    ]
    public void BitCalendarShouldShowEveryDayOfEsfandInAFaIrCulture(int year, int month, int day, int persianYear, int daysInEsfand)
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(year, month, day));

        var days = GetInMonthDays(component);

        Assert.Contains(persianYear.ToString(CultureInfo.InvariantCulture), GetMonthTitle(component));
        Assert.HasCount(daysInEsfand, days);
        CollectionAssert.AreEqual(Enumerable.Range(1, daysInEsfand).Select(d => d.ToString(CultureInfo.InvariantCulture)).ToArray(), days);
    }

    [TestMethod]
    public void BitCalendarShouldStartTheWeekOnSaturdayInAFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        var headers = component.FindAll(".bit-cal-dgh .bit-cal-wlb");

        Assert.AreEqual(DayOfWeek.Saturday, culture.DateTimeFormat.FirstDayOfWeek);
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Saturday), headers[0].GetAttribute("aria-label"));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Friday), headers[6].GetAttribute("aria-label"));

        // The first of Esfand 1403 is a Wednesday (19 February 2025), the fifth day of a week that starts on Saturday.
        Assert.AreEqual(4, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitCalendarFirstDayOfWeekShouldWinOverTheFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.FirstDayOfWeek, DayOfWeek.Monday));

        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Monday), component.FindAll(".bit-cal-dgh .bit-cal-wlb")[0].GetAttribute("aria-label"));
        Assert.AreEqual(2, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitCalendarShouldNavigateAcrossThePersianNewYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        DateTimeOffset? changedMonth = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.OnMonthChange, (DateTimeOffset m) => changedMonth = m));

        component.Find(NEXT_MONTH).Click();

        // Farvardin 1404 starts on Friday 21 March 2025 and has thirty-one days.
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
        Assert.AreEqual(6, GetColumnOfFirstDay(component));
        Assert.AreEqual("1404", component.Find(".bit-cal-mwp .bit-cal-ptb").TextContent.Trim());
        Assert.AreEqual(Utc(2025, 3, 21), changedMonth);

        component.Find(PREV_MONTH).Click();
        component.Find(PREV_MONTH).Click();

        // Bahman 1403 has thirty days.
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(11)} 1403", GetMonthTitle(component));
        Assert.HasCount(30, GetInMonthDays(component));
        Assert.AreEqual(Utc(2025, 1, 20), changedMonth);
    }

    [TestMethod]
    public void BitCalendarShouldSelectTheGregorianInstantOfAPersianDay()
    {
        DateTimeOffset? value = null;

        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        // The 30th of Esfand 1403, the leap day of the Persian calendar, is 20 March 2025.
        GetDayButton(component, "30").Click();

        Assert.AreEqual(Utc(2025, 3, 20), value);
        Assert.AreEqual(TimeSpan.Zero, value!.Value.Offset);
        Assert.AreEqual("30", component.Find(".bit-cal-dbt[aria-selected='true']").TextContent.Trim());
        // The selection is announced in the culture's own short date.
        Assert.Contains("1403/12/30", component.Find(".bit-cal-sdt").TextContent);
    }

    [TestMethod]
    public void BitCalendarSelectingAnOutsideDayShouldMoveIntoTheNextPersianYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        // The days trailing Esfand 1403 in its grid are the first days of Farvardin 1404.
        component.FindAll(".bit-cal-dbt.bit-cal-dbo").First(b => b.TextContent.Trim() == "1").Click();

        Assert.AreEqual(Utc(2025, 3, 21), value);
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
    }

    [TestMethod]
    public void BitCalendarShouldLabelThePersianDaysWithTheirPersianDate()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        var label = GetDayButton(component, "30").GetAttribute("aria-label");

        Assert.AreEqual(new DateTime(2025, 3, 20).ToString(culture.DateTimeFormat.LongDatePattern, culture), label);
        Assert.Contains("1403", label!);
    }

    [TestMethod]
    public void BitCalendarShouldMarkThePersianToday()
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p => p.Add(x => x.Today, Utc(2025, 3, 15)));

        var today = component.Find(".bit-cal-dbt[aria-current='date']");

        Assert.AreEqual("25", today.TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarMonthPickerShouldListThePersianMonths()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        var months = component.FindAll(".bit-cal-mwp .bit-cal-pkb");

        CollectionAssert.AreEqual(culture.DateTimeFormat.MonthNames.Take(12).ToArray(), months.Select(m => m.GetAttribute("title")).ToArray());
        Assert.AreEqual("true", months[11].GetAttribute("aria-selected"));

        months[0].Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1403", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
    }

    [TestMethod]
    public void BitCalendarYearPickerShouldListThePersianYears()
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15));

        component.Find(".bit-cal-mwp .bit-cal-ptb").Click();

        var years = component.FindAll(".bit-cal-mwp .bit-cal-pkb").Select(y => y.TextContent.Trim()).ToArray();

        Assert.AreEqual("1402", years[0]);
        Assert.Contains("1403", years);
        Assert.AreEqual("true", component.FindAll(".bit-cal-mwp .bit-cal-pkb").First(y => y.TextContent.Trim() == "1403").GetAttribute("aria-selected"));
    }

    #endregion

    #region ar-SA (UmAlQuraCalendar)

    [TestMethod]
    public void BitCalendarShouldShowTheUmAlQuraMonthOfAnArSaCulture()
    {
        var culture = TestCultures.ArSaUmAlQura;

        // 15 March 2025 is the 15th of Ramadan 1446 in the Umm al-Qura calendar, whose Ramadan 1446 has 29 days.
        var component = Render(culture, Utc(2025, 3, 15));

        Assert.IsInstanceOfType<UmAlQuraCalendar>(culture.DateTimeFormat.Calendar);
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(9)} 1446", GetMonthTitle(component));
        Assert.HasCount(29, GetInMonthDays(component));
        Assert.AreEqual("1446", component.Find(".bit-cal-mwp .bit-cal-ptb").TextContent.Trim());
    }

    [TestMethod]
    public void BitCalendarShouldStartTheWeekOnTheFirstDayOfAnArSaCulture()
    {
        var culture = TestCultures.ArSaUmAlQura;
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        var component = Render(culture, Utc(2025, 3, 15));

        Assert.AreEqual(culture.DateTimeFormat.GetDayName(firstDayOfWeek), component.FindAll(".bit-cal-dgh .bit-cal-wlb")[0].GetAttribute("aria-label"));
        // The first of Ramadan 1446 (Umm al-Qura) is Saturday 1 March 2025.
        Assert.AreEqual(((int)DayOfWeek.Saturday - (int)firstDayOfWeek + 7) % 7, GetColumnOfFirstDay(component));
    }

    [TestMethod,
        DataRow("1", 2025, 3, 1),
        DataRow("29", 2025, 3, 29)
    ]
    public void BitCalendarShouldSelectTheGregorianInstantOfAnUmAlQuraDay(string day, int year, int month, int dayOfMonth)
    {
        DateTimeOffset? value = null;

        var component = Render(TestCultures.ArSaUmAlQura, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, day).Click();

        Assert.AreEqual(Utc(year, month, dayOfMonth), value);
    }

    [TestMethod]
    public void BitCalendarShouldNavigateAcrossTheHijriNewYearOfAnArSaCulture()
    {
        var culture = TestCultures.ArSaUmAlQura;
        var umAlQura = new UmAlQuraCalendar();

        var component = Render(culture, Utc(1446, 12, 10, umAlQura));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(12)} 1446", GetMonthTitle(component));
        Assert.HasCount(umAlQura.GetDaysInMonth(1446, 12), GetInMonthDays(component));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1447", GetMonthTitle(component));
        Assert.HasCount(umAlQura.GetDaysInMonth(1447, 1), GetInMonthDays(component));
    }

    [TestMethod]
    public void BitCalendarShouldFollowTheDirectionOfAnArSaCulture()
    {
        var rtl = Render(CultureInfo.GetCultureInfo("ar-SA"), Utc(2025, 3, 15));
        var ltr = Render(CultureInfo.GetCultureInfo("ar-SA"), Utc(2025, 3, 15), p => p.Add(x => x.Dir, BitDir.Ltr));

        Assert.IsTrue(rtl.Find(".bit-cal").ClassList.Contains("bit-rtl"));
        Assert.IsNull(rtl.Find(".bit-cal").GetAttribute("dir"));
        Assert.IsFalse(ltr.Find(".bit-cal").ClassList.Contains("bit-rtl"));
        Assert.AreEqual("ltr", ltr.Find(".bit-cal").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitCalendarShouldFollowTheDirectionOfAFaIrCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15));

        Assert.IsTrue(component.Find(".bit-cal").ClassList.Contains("bit-rtl"));
    }

    #endregion

    #region HijriCalendar

    [TestMethod]
    public void BitCalendarShouldLayOutTheMonthsOfAHijriCalendar()
    {
        var culture = CreateHijriCulture();
        var hijri = culture.Calendar;
        var march15 = new DateTime(2025, 3, 15);
        var hijriYear = hijri.GetYear(march15);
        var hijriMonth = hijri.GetMonth(march15);

        var component = Render(culture, Utc(2025, 3, 15));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(hijriMonth)} {hijriYear}", GetMonthTitle(component));
        Assert.HasCount(hijri.GetDaysInMonth(hijriYear, hijriMonth), GetInMonthDays(component));
        // The tabular Hijri calendar and the Umm al-Qura one do not agree on the length of Ramadan 1446.
        Assert.AreNotEqual(new UmAlQuraCalendar().GetDaysInMonth(1446, 9), GetInMonthDays(component).Length);
    }

    [TestMethod]
    public void BitCalendarShouldSelectTheGregorianInstantOfAHijriDay()
    {
        var culture = CreateHijriCulture();
        var hijri = culture.Calendar;
        var march15 = new DateTime(2025, 3, 15);
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, "1").Click();

        var expected = new DateTime(hijri.GetYear(march15), hijri.GetMonth(march15), 1, hijri);
        Assert.AreEqual(new DateTimeOffset(expected, TimeSpan.Zero), value);
        Assert.AreEqual(expected.ToString(culture.DateTimeFormat.LongDatePattern, culture), GetDayButton(component, "1").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitCalendarShouldFollowACalendarSetThroughTheDateTimeFormat()
    {
        // The documented way of switching the calendar of a culture is DateTimeFormat.Calendar: CultureInfo.Calendar is
        // read-only, which is why the demo's CultureInfoHelper has to reach for its private field instead.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
        culture.DateTimeFormat.Calendar = new HijriCalendar();
        var hijri = culture.DateTimeFormat.Calendar;
        var march15 = new DateTime(2025, 3, 15);

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.Value, Utc(2025, 3, 15)));

        var selected = component.Find(".bit-cal-dbt[aria-selected='true']");

        // The day a cell shows and the date it is labelled with are the same day of the same calendar.
        Assert.AreEqual(march15.ToString(culture.DateTimeFormat.LongDatePattern, culture), selected.GetAttribute("aria-label"));
        Assert.AreEqual(hijri.GetDayOfMonth(march15).ToString(CultureInfo.InvariantCulture), selected.TextContent.Trim());
        Assert.HasCount(hijri.GetDaysInMonth(hijri.GetYear(march15), hijri.GetMonth(march15)), GetInMonthDays(component));
    }

    #endregion

    #region de-DE, en-US and the invariant culture (GregorianCalendar)

    [TestMethod]
    public void BitCalendarShouldStartTheWeekOnMondayAndNameTheMonthsOfADeDeCulture()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");

        var component = Render(culture, Utc(2025, 3, 15));

        var headers = component.FindAll(".bit-cal-dgh .bit-cal-wlb");

        Assert.IsInstanceOfType<GregorianCalendar>(culture.Calendar);
        Assert.AreEqual(DayOfWeek.Monday, culture.DateTimeFormat.FirstDayOfWeek);
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(3)} 2025", GetMonthTitle(component));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Monday), headers[0].GetAttribute("aria-label"));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Sunday), headers[6].GetAttribute("aria-label"));
        Assert.HasCount(31, GetInMonthDays(component));

        // 1 March 2025 is a Saturday, the sixth day of a week that starts on Monday.
        Assert.AreEqual(5, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitCalendarShouldAnnounceTheSelectionInTheShortDateOfADeDeCulture()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, "15").Click();

        Assert.AreEqual(Utc(2025, 3, 15), value);
        Assert.AreEqual(new DateTime(2025, 3, 15).ToString(culture.DateTimeFormat.LongDatePattern, culture), GetDayButton(component, "15").GetAttribute("aria-label"));
        // The selection is announced in the culture's own short date - day first, dotted.
        Assert.Contains("15.03.2025", component.Find(".bit-cal-sdt").TextContent);
    }

    [TestMethod]
    public void BitCalendarDateFormatShouldWinOverTheShortDatePatternOfTheCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("de-DE"), Utc(2025, 3, 15), p =>
        {
            p.Add(x => x.DateFormat, "yyyy-MM-dd");
            p.Add(x => x.Value, Utc(2025, 3, 15));
        });

        Assert.Contains("2025-03-15", component.Find(".bit-cal-sdt").TextContent);
        Assert.IsFalse(component.Find(".bit-cal-sdt").TextContent.Contains("15.03.2025"));
    }

    [TestMethod]
    public void BitCalendarShouldStartTheWeekOnSundayInAnEnUsCulture()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var component = Render(culture, Utc(2025, 3, 15));

        var headers = component.FindAll(".bit-cal-dgh .bit-cal-wlb");

        Assert.AreEqual(DayOfWeek.Sunday, culture.DateTimeFormat.FirstDayOfWeek);
        Assert.AreEqual("March 2025", GetMonthTitle(component));
        Assert.AreEqual("Sunday", headers[0].GetAttribute("aria-label"));
        Assert.AreEqual("Saturday", headers[6].GetAttribute("aria-label"));

        // 1 March 2025 is a Saturday, the last day of a week that starts on Sunday.
        Assert.AreEqual(6, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitCalendarInvariantCultureShouldLayOutTheGregorianMonthAndAnnounceTheIsoLikeShortDate()
    {
        DateTimeOffset? value = null;

        var component = Render(CultureInfo.InvariantCulture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        Assert.AreEqual("March 2025", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
        Assert.AreEqual(6, GetColumnOfFirstDay(component));
        Assert.IsFalse(component.Find(".bit-cal").ClassList.Contains("bit-rtl"));

        GetDayButton(component, "15").Click();

        Assert.AreEqual(Utc(2025, 3, 15), value);
        Assert.Contains("03/15/2025", component.Find(".bit-cal-sdt").TextContent);
    }

    [TestMethod]
    public void BitCalendarShouldNavigateAcrossTheGregorianNewYearInADeDeCulture()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        DateTimeOffset? changedMonth = null;

        var component = Render(culture, Utc(2025, 12, 15), p => p.Add(x => x.OnMonthChange, (DateTimeOffset m) => changedMonth = m));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 2026", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
        Assert.AreEqual(Utc(2026, 1, 1), changedMonth);
        Assert.AreEqual("2026", component.Find(".bit-cal-mwp .bit-cal-ptb").TextContent.Trim());
    }

    #endregion

    private IRenderedComponent<BitCalendar> Render(CultureInfo culture, DateTimeOffset startingValue, Action<ComponentParameterCollectionBuilder<BitCalendar>>? more = null)
    {
        return RenderComponent<BitCalendar>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, startingValue);
            more?.Invoke(parameters);
        });
    }

    private static string GetMonthTitle(IRenderedComponent<BitCalendar> component)
    {
        return component.Find(".bit-cal-dwp .bit-cal-pkt, .bit-cal-dwp .bit-cal-ptb").TextContent.Trim();
    }

    private static string[] GetInMonthDays(IRenderedComponent<BitCalendar> component)
    {
        return component.FindAll(".bit-cal-dbt:not(.bit-cal-dbo)").Select(b => b.TextContent.Trim()).ToArray();
    }

    private static IElement GetDayButton(IRenderedComponent<BitCalendar> component, string day)
    {
        return component.FindAll(".bit-cal-dbt:not(.bit-cal-dbo)").First(b => b.TextContent.Trim() == day);
    }

    // The column the first day of the month sits in, counted from the first day of the week.
    private static int GetColumnOfFirstDay(IRenderedComponent<BitCalendar> component)
    {
        var cells = component.FindAll(".bit-cal-dgr")[0].Children.Where(c => c.ClassList.Contains("bit-cal-dbt") || c.ClassList.Contains("bit-cal-dbe")).ToList();

        return cells.FindIndex(c => c.ClassList.Contains("bit-cal-dbo") is false && c.TextContent.Trim() == "1");
    }

    private static DateTimeOffset Utc(int year, int month, int day) => new(new DateTime(year, month, day), TimeSpan.Zero);

    private static DateTimeOffset Utc(int year, int month, int day, System.Globalization.Calendar calendar) => new(new DateTime(year, month, day, calendar), TimeSpan.Zero);

    // A culture whose own calendar is the tabular Hijri one. CultureInfo.Calendar is read-only, so it is replaced the
    // way the demo's CultureInfoHelper replaces it for the Persian calendar.
    internal static CultureInfo CreateHijriCulture()
    {
        var culture = CultureInfo.CreateSpecificCulture("ar-SA");
        culture.GetType().GetField("_calendar", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(culture, new HijriCalendar());
        culture.DateTimeFormat.Calendar = new HijriCalendar();

        return culture;
    }
}
