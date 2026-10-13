using System;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using Bit.BlazorUI.Tests.Components.Inputs.Calendar;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.DatePicker;

// The picker's calendar is laid out in the calendar of its culture and its input writes and reads the culture's own
// dates, while the value it hands back is the same instant whatever calendar it was picked or typed in. Every test runs
// in UTC so the value of a picked day is that day's midnight, whatever machine runs it.
[TestClass]
public class BitDatePickerCultureTests : BunitTestContext
{
    private const string NEXT_MONTH = "button[aria-label='Go to next month']";
    private const string PREV_MONTH = "button[aria-label='Go to previous month']";

    #region fa-IR (PersianCalendar)

    [TestMethod]
    public void BitDatePickerShouldShowThePersianMonthAndYearOfAFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(12)} 1403", GetMonthTitle(component));
        Assert.AreEqual("1403", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent.Trim());
    }

    [TestMethod,
        DataRow(2025, 3, 15, 1403, 30), // 1403 is a leap year of the Persian calendar: Esfand has thirty days
        DataRow(2024, 3, 10, 1402, 29)  // 1402 is a common one: Esfand has twenty-nine
    ]
    public void BitDatePickerShouldShowEveryDayOfEsfandInAFaIrCulture(int year, int month, int day, int persianYear, int daysInEsfand)
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(year, month, day));

        Assert.Contains(persianYear.ToString(CultureInfo.InvariantCulture), GetMonthTitle(component));
        CollectionAssert.AreEqual(Enumerable.Range(1, daysInEsfand).Select(d => d.ToString(CultureInfo.InvariantCulture)).ToArray(), GetInMonthDays(component));
    }

    [TestMethod]
    public void BitDatePickerShouldStartTheWeekOnSaturdayInAFaIrCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");

        var component = Render(culture, Utc(2025, 3, 15));

        var headers = component.FindAll(".bit-dtp-dgh .bit-dtp-wlb");

        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Saturday), headers[0].GetAttribute("aria-label"));
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(DayOfWeek.Friday), headers[6].GetAttribute("aria-label"));
        // The first of Esfand 1403 is a Wednesday, the fifth day of a week that starts on Saturday.
        Assert.AreEqual(4, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitDatePickerShouldNavigateAcrossThePersianNewYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        DateTimeOffset? changedMonth = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.OnMonthChange, (DateTimeOffset m) => changedMonth = m));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
        Assert.HasCount(31, GetInMonthDays(component));
        // Farvardin 1404 starts on Friday 21 March 2025, the last day of a week that starts on Saturday.
        Assert.AreEqual(6, GetColumnOfFirstDay(component));
        Assert.AreEqual(Utc(2025, 3, 21), changedMonth);

        component.Find(PREV_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(12)} 1403", GetMonthTitle(component));
        Assert.AreEqual(Utc(2025, 2, 19), changedMonth);
    }

    [TestMethod]
    public void BitDatePickerShouldSelectTheGregorianInstantOfAPersianDay()
    {
        DateTimeOffset? value = null;

        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, "30").Click();

        Assert.AreEqual(Utc(2025, 3, 20), value);
        // The input writes the value as a Persian date.
        Assert.AreEqual("1403/12/30", component.Find(".bit-dtp-inp").GetAttribute("value"));
        Assert.AreEqual("true", GetDayButton(component, "30").GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitDatePickerSelectingAnOutsideDayShouldMoveIntoTheNextPersianYear()
    {
        var culture = CultureInfo.GetCultureInfo("fa-IR");
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        component.FindAll(".bit-dtp-dbt.bit-dtp-dbo").First(b => b.TextContent.Trim() == "1").Click();

        Assert.AreEqual(Utc(2025, 3, 21), value);
        Assert.AreEqual("1404/1/1", component.Find(".bit-dtp-inp").GetAttribute("value"));
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1404", GetMonthTitle(component));
    }

    [TestMethod,
        DataRow("1404/1/1", 2025, 3, 21),
        DataRow("1403/12/30", 2025, 3, 20)
    ]
    public void BitDatePickerShouldReadATypedPersianDate(string typed, int year, int month, int day)
    {
        DateTimeOffset? value = null;

        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15), p =>
        {
            p.Add(x => x.AllowTextInput, true);
            p.Bind(x => x.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input(typed);

        Assert.AreEqual(Utc(year, month, day), value);
    }

    [TestMethod]
    public void BitDatePickerShouldRefuseTheLeapDayOfACommonPersianYear()
    {
        DateTimeOffset? value = null;

        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2024, 3, 10), p =>
        {
            p.Add(x => x.AllowTextInput, true);
            p.Bind(x => x.Value, value, v => value = v);
        });

        // Esfand 1402 has twenty-nine days.
        component.Find(".bit-dtp-inp").Input("1402/12/30");

        Assert.IsNull(value);
    }

    [TestMethod]
    public void BitDatePickerShouldFollowTheDirectionOfAFaIrCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("fa-IR"), Utc(2025, 3, 15));

        Assert.IsTrue(component.Find(".bit-dtp").ClassList.Contains("bit-rtl"));
        Assert.IsNull(component.Find(".bit-dtp").GetAttribute("dir"));
    }

    #endregion

    #region ar-SA (UmAlQuraCalendar)

    [TestMethod]
    public void BitDatePickerShouldShowTheUmAlQuraMonthOfAnArSaCulture()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var firstDayOfWeek = culture.DateTimeFormat.FirstDayOfWeek;

        var component = Render(culture, Utc(2025, 3, 15));

        // Ramadan 1446 of the Umm al-Qura calendar starts on Saturday 1 March 2025 and has twenty-nine days.
        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(9)} 1446", GetMonthTitle(component));
        Assert.HasCount(29, GetInMonthDays(component));
        Assert.AreEqual("1446", component.Find(".bit-dtp-mwp .bit-dtp-ptb").TextContent.Trim());
        Assert.AreEqual(culture.DateTimeFormat.GetDayName(firstDayOfWeek), component.FindAll(".bit-dtp-dgh .bit-dtp-wlb")[0].GetAttribute("aria-label"));
        Assert.AreEqual(((int)DayOfWeek.Saturday - (int)firstDayOfWeek + 7) % 7, GetColumnOfFirstDay(component));
    }

    [TestMethod]
    public void BitDatePickerShouldSelectTheGregorianInstantOfAnUmAlQuraDay()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        GetDayButton(component, "1").Click();

        Assert.AreEqual(Utc(2025, 3, 1), value);
        Assert.AreEqual(new DateTime(2025, 3, 1).ToString(culture.DateTimeFormat.ShortDatePattern, culture), component.Find(".bit-dtp-inp").GetAttribute("value"));
        Assert.Contains("1446", component.Find(".bit-dtp-inp").GetAttribute("value")!);
    }

    [TestMethod]
    public void BitDatePickerShouldReadBackTheUmAlQuraDateItWrites()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p =>
        {
            p.Add(x => x.AllowTextInput, true);
            p.Bind(x => x.Value, value, v => value = v);
        });

        component.Find(".bit-dtp-inp").Input(new DateTime(2025, 3, 29).ToString(culture.DateTimeFormat.ShortDatePattern, culture));

        Assert.AreEqual(Utc(2025, 3, 29), value);
    }

    [TestMethod]
    public void BitDatePickerShouldNavigateAcrossTheHijriNewYearOfAnArSaCulture()
    {
        var culture = CultureInfo.GetCultureInfo("ar-SA");
        var umAlQura = new UmAlQuraCalendar();

        var component = Render(culture, new DateTimeOffset(new DateTime(1446, 12, 10, umAlQura), TimeSpan.Zero));

        component.Find(NEXT_MONTH).Click();

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(1)} 1447", GetMonthTitle(component));
        Assert.HasCount(umAlQura.GetDaysInMonth(1447, 1), GetInMonthDays(component));
    }

    [TestMethod]
    public void BitDatePickerShouldFollowTheDirectionOfAnArSaCulture()
    {
        var component = Render(CultureInfo.GetCultureInfo("ar-SA"), Utc(2025, 3, 15), p => p.Add(x => x.IsOpen, true));

        Assert.IsTrue(component.Find(".bit-dtp").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-dtp-cal").ClassList.Contains("bit-dtp-rtl"));
        Assert.IsNull(component.Find(".bit-dtp-cal").GetAttribute("dir"));
    }

    #endregion

    #region HijriCalendar

    [TestMethod]
    public void BitDatePickerShouldSelectTheGregorianInstantOfAHijriDay()
    {
        var culture = BitCalendarCultureTests.CreateHijriCulture();
        var hijri = culture.Calendar;
        var march15 = new DateTime(2025, 3, 15);
        var hijriYear = hijri.GetYear(march15);
        var hijriMonth = hijri.GetMonth(march15);
        DateTimeOffset? value = null;

        var component = Render(culture, Utc(2025, 3, 15), p => p.Bind(x => x.Value, value, v => value = v));

        Assert.AreEqual($"{culture.DateTimeFormat.GetMonthName(hijriMonth)} {hijriYear}", GetMonthTitle(component));
        Assert.HasCount(hijri.GetDaysInMonth(hijriYear, hijriMonth), GetInMonthDays(component));

        GetDayButton(component, "1").Click();

        var expected = new DateTime(hijriYear, hijriMonth, 1, hijri);
        Assert.AreEqual(new DateTimeOffset(expected, TimeSpan.Zero), value);
        Assert.AreEqual(expected.ToString(culture.DateTimeFormat.ShortDatePattern, culture), component.Find(".bit-dtp-inp").GetAttribute("value"));
    }

    [TestMethod]
    public void BitDatePickerShouldFollowACalendarSetThroughTheDateTimeFormat()
    {
        // The documented way of switching the calendar of a culture is DateTimeFormat.Calendar: CultureInfo.Calendar is
        // read-only, which is why the demo's CultureInfoHelper has to reach for its private field instead.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
        culture.DateTimeFormat.Calendar = new HijriCalendar();
        var hijri = culture.DateTimeFormat.Calendar;
        var march15 = new DateTime(2025, 3, 15);

        var component = Render(culture, Utc(2025, 3, 15), p => p.Add(x => x.Value, Utc(2025, 3, 15)));

        var selected = component.Find(".bit-dtp-dbt[aria-selected='true']");

        Assert.AreEqual(march15.ToString(culture.DateTimeFormat.ShortDatePattern, culture), component.Find(".bit-dtp-inp").GetAttribute("value"));
        Assert.AreEqual(hijri.GetDayOfMonth(march15).ToString(CultureInfo.InvariantCulture), selected.TextContent.Trim());
    }

    #endregion

    private IRenderedComponent<BitDatePicker> Render(CultureInfo culture, DateTimeOffset startingValue, Action<ComponentParameterCollectionBuilder<BitDatePicker>>? more = null)
    {
        return RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.StartingValue, startingValue);
            more?.Invoke(parameters);
        });
    }

    private static string GetMonthTitle(IRenderedComponent<BitDatePicker> component)
    {
        return component.Find(".bit-dtp-dwp .bit-dtp-pkt, .bit-dtp-dwp .bit-dtp-ptb").TextContent.Trim();
    }

    private static string[] GetInMonthDays(IRenderedComponent<BitDatePicker> component)
    {
        return component.FindAll(".bit-dtp-dbt:not(.bit-dtp-dbo)").Select(b => b.TextContent.Trim()).ToArray();
    }

    private static IElement GetDayButton(IRenderedComponent<BitDatePicker> component, string day)
    {
        return component.FindAll(".bit-dtp-dbt:not(.bit-dtp-dbo)").First(b => b.TextContent.Trim() == day);
    }

    private static int GetColumnOfFirstDay(IRenderedComponent<BitDatePicker> component)
    {
        var cells = component.FindAll(".bit-dtp-dgr")[0].Children.Where(c => c.ClassList.Contains("bit-dtp-dbt") || c.ClassList.Contains("bit-dtp-dbe")).ToList();

        return cells.FindIndex(c => c.ClassList.Contains("bit-dtp-dbo") is false && c.TextContent.Trim() == "1");
    }

    [TestMethod]
    public void BitDatePickerShouldNameTheMonthsOfACommonYearOfAThirteenMonthCalendar()
    {
        // The month names of a Hebrew culture are the thirteen of a leap year, so a common year names its seventh
        // month onwards one place further along that table - as .NET's own formatter and BitCalendar do. 5785 is
        // a common year, and the tenth of April 2025 falls in its seventh month, which is Nisan. The calendar is
        // set the documented way, through DateTimeFormat.Calendar.
        var culture = CultureInfo.CreateSpecificCulture("he-IL");
        culture.DateTimeFormat.Calendar = new HebrewCalendar();

        var inNisan = new DateTime(2025, 4, 10);
        var nisan = inNisan.ToString("MMMM", culture);

        var component = RenderComponent<BitDatePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeZone, TimeZoneInfo.Utc);
            parameters.Add(p => p.Value, new DateTimeOffset(inNisan, TimeSpan.Zero));
        });

        // The seventh name of the table is the second Adar, which a common year does not have at all.
        Assert.AreNotEqual(nisan, culture.DateTimeFormat.GetMonthName(7));
        Assert.AreEqual(nisan, component.FindAll(".bit-dtp-pkb")[6].GetAttribute("title"));
        Assert.Contains(nisan, component.Find(".bit-dtp-ptb, .bit-dtp-pkt").TextContent);
    }

    private static DateTimeOffset Utc(int year, int month, int day) => new(new DateTime(year, month, day), TimeSpan.Zero);
}
