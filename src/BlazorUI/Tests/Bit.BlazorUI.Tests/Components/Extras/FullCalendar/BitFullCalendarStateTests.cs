using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the view-restriction and read-only rules on the shared calendar state, which is where
/// every entry point (view tabs, bound parameters, indirect navigation) is funnelled through.
/// </summary>
[TestClass]
public class BitFullCalendarStateTests
{
    private static BitFullCalendarState CreateState()
    {
        var state = new BitFullCalendarState();
        state.Initialize([]);
        return state;
    }

    private static List<BitFullCalendarResource> Resources() =>
    [
        new() { Id = "r1", Title = "Room 1" }
    ];

    [TestMethod]
    public void ViewsShouldDefaultToEveryViewInDeclarationOrder()
    {
        var state = CreateState();

        CollectionAssert.AreEqual(
            new[]
            {
                BitFullCalendarView.Day,
                BitFullCalendarView.Week,
                BitFullCalendarView.Month,
                BitFullCalendarView.Year,
                BitFullCalendarView.Agenda
            },
            state.Views.ToArray());
    }

    [TestMethod]
    public void SyncViewsShouldKeepTheSuppliedOrder()
    {
        var state = CreateState();

        state.SyncViews([BitFullCalendarView.Agenda, BitFullCalendarView.Day]);

        CollectionAssert.AreEqual(
            new[] { BitFullCalendarView.Agenda, BitFullCalendarView.Day },
            state.Views.ToArray());
    }

    [TestMethod]
    public void SyncViewsShouldRestoreEveryViewForNullOrEmpty()
    {
        var state = CreateState();
        state.SyncViews([BitFullCalendarView.Day]);

        state.SyncViews(null);
        Assert.AreEqual(5, state.Views.Count);

        state.SyncViews([BitFullCalendarView.Day]);
        state.SyncViews([]);
        Assert.AreEqual(5, state.Views.Count);
    }

    [TestMethod]
    public void SyncViewsShouldDropRepeatedAndUndefinedEntries()
    {
        var state = CreateState();

        state.SyncViews(
        [
            BitFullCalendarView.Week,
            BitFullCalendarView.Week,
            (BitFullCalendarView)42,
            BitFullCalendarView.Day
        ]);

        CollectionAssert.AreEqual(
            new[] { BitFullCalendarView.Week, BitFullCalendarView.Day },
            state.Views.ToArray());
    }

    [TestMethod]
    public void SyncViewsShouldFallBackToEveryViewWhenNothingSurvivesNormalization()
    {
        var state = CreateState();

        state.SyncViews([(BitFullCalendarView)42, (BitFullCalendarView)43]);

        Assert.AreEqual(5, state.Views.Count);
    }

    [TestMethod]
    public void SyncViewsShouldClampTheActiveViewIntoTheNewSet()
    {
        var state = CreateState();
        Assert.AreEqual(BitFullCalendarView.Month, state.View);

        state.SyncViews([BitFullCalendarView.Week, BitFullCalendarView.Day]);

        Assert.AreEqual(BitFullCalendarView.Week, state.View);
    }

    [TestMethod]
    public void SyncViewsShouldLeaveAnAllowedActiveViewAlone()
    {
        var state = CreateState();

        state.SyncViews([BitFullCalendarView.Agenda, BitFullCalendarView.Month]);

        Assert.AreEqual(BitFullCalendarView.Month, state.View);
    }

    [TestMethod]
    public void SetViewShouldClampAnExcludedViewToTheFirstAllowedOne()
    {
        var state = CreateState();
        state.SyncViews([BitFullCalendarView.Week, BitFullCalendarView.Day]);

        state.SetView(BitFullCalendarView.Year);

        Assert.AreEqual(BitFullCalendarView.Week, state.View);
    }

    [TestMethod]
    public void IsViewAvailableShouldReflectTheAllowedSet()
    {
        var state = CreateState();
        state.SyncViews([BitFullCalendarView.Week, BitFullCalendarView.Day]);

        Assert.IsTrue(state.IsViewAvailable(BitFullCalendarView.Week));
        Assert.IsFalse(state.IsViewAvailable(BitFullCalendarView.Month));
    }

    [TestMethod]
    public void AvailableViewsShouldDropTheNonTimelineViewsInTimelineMode()
    {
        var state = CreateState();
        state.SyncResources(Resources());
        state.SetMode(BitFullCalendarMode.Timeline);

        CollectionAssert.AreEqual(
            new[] { BitFullCalendarView.Day, BitFullCalendarView.Week, BitFullCalendarView.Month },
            state.AvailableViews.ToArray());
    }

    [TestMethod]
    public void TimelineModeShouldKeepFallingBackToTheWeekLayoutWhenWeekIsAllowed()
    {
        var state = CreateState();
        state.SyncResources(Resources());
        state.SetView(BitFullCalendarView.Year);

        state.SetMode(BitFullCalendarMode.Timeline);

        Assert.AreEqual(BitFullCalendarView.Week, state.View);
    }

    [TestMethod]
    public void TimelineModeShouldFallBackToTheFirstAllowedViewWhenWeekIsExcluded()
    {
        var state = CreateState();
        state.SyncResources(Resources());
        state.SyncViews([BitFullCalendarView.Year, BitFullCalendarView.Month, BitFullCalendarView.Day]);
        state.SetView(BitFullCalendarView.Year);

        state.SetMode(BitFullCalendarMode.Timeline);

        Assert.AreEqual(BitFullCalendarMode.Timeline, state.Mode);
        // Week is not allowed, so the clamp lands on the first allowed timeline view instead.
        Assert.AreEqual(BitFullCalendarView.Month, state.View);
    }

    [TestMethod]
    public void TimelineModeShouldBeUnavailableWithoutResources()
    {
        var state = CreateState();

        Assert.IsFalse(state.IsTimelineModeAvailable);

        state.SetMode(BitFullCalendarMode.Timeline);

        Assert.AreEqual(BitFullCalendarMode.Event, state.Mode);
    }

    [TestMethod]
    public void TimelineModeShouldBeUnavailableWhenNoAllowedViewSupportsIt()
    {
        var state = CreateState();
        state.SyncResources(Resources());
        state.SyncViews([BitFullCalendarView.Year, BitFullCalendarView.Agenda]);

        Assert.IsFalse(state.IsTimelineModeAvailable);

        state.SetMode(BitFullCalendarMode.Timeline);

        Assert.AreEqual(BitFullCalendarMode.Event, state.Mode);
    }

    [TestMethod]
    public void SyncViewsShouldLeaveTimelineModeWhenItRemovesEveryTimelineView()
    {
        var state = CreateState();
        state.SyncResources(Resources());
        state.SetMode(BitFullCalendarMode.Timeline);
        Assert.AreEqual(BitFullCalendarMode.Timeline, state.Mode);

        state.SyncViews([BitFullCalendarView.Year, BitFullCalendarView.Agenda]);

        Assert.AreEqual(BitFullCalendarMode.Event, state.Mode);
        Assert.AreEqual(BitFullCalendarView.Year, state.View);
    }

    [TestMethod]
    public void SyncViewsShouldNotNotifyWhenTheSetIsUnchanged()
    {
        var state = CreateState();
        state.SyncViews([BitFullCalendarView.Week, BitFullCalendarView.Day]);

        var notifications = 0;
        state.OnStateChanged += () => notifications++;

        // A fresh list with the same contents must short-circuit: the parameter is re-supplied on
        // every OnParametersSet, and re-notifying there would loop the render.
        state.SyncViews([BitFullCalendarView.Week, BitFullCalendarView.Day]);

        Assert.AreEqual(0, notifications);
    }

    [TestMethod]
    public void SetReadOnlyShouldBlockDragStart()
    {
        var state = CreateState();
        var ev = new BitFullCalendarEvent { Id = "1", Title = "Standup" };

        state.SetReadOnly(true);
        state.StartDrag(ev);

        Assert.IsFalse(state.IsDragging);
        Assert.IsNull(state.DraggedEvent);
    }

    [TestMethod]
    public void SetReadOnlyShouldDropADragThatIsAlreadyInFlight()
    {
        var state = CreateState();
        var ev = new BitFullCalendarEvent { Id = "1", Title = "Standup" };
        state.StartDrag(ev);
        Assert.IsTrue(state.IsDragging);

        state.SetReadOnly(true);

        Assert.IsFalse(state.IsDragging);
    }

    [TestMethod]
    public void ReadOnlyDropShouldNotMoveTheEvent()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var ev = new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = start, EndDate = start.AddHours(1) };
        var state = new BitFullCalendarState();
        state.Initialize([ev]);

        state.SetReadOnly(true);
        state.StartDrag(ev);
        state.HandleDrop(start.Date.AddDays(1), 14, 0);

        var stored = state.AllEvents.Single();
        Assert.AreEqual(start, stored.StartDate);
    }

    [TestMethod]
    public void SetReadOnlyShouldRoundTrip()
    {
        var state = CreateState();
        Assert.IsFalse(state.ReadOnly);

        state.SetReadOnly(true);
        Assert.IsTrue(state.ReadOnly);

        state.SetReadOnly(false);
        Assert.IsFalse(state.ReadOnly);

        var ev = new BitFullCalendarEvent { Id = "1", Title = "Standup" };
        state.StartDrag(ev);
        Assert.IsTrue(state.IsDragging);
    }
}
