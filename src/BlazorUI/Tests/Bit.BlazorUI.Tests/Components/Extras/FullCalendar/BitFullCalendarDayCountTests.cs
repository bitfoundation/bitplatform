using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers a week view shorter than the week (<see cref="BitFullCalendarSettings.WeekDayCount"/>): the run of days it
/// shows, how it turns, the hidden days it skips, and that selecting a day inside it does not slide it.
/// </summary>
[TestClass]
public class BitFullCalendarDayCountTests : BunitTestContext
{
    // A Wednesday.
    private static readonly DateTime Anchor = new(2031, 6, 18);

    private IRenderedComponent<BitFullCalendar> RenderCalendar(BitFullCalendarSettings settings, Action<BitFullCalendarDateChangeEventArgs>? onDateChange = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Week);
            parameters.Add(p => p.Settings, settings);
            if (onDateChange is not null)
                parameters.Add(p => p.OnDateChange, onDateChange);
        });
    }

    [TestMethod]
    public void BitFullCalendarShouldWalkShownDaysForAShortWeek()
    {
        var hidden = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
        var friday = Anchor.AddDays(2);

        CollectionAssert.AreEqual(new[] { Anchor, Anchor.AddDays(1), Anchor.AddDays(2) }, BitFullCalendarHelpers.GetWeekDates(Anchor, weekDayCount: 3));
        CollectionAssert.AreEqual(new[] { friday, friday.AddDays(3), friday.AddDays(4) }, BitFullCalendarHelpers.GetWeekDates(friday, hiddenDays: hidden, weekDayCount: 3));

        Assert.AreEqual(friday.AddDays(5), BitFullCalendarHelpers.NavigateDate(friday, BitFullCalendarView.Week, true, hiddenDays: hidden, weekDayCount: 3),
                        "Fri, Mon, Tue turn to Wed, Thu, Fri");
        Assert.AreEqual(friday, BitFullCalendarHelpers.NavigateDate(friday.AddDays(5), BitFullCalendarView.Week, false, hiddenDays: hidden, weekDayCount: 3));
    }

    [TestMethod]
    public void BitFullCalendarShouldTreatOutOfRangeDayCountsAsTheWholeWeek()
    {
        Assert.IsNull(BitFullCalendarHelpers.NormalizeWeekDayCount(0));
        Assert.IsNull(BitFullCalendarHelpers.NormalizeWeekDayCount(7));
        Assert.AreEqual(7, BitFullCalendarHelpers.GetWeekDates(Anchor, weekDayCount: 9).Length);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderOneColumnPerDayOfAShortWeek()
    {
        var component = RenderCalendar(new() { WeekDayCount = 3 });

        Assert.AreEqual(3, component.FindAll(".bit-bfc-week-day-col").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-week-mobile-warning").Count, "three days are what a phone is meant to show");
        var expected = BitFullCalendarHelpers.RangeText(BitFullCalendarView.Week, Anchor, new System.Globalization.CultureInfo("en-US"), weekDayCount: 3);
        Assert.AreEqual(expected, component.Find(".bit-bfc-header-title").TextContent.Trim());
        StringAssert.Contains(expected, "20", "the title runs to the third day");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotSlideAShortWeekWhenADayInsideItIsSelected()
    {
        var state = RenderCalendar(new() { WeekDayCount = 3 }).Instance.State;

        state.SetSelectedDate(Anchor.AddDays(2));

        Assert.AreEqual(Anchor, state.GetVisibleWeekDates()[0]);

        state.SetSelectedDate(Anchor.AddDays(5));
        Assert.AreEqual(Anchor.AddDays(5), state.GetVisibleWeekDates()[0], "a day outside the run starts a new one");
    }

    [TestMethod]
    public void BitFullCalendarShouldTurnAShortWeekByItsOwnLength()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderCalendar(new() { WeekDayCount = 3 }, ranges.Add);

        component.Instance.NavigateNext();

        Assert.AreEqual((Anchor.AddDays(3), Anchor.AddDays(5)), component.Instance.GetVisibleRange());
        var last = ranges.Last();
        Assert.AreEqual(Anchor.AddDays(3), last.Start);
        Assert.AreEqual(Anchor.AddDays(5), last.End);

        component.Instance.NavigatePrevious();
        Assert.AreEqual((Anchor, Anchor.AddDays(2)), component.Instance.GetVisibleRange());
    }

    [TestMethod]
    public void BitFullCalendarShouldMeasureOtherViewsFromTheSelectedDateNotTheRun()
    {
        var component = RenderCalendar(new() { WeekDayCount = 3 });
        var state = component.Instance.State;

        // The run starts on Wednesday; Friday, its last day, is opened in the day view.
        state.SetSelectedDate(Anchor.AddDays(2));
        component.Instance.ChangeView(BitFullCalendarView.Day);

        Assert.AreEqual((Anchor.AddDays(2), Anchor.AddDays(2)), component.Instance.GetVisibleRange());
        component.Instance.NavigateNext();
        Assert.AreEqual(Anchor.AddDays(3), state.SelectedDate, "next is the day after the one shown, not after the run's start");
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepTheCultureWeekWithoutADayCount()
    {
        var component = RenderCalendar(new());

        Assert.AreEqual(7, component.FindAll(".bit-bfc-week-day-col").Count);
        Assert.AreEqual((new DateTime(2031, 6, 15), new DateTime(2031, 6, 21)), component.Instance.GetVisibleRange());
    }
}
