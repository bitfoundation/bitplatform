using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The resource timeline of BitFullCalendar (/e2e/full-calendar/timeline): resource rows with a ResourceTemplate, events
/// placed on their row and time with a TimelineEventTemplate, the "Unassigned" row, dragging a block to another row,
/// resizing it by its edge, the keyboard, the day, week and month layouts, ScrollToTimeAsync, and Timeline mode refused
/// where no view can lay it out.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string TimelinePage = "/e2e/full-calendar/timeline";

    private ILocator Tl => Page.Locator("#tl");

    private ILocator TlBlock(string id) => Tl.Locator($".bit-bfc-timeline-event[data-bit-bfc-event='{id}']");

    [TestMethod]
    public async Task Timeline_RowsAreTheResourcesWithTheirTemplate_AndUnmatchedEventsAreUnassigned()
    {
        await OpenAsync(TimelinePage);

        await Expect(Tl.Locator(".bit-bfc-mode-tab.active")).ToHaveTextAsync("Timeline");
        await Expect(Tl.Locator(".resource-header")).ToHaveCountAsync(3);
        await Expect(Tl.Locator(".resource-avatar")).ToHaveTextAsync(["H", "T", "W"]);
        await Expect(Tl.Locator(".resource-name")).ToHaveTextAsync(["HQ - Bay Wing", "The Garden", "War Room (B1)"]);
        await Expect(Tl.Locator(".resource-sub").First).ToHaveTextAsync("Headquarters");
        await Expect(Tl.Locator(".bit-bfc-tl-resource-cell-unassigned")).ToHaveTextAsync("Unassigned");

        var rows = Tl.Locator(".bit-bfc-tl-body-row");
        await Expect(rows.Nth(0).Locator(".bit-bfc-timeline-event")).ToHaveCountAsync(2);
        await Expect(rows.Nth(3).Locator("[data-bit-bfc-event='t5']")).ToBeVisibleAsync();

        // the template draws the content; the block keeps its name, its drag and its handles
        var block = TlBlock("t1");
        await Expect(block.Locator(".tl-card")).ToHaveTextAsync("Design Review room-bay");
        await Expect(block.Locator(".bit-bfc-timeline-event-title")).ToHaveCountAsync(0);
        await Expect(block).ToHaveAttributeAsync("aria-label", "Design Review, 10:00 - 11:00");
        await Expect(block).ToHaveAttributeAsync("draggable", "true");
        await Expect(block.Locator(".bit-bfc-resize-handle-h")).ToHaveCountAsync(2);

        // an hour is 96px along the axis
        var anchor = block.Locator("xpath=..");
        Assert.AreEqual(960, await anchor.EvaluateAsync<double>("e => e.offsetLeft"), 1);
        Assert.AreEqual(96, await anchor.EvaluateAsync<double>("e => e.getBoundingClientRect().width"), 1);
        await Expect(Tl.Locator(".bit-bfc-tl-now")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task Timeline_DraggingABlockToAnotherRow_ReassignsItsResource()
    {
        await OpenAsync(TimelinePage);

        await DragWithMouseAsync(Tl, TlBlock("t1"), Slot(Tl, "The Garden, 11:00"));

        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 11:00|2025-07-16 12:00|room-garden");
        await Expect(Tl.Locator(".bit-bfc-tl-body-row").Nth(1).Locator("[data-bit-bfc-event='t1']")).ToBeVisibleAsync();
        await Expect(TlBlock("t1").Locator(".tl-card")).ToHaveTextAsync("Design Review room-garden");
    }

    [TestMethod]
    public async Task Timeline_ResizingABlockByItsEnd_StretchesItAlongTheAxis()
    {
        await OpenAsync(TimelinePage);

        await DragByMouseAsync(TlBlock("t1").Locator(".bit-bfc-resize-handle-end"), 96, 0);

        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Resize|Design Review|2025-07-16 10:00|2025-07-16 12:00|room-bay");
        Assert.AreEqual(192, await TlBlock("t1").Locator("xpath=..").EvaluateAsync<double>("e => e.getBoundingClientRect().width"), 1);
    }

    [TestMethod]
    public async Task Timeline_ResizingABlockByItsStart_MovesItsStart()
    {
        await OpenAsync(TimelinePage);

        await DragByMouseAsync(TlBlock("t1").Locator(".bit-bfc-resize-handle-start"), -96, 0);

        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Resize|Design Review|2025-07-16 09:00|2025-07-16 11:00|room-bay");
    }

    [TestMethod]
    public async Task Timeline_TheKeyboardMovesAndResizesABlockAlongTheAxis()
    {
        await OpenAsync(TimelinePage);
        var block = TlBlock("t1");
        await block.FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowRight");
        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 10:30|2025-07-16 11:30|room-bay");
        await Expect(block).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Resize|Design Review|2025-07-16 10:30|2025-07-16 12:00|room-bay");
        await Expect(block).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 10:00|2025-07-16 11:30|room-bay");
        await Expect(block).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog(Tl).Locator("h3")).ToHaveTextAsync("Design Review");
        await Expect(Dialog(Tl).Locator(".bit-bfc-event-detail-row", new() { HasText = "Resource" })).ToContainTextAsync("HQ - Bay Wing");
    }

    [TestMethod]
    public async Task Timeline_ASlotClick_AddsAnEventOnThatRowsResource()
    {
        await OpenAsync(TimelinePage);

        await Slot(Tl, "War Room (B1), 16:00").ClickAsync();

        var dialog = Dialog(Tl);
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 16:00");
        await Expect(dialog.Locator("select[aria-label='Resource']")).ToHaveValueAsync("room-war");
        await Expect(dialog.Locator("select[aria-label='Resource'] option")).ToHaveTextAsync(["(none)", "HQ - Bay Wing", "The Garden", "War Room (B1)"]);
        await dialog.GetByPlaceholder("Event title").FillAsync("Postmortem");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();
        await Expect(Page.Locator("#tl-change")).ToHaveTextAsync("Add|Dialog|Postmortem|2025-07-16 16:00|2025-07-16 16:30|room-war");
        await Expect(Tl.Locator(".bit-bfc-tl-body-row").Nth(2).Locator(".tl-card", new() { HasText = "Postmortem" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Timeline_TheSlotsAreOneTabStop_WalkedAlongTheAxisAndAcrossTheRows()
    {
        await OpenAsync(TimelinePage);
        var stop = Tl.Locator(".bit-bfc-tl-scroll [data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveCountAsync(1);
        await stop.FocusAsync();
        var first = await stop.GetAttributeAsync("aria-label");
        Assert.AreEqual("Add event, HQ - Bay Wing, 00:00", first);

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Add event, HQ - Bay Wing, 00:30");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, The Garden, 00:30");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedLabelAsync("Add event, The Garden, 23:30");
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedLabelAsync("Add event, The Garden, 00:00");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedLabelAsync("Add event, HQ - Bay Wing, 00:00");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog(Tl).Locator("select[aria-label='Resource']")).ToHaveValueAsync("room-bay");
    }

    [TestMethod]
    public async Task Timeline_TheWeekAndMonthLayouts_LayTheDaysAlongTheAxis()
    {
        await OpenAsync(TimelinePage);

        await SwitchViewAsync(Tl, "Week");
        await Expect(Title(Tl)).ToHaveTextAsync("Jul 13, 2025 - Jul 19, 2025");
        await Expect(Tl.Locator(".bit-bfc-tl-day-header-name")).ToHaveTextAsync(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]);
        Assert.AreEqual(24 * 96, await Tl.Locator(".bit-bfc-tl-day-header").First.EvaluateAsync<double>("e => e.getBoundingClientRect().width"), 1);
        await Expect(TlBlock("t1")).ToBeAttachedAsync();

        await SwitchViewAsync(Tl, "Month");
        await Expect(Title(Tl)).ToHaveTextAsync("July 2025");
        await Expect(Tl.Locator(".bit-bfc-tl-time-cell")).ToHaveCountAsync(31);
        await Expect(Tl.Locator(".bit-bfc-tl-time-cell.today")).ToHaveCountAsync(1);
        await Expect(TlBlock("t3")).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task Timeline_ScrollToTimeAsync_ScrollsTheTimeAxis()
    {
        await OpenAsync(TimelinePage);

        await Page.Locator("#btn-tl-scroll-14").ClickAsync();

        await Expect(Page.Locator("#tl-scroll")).ToHaveTextAsync("True");
        // 14:00 is 1344px along the axis, or as far as the scroller goes
        var scroll = await Tl.Locator(".bit-bfc-tl-scroll").EvaluateAsync<double[]>("e => [e.scrollLeft, e.scrollWidth - e.clientWidth]");
        var (scrollLeft, maxScroll) = (scroll[0], scroll[1]);
        Assert.AreEqual(Math.Min(14 * 96, maxScroll), scrollLeft, 2);
        Assert.IsTrue(scrollLeft > 768, "it moved on from the start-of-day hour (8:00)");

        await SwitchViewAsync(Tl, "Month");
        await Page.Locator("#btn-tl-scroll-14").ClickAsync();
        await Expect(Page.Locator("#tl-scroll")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Timeline_IsRefused_WhereNoViewCanLayItOut()
    {
        await OpenAsync(TimelinePage);
        var cal = Page.Locator("#no-tl");

        await Expect(cal.Locator(".bit-bfc-mode-tabs")).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-year")).ToBeVisibleAsync();
        await Expect(cal.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Year", "Agenda"]);
        await Expect(Page.Locator("#no-tl-body")).ToHaveAttributeAsync("aria-labelledby", "no-tl-view-3");
    }
}
