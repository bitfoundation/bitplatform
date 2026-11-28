using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// The range <see cref="BitFullCalendar.OnDateChange"/> reports and <see cref="BitFullCalendar.GetVisibleRange"/> answers
/// is every day the view shows. The month grid leads and trails with days of the neighbouring months that show their
/// events like any other day, so an app loading the events of the reported range has to be told about them - unless
/// <see cref="BitFullCalendarSettings.ShowNonCurrentDates"/> leaves them blank.
/// </summary>
[TestClass]
public class BitFullCalendarVisibleRangeTests : BunitTestContext
{
    // July 2025 starts on a Tuesday and ends on a Thursday: in en-US, whose weeks start on Sunday, its grid runs from
    // Sunday June 29 to Saturday August 2 in five rows, and to Saturday August 9 in six.
    private static readonly DateTime Anchor = new(2025, 7, 16);

    private IRenderedComponent<BitFullCalendar> RenderCalendar(BitFullCalendarView view, BitFullCalendarSettings? settings, List<BitFullCalendarDateChangeEventArgs> ranges)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, view);
            parameters.Add(p => p.OnDateChange, (BitFullCalendarDateChangeEventArgs r) => ranges.Add(r));

            if (settings is not null)
                parameters.Add(p => p.Settings, settings);
        });
    }

    [TestMethod]
    public void TheMonthViewReportsTheDaysItBorrowsFromTheNeighbouringMonths()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderCalendar(BitFullCalendarView.Month, null, ranges);

        Assert.HasCount(1, ranges);
        Assert.AreEqual(new DateTime(2025, 6, 29), ranges[0].Start);
        Assert.AreEqual(new DateTime(2025, 8, 2), ranges[0].End);
        Assert.AreEqual((ranges[0].Start, ranges[0].End), component.Instance.GetVisibleRange());
    }

    [TestMethod]
    public void ASixthRowIsInTheRange()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        RenderCalendar(BitFullCalendarView.Month, new BitFullCalendarSettings { FixedWeekCount = true }, ranges);

        Assert.AreEqual(new DateTime(2025, 6, 29), ranges[^1].Start);
        Assert.AreEqual(new DateTime(2025, 8, 9), ranges[^1].End);
    }

    [TestMethod]
    public void BlankBorrowedDaysAreNotInTheRange()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        RenderCalendar(BitFullCalendarView.Month, new BitFullCalendarSettings { ShowNonCurrentDates = false }, ranges);

        Assert.AreEqual(new DateTime(2025, 7, 1), ranges[^1].Start);
        Assert.AreEqual(new DateTime(2025, 7, 31), ranges[^1].End);
    }

    [TestMethod]
    public void TheAgendaKeepsToTheMonth()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        RenderCalendar(BitFullCalendarView.Agenda, null, ranges);

        Assert.AreEqual(new DateTime(2025, 7, 1), ranges[^1].Start);
        Assert.AreEqual(new DateTime(2025, 7, 31), ranges[^1].End);
    }

    [TestMethod]
    public void ChangingTheShapeOfTheGridReportsTheNewRange()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderCalendar(BitFullCalendarView.Month, null, ranges);

        component.Render(parameters => parameters.Add(p => p.Settings, new BitFullCalendarSettings { FixedWeekCount = true }));

        Assert.HasCount(2, ranges);
        Assert.AreEqual(new DateTime(2025, 8, 9), ranges[1].End);

        component.Render(parameters => parameters.Add(p => p.Settings, new BitFullCalendarSettings { FixedWeekCount = true, ShowNonCurrentDates = false }));

        Assert.HasCount(3, ranges);
        Assert.AreEqual((new DateTime(2025, 7, 1), new DateTime(2025, 7, 31)), (ranges[2].Start, ranges[2].End));
    }

    [TestMethod]
    public void TheTimelineMonthKeepsToTheMonth()
    {
        // The timeline's month view has a column per day of the month and borrows no days from its neighbours.
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.Resources, [new BitFullCalendarResource { Id = "r1", Title = "Room 1" }]);
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultMode, BitFullCalendarMode.Timeline);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Month);
            parameters.Add(p => p.OnDateChange, (BitFullCalendarDateChangeEventArgs r) => ranges.Add(r));
        });

        Assert.AreEqual((new DateTime(2025, 7, 1), new DateTime(2025, 7, 31)), (ranges[^1].Start, ranges[^1].End));
        Assert.AreEqual((new DateTime(2025, 7, 1), new DateTime(2025, 7, 31)), component.Instance.GetVisibleRange());

        // Back to the event grid, the month view shows the borrowed days again, and the new range is reported.
        component.InvokeAsync(() => component.Instance.ChangeMode(BitFullCalendarMode.Event));

        Assert.AreEqual((new DateTime(2025, 6, 29), new DateTime(2025, 8, 2)), (ranges[^1].Start, ranges[^1].End));
    }

    [TestMethod]
    public void ASixthRowShowsTheOccurrencesOfARecurringEventOnIt()
    {
        // February 2026 starts on a Sunday and ends on a Saturday: four rows in en-US, Feb 1 to 28, and six with
        // FixedWeekCount, to March 14. The series is expanded over the range, so the new rows need it expanded again.
        var february = new DateTime(2026, 2, 1);
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new()
                {
                    Id = "standup", Title = "Standup", StartDate = february.AddHours(9), EndDate = february.AddHours(10),
                    Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
                },
            ]);
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, february.AddDays(14));
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Month);
        });

        var lastDay = new DateTime(2026, 3, 14);
        Assert.IsFalse(component.Instance.State.ExpandedEvents.Any(e => e.StartDate.Date == lastDay));

        component.Render(parameters => parameters.Add(p => p.Settings, new BitFullCalendarSettings { FixedWeekCount = true }));

        Assert.IsTrue(component.Instance.State.ExpandedEvents.Any(e => e.StartDate.Date == lastDay));
    }
}
