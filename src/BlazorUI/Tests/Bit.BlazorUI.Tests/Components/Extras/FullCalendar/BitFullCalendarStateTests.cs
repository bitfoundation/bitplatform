using System;
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

    #region Visible hours, slots, and grid shape

    [TestMethod]
    public void SetVisibleHoursShouldNormalizeAndPullTheScrollAnchorInside()
    {
        var state = CreateState();
        state.SetStartOfDayHour(8);

        state.SetVisibleHours(10, 16);

        Assert.AreEqual(10, state.VisibleStartHour);
        Assert.AreEqual(16, state.VisibleEndHour);
        Assert.AreEqual(6, state.VisibleHourCount);
        CollectionAssert.AreEqual(new[] { 10, 11, 12, 13, 14, 15 }, state.VisibleHours.ToArray());
        Assert.AreEqual(10, state.StartOfDayHour, "the scroll anchor cannot sit outside the rendered window");
    }

    [TestMethod]
    public void SetVisibleHoursShouldCorrectAnInvertedWindow()
    {
        var state = CreateState();

        state.SetVisibleHours(14, 9);

        Assert.AreEqual(14, state.VisibleStartHour);
        Assert.AreEqual(15, state.VisibleEndHour);
    }

    [TestMethod]
    public void SetStartOfDayHourShouldClampIntoTheVisibleWindow()
    {
        var state = CreateState();
        state.SetVisibleHours(8, 18);

        state.SetStartOfDayHour(3);
        Assert.AreEqual(8, state.StartOfDayHour);

        state.SetStartOfDayHour(23);
        Assert.AreEqual(17, state.StartOfDayHour);
    }

    [TestMethod]
    public void SetSlotDurationMinutesShouldNormalizeAndRebuildTheSlotOffsets()
    {
        var state = CreateState();

        state.SetSlotDurationMinutes(15);
        Assert.AreEqual(15, state.SlotDurationMinutes);
        CollectionAssert.AreEqual(new[] { 0, 15, 30, 45 }, state.SlotMinutes);

        // 45 is not a divisor of 60; it rounds to the nearest accepted duration.
        state.SetSlotDurationMinutes(45);
        Assert.AreEqual(60, state.SlotDurationMinutes);
        CollectionAssert.AreEqual(new[] { 0 }, state.SlotMinutes);
    }

    [TestMethod]
    public void SetHiddenDaysShouldNormalizeAndReportTheColumnCount()
    {
        var state = CreateState();

        state.SetHiddenDays([DayOfWeek.Saturday, DayOfWeek.Saturday, DayOfWeek.Sunday]);

        Assert.AreEqual(2, state.HiddenDays.Count);
        Assert.AreEqual(5, state.VisibleWeekDayCount);
    }

    [TestMethod]
    public void SetHiddenDaysShouldRefuseASetThatHidesEveryDay()
    {
        var state = CreateState();

        state.SetHiddenDays([.. System.Enum.GetValues<DayOfWeek>()]);

        Assert.AreEqual(0, state.HiddenDays.Count);
        Assert.AreEqual(7, state.VisibleWeekDayCount);
    }

    [TestMethod]
    public void SetFirstDayOfWeekShouldOverrideTheCulture()
    {
        var state = CreateState();
        var cultureDefault = state.Culture.DateTimeFormat.FirstDayOfWeek;

        state.SetFirstDayOfWeek(DayOfWeek.Wednesday);
        Assert.AreEqual(DayOfWeek.Wednesday, state.FirstDayOfWeek);
        Assert.AreEqual(DayOfWeek.Wednesday, state.FirstDayOfWeekOverride);

        state.SetFirstDayOfWeek(null);
        Assert.AreEqual(cultureDefault, state.FirstDayOfWeek);
        Assert.IsNull(state.FirstDayOfWeekOverride);
    }

    [TestMethod]
    public void SetHiddenDaysShouldNotifyOnlyWhenTheSetChanges()
    {
        var state = CreateState();
        var notifications = 0;
        state.OnStateChanged += () => notifications++;

        state.SetHiddenDays([DayOfWeek.Sunday]);
        var afterFirst = notifications;

        state.SetHiddenDays([DayOfWeek.Sunday]);

        Assert.IsTrue(afterFirst > 0);
        Assert.AreEqual(afterFirst, notifications);
    }

    #endregion

    #region Date bounds

    [TestMethod]
    public void SetDateBoundsShouldPullTheSelectedDateInside()
    {
        var state = CreateState();
        var min = new System.DateTime(2026, 8, 10);
        var max = new System.DateTime(2026, 8, 20);
        state.SetSelectedDate(new System.DateTime(2026, 9, 1));

        state.SetDateBounds(min, max);

        Assert.AreEqual(max, state.SelectedDate);
    }

    [TestMethod]
    public void SetDateBoundsShouldIgnoreAnInvertedWindow()
    {
        var state = CreateState();

        state.SetDateBounds(new System.DateTime(2026, 8, 20), new System.DateTime(2026, 8, 10));

        Assert.IsNull(state.MinDate);
        Assert.IsNull(state.MaxDate);
    }

    [TestMethod]
    public void ClampToAllowedRangeShouldKeepTheTimeOfDay()
    {
        var state = CreateState();
        state.SetDateBounds(new System.DateTime(2026, 8, 10), new System.DateTime(2026, 8, 20));

        var clamped = state.ClampToAllowedRange(new System.DateTime(2026, 8, 25, 14, 30, 0));

        Assert.AreEqual(new System.DateTime(2026, 8, 20, 14, 30, 0), clamped);
    }

    [TestMethod]
    public void IsDateInAllowedRangeShouldFollowTheBounds()
    {
        var state = CreateState();
        state.SetDateBounds(new System.DateTime(2026, 8, 10), new System.DateTime(2026, 8, 20));

        Assert.IsFalse(state.IsDateInAllowedRange(new System.DateTime(2026, 8, 9)));
        Assert.IsTrue(state.IsDateInAllowedRange(new System.DateTime(2026, 8, 10)));
        Assert.IsTrue(state.IsDateInAllowedRange(new System.DateTime(2026, 8, 20, 23, 0, 0)));
        Assert.IsFalse(state.IsDateInAllowedRange(new System.DateTime(2026, 8, 21)));
    }

    [TestMethod]
    public void NavigationShouldStopAtTheBounds()
    {
        var state = CreateState();
        state.SetView(BitFullCalendarView.Day);
        state.SetDateBounds(new System.DateTime(2026, 8, 10), new System.DateTime(2026, 8, 12));
        state.SetSelectedDate(new System.DateTime(2026, 8, 10));

        Assert.IsFalse(state.CanNavigatePrevious);
        state.NavigatePrevious();
        Assert.AreEqual(new System.DateTime(2026, 8, 10), state.SelectedDate);

        Assert.IsTrue(state.CanNavigateNext);
        state.NavigateNext();
        Assert.AreEqual(new System.DateTime(2026, 8, 11), state.SelectedDate);

        state.NavigateNext();
        Assert.AreEqual(new System.DateTime(2026, 8, 12), state.SelectedDate);
        Assert.IsFalse(state.CanNavigateNext);
        state.NavigateNext();
        Assert.AreEqual(new System.DateTime(2026, 8, 12), state.SelectedDate);
    }

    [TestMethod]
    public void NavigationShouldStayUnboundedWithoutBounds()
    {
        var state = CreateState();

        Assert.IsTrue(state.CanNavigatePrevious);
        Assert.IsTrue(state.CanNavigateNext);
    }

    #endregion

    #region Booking rules

    [TestMethod]
    public void IsRangeAvailableShouldAlwaysAllowWhileOverlapsAreAllowed()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var state = new BitFullCalendarState();
        state.Initialize([new() { Id = "1", Title = "Standup", StartDate = start, EndDate = start.AddHours(1) }]);

        Assert.IsTrue(state.IsRangeAvailable("2", start, start.AddHours(1), null));
    }

    [TestMethod]
    public void IsRangeAvailableShouldRefuseAnOverlapOnTheSameResource()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var state = new BitFullCalendarState();
        state.Initialize([new() { Id = "1", Title = "Standup", StartDate = start, EndDate = start.AddHours(1), Resource = "r1" }]);
        state.SetAllowEventOverlap(false);

        Assert.IsFalse(state.IsRangeAvailable("2", start.AddMinutes(30), start.AddMinutes(90), "r1"));
        // A different resource lane never collides.
        Assert.IsTrue(state.IsRangeAvailable("2", start.AddMinutes(30), start.AddMinutes(90), "r2"));
        // Nor does the event with itself.
        Assert.IsTrue(state.IsRangeAvailable("1", start.AddMinutes(30), start.AddMinutes(90), "r1"));
        // Nor a range that merely touches.
        Assert.IsTrue(state.IsRangeAvailable("2", start.AddHours(1), start.AddHours(2), "r1"));
    }

    [TestMethod]
    public void HandleDropShouldRefuseAnOverlapAndLeaveTheEventPut()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var moving = new BitFullCalendarEvent { Id = "2", Title = "Review", StartDate = start.AddHours(4), EndDate = start.AddHours(5) };
        var state = new BitFullCalendarState();
        state.Initialize([new() { Id = "1", Title = "Standup", StartDate = start, EndDate = start.AddHours(1) }, moving]);
        state.SetAllowEventOverlap(false);

        state.StartDrag(moving);
        var refusal = state.HandleDrop(start.Date, 9, 0);

        Assert.AreEqual(BitFullCalendarChangeRefusal.Overlap, refusal);
        Assert.AreEqual(start.AddHours(4), state.AllEvents.Single(e => e.Id == "2").StartDate);
        Assert.IsFalse(state.IsDragging);
    }

    [TestMethod]
    public void HandleDropShouldRefuseATargetOutsideTheAllowedRange()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var moving = new BitFullCalendarEvent { Id = "1", Title = "Review", StartDate = start, EndDate = start.AddHours(1) };
        var state = new BitFullCalendarState();
        state.Initialize([moving]);
        state.SetDateBounds(new System.DateTime(2026, 8, 10), new System.DateTime(2026, 8, 20));

        state.StartDrag(moving);
        var refusal = state.HandleDrop(new System.DateTime(2026, 9, 1), 9, 0);

        Assert.AreEqual(BitFullCalendarChangeRefusal.OutOfRange, refusal);
        Assert.AreEqual(start, state.AllEvents.Single().StartDate);
    }

    [TestMethod]
    public void HandleDropShouldCommitAnAllowedMoveAndKeepTheEventFlags()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var moving = new BitFullCalendarEvent
        {
            Id = "1",
            Title = "Review",
            StartDate = start,
            EndDate = start.AddHours(1),
            IsAllDay = true,
            CssClass = "custom"
        };
        var state = new BitFullCalendarState();
        state.Initialize([moving]);

        state.StartDrag(moving);
        var refusal = state.HandleDrop(start.Date.AddDays(1), 14, 0);

        Assert.AreEqual(BitFullCalendarChangeRefusal.None, refusal);
        var stored = state.AllEvents.Single();
        Assert.AreEqual(start.Date.AddDays(1).AddHours(14), stored.StartDate);
        Assert.IsTrue(stored.IsAllDay);
        Assert.AreEqual("custom", stored.CssClass);
    }

    [TestMethod]
    public void StartDragShouldRefuseAnEventThatIsLockedOnItsOwn()
    {
        var start = new System.DateTime(2026, 8, 13, 9, 0, 0);
        var locked = new BitFullCalendarEvent { Id = "1", Title = "Payroll", StartDate = start, EndDate = start.AddHours(1), IsReadOnly = true };
        var state = new BitFullCalendarState();
        state.Initialize([locked]);

        state.StartDrag(locked);

        Assert.IsFalse(state.IsDragging);
    }

    #endregion

    #region Recurrence

    [TestMethod]
    public void EventsShouldExpandARecurringMasterIntoTheVisibleRange()
    {
        var today = DateTime.Today;
        var state = new BitFullCalendarState();
        state.Initialize(
        [
            new()
            {
                Id = "series",
                Title = "Standup",
                StartDate = today.AddHours(9),
                EndDate = today.AddHours(10),
                Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
            }
        ]);
        state.SetView(BitFullCalendarView.Day);
        state.SetSelectedDate(today);

        Assert.AreEqual(1, state.AllEvents.Count, "the master stays the single stored event");
        Assert.IsNotNull(state.AllEvents.Single().Recurrence);
        // The day view pads its expansion window by a week either side.
        Assert.IsTrue(state.Events.Count > 1);
        Assert.IsTrue(state.Events.All(e => e.IsOccurrence));
        Assert.IsTrue(state.Events.All(e => e.SeriesId == "series"));
    }

    [TestMethod]
    public void EventsShouldFollowTheSeriesAsTheCalendarNavigates()
    {
        var today = DateTime.Today;
        var state = new BitFullCalendarState();
        state.Initialize(
        [
            new()
            {
                Id = "series",
                Title = "Standup",
                StartDate = today.AddHours(9),
                EndDate = today.AddHours(10),
                Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
            }
        ]);
        state.SetView(BitFullCalendarView.Day);

        state.SetSelectedDate(today.AddDays(60));

        Assert.IsTrue(state.Events.Any(e => e.StartDate.Date == today.AddDays(60).Date),
            "an open-ended series keeps producing occurrences wherever the calendar goes");
    }

    [TestMethod]
    public void ARecurringOccurrenceShouldBlockItsSlotWhenOverlapsAreDisallowed()
    {
        var today = DateTime.Today;
        var mover = new BitFullCalendarEvent { Id = "one-off", Title = "Review", StartDate = today.AddHours(14), EndDate = today.AddHours(15) };
        var state = new BitFullCalendarState();
        state.Initialize(
        [
            new()
            {
                Id = "series",
                Title = "Standup",
                StartDate = today.AddHours(9),
                EndDate = today.AddHours(10),
                Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
            },
            mover
        ]);
        state.SetView(BitFullCalendarView.Day);
        state.SetSelectedDate(today);
        state.SetAllowEventOverlap(false);

        Assert.IsFalse(state.IsRangeAvailable("one-off", today.AddHours(9).AddMinutes(30), today.AddHours(10), null));
        Assert.IsTrue(state.IsRangeAvailable("one-off", today.AddHours(11), today.AddHours(12), null));
        // The series never collides with its own occurrences.
        Assert.IsTrue(state.IsRangeAvailable("series", today.AddHours(9), today.AddHours(10), null));
    }

    [TestMethod]
    public void ARecurringSeriesShouldNotAffectTheStoreOnSync()
    {
        var today = DateTime.Today;
        var master = new BitFullCalendarEvent
        {
            Id = "series",
            Title = "Standup",
            StartDate = today.AddHours(9),
            EndDate = today.AddHours(10),
            Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 3 }
        };
        var state = new BitFullCalendarState();
        state.Initialize([master]);

        state.SyncEvents([master]);

        Assert.AreEqual(1, state.AllEvents.Count);
        Assert.AreSame(master, state.AllEvents.Single());
    }

    #endregion

    #region All-day events

    [TestMethod]
    public void IsAllDayOrMultiDayShouldCoverBothRoutesIntoTheAllDayRow()
    {
        var day = new System.DateTime(2026, 8, 13);

        var timed = new BitFullCalendarEvent { StartDate = day.AddHours(9), EndDate = day.AddHours(10) };
        var allDaySingle = new BitFullCalendarEvent { StartDate = day.AddHours(9), EndDate = day.AddHours(10), IsAllDay = true };
        var multiDay = new BitFullCalendarEvent { StartDate = day.AddHours(9), EndDate = day.AddDays(2) };

        Assert.IsFalse(timed.IsAllDayOrMultiDay);
        Assert.IsTrue(allDaySingle.IsAllDayOrMultiDay);
        Assert.IsTrue(multiDay.IsAllDayOrMultiDay);
        Assert.IsTrue(allDaySingle.IsSingleDay, "the flag does not change how long the event is");
    }

    #endregion

    #region Business hours

    /// <summary>A state whose business week is Monday to Friday, 09:00 to 17:00, and enforced.</summary>
    private static BitFullCalendarState BusinessState(bool restrict = true)
    {
        var state = CreateState();
        state.SetBusinessDays([DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);
        state.SetBusinessHours(9, 17);
        state.SetRestrictToBusinessHours(restrict);
        return state;
    }

    /// <summary>The Monday of the week containing a fixed date, so the tests never depend on "today".</summary>
    private static DateTime Monday => new(2025, 5, 12);
    private static DateTime Saturday => new(2025, 5, 17);

    [TestMethod]
    public void BusinessDaysShouldDefaultToTheWorkingWeek()
    {
        var state = CreateState();

        Assert.IsTrue(state.IsBusinessDay(DayOfWeek.Monday));
        Assert.IsFalse(state.IsBusinessDay(DayOfWeek.Sunday));
        Assert.AreEqual(9, state.BusinessStartHour);
        Assert.AreEqual(17, state.BusinessEndHour);
        Assert.IsFalse(state.HighlightBusinessHours);
        Assert.IsFalse(state.RestrictToBusinessHours);
    }

    [TestMethod]
    public void SetBusinessHoursShouldCorrectAnInvertedWindow()
    {
        var state = CreateState();

        state.SetBusinessHours(17, 9);

        Assert.AreEqual(17, state.BusinessStartHour);
        Assert.AreEqual(18, state.BusinessEndHour);
    }

    [TestMethod]
    public void IsBusinessTimeShouldFollowTheDayAndTheHourWindow()
    {
        var state = BusinessState();

        Assert.IsTrue(state.IsBusinessTime(Monday.AddHours(9)));
        Assert.IsTrue(state.IsBusinessTime(Monday.AddHours(16).AddMinutes(59)));
        // The end is exclusive, the way the rendered window is.
        Assert.IsFalse(state.IsBusinessTime(Monday.AddHours(17)));
        Assert.IsFalse(state.IsBusinessTime(Monday.AddHours(8).AddMinutes(59)));
        Assert.IsFalse(state.IsBusinessTime(Saturday.AddHours(10)));
    }

    [TestMethod]
    public void IsWithinBusinessHoursShouldRequireTheWholeRange()
    {
        var state = BusinessState();

        Assert.IsTrue(state.IsWithinBusinessHours(Monday.AddHours(9), Monday.AddHours(17)));
        Assert.IsFalse(state.IsWithinBusinessHours(Monday.AddHours(8), Monday.AddHours(10)));
        Assert.IsFalse(state.IsWithinBusinessHours(Monday.AddHours(16), Monday.AddHours(18)));
    }

    [TestMethod]
    public void IsWithinBusinessHoursShouldRefuseARangeCrossingANonBusinessDay()
    {
        var state = BusinessState();

        // Friday 09:00 to Monday 17:00 covers the weekend.
        var friday = Monday.AddDays(4);
        Assert.IsFalse(state.IsWithinBusinessHours(friday.AddHours(9), Monday.AddDays(7).AddHours(17)));
    }

    [TestMethod]
    public void IsWithinBusinessHoursShouldJudgeAWholeDayRangeByItsDaysAlone()
    {
        var state = BusinessState();

        // An all-day event covers whole dates and carries no clock time, so only the days count -
        // otherwise no all-day event could ever satisfy a 09:00-17:00 window.
        Assert.IsTrue(state.IsWithinBusinessHours(Monday, Monday.AddDays(1)));
        Assert.IsTrue(state.IsWithinBusinessHours(Monday, Monday.AddDays(3)));
        // The weekend still breaks it.
        Assert.IsFalse(state.IsWithinBusinessHours(Saturday, Saturday.AddDays(1)));
    }

    [TestMethod]
    public void ValidateRangeShouldReportEachRefusalInTurn()
    {
        var state = BusinessState();
        state.SetDateBounds(Monday.AddDays(-7), Monday.AddDays(7));

        Assert.AreEqual(
            BitFullCalendarChangeRefusal.None,
            state.ValidateRange("x", Monday.AddHours(10), Monday.AddHours(11), null));

        Assert.AreEqual(
            BitFullCalendarChangeRefusal.OutsideBusinessHours,
            state.ValidateRange("x", Monday.AddHours(7), Monday.AddHours(8), null));

        Assert.AreEqual(
            BitFullCalendarChangeRefusal.OutOfRange,
            state.ValidateRange("x", Monday.AddDays(30).AddHours(10), Monday.AddDays(30).AddHours(11), null));
    }

    [TestMethod]
    public void ValidateRangeShouldStayOutOfTheWayWhenNothingIsEnforced()
    {
        var state = CreateState();

        // No bounds, overlaps allowed, business hours only shading: every range is committable.
        Assert.AreEqual(
            BitFullCalendarChangeRefusal.None,
            state.ValidateRange("x", Saturday.AddHours(2), Saturday.AddHours(3), null));
    }

    [TestMethod]
    public void ValidateRangeShouldStillReportAnOverlap()
    {
        var state = new BitFullCalendarState();
        state.Initialize(
        [
            new BitFullCalendarEvent { Id = "1", StartDate = Monday.AddHours(10), EndDate = Monday.AddHours(11) }
        ]);
        state.SetSelectedDate(Monday);
        state.SetAllowEventOverlap(false);

        Assert.AreEqual(
            BitFullCalendarChangeRefusal.Overlap,
            state.ValidateRange("2", Monday.AddHours(10).AddMinutes(30), Monday.AddHours(11).AddMinutes(30), null));
    }

    [TestMethod]
    public void HandleDropShouldRefuseATargetOutsideTheBusinessHours()
    {
        var ev = new BitFullCalendarEvent { Id = "1", StartDate = Monday.AddHours(10), EndDate = Monday.AddHours(11) };
        var state = new BitFullCalendarState();
        state.Initialize([ev]);
        state.SetSelectedDate(Monday);
        state.SetBusinessHours(9, 17);
        state.SetRestrictToBusinessHours(true);

        state.StartDrag(ev);
        var refusal = state.HandleDrop(Monday, hour: 6);

        Assert.AreEqual(BitFullCalendarChangeRefusal.OutsideBusinessHours, refusal);
        Assert.AreEqual(Monday.AddHours(10), state.AllEvents[0].StartDate, "the event stays where it was");
        Assert.IsFalse(state.IsDragging);
    }

    [TestMethod]
    public void HandleDropShouldCommitAMoveInsideTheBusinessHours()
    {
        var ev = new BitFullCalendarEvent { Id = "1", StartDate = Monday.AddHours(10), EndDate = Monday.AddHours(11) };
        var state = new BitFullCalendarState();
        state.Initialize([ev]);
        state.SetSelectedDate(Monday);
        state.SetBusinessHours(9, 17);
        state.SetRestrictToBusinessHours(true);

        state.StartDrag(ev);

        Assert.AreEqual(BitFullCalendarChangeRefusal.None, state.HandleDrop(Monday, hour: 14));
        Assert.AreEqual(Monday.AddHours(14), state.AllEvents[0].StartDate);
    }

    #endregion

    #region Month grid preferences

    [TestMethod]
    public void MonthGridPreferencesShouldRoundTrip()
    {
        var state = CreateState();

        Assert.IsFalse(state.FixedWeekCount);
        Assert.IsTrue(state.ShowNonCurrentDates);
        Assert.IsFalse(state.NavLinks);

        state.SetFixedWeekCount(true);
        state.SetShowNonCurrentDates(false);
        state.SetNavLinks(true);

        Assert.IsTrue(state.FixedWeekCount);
        Assert.IsFalse(state.ShowNonCurrentDates);
        Assert.IsTrue(state.NavLinks);
    }

    [TestMethod]
    public void MonthGridPreferencesShouldNotifyOnlyWhenTheyChange()
    {
        var state = CreateState();
        var notifications = 0;
        state.OnStateChanged += () => notifications++;

        state.SetFixedWeekCount(false);
        Assert.AreEqual(0, notifications);

        state.SetFixedWeekCount(true);
        Assert.AreEqual(1, notifications);
    }

    #endregion

    #region Date range reporting

    [TestMethod]
    public void TheDateRangeShouldBeReportedOnlyWhenItActuallyMoves()
    {
        var state = CreateState();
        state.SetSelectedDate(Monday);
        state.SetView(BitFullCalendarView.Week);

        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        state.OnDateRangeChanged += ranges.Add;

        // Another day of the SAME week: the selected date moves, the visible range does not, so
        // consumers are not asked to re-fetch the events they already have.
        state.SetSelectedDate(Monday.AddDays(2));
        Assert.AreEqual(0, ranges.Count);

        state.SetSelectedDate(Monday.AddDays(7));
        Assert.AreEqual(1, ranges.Count);
    }

    [TestMethod]
    public void TheDateRangeShouldBeReportedWhenTheViewChangesItsShape()
    {
        var state = CreateState();
        state.SetSelectedDate(Monday);
        state.SetView(BitFullCalendarView.Week);

        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        state.OnDateRangeChanged += ranges.Add;

        // Same date, a different span around it.
        state.SetView(BitFullCalendarView.Month);

        Assert.AreEqual(1, ranges.Count);
        Assert.AreEqual(BitFullCalendarView.Month, ranges[0].View);
    }

    #endregion
}
