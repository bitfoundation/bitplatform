using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the date/grid maths every calendar view is laid out from: the hidden-day and
/// first-day-of-week shape of a grid, the era-safe month/year anchors, the visible-hour window and
/// its slots, and the overlap rules the placement helpers share.
/// </summary>
[TestClass]
public class BitFullCalendarHelpersTests
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly CultureInfo EnUs = new("en-US");

    private static BitFullCalendarEvent Event(DateTime start, DateTime end, string id = "e1") =>
        new() { Id = id, Title = id, StartDate = start, EndDate = end };

    #region Hidden days and first day of week

    [TestMethod]
    public void NormalizeHiddenDaysShouldDropDuplicatesAndUndefinedValues()
    {
        var result = BitFullCalendarHelpers.NormalizeHiddenDays(
            [DayOfWeek.Saturday, DayOfWeek.Saturday, (DayOfWeek)42, DayOfWeek.Sunday]);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(result.Contains(DayOfWeek.Sunday));
    }

    [TestMethod]
    public void NormalizeHiddenDaysShouldRefuseASetThatHidesEveryDay()
    {
        var all = Enum.GetValues<DayOfWeek>().ToArray();

        Assert.AreEqual(0, BitFullCalendarHelpers.NormalizeHiddenDays(all).Count);
    }

    [TestMethod]
    public void NormalizeHiddenDaysShouldTreatNullAndEmptyAsNothingHidden()
    {
        Assert.AreEqual(0, BitFullCalendarHelpers.NormalizeHiddenDays(null).Count);
        Assert.AreEqual(0, BitFullCalendarHelpers.NormalizeHiddenDays([]).Count);
    }

    [TestMethod]
    public void ResolveFirstDayOfWeekShouldPreferTheExplicitOverride()
    {
        Assert.AreEqual(DayOfWeek.Monday, BitFullCalendarHelpers.ResolveFirstDayOfWeek(EnUs, DayOfWeek.Monday));
        Assert.AreEqual(EnUs.DateTimeFormat.FirstDayOfWeek, BitFullCalendarHelpers.ResolveFirstDayOfWeek(EnUs));
    }

    [TestMethod]
    public void ResolveFirstDayOfWeekShouldIgnoreAnUndefinedOverride()
    {
        Assert.AreEqual(EnUs.DateTimeFormat.FirstDayOfWeek, BitFullCalendarHelpers.ResolveFirstDayOfWeek(EnUs, (DayOfWeek)42));
    }

    [TestMethod]
    public void GetVisibleWeekDayCountShouldFollowTheHiddenDaySet()
    {
        Assert.AreEqual(7, BitFullCalendarHelpers.GetVisibleWeekDayCount(null));
        Assert.AreEqual(5, BitFullCalendarHelpers.GetVisibleWeekDayCount([DayOfWeek.Saturday, DayOfWeek.Sunday]));
    }

    [TestMethod]
    public void GetWeekDatesShouldStartOnTheOverriddenDayAndDropHiddenOnes()
    {
        // 2024-05-15 is a Wednesday.
        var dates = BitFullCalendarHelpers.GetWeekDates(
            new DateTime(2024, 5, 15), EnUs, DayOfWeek.Monday, [DayOfWeek.Saturday, DayOfWeek.Sunday]);

        Assert.AreEqual(5, dates.Length);
        Assert.AreEqual(new DateTime(2024, 5, 13), dates[0]);
        Assert.AreEqual(new DateTime(2024, 5, 17), dates[^1]);
        Assert.IsFalse(dates.Any(d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
    }

    [TestMethod]
    public void GetWeekDayHeadersShouldMatchTheVisibleColumns()
    {
        var headers = BitFullCalendarHelpers.GetWeekDayHeaders(EnUs, DayOfWeek.Monday, [DayOfWeek.Sunday]);

        Assert.AreEqual(6, headers.Length);
        Assert.AreEqual(EnUs.DateTimeFormat.GetShortestDayName(DayOfWeek.Monday), headers[0]);
        Assert.AreEqual(EnUs.DateTimeFormat.GetShortestDayName(DayOfWeek.Saturday), headers[^1]);
    }

    [TestMethod]
    public void GetAbbreviatedWeekDayHeadersShouldMatchTheVisibleColumns()
    {
        var headers = BitFullCalendarHelpers.GetAbbreviatedWeekDayHeaders(EnUs, DayOfWeek.Monday, [DayOfWeek.Sunday]);

        Assert.AreEqual(6, headers.Length);
        Assert.AreEqual(EnUs.DateTimeFormat.GetAbbreviatedDayName(DayOfWeek.Monday), headers[0]);
    }

    [TestMethod]
    public void GetCalendarCellsShouldStayRectangularWhenDaysAreHidden()
    {
        var cells = BitFullCalendarHelpers.GetCalendarCells(
            new DateTime(2024, 5, 15), EnUs, DayOfWeek.Monday, [DayOfWeek.Saturday, DayOfWeek.Sunday]);

        Assert.AreEqual(0, cells.Count % 5, "every row must keep the same number of visible columns");
        Assert.IsFalse(cells.Any(c => c.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
        // Every weekday of the month is still present.
        Assert.AreEqual(23, cells.Count(c => c.CurrentMonth));
    }

    [TestMethod]
    public void GetCalendarCellsShouldStartTheGridOnTheOverriddenFirstDay()
    {
        var cells = BitFullCalendarHelpers.GetCalendarCells(new DateTime(2024, 5, 15), EnUs, DayOfWeek.Monday, null);

        Assert.AreEqual(DayOfWeek.Monday, cells[0].Date.DayOfWeek);
        Assert.AreEqual(0, cells.Count % 7);
    }

    [TestMethod]
    public void NavigateDateShouldSkipHiddenDaysInTheDayView()
    {
        // Friday 2024-05-17 → the next visible day is Monday when the weekend is hidden.
        var next = BitFullCalendarHelpers.NavigateDate(
            new DateTime(2024, 5, 17), BitFullCalendarView.Day, forward: true, EnUs, [DayOfWeek.Saturday, DayOfWeek.Sunday]);

        Assert.AreEqual(new DateTime(2024, 5, 20), next);
    }

    [TestMethod]
    public void NavigateDateShouldSkipHiddenDaysBackwardsToo()
    {
        var previous = BitFullCalendarHelpers.NavigateDate(
            new DateTime(2024, 5, 20), BitFullCalendarView.Day, forward: false, EnUs, [DayOfWeek.Saturday, DayOfWeek.Sunday]);

        Assert.AreEqual(new DateTime(2024, 5, 17), previous);
    }

    #endregion

    #region Week numbers and era-safe anchors

    [TestMethod]
    public void GetWeekNumberShouldFollowIso8601()
    {
        // 2024-01-01 is a Monday, so it opens ISO week 1.
        Assert.AreEqual(1, BitFullCalendarHelpers.GetWeekNumber(new DateTime(2024, 1, 1)));
        // 2023-01-01 is a Sunday, which ISO counts as the last week of 2022.
        Assert.AreEqual(52, BitFullCalendarHelpers.GetWeekNumber(new DateTime(2023, 1, 1)));
    }

    [TestMethod]
    public void StartAndEndOfCulturalMonthShouldBracketTheGregorianMonth()
    {
        var date = new DateTime(2024, 2, 17);

        Assert.AreEqual(new DateTime(2024, 2, 1), BitFullCalendarHelpers.StartOfCulturalMonth(date, EnUs));
        Assert.AreEqual(new DateTime(2024, 2, 29), BitFullCalendarHelpers.EndOfCulturalMonth(date, EnUs));
    }

    [TestMethod]
    public void StartAndEndOfCulturalYearShouldBracketTheGregorianYear()
    {
        var date = new DateTime(2024, 7, 4);

        Assert.AreEqual(new DateTime(2024, 1, 1), BitFullCalendarHelpers.StartOfCulturalYear(date, EnUs));
        Assert.AreEqual(new DateTime(2024, 12, 31), BitFullCalendarHelpers.EndOfCulturalYear(date, EnUs));
    }

    [TestMethod]
    public void CulturalAnchorsShouldFollowANonGregorianCalendar()
    {
        var culture = new CultureInfo("fa-IR") { DateTimeFormat = { Calendar = new PersianCalendar() } };
        var cal = culture.Calendar;
        var date = new DateTime(2024, 7, 4);

        var monthStart = BitFullCalendarHelpers.StartOfCulturalMonth(date, culture);
        var monthEnd = BitFullCalendarHelpers.EndOfCulturalMonth(date, culture);

        Assert.AreEqual(1, cal.GetDayOfMonth(monthStart));
        Assert.AreEqual(cal.GetMonth(date), cal.GetMonth(monthStart));
        Assert.AreEqual(cal.GetMonth(date), cal.GetMonth(monthEnd));
        Assert.AreEqual(1, cal.GetDayOfMonth(monthEnd.AddDays(1)));

        var yearStart = BitFullCalendarHelpers.StartOfCulturalYear(date, culture);
        var yearEnd = BitFullCalendarHelpers.EndOfCulturalYear(date, culture);

        Assert.AreEqual(cal.GetYear(date), cal.GetYear(yearStart));
        Assert.AreEqual(cal.GetYear(date), cal.GetYear(yearEnd));
        Assert.AreEqual(1, cal.GetMonth(yearStart));
        Assert.AreEqual(1, cal.GetDayOfMonth(yearStart));
        Assert.AreEqual(cal.GetYear(date) + 1, cal.GetYear(yearEnd.AddDays(1)));
    }

    [TestMethod]
    public void GetDateRangeShouldFollowTheFirstDayOverrideInTheWeekView()
    {
        var (start, end) = BitFullCalendarHelpers.GetDateRange(
            BitFullCalendarView.Week, new DateTime(2024, 5, 15), EnUs, DayOfWeek.Monday);

        Assert.AreEqual(new DateTime(2024, 5, 13), start);
        Assert.AreEqual(new DateTime(2024, 5, 19), end);
    }

    [TestMethod]
    public void GetDateRangeShouldBracketTheMonthAndTheYear()
    {
        var (monthStart, monthEnd) = BitFullCalendarHelpers.GetDateRange(
            BitFullCalendarView.Month, new DateTime(2024, 2, 17), EnUs);
        Assert.AreEqual(new DateTime(2024, 2, 1), monthStart);
        Assert.AreEqual(new DateTime(2024, 2, 29), monthEnd);

        var (yearStart, yearEnd) = BitFullCalendarHelpers.GetDateRange(
            BitFullCalendarView.Year, new DateTime(2024, 2, 17), EnUs);
        Assert.AreEqual(new DateTime(2024, 1, 1), yearStart);
        Assert.AreEqual(new DateTime(2024, 12, 31), yearEnd);
    }

    #endregion

    #region Visible hours and slots

    [TestMethod]
    public void NormalizeVisibleHoursShouldClampAndKeepAtLeastOneHour()
    {
        Assert.AreEqual((0, 24), BitFullCalendarHelpers.NormalizeVisibleHours(0, 24));
        Assert.AreEqual((8, 18), BitFullCalendarHelpers.NormalizeVisibleHours(8, 18));
        // Out of range values are pulled into the accepted band.
        Assert.AreEqual((0, 24), BitFullCalendarHelpers.NormalizeVisibleHours(-5, 99));
        // An end at or below the start is corrected to one hour past it.
        Assert.AreEqual((10, 11), BitFullCalendarHelpers.NormalizeVisibleHours(10, 10));
        Assert.AreEqual((10, 11), BitFullCalendarHelpers.NormalizeVisibleHours(10, 3));
    }

    [TestMethod]
    public void GetSlotsPerHourShouldDivideTheHour()
    {
        Assert.AreEqual(2, BitFullCalendarHelpers.GetSlotsPerHour(30));
        Assert.AreEqual(4, BitFullCalendarHelpers.GetSlotsPerHour(15));
        Assert.AreEqual(1, BitFullCalendarHelpers.GetSlotsPerHour(60));
    }

    [TestMethod]
    public void GetSlotMinutesShouldListEverySlotOffsetAscending()
    {
        CollectionAssert.AreEqual(new[] { 0, 30 }, BitFullCalendarHelpers.GetSlotMinutes(30));
        CollectionAssert.AreEqual(new[] { 0, 15, 30, 45 }, BitFullCalendarHelpers.GetSlotMinutes(15));
        CollectionAssert.AreEqual(new[] { 0 }, BitFullCalendarHelpers.GetSlotMinutes(60));
    }

    [TestMethod]
    public void GetEventBlockStyleShouldMeasureFromTheGridsFirstHour()
    {
        var day = new DateTime(2024, 5, 15);
        var ev = Event(day.AddHours(9), day.AddHours(10));

        var fromMidnight = BitFullCalendarHelpers.GetEventBlockStyle(ev, day, 0, 1);
        var fromEight = BitFullCalendarHelpers.GetEventBlockStyle(ev, day, 0, 1, visibleStartHour: 8);

        Assert.AreEqual(9 * BitFullCalendarHelpers.HourHeightPx, fromMidnight.TopPx, 0.001);
        Assert.AreEqual(1 * BitFullCalendarHelpers.HourHeightPx, fromEight.TopPx, 0.001);
    }

    [TestMethod]
    public void GetTimelineBlockPositionShouldClipToTheVisibleHourWindow()
    {
        var day = new DateTime(2024, 5, 15);
        // 06:00 - 20:00 against an 08:00 - 18:00 window clips to the full ten rendered hours.
        var ev = Event(day.AddHours(6), day.AddHours(20));

        var pos = BitFullCalendarHelpers.GetTimelineBlockPosition(ev, day, 96, 8, 18);

        Assert.IsNotNull(pos);
        Assert.AreEqual(0, pos!.Value.LeftPx, 0.001);
        Assert.AreEqual(10 * 96, pos.Value.WidthPx, 0.001);
    }

    [TestMethod]
    public void GetTimelineBlockPositionShouldDropAnEventOutsideTheWindow()
    {
        var day = new DateTime(2024, 5, 15);
        var ev = Event(day.AddHours(19), day.AddHours(20));

        Assert.IsNull(BitFullCalendarHelpers.GetTimelineBlockPosition(ev, day, 96, 8, 18));
    }

    [TestMethod]
    public void IsNowInVisibleHoursShouldFollowTheWindow()
    {
        // The full window always contains "now"; a one-hour window an hour away never does.
        Assert.IsTrue(BitFullCalendarHelpers.IsNowInVisibleHours(0, 24));

        var nowHour = DateTime.Now.Hour;
        var awayStart = (nowHour + 2) % 23;
        Assert.IsFalse(BitFullCalendarHelpers.IsNowInVisibleHours(awayStart, awayStart + 1));
    }

    #endregion

    #region Recurrence

    private static BitFullCalendarEvent Series(DateTime start, BitFullCalendarRecurrence rule, TimeSpan? duration = null) =>
        new()
        {
            Id = "series",
            Title = "Standup",
            StartDate = start,
            EndDate = start + (duration ?? TimeSpan.FromHours(1)),
            Recurrence = rule
        };

    [TestMethod]
    public void ExpandRecurrencesShouldLeaveOneOffEventsAlone()
    {
        var day = new DateTime(2024, 5, 15);
        var ev = Event(day.AddHours(9), day.AddHours(10));

        var result = BitFullCalendarHelpers.ExpandRecurrences([ev], day, day.AddDays(7));

        Assert.AreEqual(1, result.Count);
        Assert.AreSame(ev, result[0], "a one-off event is passed through untouched");
    }

    [TestMethod]
    public void ExpandRecurrencesShouldRepeatDaily()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(4));

        Assert.AreEqual(5, result.Count);
        Assert.AreEqual(start, result[0].StartDate);
        Assert.AreEqual(start.AddDays(4), result[^1].StartDate);
        Assert.IsTrue(result.All(o => o.EndDate - o.StartDate == TimeSpan.FromHours(1)), "every occurrence keeps the master's length");
    }

    [TestMethod]
    public void ExpandRecurrencesShouldHonourTheInterval()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = 3 });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(9));

        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(3), start.AddDays(6), start.AddDays(9) },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldStopAtTheCount()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 3 });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(30));

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(start.AddDays(2), result[^1].StartDate);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldStopAtTheUntilDate()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Until = start.Date.AddDays(2) });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(30));

        Assert.AreEqual(3, result.Count, "Until is inclusive");
        Assert.AreEqual(start.AddDays(2), result[^1].StartDate);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldSkipExceptionDates()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new()
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Daily,
            // The time of day on an exception must not matter.
            ExceptionDates = [start.Date.AddDays(1).AddHours(17)]
        });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(3));

        CollectionAssert.AreEqual(
            new[] { start, start.AddDays(2), start.AddDays(3) },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldRepeatOnTheListedWeekDays()
    {
        // 2024-05-15 is a Wednesday.
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new()
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
            DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
        });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(7));

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 5, 15, 9, 0, 0), // Wed
                new DateTime(2024, 5, 17, 9, 0, 0), // Fri
                new DateTime(2024, 5, 20, 9, 0, 0), // Mon
                new DateTime(2024, 5, 22, 9, 0, 0), // Wed
            },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldFollowTheStartWeekDayWhenNoneAreListed()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Weekly });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(21));

        Assert.IsTrue(result.All(o => o.StartDate.DayOfWeek == DayOfWeek.Wednesday));
        Assert.AreEqual(4, result.Count);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldSkipAMonthTooShortForTheDay()
    {
        var start = new DateTime(2024, 1, 31, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Monthly });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, new DateTime(2024, 5, 31));

        // February and April have no 31st, so the series simply skips them.
        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 1, 31, 9, 0, 0),
                new DateTime(2024, 3, 31, 9, 0, 0),
                new DateTime(2024, 5, 31, 9, 0, 0),
            },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldRepeatYearlyOnlyOnRealDates()
    {
        var start = new DateTime(2024, 2, 29, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Yearly });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, new DateTime(2032, 12, 31));

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 2, 29, 9, 0, 0),
                new DateTime(2028, 2, 29, 9, 0, 0),
                new DateTime(2032, 2, 29, 9, 0, 0),
            },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldOnlyReturnOccurrencesInsideTheRange()
    {
        var start = new DateTime(2024, 5, 1, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], new DateTime(2024, 5, 10), new DateTime(2024, 5, 12));

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 5, 10, 9, 0, 0),
                new DateTime(2024, 5, 11, 9, 0, 0),
                new DateTime(2024, 5, 12, 9, 0, 0),
            },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldKeepAnOccurrenceThatReachesIntoTheRange()
    {
        // A three-day occurrence starting the day before the range still shows inside it.
        var start = new DateTime(2024, 5, 1, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Weekly }, TimeSpan.FromDays(3));

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], new DateTime(2024, 5, 9), new DateTime(2024, 5, 9));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(new DateTime(2024, 5, 8, 9, 0, 0), result[0].StartDate);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldTagEveryOccurrenceWithItsSeries()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 2 });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(1));

        Assert.IsTrue(result.All(o => o.SeriesId == "series"));
        Assert.IsTrue(result.All(o => o.IsOccurrence));
        Assert.IsTrue(result.All(o => o.IsReadOnly), "the master is what a consumer edits");
        CollectionAssert.AreEqual(new[] { start.Date, start.Date.AddDays(1) }, result.Select(o => o.OccurrenceDate!.Value).ToArray());
        Assert.AreEqual(result.Select(o => o.Id).Distinct().Count(), result.Count, "occurrence ids stay unique");
    }

    [TestMethod]
    public void ExpandRecurrencesShouldCarryTheMastersDisplayFields()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 1 });
        series.Color = "green";
        series.Resource = "room-1";
        series.CssClass = "tentative";
        series.IsAllDay = true;
        series.Attendees = [new BitFullCalendarAttendee { FirstName = "Ada" }];

        var occurrence = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date)[0];

        Assert.AreEqual("green", occurrence.Color);
        Assert.AreEqual("room-1", occurrence.Resource);
        Assert.AreEqual("tentative", occurrence.CssClass);
        Assert.IsTrue(occurrence.IsAllDay);
        Assert.AreEqual("Ada", occurrence.Attendees.Single().FirstName);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldTreatAnIntervalBelowOneAsOne()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = 0 });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(2));

        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldReachAWindowFarFromAnOpenEndedSeriesStart()
    {
        // The regression this guards: walking every occurrence from the start would spend the whole
        // budget years before the visible range and render an established series as empty.
        var start = new DateTime(2000, 1, 3, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], new DateTime(2024, 5, 15), new DateTime(2024, 5, 17));

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 5, 15, 9, 0, 0),
                new DateTime(2024, 5, 16, 9, 0, 0),
                new DateTime(2024, 5, 17, 9, 0, 0),
            },
            result.Select(o => o.StartDate).ToArray());
    }

    [TestMethod]
    public void ExpandRecurrencesShouldKeepTheIntervalPhaseWhenSkippingAhead()
    {
        // Every third day from 2000-01-03; 2024-05-15 is 8899 days later, which is not a multiple of
        // three, so the phase decides whether the 15th itself is an occurrence.
        var start = new DateTime(2000, 1, 3, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = 3 });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], new DateTime(2024, 5, 15), new DateTime(2024, 5, 21));

        Assert.IsTrue(result.Count > 0);
        Assert.IsTrue(result.All(o => (o.StartDate.Date - start.Date).Days % 3 == 0), "the series keeps its phase");
        Assert.IsTrue(result.All(o => o.StartDate.Date >= new DateTime(2024, 5, 15)));
    }

    [TestMethod]
    public void ExpandRecurrencesShouldReachAFarWindowForWeeklyMonthlyAndYearlySeries()
    {
        var start = new DateTime(2000, 1, 5, 9, 0, 0); // a Wednesday
        var weekly = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Weekly });
        var monthly = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Monthly });
        var yearly = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Yearly });

        var may = (Start: new DateTime(2024, 5, 1), End: new DateTime(2024, 5, 31));

        // Five Wednesdays in May 2024, and the monthly series' own 5th.
        Assert.AreEqual(5, BitFullCalendarHelpers.ExpandRecurrences([weekly], may.Start, may.End).Count);
        Assert.AreEqual(1, BitFullCalendarHelpers.ExpandRecurrences([monthly], may.Start, may.End).Count);
        // A yearly series only lands in the month it started in.
        Assert.AreEqual(0, BitFullCalendarHelpers.ExpandRecurrences([yearly], may.Start, may.End).Count);

        var january = BitFullCalendarHelpers.ExpandRecurrences([yearly], new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
        Assert.AreEqual(1, january.Count);
        Assert.AreEqual(new DateTime(2024, 1, 5, 9, 0, 0), january[0].StartDate);
    }

    [TestMethod]
    public void ExpandRecurrencesShouldSurviveAnAbsurdInterval()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = int.MaxValue });

        var result = BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(7));

        Assert.AreEqual(1, result.Count, "only the first occurrence is representable");
    }

    [TestMethod]
    public void ExpandRecurrencesShouldReturnNothingForANonPositiveCount()
    {
        var start = new DateTime(2024, 5, 15, 9, 0, 0);
        var series = Series(start, new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 0 });

        Assert.AreEqual(0, BitFullCalendarHelpers.ExpandRecurrences([series], start.Date, start.Date.AddDays(5)).Count);
    }

    #endregion

    #region Overlap and month-cell placement

    [TestMethod]
    public void EventsOverlapShouldIgnoreTouchingRanges()
    {
        var day = new DateTime(2024, 5, 15);
        var first = Event(day.AddHours(9), day.AddHours(10), "a");
        var touching = Event(day.AddHours(10), day.AddHours(11), "b");
        var overlapping = Event(day.AddHours(9).AddMinutes(30), day.AddHours(10).AddMinutes(30), "c");

        Assert.IsFalse(BitFullCalendarHelpers.EventsOverlap(first, touching));
        Assert.IsTrue(BitFullCalendarHelpers.EventsOverlap(first, overlapping));
    }

    [TestMethod]
    public void CalculateMonthEventPositionsShouldHonourTheCellCapacity()
    {
        var day = new DateTime(2024, 5, 15);
        var singles = Enumerable.Range(0, 5)
            .Select(i => Event(day.AddHours(9 + i), day.AddHours(10 + i), $"e{i}"))
            .ToList();

        var withThree = BitFullCalendarHelpers.CalculateMonthEventPositions([], singles, day, EnUs, 3);
        var withFive = BitFullCalendarHelpers.CalculateMonthEventPositions([], singles, day, EnUs, 5);

        Assert.AreEqual(3, withThree.Count, "only the first three fit into a three-slot cell");
        Assert.AreEqual(5, withFive.Count);
    }

    [TestMethod]
    public void GetMonthCellEventsShouldAssignRowsUpToTheCellCapacity()
    {
        var day = new DateTime(2024, 5, 15);
        var events = Enumerable.Range(0, 4)
            .Select(i => Event(day.AddHours(9 + i), day.AddHours(10 + i), $"e{i}"))
            .ToList();
        var positions = BitFullCalendarHelpers.CalculateMonthEventPositions([], events, day, EnUs, 2);

        var cellEvents = BitFullCalendarHelpers.GetMonthCellEvents(day, events, positions, 2);

        Assert.AreEqual(4, cellEvents.Count, "every event on the day is still reported");
        Assert.AreEqual(2, cellEvents.Count(e => e.Position >= 0), "only two of them get a visible row");
        Assert.AreEqual(2, cellEvents.Count(e => e.Position < 0));
    }

    [TestMethod]
    public void GetEventsForDayShouldPutAnAllDaySingleDateEventInTheAllDayRow()
    {
        var day = new DateTime(2024, 5, 15);
        var timed = Event(day.AddHours(9), day.AddHours(10), "timed");
        var allDay = new BitFullCalendarEvent
        {
            Id = "allday",
            Title = "All day",
            StartDate = day,
            EndDate = day.AddDays(1),
            IsAllDay = true
        };
        // A single-date event that is not marked all-day stays on the time grid.
        var markedSingleDate = new BitFullCalendarEvent
        {
            Id = "marked",
            Title = "Marked",
            StartDate = day.AddHours(9),
            EndDate = day.AddHours(11),
            IsAllDay = true
        };

        var all = BitFullCalendarHelpers.GetEventsForDay([timed, allDay, markedSingleDate], day);
        var allDayRow = BitFullCalendarHelpers.GetEventsForDay([timed, allDay, markedSingleDate], day, allDayRowOnly: true);

        Assert.AreEqual(3, all.Count);
        CollectionAssert.AreEquivalent(new[] { "allday", "marked" }, allDayRow.Select(e => e.Id).ToArray());
    }

    [TestMethod]
    public void GetEventsForWeekShouldFollowTheFirstDayOverride()
    {
        // Sunday 2024-05-12 belongs to the previous week when the week starts on Monday.
        var sunday = new DateTime(2024, 5, 12);
        var ev = Event(sunday.AddHours(9), sunday.AddHours(10));

        var mondayWeek = BitFullCalendarHelpers.GetEventsForWeek([ev], new DateTime(2024, 5, 15), EnUs, DayOfWeek.Monday);
        var sundayWeek = BitFullCalendarHelpers.GetEventsForWeek([ev], new DateTime(2024, 5, 15), EnUs, DayOfWeek.Sunday);

        Assert.AreEqual(0, mondayWeek.Count);
        Assert.AreEqual(1, sundayWeek.Count);
    }

    [TestMethod]
    public void CreateDraftEventForTimeSlotShouldUseTheSuppliedDuration()
    {
        var day = new DateTime(2024, 5, 15);

        var draft = BitFullCalendarHelpers.CreateDraftEventForTimeSlot(day, 9, 15, 15);

        Assert.AreEqual(day.AddHours(9).AddMinutes(15), draft.StartDate);
        Assert.AreEqual(day.AddHours(9).AddMinutes(30), draft.EndDate);
    }

    [TestMethod]
    public void FormatTimeShouldFollowTheRequestedClock()
    {
        var moment = new DateTime(2024, 5, 15, 13, 5, 0);

        Assert.AreEqual("13:05", BitFullCalendarHelpers.FormatTime(moment, use24Hour: true, Invariant));
        StringAssert.StartsWith(BitFullCalendarHelpers.FormatTime(moment, use24Hour: false, Invariant), "1:05");
    }

    #endregion
}
