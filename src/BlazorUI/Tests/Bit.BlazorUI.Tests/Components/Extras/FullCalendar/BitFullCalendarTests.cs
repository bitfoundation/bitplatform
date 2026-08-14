using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the rendered surface of the ReadOnly and Views parameters: which affordances reach the
/// DOM, and which ones stay out of reach of the pointer and the keyboard.
/// </summary>
[TestClass]
public class BitFullCalendarTests : BunitTestContext
{
    private const string AddButtonSelector = ".bit-bfc-header-right .bit-bfc-btn-primary";

    private static List<BitFullCalendarEvent> Events()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = "1", Title = "Standup", Description = "Daily sync", StartDate = today.AddHours(9), EndDate = today.AddHours(10) }
        ];
    }

    private static List<BitFullCalendarResource> Resources() =>
    [
        new() { Id = "r1", Title = "Room 1" }
    ];

    /// <summary>
    /// Renders a calendar over the shared event, carrying only the parameters the caller asks for so
    /// each test reads as the configuration it is about. Tests that bind a parameter or hook a
    /// callback build their own parameter set instead.
    /// </summary>
    private IRenderedComponent<BitFullCalendar> RenderCalendar(bool readOnly = false,
                                                               BitFullCalendarView? defaultView = null,
                                                               IReadOnlyList<BitFullCalendarView>? views = null,
                                                               bool resources = false,
                                                               BitFullCalendarMode? defaultMode = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());

            if (readOnly)
            {
                parameters.Add(p => p.ReadOnly, true);
            }

            if (defaultView is not null)
            {
                parameters.Add(p => p.DefaultView, defaultView);
            }

            if (views is not null)
            {
                parameters.Add(p => p.Views, views);
            }

            if (resources)
            {
                parameters.Add(p => p.Resources, Resources());
            }

            if (defaultMode is not null)
            {
                parameters.Add(p => p.DefaultMode, defaultMode);
            }
        });
    }

    #region ReadOnly

    [TestMethod]
    public void BitFullCalendarShouldRenderTheAddAffordancesByDefault()
    {
        var component = RenderCalendar();

        Assert.IsFalse(component.Find(".bit-bfc").ClassList.Contains("bit-bfc-readonly"));
        Assert.AreEqual(1, component.FindAll(AddButtonSelector).Count);
        Assert.IsTrue(component.FindAll(".bit-bfc-cell-add-hint").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldHideTheAddButtonAndCellHints()
    {
        var component = RenderCalendar(readOnly: true);

        Assert.IsTrue(component.Find(".bit-bfc").ClassList.Contains("bit-bfc-readonly"));
        Assert.AreEqual(0, component.FindAll(AddButtonSelector).Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-cell-add-hint").Count);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldKeepNavigationAndFiltering()
    {
        var component = RenderCalendar(readOnly: true);

        // Everything that does not modify events has to survive read-only.
        Assert.AreEqual(5, component.FindAll(".bit-bfc-view-tab").Count);
        Assert.IsNotNull(component.Find(".bit-bfc-header-left"));
        Assert.IsTrue(component.FindAll(".bit-bfc-header-right .bit-bfc-dropdown").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldMakeMonthEventsNonDraggable()
    {
        var editable = RenderCalendar();
        Assert.AreEqual("true", editable.Find(".bit-bfc-event-badge").GetAttribute("draggable"));

        var readOnly = RenderCalendar(readOnly: true);

        Assert.AreEqual("false", readOnly.Find(".bit-bfc-event-badge").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldNotOpenTheAddDialogFromAMonthCell()
    {
        var component = RenderCalendar(readOnly: true);

        component.Find(".bit-bfc-month-cell").Click();

        // The add/edit dialog is the only one carrying form fields.
        Assert.AreEqual(0, component.FindAll(".bit-bfc-field").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldOpenTheAddDialogFromAMonthCellWhenEditable()
    {
        var component = RenderCalendar();

        component.Find(".bit-bfc-month-cell").Click();

        Assert.IsTrue(component.FindAll(".bit-bfc-field").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldNotRaiseOnAddClick()
    {
        var editableClicks = 0;
        var editable = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.OnAddClick, EventCallback.Factory.Create<BitFullCalendarEvent?>(this, _ => editableClicks++));
        });
        editable.Find(".bit-bfc-month-cell").Click();
        Assert.AreEqual(1, editableClicks);

        var readOnlyClicks = 0;
        var readOnly = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.ReadOnly, true);
            parameters.Add(p => p.OnAddClick, EventCallback.Factory.Create<BitFullCalendarEvent?>(this, _ => readOnlyClicks++));
        });
        readOnly.Find(".bit-bfc-month-cell").Click();

        Assert.AreEqual(0, readOnlyClicks);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldDropEditAndDeleteFromTheEventDetails()
    {
        var editable = RenderCalendar();
        editable.Find(".bit-bfc-event-badge").Click();
        Assert.AreEqual(1, editable.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Count);
        Assert.AreEqual(3, editable.FindAll(".bit-bfc-dialog-footer button").Count);

        var readOnly = RenderCalendar(readOnly: true);
        readOnly.Find(".bit-bfc-event-badge").Click();

        // The details stay readable; only the mutating actions go, leaving Close on its own.
        Assert.IsNotNull(readOnly.Find(".bit-bfc-dialog"));
        Assert.AreEqual(0, readOnly.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Count);
        Assert.AreEqual(1, readOnly.FindAll(".bit-bfc-dialog-footer button").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldCloseAnOpenEditDialogWhenReadOnlyIsTurnedOn()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(".bit-bfc-event-badge").Click();
        // Details footer while editable: Edit, Delete, Close.
        component.FindAll(".bit-bfc-dialog-footer button")[0].Click();
        // The edit dialog opens on top of the details dialog, and it owns the only Save button.
        Assert.AreEqual(2, component.FindAll(".bit-bfc-dialog").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Count);

        // Read-only arrives while the form is open: every entry point only checks read-only when it
        // opens the dialog, so the open form has to close itself instead of staying live.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, true));

        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Count);
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldStripTheHourSlotButtonSemantics()
    {
        var editable = RenderCalendar(defaultView: BitFullCalendarView.Day);
        var editableSlot = editable.Find(".bit-bfc-hour-slot");
        Assert.AreEqual("button", editableSlot.GetAttribute("role"));
        Assert.AreEqual("0", editableSlot.GetAttribute("tabindex"));

        var readOnly = RenderCalendar(readOnly: true, defaultView: BitFullCalendarView.Day);

        // An inert slot must not be announced or reachable by keyboard.
        var readOnlySlot = readOnly.Find(".bit-bfc-hour-slot");
        Assert.IsNull(readOnlySlot.GetAttribute("role"));
        Assert.IsNull(readOnlySlot.GetAttribute("tabindex"));
        Assert.IsNull(readOnlySlot.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldRemoveTheResizeHandles()
    {
        var editable = RenderCalendar(defaultView: BitFullCalendarView.Day);
        Assert.AreEqual(2, editable.FindAll(".bit-bfc-resize-handle").Count);

        var readOnly = RenderCalendar(readOnly: true, defaultView: BitFullCalendarView.Day);

        Assert.AreEqual(0, readOnly.FindAll(".bit-bfc-resize-handle").Count);
        Assert.AreEqual("false", readOnly.Find(".bit-bfc-event-block").GetAttribute("draggable"));
    }

    [TestMethod]
    public async Task BitFullCalendarShouldNotResumeADayResizeThatCrossedAReadOnlySwitch()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        var block = component.FindComponent<BitFcEventBlock>().Instance;
        await component.InvokeAsync(() => block.OnResizeStart("bottom"));
        await component.InvokeAsync(() => block.OnResizeMove("bottom", 60));
        Assert.AreEqual(1, component.FindAll(".bit-bfc-resize-preview").Count);

        // Read-only lands mid-gesture: the preview goes away and the gesture is cancelled outright.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, true));
        Assert.AreSame(block, component.FindComponent<BitFcEventBlock>().Instance);
        await component.InvokeAsync(() => block.OnResizeMove("bottom", 90));
        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-preview").Count);

        // Read-only switched back off before the pointer is released: the cancelled gesture must not
        // pick up again, so neither the remaining moves nor the release may change the event.
        component.Render(parameters => parameters.Add(p => p.ReadOnly, false));
        await component.InvokeAsync(() => block.OnResizeMove("bottom", 120));
        await component.InvokeAsync(block.OnResizeEnd);

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-preview").Count);

        // A brand new gesture still resizes: cancelling must not wedge the block.
        await component.InvokeAsync(() => block.OnResizeStart("bottom"));
        await component.InvokeAsync(() => block.OnResizeMove("bottom", 60));
        await component.InvokeAsync(block.OnResizeEnd);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeSource.Resize, changes[0].Source);
    }

    [TestMethod]
    public async Task BitFullCalendarShouldNotResumeATimelineResizeThatCrossedAReadOnlySwitch()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Resources, Resources());
            parameters.Add(p => p.DefaultMode, BitFullCalendarMode.Timeline);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        var block = component.FindComponent<BitFcTimelineEventBlock>().Instance;
        await component.InvokeAsync(() => block.OnResizeStart("end"));
        await component.InvokeAsync(() => block.OnResizeMove("end", 200));
        Assert.AreEqual(1, component.FindAll(".bit-bfc-resize-preview").Count);

        component.Render(parameters => parameters.Add(p => p.ReadOnly, true));
        Assert.AreSame(block, component.FindComponent<BitFcTimelineEventBlock>().Instance);
        await component.InvokeAsync(() => block.OnResizeMove("end", 240));
        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-preview").Count);

        component.Render(parameters => parameters.Add(p => p.ReadOnly, false));
        await component.InvokeAsync(() => block.OnResizeMove("end", 280));
        await component.InvokeAsync(block.OnResizeEnd);

        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-preview").Count);

        await component.InvokeAsync(() => block.OnResizeStart("end"));
        await component.InvokeAsync(() => block.OnResizeMove("end", 200));
        await component.InvokeAsync(block.OnResizeEnd);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeSource.Resize, changes[0].Source);
    }

    [TestMethod]
    public void BitFullCalendarReadOnlyShouldStripTheTimelineSlotButtonSemantics()
    {
        var editable = RenderCalendar(defaultView: BitFullCalendarView.Day, resources: true, defaultMode: BitFullCalendarMode.Timeline);
        Assert.AreEqual("button", editable.Find(".bit-bfc-tl-cell-slot").GetAttribute("role"));
        Assert.IsTrue(editable.FindAll(".bit-bfc-cell-add-hint").Count > 0);

        var readOnly = RenderCalendar(readOnly: true, defaultView: BitFullCalendarView.Day, resources: true, defaultMode: BitFullCalendarMode.Timeline);

        var slot = readOnly.Find(".bit-bfc-tl-cell-slot");
        Assert.IsNull(slot.GetAttribute("role"));
        Assert.IsNull(slot.GetAttribute("tabindex"));
        Assert.AreEqual(0, readOnly.FindAll(".bit-bfc-cell-add-hint").Count);
        Assert.AreEqual("false", readOnly.Find(".bit-bfc-timeline-event").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFullCalendarShouldRestoreTheAffordancesWhenReadOnlyIsTurnedOff()
    {
        var component = RenderCalendar(readOnly: true);
        Assert.AreEqual(0, component.FindAll(AddButtonSelector).Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.ReadOnly, false);
        });

        Assert.AreEqual(1, component.FindAll(AddButtonSelector).Count);
        Assert.AreEqual("true", component.Find(".bit-bfc-event-badge").GetAttribute("draggable"));
        Assert.IsFalse(component.Find(".bit-bfc").ClassList.Contains("bit-bfc-readonly"));
    }

    #endregion

    #region Views

    [TestMethod]
    public void BitFullCalendarShouldRenderEveryViewTabByDefault()
    {
        var component = RenderCalendar();

        var labels = component.FindAll(".bit-bfc-view-tab").Select(t => t.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(new[] { "Day", "Week", "Month", "Year", "Agenda" }, labels);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldRestrictAndOrderTheTabs()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Agenda, BitFullCalendarView.Week]);

        var labels = component.FindAll(".bit-bfc-view-tab").Select(t => t.TextContent.Trim()).ToArray();

        CollectionAssert.AreEqual(new[] { "Agenda", "Week" }, labels);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldCollapseTheTabStripForASingleView()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Month]);

        Assert.AreEqual(0, component.FindAll(".bit-bfc-view-tabs").Count);
        Assert.IsNotNull(component.Find(".bit-bfc-month"));
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldClampTheActiveViewIntoTheAllowedSet()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Agenda, BitFullCalendarView.Week]);

        // Month is the component default and is excluded here, so the first allowed view renders.
        Assert.AreEqual(BitFullCalendarView.Agenda, component.Instance.View);
        Assert.IsNotNull(component.Find(".bit-bfc-agenda"));
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldClampAnExcludedDefaultView()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Year,
                                       views: [BitFullCalendarView.Week, BitFullCalendarView.Day]);

        Assert.AreEqual(BitFullCalendarView.Week, component.Instance.View);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldClampAndPushBackAnExcludedBoundView()
    {
        var view = BitFullCalendarView.Year;

        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Views, [BitFullCalendarView.Week, BitFullCalendarView.Day]);
            parameters.Bind(p => p.View, view, v => view = v);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(BitFullCalendarView.Week, view);
            Assert.AreEqual(BitFullCalendarView.Week, component.Instance.View);
        });
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldPushBackAnExcludedBoundViewThatClampsToTheActiveView()
    {
        var view = BitFullCalendarView.Agenda;

        // Month is both the active view and the clamp target here, so the state reports no change at
        // all - the excluded bound value still has to be corrected.
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Views, [BitFullCalendarView.Month, BitFullCalendarView.Week]);
            parameters.Bind(p => p.View, view, v => view = v);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(BitFullCalendarView.Month, view);
            Assert.AreEqual(BitFullCalendarView.Month, component.Instance.View);
            Assert.IsNotNull(component.Find(".bit-bfc-month"));
        });
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldPushBackARefusedBoundTimelineMode()
    {
        var mode = BitFullCalendarMode.Timeline;

        // No allowed view supports the timeline layout, so the refused mode resolves to the Event mode
        // that is already active and the binding has to be corrected without a state change to react to.
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Resources, Resources());
            parameters.Add(p => p.Views, [BitFullCalendarView.Year, BitFullCalendarView.Agenda]);
            parameters.Bind(p => p.Mode, mode, m => mode = m);
        });

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(BitFullCalendarMode.Event, mode);
            Assert.AreEqual(BitFullCalendarMode.Event, component.Instance.Mode);
        });
    }

    [TestMethod]
    public void BitFullCalendarViewTabShouldSwitchTheRenderedView()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Month, BitFullCalendarView.Agenda]);
        Assert.IsNotNull(component.Find(".bit-bfc-month"));

        component.FindAll(".bit-bfc-view-tab")[1].Click();

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(BitFullCalendarView.Agenda, component.Instance.View);
            Assert.IsNotNull(component.Find(".bit-bfc-agenda"));
        });
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldMakeTheYearDrillDownStayPutWhenMonthIsExcluded()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Year, BitFullCalendarView.Day]);
        Assert.AreEqual(BitFullCalendarView.Year, component.Instance.View);

        component.Find(".bit-bfc-year-month-title").Click();

        // Month is excluded, so drilling into it must not land on some other allowed view either.
        component.WaitForAssertion(() => Assert.AreEqual(BitFullCalendarView.Year, component.Instance.View));
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldStillDrillIntoTheMonthWhenItIsAllowed()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Year);

        component.Find(".bit-bfc-year-month-title").Click();

        component.WaitForAssertion(() => Assert.AreEqual(BitFullCalendarView.Month, component.Instance.View));
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldHideTheModeTabsWhenNoViewSupportsTheTimeline()
    {
        var withTimeline = RenderCalendar(resources: true);
        Assert.AreEqual(1, withTimeline.FindAll(".bit-bfc-mode-tabs").Count);

        var withoutTimeline = RenderCalendar(views: [BitFullCalendarView.Year, BitFullCalendarView.Agenda], resources: true);

        Assert.AreEqual(0, withoutTimeline.FindAll(".bit-bfc-mode-tabs").Count);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldIgnoreAnExcludedTimelineModeDefault()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Year, BitFullCalendarView.Agenda],
                                       resources: true,
                                       defaultMode: BitFullCalendarMode.Timeline);

        Assert.AreEqual(BitFullCalendarMode.Event, component.Instance.Mode);
        Assert.AreEqual(BitFullCalendarView.Year, component.Instance.View);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldIntersectWithTheTimelineLayouts()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Agenda, BitFullCalendarView.Week, BitFullCalendarView.Day],
                                       resources: true,
                                       defaultMode: BitFullCalendarMode.Timeline);

        var labels = component.FindAll(".bit-bfc-view-tab").Select(t => t.TextContent.Trim()).ToArray();

        // Agenda is allowed but the timeline cannot lay it out, so only the supported ones remain.
        CollectionAssert.AreEqual(new[] { "Week", "Day" }, labels);
    }

    [TestMethod]
    public void BitFullCalendarViewsShouldReactToALaterChange()
    {
        var component = RenderCalendar();
        Assert.AreEqual(5, component.FindAll(".bit-bfc-view-tab").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Views, [BitFullCalendarView.Day, BitFullCalendarView.Week]);
        });

        Assert.AreEqual(2, component.FindAll(".bit-bfc-view-tab").Count);
        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.View);
    }

    #endregion
}
