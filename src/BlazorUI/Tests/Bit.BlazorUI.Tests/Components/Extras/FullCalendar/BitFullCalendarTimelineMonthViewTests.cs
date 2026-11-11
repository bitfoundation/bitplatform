using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers <see cref="BitFcTimelineMonthView"/> through the calendar that renders it (the Timeline mode's Month view):
/// the day axis, one lane area per resource plus the unassigned row, how events are laned and placed on the day
/// columns, the day cells as add targets and as one roving tab stop, and the day-precision moves by pointer and key.
/// </summary>
[TestClass]
public class BitFullCalendarTimelineMonthViewTests : BunitTestContext
{
    // A Wednesday of a 31-day month that starts on a Sunday; the calendar's clock is pinned to it.
    private static readonly DateTime Anchor = new(2026, 3, 18);

    private const int DayWidth = BitFullCalendarHelpers.TimelineDayWidthPx;

    private static List<BitFullCalendarResource> Resources() =>
    [
        new() { Id = "r1", Title = "Room 1", Subtitle = "Floor 2" },
        new() { Id = "r2", Title = "Room 2" },
    ];

    private static BitFullCalendarEvent Event(string id, DateTime start, DateTime end, string? resource = "r1")
        => new() { Id = id, Title = id, StartDate = start, EndDate = end, Resource = resource };

    private IRenderedComponent<BitFullCalendar> RenderTimelineMonth(List<BitFullCalendarEvent>? events = null,
                                                                    Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null,
                                                                    DateTime? date = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, events ?? []);
            parameters.Add(p => p.Resources, Resources());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, date ?? Anchor);
            parameters.Add(p => p.DefaultMode, BitFullCalendarMode.Timeline);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Month);
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(12)));
            configure?.Invoke(parameters);
        });
    }

    /// <summary>The day cells of one rendered row, the resource rows first and the unassigned row last.</summary>
    private static List<AngleSharp.Dom.IElement> RowCells(IRenderedComponent<BitFullCalendar> component, int row)
        => [.. component.FindAll(".bit-bfc-tl-body-row")[row].QuerySelectorAll(".bit-bfc-tl-cell-day")];

    private static AngleSharp.Dom.IElement RovingCell(IRenderedComponent<BitFullCalendar> component)
        => component.FindAll(".bit-bfc-tl-cell-day").Single(c => c.GetAttribute("tabindex") == "0");

    #region Day axis and rows

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldRenderOneColumnPerDayOfTheMonth()
    {
        var component = RenderTimelineMonth();

        var days = component.FindAll(".bit-bfc-tl-time-row-slots .bit-bfc-tl-time-cell");
        Assert.HasCount(31, days);
        // The day number and the culture's shortest day name.
        var shortest = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
        Assert.AreEqual($"1 {shortest.GetShortestDayName(DayOfWeek.Sunday)}", days[0].TextContent.Trim());
        Assert.AreEqual($"31 {shortest.GetShortestDayName(DayOfWeek.Tuesday)}", days[^1].TextContent.Trim());
        Assert.IsTrue(days.All(d => d.GetAttribute("style") == $"width:{DayWidth}px;"));

        Assert.AreEqual("Resource", component.Find(".bit-bfc-tl-corner").TextContent.Trim());
        Assert.Contains($"width:{31 * DayWidth}px;", component.Find(".bit-bfc-tl-time-header").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldMarkTodayAsTheScrollTarget()
    {
        var component = RenderTimelineMonth();

        var days = component.FindAll(".bit-bfc-tl-time-row-slots .bit-bfc-tl-time-cell");
        var today = days.Single(d => d.ClassList.Contains("today"));
        Assert.AreEqual($"18 {CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetShortestDayName(DayOfWeek.Wednesday)}", today.TextContent.Trim());
        Assert.AreEqual("true", today.GetAttribute("data-bit-bfc-tl-scroll-target"));
        Assert.ContainsSingle(d => d.HasAttribute("data-bit-bfc-tl-scroll-target"), days);

        Context.JSInterop.VerifyInvoke("BitBlazorUI.FullCalendar.scrollTimelineToTarget");
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldNotMarkTodayInAnotherMonth()
    {
        var component = RenderTimelineMonth(date: new DateTime(2026, 4, 10));

        var days = component.FindAll(".bit-bfc-tl-time-row-slots .bit-bfc-tl-time-cell");
        Assert.HasCount(30, days);
        Assert.AreEqual(0, days.Count(d => d.ClassList.Contains("today")));
        Assert.AreEqual(0, days.Count(d => d.HasAttribute("data-bit-bfc-tl-scroll-target")));
        Assert.IsEmpty(component.FindAll(".bit-bfc-tl-now"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldRenderOneRowPerResource()
    {
        var component = RenderTimelineMonth();

        var cells = component.FindAll(".bit-bfc-tl-resource-cell");
        Assert.HasCount(2, cells, "no unassigned row while every event has a resource");
        Assert.AreEqual("Room 1", cells[0].QuerySelector(".bit-bfc-tl-resource-title")!.TextContent.Trim());
        Assert.AreEqual("Floor 2", cells[0].QuerySelector(".bit-bfc-tl-resource-subtitle")!.TextContent.Trim());
        Assert.IsNull(cells[1].QuerySelector(".bit-bfc-tl-resource-subtitle"));

        Assert.HasCount(31, RowCells(component, 0));
        Assert.HasCount(31, RowCells(component, 1));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldRenderTheResourceTemplate()
    {
        var component = RenderTimelineMonth(configure: p =>
            p.Add(c => c.ResourceTemplate, (BitFullCalendarResource r) => $"<b class='custom-resource'>{r.Id}</b>"));

        Assert.AreSequenceEqual(new[] { "r1", "r2" }, component.FindAll(".custom-resource").Select(r => r.TextContent).ToArray());
        Assert.IsEmpty(component.FindAll(".bit-bfc-tl-resource-title"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldPutUnassignedEventsInARowOfTheirOwn()
    {
        var component = RenderTimelineMonth(
        [
            Event("nobody", Anchor.AddHours(9), Anchor.AddHours(10), resource: null),
            Event("ghost", Anchor.AddDays(1).AddHours(9), Anchor.AddDays(1).AddHours(10), resource: "deleted-room"),
            Event("room1", Anchor.AddHours(9), Anchor.AddHours(10)),
        ]);

        var rows = component.FindAll(".bit-bfc-tl-body-row");
        Assert.HasCount(3, rows);

        var unassigned = rows[2];
        Assert.IsNotNull(unassigned.QuerySelector(".bit-bfc-tl-resource-cell-unassigned"));
        Assert.AreEqual("Unassigned", unassigned.QuerySelector(".bit-bfc-tl-resource-title")!.TextContent.Trim());

        // An unknown resource id lands there too, rather than vanishing.
        Assert.AreSequenceEqual(
            new[] { "nobody", "ghost" }, unassigned.QuerySelectorAll("[data-bit-bfc-event]").Select(e => e.GetAttribute("data-bit-bfc-event")).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
        Assert.AreSequenceEqual(
            new[] { "room1" }, rows[0].QuerySelectorAll("[data-bit-bfc-event]").Select(e => e.GetAttribute("data-bit-bfc-event")).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldNameTheUnassignedRowThroughTheTexts()
    {
        var component = RenderTimelineMonth(
            [Event("nobody", Anchor.AddHours(9), Anchor.AddHours(10), resource: null)],
            p => p.Add(c => c.Texts, new BitFullCalendarTexts { NoResourceLabel = "Nobody", ResourceColumnHeader = "Room" }));

        Assert.AreEqual("Nobody", component.Find(".bit-bfc-tl-resource-cell-unassigned").TextContent.Trim());
        Assert.AreEqual("Room", component.Find(".bit-bfc-tl-corner").TextContent.Trim());
    }

    #endregion

    #region Lanes and placement

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldStackOverlappingEventsInLanes()
    {
        var component = RenderTimelineMonth(
        [
            Event("a", new DateTime(2026, 3, 3, 9, 0, 0), new DateTime(2026, 3, 5, 17, 0, 0)),
            Event("b", new DateTime(2026, 3, 4, 9, 0, 0), new DateTime(2026, 3, 4, 10, 0, 0)),
            // Starts the day after "a" ends, so it shares the first lane.
            Event("c", new DateTime(2026, 3, 6, 9, 0, 0), new DateTime(2026, 3, 6, 10, 0, 0)),
        ]);

        var rows = component.FindAll(".bit-bfc-tl-body-row");
        // Two lanes of 40px, a 4px gap and 4px padding on either side; an empty row keeps the 60px minimum.
        Assert.Contains("height:92px;", rows[0].GetAttribute("style")!);
        Assert.Contains("height:60px;", rows[1].GetAttribute("style")!);

        string Place(string id) => component.Find($"[data-bit-bfc-event='{id}']").ParentElement!.GetAttribute("style")!;
        Assert.Contains("top:4px;", Place("a"));
        Assert.Contains("top:48px;", Place("b"));
        Assert.Contains("top:4px;", Place("c"));
        Assert.Contains("height:40px;", Place("a"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldPlaceAnEventOnTheDaysItCovers()
    {
        var component = RenderTimelineMonth(
        [
            Event("span", new DateTime(2026, 3, 3, 9, 0, 0), new DateTime(2026, 3, 5, 17, 0, 0)),
            // A midnight end does not take the day it falls on.
            Event("midnight", new DateTime(2026, 3, 10), new DateTime(2026, 3, 12), resource: "r2"),
        ]);

        string Style(string id) => component.Find($"[data-bit-bfc-event='{id}']").ParentElement!.GetAttribute("style")!;

        Assert.Contains($"inset-inline-start:{2 * DayWidth}.00px;", Style("span"));
        Assert.Contains($"width:{3 * DayWidth - 2}.00px;", Style("span"));
        Assert.Contains($"inset-inline-start:{9 * DayWidth}.00px;", Style("midnight"));
        Assert.Contains($"width:{2 * DayWidth - 2}.00px;", Style("midnight"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldClipAnEventToTheMonth()
    {
        var component = RenderTimelineMonth(
        [
            Event("from-feb", new DateTime(2026, 2, 26, 9, 0, 0), new DateTime(2026, 3, 2, 10, 0, 0)),
            Event("into-apr", new DateTime(2026, 3, 30, 9, 0, 0), new DateTime(2026, 4, 3, 10, 0, 0)),
            Event("april", new DateTime(2026, 4, 6, 9, 0, 0), new DateTime(2026, 4, 6, 10, 0, 0)),
        ]);

        string Style(string id) => component.Find($"[data-bit-bfc-event='{id}']").ParentElement!.GetAttribute("style")!;

        Assert.Contains("inset-inline-start:0.00px;", Style("from-feb"));
        Assert.Contains($"width:{2 * DayWidth - 2}.00px;", Style("from-feb"));
        Assert.Contains($"inset-inline-start:{29 * DayWidth}.00px;", Style("into-apr"));
        Assert.Contains($"width:{2 * DayWidth - 2}.00px;", Style("into-apr"));
        Assert.IsEmpty(component.FindAll("[data-bit-bfc-event='april']"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldRenderAnEventAsANamedButton()
    {
        var component = RenderTimelineMonth([Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))]);

        var block = component.Find("[data-bit-bfc-event='Standup']");
        Assert.AreEqual("button", block.GetAttribute("role"));
        Assert.AreEqual("0", block.GetAttribute("tabindex"));
        Assert.StartsWith("Standup, ", block.GetAttribute("aria-label"));
        Assert.AreEqual("Standup", block.QuerySelector(".bit-bfc-timeline-event-title")!.TextContent.Trim());
        Assert.AreEqual("true", block.GetAttribute("draggable"));
        Assert.AreEqual(2, block.QuerySelectorAll(".bit-bfc-resize-handle-h").Length);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldRenderTheTimelineEventTemplate()
    {
        var component = RenderTimelineMonth(
            [Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))],
            p => p.Add(c => c.TimelineEventTemplate, (BitFullCalendarEvent e) => $"<i class='custom-event'>{e.Title}!</i>"));

        Assert.AreEqual("Standup!", component.Find("[data-bit-bfc-event='Standup'] .custom-event").TextContent);
        Assert.IsEmpty(component.FindAll(".bit-bfc-timeline-event-title"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthDotVariantShouldDrawTheEventsColor()
    {
        var events = new List<BitFullCalendarEvent> { Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10)) };
        events[0].Color = "#2563eb";
        var component = RenderTimelineMonth(events, p => p.Add(c => c.Settings, new BitFullCalendarSettings { BadgeVariant = BitFullCalendarBadgeVariant.Dot }));

        Assert.Contains("--bit-bfc-evt-color:#2563eb;", component.Find("[data-bit-bfc-event='Standup'] .bit-bfc-bullet").GetAttribute("style")!);
    }

    #endregion

    #region Clicking events and day cells

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldHandAClickedEventToOnEventClick()
    {
        BitFullCalendarEvent? clicked = null;
        var component = RenderTimelineMonth(
            [Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))],
            p => p.Add(c => c.OnEventClick, (BitFullCalendarEvent e) => clicked = e));

        component.Find("[data-bit-bfc-event='Standup']").Click();
        var actualValue = clicked?.Id;

        Assert.AreEqual("Standup", actualValue);
        Assert.IsEmpty(component.FindAll(".bit-bfc-dialog"), "the app's handler replaces the details dialog");
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldOpenTheDetailsOfAClickedEvent()
    {
        var component = RenderTimelineMonth([Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))]);

        component.Find("[data-bit-bfc-event='Standup']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        var dialog = component.Find(".bit-bfc-dialog");
        Assert.Contains("Standup", dialog.TextContent);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldDraftAnEventOnTheClickedDayAndResource()
    {
        BitFullCalendarEvent? draft = null;
        var component = RenderTimelineMonth(configure: p => p.Add(c => c.OnAddClick, (BitFullCalendarEvent? e) => draft = e));

        RowCells(component, 1)[19].Click();

        Assert.IsNotNull(draft);
        Assert.AreEqual("r2", draft.Resource);
        // The draft starts at the start of the working day, one slot long.
        Assert.AreEqual(new DateTime(2026, 3, 20, 8, 0, 0), draft.StartDate);
        Assert.AreEqual(new DateTime(2026, 3, 20, 8, 30, 0), draft.EndDate);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldDraftAnUnassignedEventFromTheUnassignedRow()
    {
        BitFullCalendarEvent? draft = null;
        var component = RenderTimelineMonth(
            [Event("nobody", Anchor.AddHours(9), Anchor.AddHours(10), resource: null)],
            p => p.Add(c => c.OnAddClick, (BitFullCalendarEvent? e) => draft = e));

        RowCells(component, 2)[0].Click();

        Assert.IsNotNull(draft);
        Assert.IsNull(draft.Resource);
        Assert.AreEqual(new DateTime(2026, 3, 1, 8, 0, 0), draft.StartDate);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldOpenTheAddDialogWithoutOnAddClick()
    {
        var component = RenderTimelineMonth();

        RowCells(component, 0)[4].Click();

        Assert.IsNotEmpty(component.FindAll(".bit-bfc-field"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldAddFromTheKeyboardOnce()
    {
        var drafts = 0;
        var component = RenderTimelineMonth(configure: p => p.Add(c => c.OnAddClick, (BitFullCalendarEvent? _) => drafts++));

        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = "Enter" });
        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = " " });
        // A held key repeats, and a repeat must not draft again.
        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = "Enter", Repeat = true });

        Assert.AreEqual(2, drafts);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldNameADayCellAsAnAddTarget()
    {
        var component = RenderTimelineMonth();

        var first = RowCells(component, 0)[0];
        Assert.AreEqual("button", first.GetAttribute("role"));
        Assert.AreEqual("Add event, Room 1, Sunday, March 1, 2026", first.GetAttribute("aria-label"));
        Assert.IsNull(first.GetAttribute("aria-disabled"));
        Assert.IsNotNull(first.QuerySelector(".bit-bfc-cell-add-hint"));

        Assert.Contains($"inset-inline-start:{2 * DayWidth}px;", RowCells(component, 1)[2].GetAttribute("style")!);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldStripTheAddTargetsWhenReadOnly()
    {
        var drafts = 0;
        var component = RenderTimelineMonth(configure: p =>
        {
            p.Add(c => c.ReadOnly, true);
            p.Add(c => c.OnAddClick, (BitFullCalendarEvent? _) => drafts++);
        });

        var cells = component.FindAll(".bit-bfc-tl-cell-day");
        Assert.IsTrue(cells.All(c => c.HasAttribute("role") is false));
        Assert.IsTrue(cells.All(c => c.HasAttribute("tabindex") is false));
        Assert.IsTrue(cells.All(c => c.HasAttribute("aria-label") is false));
        Assert.IsEmpty(component.FindAll(".bit-bfc-cell-add-hint"));

        cells[3].Click();
        Assert.AreEqual(0, drafts);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldShadeTheOffDaysOnlyWhenAsked()
    {
        var plain = RenderTimelineMonth();
        Assert.IsEmpty(plain.FindAll(".bit-bfc-slot-off"));

        var highlighted = RenderTimelineMonth(configure: p => p.Add(c => c.Settings, new BitFullCalendarSettings { HighlightBusinessHours = true }));

        // March 2026 has nine weekend days (1, 7, 8, 14, 15, 21, 22, 28, 29), shaded in both rows.
        var row = RowCells(highlighted, 0);
        Assert.AreSequenceEqual(
            new[] { 0, 6, 7, 13, 14, 20, 21, 27, 28 }, Enumerable.Range(0, row.Count).Where(i => row[i].ClassList.Contains("bit-bfc-slot-off")).ToArray());
        Assert.HasCount(18, highlighted.FindAll(".bit-bfc-slot-off"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldDrawTheNowRuleInTodaysColumn()
    {
        var component = RenderTimelineMonth();

        // One per row: today's column starts at 17 days in, and noon is half of it.
        var rules = component.FindAll(".bit-bfc-tl-now");
        Assert.HasCount(2, rules);
        Assert.Contains($"inset-inline-start:{17 * DayWidth + DayWidth / 2}.00px;", rules[0].GetAttribute("style")!);

        var hidden = RenderTimelineMonth(configure: p => p.Add(c => c.Settings, new BitFullCalendarSettings { ShowCurrentTimeIndicator = false }));
        Assert.IsEmpty(hidden.FindAll(".bit-bfc-tl-now"));
    }

    #endregion

    #region Keyboard

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldBeOneTabStop()
    {
        var component = RenderTimelineMonth();

        var cells = component.FindAll(".bit-bfc-tl-cell-day");
        Assert.HasCount(62, cells);
        Assert.ContainsSingle(c => c.GetAttribute("tabindex") == "0", cells);
        Assert.AreEqual(RowCells(component, 0)[0].Id, RovingCell(component).Id);
        Assert.IsTrue(cells.All(c => c.GetAttribute("data-bit-bfc-roving") == "nopage"));
    }

    [TestMethod]
    [DataRow("ArrowRight", 0, 1)]
    [DataRow("ArrowLeft", 0, 0)]
    [DataRow("ArrowDown", 1, 0)]
    [DataRow("ArrowUp", 0, 0)]
    [DataRow("End", 0, 30)]
    [DataRow("Home", 0, 0)]
    [DataRow("PageDown", 0, 0)]
    public void BitFullCalendarTimelineMonthKeysShouldMoveTheTabStop(string key, int row, int day)
    {
        var component = RenderTimelineMonth();

        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(RowCells(component, row)[day].Id, RovingCell(component).Id);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthKeysShouldWalkTheWholeGrid()
    {
        var component = RenderTimelineMonth();

        void Press(string key) => RovingCell(component).KeyDown(new KeyboardEventArgs { Key = key });

        Press("End");
        Press("ArrowRight");
        Press("ArrowDown");
        Press("ArrowDown");
        Assert.AreEqual(RowCells(component, 1)[30].Id, RovingCell(component).Id, "both edges hold");

        Press("Home");
        Press("ArrowUp");
        Assert.AreEqual(RowCells(component, 0)[0].Id, RovingCell(component).Id);
    }

    [TestMethod]
    [DataRow("ArrowLeft", 1)]
    [DataRow("ArrowRight", 0)]
    public void BitFullCalendarTimelineMonthArrowsShouldFollowTheReadingDirectionInRtl(string key, int day)
    {
        var component = RenderTimelineMonth(configure: p => p.Add(c => c.Dir, BitDir.Rtl));

        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(RowCells(component, 0)[day].Id, RovingCell(component).Id);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldMoveTheFocusWithTheTabStop()
    {
        var component = RenderTimelineMonth();

        RovingCell(component).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var focus = Context.JSInterop.VerifyInvoke("BitBlazorUI.FullCalendar.focusElement");
        Assert.AreEqual(RowCells(component, 1)[0].Id, focus.Arguments[^1]);
    }

    #endregion

    #region Moving events

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldMoveADroppedEventToTheDayAndResourceKeepingItsTime()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderTimelineMonth(
            [Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))],
            p => p.Add(c => c.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add)));
        var state = component.Instance.State;

        component.Find("[data-bit-bfc-event='Standup']").DragStart();
        Assert.IsTrue(state.IsDragging);

        var target = RowCells(component, 1)[24];
        target.DragEnter();
        Assert.IsTrue(RowCells(component, 1)[24].ClassList.Contains("bit-bfc-tl-drop-preview"));
        Assert.ContainsSingle(component.FindAll(".bit-bfc-tl-drop-preview"));

        RowCells(component, 1)[24].Drop();

        Assert.IsFalse(state.IsDragging);
        var moved = state.AllEvents.Single();
        Assert.AreEqual(new DateTime(2026, 3, 25, 9, 0, 0), moved.StartDate);
        Assert.AreEqual(new DateTime(2026, 3, 25, 10, 0, 0), moved.EndDate);
        Assert.AreEqual("r2", moved.Resource);
        Assert.ContainsSingle(changes);
        Assert.AreEqual(BitFullCalendarChangeSource.Drag, changes[0].Source);
        Assert.IsEmpty(component.FindAll(".bit-bfc-tl-drop-preview"));
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldIgnoreADropWithoutADrag()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderTimelineMonth(
            [Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))],
            p => p.Add(c => c.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add)));

        RowCells(component, 1)[24].DragEnter();
        RowCells(component, 1)[24].Drop();

        Assert.IsEmpty(component.FindAll(".bit-bfc-tl-drop-preview"));
        Assert.IsEmpty(changes);
        Assert.AreEqual(Anchor.AddHours(9), component.Instance.State.AllEvents.Single().StartDate);
    }

    [TestMethod]
    public void BitFullCalendarTimelineMonthShouldMoveAndResizeByAWholeDayFromTheKeyboard()
    {
        var component = RenderTimelineMonth([Event("Standup", Anchor.AddHours(9), Anchor.AddHours(10))]);
        var state = component.Instance.State;

        component.Find("[data-bit-bfc-event='Standup']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

        Assert.AreEqual(Anchor.AddDays(1).AddHours(9), state.AllEvents.Single().StartDate);
        Assert.AreEqual(Anchor.AddDays(1).AddHours(10), state.AllEvents.Single().EndDate);

        component.Find("[data-bit-bfc-event='Standup']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });

        Assert.AreEqual(Anchor.AddDays(1).AddHours(9), state.AllEvents.Single().StartDate);
        Assert.AreEqual(Anchor.AddDays(2).AddHours(10), state.AllEvents.Single().EndDate);
    }

    #endregion

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}
