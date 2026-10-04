using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the calendar's clock (<see cref="BitFullCalendar.TimeProvider"/>), the background and blocking events, the
/// length of the draft the toolbar's add button hands out, and the toast's live regions.
/// </summary>
[TestClass]
public class BitFullCalendarBackgroundAndClockTests : BunitTestContext
{
    // A Wednesday, far enough from the real today that a test reading the system clock would fail.
    private static readonly DateTime Anchor = new(2031, 6, 18);

    private static List<BitFullCalendarEvent> Events() =>
    [
        new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) },
        new() { Id = "lunch", Title = "Lunch", StartDate = Anchor.AddHours(12), EndDate = Anchor.AddHours(13), IsBackground = true, IsBlocking = true },
        new() { Id = "holiday", Title = "Holiday", StartDate = Anchor.AddDays(1), EndDate = Anchor.AddDays(1), IsAllDay = true, IsBackground = true },
        new() { Id = "upkeep", Title = "Upkeep", StartDate = Anchor.AddHours(15), EndDate = Anchor.AddHours(16), Resource = "r1", IsBlocking = true },
    ];

    private IRenderedComponent<BitFullCalendar> RenderCalendar(Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(11)));
            configure?.Invoke(parameters);
        });
    }

    #region Clock

    [TestMethod]
    public void BitFullCalendarShouldOpenOnTheTodayOfItsTimeProvider()
    {
        var component = RenderCalendar();

        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate);
        Assert.AreEqual(Anchor, component.Instance.State.Today);
    }

    [TestMethod]
    public void BitFullCalendarShouldMarkAndReturnToTheTodayOfItsTimeProvider()
    {
        var component = RenderCalendar(p => p.Add(x => x.DefaultDate, Anchor.AddMonths(-2)));

        component.Instance.GoToToday();
        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate);

        component.WaitForAssertion(() =>
        {
            var today = component.Find(".bit-bfc-month-cell-day.today");
            Assert.AreEqual(Anchor.Day.ToString(), today.TextContent.Trim());
        });
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepADefaultDateOverTheClock()
    {
        var component = RenderCalendar(p => p.Add(x => x.DefaultDate, Anchor.AddDays(-40)));

        Assert.AreEqual(Anchor.AddDays(-40), component.Instance.State.SelectedDate);
    }

    [TestMethod]
    public void BitFullCalendarShouldReportTheRangeAClockChangeMovesTo()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderCalendar(p => p.Add(x => x.OnDateChange, (BitFullCalendarDateChangeEventArgs r) => ranges.Add(r)));
        ranges.Clear();

        component.Render(p => p.Add(x => x.TimeProvider, new FixedTimeProvider(Anchor.AddMonths(1).AddHours(11))));

        Assert.AreEqual(Anchor.AddMonths(1), component.Instance.State.SelectedDate);
        component.WaitForAssertion(() => Assert.IsTrue(ranges.Any(r => r.Start <= Anchor.AddMonths(1) && r.End >= Anchor.AddMonths(1))));
    }

    #endregion

    #region Background events

    [TestMethod]
    public void BitFullCalendarShouldKeepBackgroundEventsOutOfTheEventList()
    {
        var component = RenderCalendar();
        var state = component.Instance.State;

        CollectionAssert.AreEquivalent(new[] { "1", "upkeep" }, state.Events.Select(e => e.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "lunch", "holiday" }, state.BackgroundEvents.Select(e => e.Id).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarShouldDrawBackgroundEventsAsInertBandsInTheTimeGrid()
    {
        var component = RenderCalendar(p => p.Add(x => x.DefaultView, BitFullCalendarView.Week));

        var bands = component.FindAll(".bit-bfc-bg-event");
        var lunch = bands.Single(b => b.TextContent.Contains("Lunch"));
        Assert.IsNull(lunch.GetAttribute("role"));
        Assert.IsNull(lunch.GetAttribute("tabindex"));
        Assert.AreEqual("true", lunch.GetAttribute("aria-hidden"), "the slots speak the band while the grid is editable");
        Assert.IsTrue(lunch.ClassList.Contains("bit-bfc-bg-event-blocking"));

        // The all-day holiday fills the next day's whole column, and is named in that day's sticky header instead of at
        // 00:00, which the grid usually has scrolled away.
        var holidayBand = bands.Single(b => b.TextContent.Contains("Lunch") is false);
        Assert.AreEqual(string.Empty, holidayBand.TextContent.Trim(), "the holiday band carries no title of its own");
        var label = component.Find(".bit-bfc-week-header .bit-bfc-bg-event-label");
        Assert.AreEqual("Holiday", label.TextContent.Trim());

        // Neither is an event card the keyboard or the pointer could pick up.
        Assert.IsFalse(component.FindAll("[data-bit-bfc-event]").Any(e => e.GetAttribute("data-bit-bfc-event") is "lunch" or "holiday"));
    }

    [TestMethod]
    public void BitFullCalendarShouldSpeakABackgroundEventWithTheSlotsItCovers()
    {
        var component = RenderCalendar(p => p.Add(x => x.DefaultView, BitFullCalendarView.Day));

        var covered = component.FindAll("[role=button][aria-label]")
                               .Where(s => s.GetAttribute("aria-label")!.EndsWith(", Lunch", StringComparison.Ordinal))
                               .ToList();
        Assert.AreEqual(2, covered.Count, "12:00 and 12:30 lie under the lunch band");
    }

    [TestMethod]
    public void BitFullCalendarShouldTintOnlyTheWholeDaysABackgroundEventCovers()
    {
        var component = RenderCalendar();

        var tinted = component.FindAll(".bit-bfc-bg-event-day");
        Assert.AreEqual(1, tinted.Count, "the timed lunch break has no clock axis to be drawn on in the month grid");
        StringAssert.Contains(tinted[0].TextContent, "Holiday");
    }

    [TestMethod]
    public void BitFullCalendarShouldLeaveBackgroundTitlesReadableWhenReadOnly()
    {
        var component = RenderCalendar(p =>
        {
            p.Add(x => x.DefaultView, BitFullCalendarView.Week);
            p.Add(x => x.ReadOnly, true);
        });

        Assert.IsTrue(component.FindAll(".bit-bfc-bg-event").All(b => b.HasAttribute("aria-hidden") is false));
    }

    #endregion

    #region Blocking

    [TestMethod]
    public void BitFullCalendarShouldRefuseARangeOverlappingABlockingEvent()
    {
        var state = RenderCalendar().Instance.State;

        Assert.IsTrue(state.AllowEventOverlap, "blocking holds even while overlaps are allowed");
        Assert.AreEqual(BitFullCalendarChangeRefusal.Blocked, state.ValidateRange("1", Anchor.AddHours(12).AddMinutes(30), Anchor.AddHours(14), null));
        Assert.AreEqual(BitFullCalendarChangeRefusal.Blocked, state.ValidateRange("1", Anchor.AddHours(12), Anchor.AddHours(13), "r2"),
                        "a blocker without a resource blocks every resource");
        Assert.AreEqual(BitFullCalendarChangeRefusal.None, state.ValidateRange("1", Anchor.AddHours(13), Anchor.AddHours(14), null),
                        "the end is exclusive");
    }

    [TestMethod]
    public void BitFullCalendarShouldBlockOnlyTheResourceOfAResourceBlocker()
    {
        var state = RenderCalendar().Instance.State;

        Assert.AreEqual(BitFullCalendarChangeRefusal.Blocked, state.ValidateRange("1", Anchor.AddHours(15), Anchor.AddHours(16), "r1"));
        Assert.AreEqual(BitFullCalendarChangeRefusal.None, state.ValidateRange("1", Anchor.AddHours(15), Anchor.AddHours(16), "r2"));
        Assert.AreEqual(BitFullCalendarChangeRefusal.None, state.ValidateRange("upkeep", Anchor.AddHours(15), Anchor.AddHours(16), "r1"),
                        "an event never blocks itself");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotCountANonBlockingBackgroundEventAsAnOverlap()
    {
        var state = RenderCalendar(p => p.Add(x => x.Settings, new BitFullCalendarSettings { AllowEventOverlap = false })).Instance.State;

        Assert.AreEqual(BitFullCalendarChangeRefusal.None, state.ValidateRange("1", Anchor.AddDays(1).AddHours(9), Anchor.AddDays(1).AddHours(10), null),
                        "the holiday is context, not a booking");
        Assert.AreEqual(BitFullCalendarChangeRefusal.Overlap, state.ValidateRange("new", Anchor.AddHours(9), Anchor.AddHours(10), null));
    }

    [TestMethod]
    public void BitFullCalendarShouldRefuseADropOntoABlockingEvent()
    {
        var state = RenderCalendar().Instance.State;

        var standup = state.Events.Single(e => e.Id == "1");
        state.StartDrag(standup);
        var refusal = state.HandleDrop(Anchor, 12, 0);

        Assert.AreEqual(BitFullCalendarChangeRefusal.Blocked, refusal);
        Assert.AreEqual(Anchor.AddHours(9), state.Events.Single(e => e.Id == "1").StartDate, "the event stays where it was");
    }

    #endregion

    #region Drafts and notices

    [TestMethod]
    public void BitFullCalendarShouldHandOutAOneSlotDraftFromTheAddButton()
    {
        BitFullCalendarEvent? draft = null;
        var component = RenderCalendar(p =>
        {
            p.Add(x => x.Settings, new BitFullCalendarSettings { SlotDurationMinutes = 15 });
            p.Add(x => x.OnAddClick, (BitFullCalendarEvent? e) => draft = e);
        });

        component.Find(".bit-bfc-btn-primary").Click();

        Assert.IsNotNull(draft);
        Assert.AreEqual(TimeSpan.FromMinutes(15), draft.Duration);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderItsLiveRegionsBeforeAnyNotice()
    {
        var component = RenderCalendar();

        Assert.AreEqual(1, component.FindAll(".bit-bfc-toast-container [role=alert]").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-toast-container [role=status]").Count);
    }

    #endregion

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}
