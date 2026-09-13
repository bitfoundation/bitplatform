using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the recurrence rule - which dates each frequency, interval, pattern, and end produces - and how the
/// calendar state expands a series into occurrences and folds an edit, delete, or drop of one back into it.
/// </summary>
[TestClass]
public class BitFullCalendarRecurrenceTests
{
    // en-US weeks start on Sunday. 2026-09-01 is a Tuesday.
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private static DateTime D(int month, int day, int hour = 9) => new(2026, month, day, hour, 0, 0);

    private static DateTime EndOf(int month, int day) => new(2026, month, day, 23, 59, 59);

    #region Rule

    [TestMethod]
    public void DailyRuleShouldRepeatEveryIntervalDays()
    {
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = 15 };

        var occurrences = rule.GetOccurrences(D(9, 1), D(9, 1), EndOf(10, 31), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 16), D(10, 1), D(10, 16), D(10, 31) }, occurrences);
    }

    [TestMethod]
    public void WeeklyRuleShouldLandOnEverySelectedDay()
    {
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
            DaysOfWeek = [DayOfWeek.Friday, DayOfWeek.Monday, DayOfWeek.Wednesday]
        };

        var occurrences = rule.GetOccurrences(D(8, 31), D(8, 31), EndOf(9, 13), EnUs);

        CollectionAssert.AreEqual(new[] { D(8, 31), D(9, 2), D(9, 4), D(9, 7), D(9, 9), D(9, 11) }, occurrences);
    }

    [TestMethod]
    public void WeeklyRuleShouldSkipWeeksByIntervalOnTheStartsWeekdayByDefault()
    {
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Weekly, Interval = 2 };

        var occurrences = rule.GetOccurrences(D(9, 1), D(9, 1), EndOf(9, 30), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 15), D(9, 29) }, occurrences);
    }

    [TestMethod]
    public void MonthlyRuleShouldLandOnTheNthWeekday()
    {
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
            WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Third,
            DaysOfWeek = [DayOfWeek.Tuesday]
        };

        var occurrences = rule.GetOccurrences(D(9, 15), D(9, 1), EndOf(12, 31), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 15), D(10, 20), D(11, 17), D(12, 15) }, occurrences);
    }

    [TestMethod]
    public void MonthlyRuleShouldLandOnTheLastWeekdayWhetherFourthOrFifth()
    {
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
            WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Last,
            DaysOfWeek = [DayOfWeek.Friday]
        };

        var occurrences = rule.GetOccurrences(D(9, 25), D(9, 1), EndOf(11, 30), EnUs);

        // October 30 is the fifth Friday of its month, November 27 the fourth.
        CollectionAssert.AreEqual(new[] { D(9, 25), D(10, 30), D(11, 27) }, occurrences);
    }

    [TestMethod]
    public void MonthlyRuleShouldSkipMonthsTooShortForTheStartsDay()
    {
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Monthly };

        var occurrences = rule.GetOccurrences(D(1, 31), D(1, 1), EndOf(6, 30), EnUs);

        CollectionAssert.AreEqual(new[] { D(1, 31), D(3, 31), D(5, 31) }, occurrences);
    }

    [TestMethod]
    public void YearlyRuleShouldOnlyLandOnYearsThatHaveTheStartsDay()
    {
        var start = new DateTime(2028, 2, 29, 9, 0, 0);
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Yearly };

        var occurrences = rule.GetOccurrences(start, start, new DateTime(2036, 12, 31), EnUs);

        CollectionAssert.AreEqual(new[] { start, new DateTime(2032, 2, 29, 9, 0, 0), new DateTime(2036, 2, 29, 9, 0, 0) }, occurrences);
    }

    [TestMethod]
    public void MonthlyRuleShouldRepeatOnTheDayOfTheCulturesMonth()
    {
        var persian = CultureInfo.GetCultureInfo("fa-IR");
        var calendar = new PersianCalendar();
        var start = calendar.ToDateTime(1405, 6, 15, 10, 0, 0, 0);
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Monthly };

        var occurrences = rule.GetOccurrences(start, start, calendar.AddMonths(start, 3), persian);

        Assert.AreEqual(4, occurrences.Count);
        foreach (var occurrence in occurrences)
        {
            Assert.AreEqual(15, calendar.GetDayOfMonth(occurrence));
        }
    }

    [TestMethod]
    public void SeriesStartShouldAlwaysBeTheFirstOccurrence()
    {
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Weekly, DaysOfWeek = [DayOfWeek.Monday] };

        var occurrences = rule.GetOccurrences(D(9, 1), D(9, 1), EndOf(9, 14), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 7), D(9, 14) }, occurrences);
    }

    [TestMethod]
    public void CountShouldIncludeTheStartAndNotBeExtendedBySkippedDates()
    {
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Daily,
            Count = 5,
            ExceptionDates = [new DateTime(2026, 9, 3)]
        };

        var occurrences = rule.GetOccurrences(D(9, 1), D(9, 1), EndOf(9, 30), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 2), D(9, 4), D(9, 5) }, occurrences);
    }

    [TestMethod]
    public void UntilShouldBeInclusiveAndIgnoreTheTimeOfDay()
    {
        var rule = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Until = new DateTime(2026, 9, 3) };

        var occurrences = rule.GetOccurrences(D(9, 1, 18), D(9, 1), EndOf(9, 30), EnUs);

        CollectionAssert.AreEqual(new[] { D(9, 1, 18), D(9, 2, 18), D(9, 3, 18) }, occurrences);
    }

    [TestMethod]
    public void AdditionalDatesShouldAddOccurrencesAtTheSeriesTimeUnlessSkipped()
    {
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
            Count = 1,
            AdditionalDates = [new DateTime(2026, 9, 10), new DateTime(2026, 9, 12)],
            ExceptionDates = [new DateTime(2026, 9, 12)]
        };

        var occurrences = rule.GetOccurrences(D(9, 1), D(9, 1), EndOf(9, 30), EnUs);

        // Extra dates are not limited by the count, but a skipped date wins over an extra one.
        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 10) }, occurrences);
    }

    [TestMethod]
    [DataRow(BitFullCalendarRecurrenceFrequency.Daily, 3, false)]
    [DataRow(BitFullCalendarRecurrenceFrequency.Weekly, 2, false)]
    [DataRow(BitFullCalendarRecurrenceFrequency.Monthly, 1, false)]
    [DataRow(BitFullCalendarRecurrenceFrequency.Monthly, 2, true)]
    [DataRow(BitFullCalendarRecurrenceFrequency.Yearly, 1, false)]
    [DataRow(BitFullCalendarRecurrenceFrequency.Yearly, 1, true)]
    public void AnUncountedRuleShouldProduceTheSameDatesForADistantRangeAsAFullWalk(BitFullCalendarRecurrenceFrequency frequency, int interval, bool lastWeekday)
    {
        var start = new DateTime(2019, 1, 31, 8, 0, 0);
        var rule = new BitFullCalendarRecurrence
        {
            Frequency = frequency,
            Interval = interval,
            DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Thursday],
            WeekOfMonth = lastWeekday ? BitFullCalendarRecurrenceWeekOfMonth.Last : null
        };
        var rangeStart = new DateTime(2026, 3, 1);
        var rangeEnd = new DateTime(2027, 6, 30);

        // Asking from the series start walks every period; asking from the range jumps ahead.
        var walked = rule.GetOccurrences(start, start, rangeEnd, EnUs).Where(d => d >= rangeStart).ToList();
        var jumped = rule.GetOccurrences(start, rangeStart, rangeEnd, EnUs);

        Assert.IsTrue(jumped.Count > 0);
        CollectionAssert.AreEqual(walked, jumped);
    }

    [TestMethod]
    public void DescribeRecurrenceShouldSummarizeTheRule()
    {
        var texts = new BitFullCalendarTexts();

        var weekly = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
            Interval = 2,
            DaysOfWeek = [DayOfWeek.Friday, DayOfWeek.Monday],
            Until = new DateTime(2026, 10, 30)
        };
        Assert.AreEqual("Every 2 week(s), on Mon, Fri, until Oct 30, 2026", BitFullCalendarHelpers.DescribeRecurrence(weekly, D(8, 31), texts, EnUs));

        var monthly = new BitFullCalendarRecurrence
        {
            Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
            WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Third,
            DaysOfWeek = [DayOfWeek.Tuesday],
            Count = 5
        };
        Assert.AreEqual("Monthly, on the third Tuesday, 5 times", BitFullCalendarHelpers.DescribeRecurrence(monthly, D(9, 15), texts, EnUs));
    }

    #endregion

    #region State

    private static BitFullCalendarRecurrence TuesdaysAndThursdays() => new()
    {
        Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
        DaysOfWeek = [DayOfWeek.Tuesday, DayOfWeek.Thursday]
    };

    private static BitFullCalendarState CreateState(BitFullCalendarRecurrence rule)
    {
        var series = new BitFullCalendarEvent
        {
            Id = "series",
            Title = "Standup",
            Description = "Daily sync",
            StartDate = D(9, 1),
            EndDate = D(9, 1).AddMinutes(30),
            Recurrence = rule
        };

        var state = new BitFullCalendarState();
        state.Initialize([series], EnUs);
        state.SetSelectedDate(new DateTime(2026, 9, 15));
        return state;
    }

    private static BitFullCalendarEvent OccurrenceOn(BitFullCalendarState state, int month, int day)
        => state.Events.Single(e => e.IsOccurrence && e.StartDate.Date == new DateTime(2026, month, day));

    private static List<BitFullCalendarEvent> September(BitFullCalendarState state)
        => [.. state.Events.Where(e => e.StartDate.Year == 2026 && e.StartDate.Month == 9).OrderBy(e => e.StartDate)];

    private static BitFullCalendarEvent Copy(BitFullCalendarEvent ev) => BitFullCalendarChangeNotifier.CloneEvent(ev);

    private static void Apply(BitFullCalendarState state, IEnumerable<BitFullCalendarChangeEventArgs> changes)
    {
        foreach (var change in changes)
        {
            state.ApplyChange(change);
        }
    }

    [TestMethod]
    public void StateShouldRenderTheOccurrencesInsteadOfTheSeries()
    {
        var state = CreateState(TuesdaysAndThursdays());

        Assert.AreEqual(1, state.AllEvents.Count);
        Assert.IsTrue(state.Events.All(e => e.IsOccurrence && e.RecurringEventId == "series"));
        Assert.AreEqual(state.Events.Count, state.Events.Select(e => e.Id).Distinct().Count());
        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 3), D(9, 8), D(9, 10), D(9, 15), D(9, 17), D(9, 22), D(9, 24), D(9, 29) },
                                  September(state).Select(e => e.StartDate).ToArray());

        state.SetSelectedDate(new DateTime(2027, 3, 10));

        Assert.IsTrue(state.Events.Any(e => e.StartDate.Year == 2027 && e.StartDate.Month == 3));
    }

    [TestMethod]
    public void EditingThisEventShouldSkipItsDateAndDetachTheOccurrence()
    {
        var state = CreateState(TuesdaysAndThursdays());
        var occurrence = OccurrenceOn(state, 9, 8);
        var updated = Copy(occurrence);
        updated.StartDate = occurrence.StartDate.AddHours(2);
        updated.EndDate = occurrence.EndDate.AddHours(2);

        var changes = state.BuildEditChanges(occurrence, updated, BitFullCalendarRecurrenceEditScope.ThisEvent, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(2, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);
        Assert.AreEqual("series", changes[0].Event.Id);
        CollectionAssert.AreEqual(new[] { new DateTime(2026, 9, 8) }, changes[0].Event.Recurrence!.ExceptionDates);
        Assert.AreEqual(BitFullCalendarChangeKind.Add, changes[1].Kind);
        Assert.IsNull(changes[1].Event.Recurrence);
        Assert.IsFalse(changes[1].Event.IsOccurrence);
        Assert.AreNotEqual(occurrence.Id, changes[1].Event.Id);
        Assert.AreEqual(D(9, 8, 11), changes[1].Event.StartDate);

        // Building the changes must not touch the series the occurrences share their rule with.
        Assert.AreEqual(0, state.AllEvents.Single().Recurrence!.ExceptionDates.Count);

        Apply(state, changes);

        var september = September(state);
        Assert.AreEqual(9, september.Count);
        Assert.IsFalse(september.Single(e => e.StartDate.Date == new DateTime(2026, 9, 8)).IsOccurrence);
    }

    [TestMethod]
    public void EditingAllEventsShouldMoveTheSeriesByAsMuchAsTheOccurrenceMoved()
    {
        var state = CreateState(TuesdaysAndThursdays());
        var occurrence = OccurrenceOn(state, 9, 17);
        var updated = Copy(occurrence);
        updated.Title = "Daily Standup";
        updated.StartDate = occurrence.StartDate.AddHours(1);
        updated.EndDate = occurrence.EndDate.AddHours(1);

        var changes = state.BuildEditChanges(occurrence, updated, BitFullCalendarRecurrenceEditScope.AllEvents, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(1, changes.Count);
        var series = changes[0].Event;
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);
        Assert.AreEqual("series", series.Id);
        Assert.IsTrue(series.IsRecurring);
        Assert.AreEqual("Daily Standup", series.Title);
        Assert.AreEqual(D(9, 1, 10), series.StartDate);
        Assert.AreEqual(D(9, 1, 10).AddMinutes(30), series.EndDate);

        Apply(state, changes);

        Assert.IsTrue(September(state).All(e => e.Title == "Daily Standup" && e.StartDate.Hour == 10));
    }

    [TestMethod]
    public void EditingThisAndFollowingShouldSplitTheSeriesAndCarryTheRemainingCount()
    {
        var state = CreateState(new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 10 });
        var occurrence = OccurrenceOn(state, 9, 4);
        var updated = Copy(occurrence);
        updated.Title = "Late standup";

        var changes = state.BuildEditChanges(occurrence, updated, BitFullCalendarRecurrenceEditScope.ThisAndFollowing, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(2, changes.Count);
        Assert.AreEqual(new DateTime(2026, 9, 3), changes[0].Event.Recurrence!.Until);
        Assert.AreEqual(BitFullCalendarChangeKind.Add, changes[1].Kind);
        Assert.AreEqual(D(9, 4), changes[1].Event.StartDate);
        Assert.AreEqual(7, changes[1].Event.Recurrence!.Count);

        Apply(state, changes);

        var september = September(state);
        Assert.AreEqual(10, september.Count);
        CollectionAssert.AreEqual(Enumerable.Repeat("Standup", 3).Concat(Enumerable.Repeat("Late standup", 7)).ToArray(),
                                  september.Select(e => e.Title).ToArray());
    }

    [TestMethod]
    public void EditingThisAndFollowingOnTheFirstOccurrenceShouldEditTheWholeSeries()
    {
        var state = CreateState(TuesdaysAndThursdays());
        var occurrence = OccurrenceOn(state, 9, 1);
        var updated = Copy(occurrence);
        updated.Title = "Renamed";

        var changes = state.BuildEditChanges(occurrence, updated, BitFullCalendarRecurrenceEditScope.ThisAndFollowing, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual("series", changes[0].Event.Id);
        Assert.IsNull(changes[0].Event.Recurrence!.Until);
    }

    [TestMethod]
    public void DeletingThisEventShouldOnlySkipItsDate()
    {
        var state = CreateState(TuesdaysAndThursdays());

        var changes = state.BuildDeleteChanges(OccurrenceOn(state, 9, 8), BitFullCalendarRecurrenceEditScope.ThisEvent, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);

        Apply(state, changes);

        Assert.AreEqual(8, September(state).Count);
        Assert.IsFalse(September(state).Any(e => e.StartDate.Date == new DateTime(2026, 9, 8)));
    }

    [TestMethod]
    public void DeletingThisAndFollowingShouldEndTheSeriesTheDayBefore()
    {
        var state = CreateState(TuesdaysAndThursdays());

        var changes = state.BuildDeleteChanges(OccurrenceOn(state, 9, 15), BitFullCalendarRecurrenceEditScope.ThisAndFollowing, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(new DateTime(2026, 9, 14), changes[0].Event.Recurrence!.Until);

        Apply(state, changes);

        CollectionAssert.AreEqual(new[] { D(9, 1), D(9, 3), D(9, 8), D(9, 10) }, September(state).Select(e => e.StartDate).ToArray());
    }

    [TestMethod]
    [DataRow(BitFullCalendarRecurrenceEditScope.AllEvents, 17)]
    [DataRow(BitFullCalendarRecurrenceEditScope.ThisAndFollowing, 1)]
    public void DeletingEveryOccurrenceShouldDeleteTheSeries(BitFullCalendarRecurrenceEditScope scope, int day)
    {
        var state = CreateState(TuesdaysAndThursdays());

        var changes = state.BuildDeleteChanges(OccurrenceOn(state, 9, day), scope, BitFullCalendarChangeSource.Dialog);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Delete, changes[0].Kind);
        Assert.AreEqual("series", changes[0].Event.Id);

        Apply(state, changes);

        Assert.AreEqual(0, state.AllEvents.Count);
        Assert.AreEqual(0, state.Events.Count);
    }

    [TestMethod]
    public void DroppingAnOccurrenceShouldMoveOnlyThatOccurrence()
    {
        var state = CreateState(TuesdaysAndThursdays());

        state.StartDrag(OccurrenceOn(state, 9, 8));
        state.HandleDrop(new DateTime(2026, 9, 9), 14, 0);

        Assert.IsFalse(state.IsDragging);
        Assert.AreEqual(2, state.AllEvents.Count);
        CollectionAssert.AreEqual(new[] { new DateTime(2026, 9, 8) }, state.AllEvents.Single(e => e.IsRecurring).Recurrence!.ExceptionDates);
        var moved = state.AllEvents.Single(e => e.IsRecurring is false);
        Assert.AreEqual(D(9, 9, 14), moved.StartDate);
        Assert.AreEqual(D(9, 9, 14).AddMinutes(30), moved.EndDate);
    }

    [TestMethod]
    public async Task CommitShouldRollEveryChangeBackWhenANotificationThrows()
    {
        var state = CreateState(TuesdaysAndThursdays());
        var notifier = new BitFullCalendarChangeNotifier(state, args => args.Kind == BitFullCalendarChangeKind.Add
            ? throw new InvalidOperationException()
            : Task.CompletedTask);
        var occurrence = OccurrenceOn(state, 9, 8);
        var updated = Copy(occurrence);
        updated.Title = "Moved";

        var threw = false;
        try
        {
            await notifier.CommitAsync(state.BuildEditChanges(occurrence, updated, BitFullCalendarRecurrenceEditScope.ThisEvent, BitFullCalendarChangeSource.Dialog));
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.IsTrue(threw);
        Assert.AreEqual(1, state.AllEvents.Count);
        Assert.AreEqual(0, state.AllEvents.Single().Recurrence!.ExceptionDates.Count);
        Assert.AreEqual(9, September(state).Count(e => e.IsOccurrence));
    }

    #endregion
}
