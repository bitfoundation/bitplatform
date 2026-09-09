using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    #region Settings round-trip

    private IRenderedComponent<BitFullCalendar> RenderWithSettings(BitFullCalendarSettings settings,
                                                                   BitFullCalendarView? defaultView = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, settings);
            if (defaultView is not null)
            {
                parameters.Add(p => p.DefaultView, defaultView);
            }
        });
    }

    /// <summary>The toggle buttons of the settings panel, in the order the panel renders them.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement> OpenSettingsMenu(IRenderedComponent<BitFullCalendar> component)
    {
        component.Find(".bit-bfc-dropdown > button").Click();
        return component.FindAll(".bit-bfc-dropdown-menu button.bit-bfc-dropdown-item");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotRevertAUserSettingOnTheNextParameterPass()
    {
        // The regression this guards: re-applying every Settings value on each parameter pass would
        // undo whatever the user had just picked in the panel as soon as the parent re-rendered.
        var settings = new BitFullCalendarSettings();
        var component = RenderWithSettings(settings);
        Assert.IsTrue(component.Instance.State.Use24HourFormat);

        OpenSettingsMenu(component)[1].Click();
        Assert.IsFalse(component.Instance.State.Use24HourFormat);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, settings);
        });

        Assert.IsFalse(component.Instance.State.Use24HourFormat, "a parent re-render must not revert the user's choice");
    }

    [TestMethod]
    public void BitFullCalendarShouldWriteAUserSettingBackOntoTheSettingsInstance()
    {
        var settings = new BitFullCalendarSettings();
        var component = RenderWithSettings(settings);

        OpenSettingsMenu(component)[4].Click();

        Assert.IsTrue(component.Instance.State.ShowWeekNumbers);
        Assert.IsTrue(settings.ShowWeekNumbers, "the panel's choice belongs on the consumer's own object");
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyAConsumerSettingChangeMadeInPlace()
    {
        var settings = new BitFullCalendarSettings();
        var component = RenderWithSettings(settings);
        Assert.AreEqual(30, component.Instance.State.SlotDurationMinutes);

        settings.SlotDurationMinutes = 15;
        component.Render(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, settings);
        });

        Assert.AreEqual(15, component.Instance.State.SlotDurationMinutes);
    }

    #endregion

    #region Time grid shape

    [TestMethod]
    public void BitFullCalendarShouldRenderOnlyTheVisibleHours()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 18 },
            BitFullCalendarView.Day);

        Assert.AreEqual(10, component.FindAll(".bit-bfc-hour-row").Count);
        Assert.AreEqual(10, component.FindAll(".bit-bfc-time-slot-label").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderEveryHourByDefault()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);

        Assert.AreEqual(24, component.FindAll(".bit-bfc-hour-row").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldSplitEachHourIntoTheConfiguredSlots()
    {
        var half = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 10 },
            BitFullCalendarView.Day);
        // Two hours × (one leading slot + one trailing slot).
        Assert.AreEqual(2, half.FindAll(".bit-bfc-hour-slot").Count);
        Assert.AreEqual(2, half.FindAll(".bit-bfc-hour-row-half").Count);

        var quarter = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 10, SlotDurationMinutes = 15 },
            BitFullCalendarView.Day);

        Assert.AreEqual(2, quarter.FindAll(".bit-bfc-hour-slot").Count);
        Assert.AreEqual(6, quarter.FindAll(".bit-bfc-hour-row-half").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderOneHourSlotForAHourLongSlotDuration()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12, SlotDurationMinutes = 60 },
            BitFullCalendarView.Day);

        Assert.AreEqual(4, component.FindAll(".bit-bfc-hour-slot").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-hour-row-half").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldNotRenderTheNowIndicatorOnAnotherDay()
    {
        var today = RenderCalendar(defaultView: BitFullCalendarView.Day);
        Assert.AreEqual(1, today.FindAll(".bit-bfc-timeline").Count);

        var otherDay = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.DefaultDate, DateTime.Today.AddDays(3));
        });

        Assert.AreEqual(0, otherDay.FindAll(".bit-bfc-timeline").Count, "\"now\" only exists on today's column");
    }

    [TestMethod]
    public void BitFullCalendarShouldDropTheNowIndicatorWhenItIsTurnedOff()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { ShowCurrentTimeIndicator = false },
            BitFullCalendarView.Day);

        Assert.AreEqual(0, component.FindAll(".bit-bfc-timeline").Count);
    }

    #endregion

    #region Work week and week numbers

    [TestMethod]
    public void BitFullCalendarHiddenDaysShouldNarrowTheMonthGrid()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday] });

        Assert.AreEqual(5, component.FindAll(".bit-bfc-month-header-cell").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-month-cell").Count % 5);
    }

    [TestMethod]
    public void BitFullCalendarHiddenDaysShouldNarrowTheWeekGrid()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday] },
            BitFullCalendarView.Week);

        Assert.AreEqual(5, component.FindAll(".bit-bfc-week-header-day").Count);
        Assert.AreEqual(5, component.FindAll(".bit-bfc-week-day-col").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepSevenColumnsByDefault()
    {
        var component = RenderCalendar();

        Assert.AreEqual(7, component.FindAll(".bit-bfc-month-header-cell").Count);
    }

    [TestMethod]
    public void BitFullCalendarFirstDayOfWeekShouldOverrideTheCulture()
    {
        var component = RenderWithSettings(new BitFullCalendarSettings { FirstDayOfWeek = DayOfWeek.Wednesday });

        Assert.AreEqual(DayOfWeek.Wednesday, component.Instance.State.FirstDayOfWeek);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderWeekNumbersOnlyWhenAskedTo()
    {
        var without = RenderCalendar();
        Assert.AreEqual(0, without.FindAll(".bit-bfc-weeknum-cell").Count);

        var with = RenderWithSettings(new BitFullCalendarSettings { ShowWeekNumbers = true });

        // One rail cell per rendered week row.
        Assert.IsTrue(with.FindAll(".bit-bfc-weeknum-cell").Count >= 4);
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderTheWeekNumberInTheWeekViewGutter()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { ShowWeekNumbers = true },
            BitFullCalendarView.Week);

        var expected = $"W{BitFullCalendarHelpers.GetWeekNumber(BitFullCalendarHelpers.StartOfWeek(DateTime.Today, component.Instance.State.Culture))}";
        Assert.AreEqual(expected, component.Find(".bit-bfc-week-header-time .bit-bfc-weeknum-cell").TextContent.Trim());
    }

    #endregion

    #region All-day events

    private static List<BitFullCalendarEvent> AllDayEvents()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = "timed", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(10) },
            new() { Id = "allday", Title = "Holiday", StartDate = today, EndDate = today.AddDays(1), IsAllDay = true }
        ];
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderAnAllDayEventInTheAllDayRow()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, AllDayEvents());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
        });

        // The timed event owns the only block on the hour grid; the all-day one sits above it.
        Assert.AreEqual(1, component.FindAll(".bit-bfc-event-block").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-multiday-row .bit-bfc-event-badge").Count);
        Assert.IsTrue(component.Find(".bit-bfc-multiday-row .bit-bfc-event-badge").ClassList.Contains("bit-bfc-event-allday"));
    }

    [TestMethod]
    public void BitFullCalendarShouldLabelAnAllDayBadgeWithTheAllDayText()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, AllDayEvents());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
        });

        var badge = component.Find(".bit-bfc-multiday-row .bit-bfc-event-badge");
        Assert.AreEqual("All day", badge.QuerySelector(".bit-bfc-event-badge-time")!.TextContent.Trim());
        StringAssert.Contains(badge.GetAttribute("aria-label"), "All day");
    }

    [TestMethod]
    public void BitFullCalendarShouldCarryTheEventCssClassOntoItsBadge()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(10), CssClass = "tentative" }
            ]);
        });

        Assert.IsTrue(component.Find(".bit-bfc-event-badge").ClassList.Contains("tentative"));
    }

    #endregion

    #region Per-event lock

    [TestMethod]
    public void BitFullCalendarShouldLockASingleEventWithoutLockingTheCalendar()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Payroll", StartDate = today.AddHours(9), EndDate = today.AddHours(10), IsReadOnly = true }
            ]);
        });

        // The calendar itself stays editable...
        Assert.AreEqual(1, component.FindAll(AddButtonSelector).Count);
        // ...but this event cannot be dragged or acted on.
        Assert.AreEqual("false", component.Find(".bit-bfc-event-badge").GetAttribute("draggable"));

        component.Find(".bit-bfc-event-badge").Click();
        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog-footer button").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldRemoveTheResizeHandlesOfALockedEvent()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Payroll", StartDate = today.AddHours(9), EndDate = today.AddHours(10), IsReadOnly = true }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-handle").Count);
    }

    #endregion

    #region Date bounds

    [TestMethod]
    public void BitFullCalendarShouldDisableNavigationAtTheBounds()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.MinDate, DateTime.Today);
            parameters.Add(p => p.MaxDate, DateTime.Today.AddDays(1));
        });

        var navButtons = component.FindAll(".bit-bfc-btn-nav");
        Assert.IsTrue(navButtons[0].HasAttribute("disabled"), "there is nothing before MinDate to navigate to");
        Assert.IsFalse(navButtons[1].HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFullCalendarShouldPullABoundDateIntoTheAllowedWindow()
    {
        var bound = DateTime.Today.AddDays(40);
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.MinDate, DateTime.Today);
            parameters.Add(p => p.MaxDate, DateTime.Today.AddDays(5));
            parameters.Add(p => p.Date, bound);
            parameters.Add(p => p.DateChanged, EventCallback.Factory.Create<DateTime>(this, d => bound = d));
        });

        Assert.AreEqual(DateTime.Today.AddDays(5), component.Instance.State.SelectedDate);
        Assert.AreEqual(DateTime.Today.AddDays(5), bound, "the corrected value is pushed back into the binding");
    }

    [TestMethod]
    public void BitFullCalendarShouldMakeAnOutOfRangeMonthCellInert()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.MinDate, DateTime.Today);
            parameters.Add(p => p.MaxDate, DateTime.Today.AddDays(2));
        });

        Assert.IsTrue(component.FindAll(".bit-bfc-month-cell.bit-bfc-out-of-range").Count > 0);
    }

    #endregion

    #region Month cell capacity

    [TestMethod]
    public void BitFullCalendarShouldRenderTheConfiguredNumberOfMonthEventSlots()
    {
        var today = DateTime.Today;
        var events = Enumerable.Range(0, 5)
            .Select(i => new BitFullCalendarEvent
            {
                Id = $"e{i}",
                Title = $"Event {i}",
                StartDate = today.AddHours(9 + i),
                EndDate = today.AddHours(10 + i)
            })
            .ToList();

        var threeSlots = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, events);
        });
        var fiveSlots = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, events);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { MaxEventsPerDayCell = 5 });
        });

        var cellCount = threeSlots.FindAll(".bit-bfc-month-cell").Count;
        Assert.AreEqual(cellCount * 3, threeSlots.FindAll(".bit-bfc-month-event-slot").Count);
        Assert.AreEqual(cellCount * 5, fiveSlots.FindAll(".bit-bfc-month-event-slot").Count);
        // Three of five shown leaves a "+2 more" affordance; all five leaves none.
        Assert.AreEqual(1, threeSlots.FindAll(".bit-bfc-month-more").Count);
        Assert.AreEqual(0, fiveSlots.FindAll(".bit-bfc-month-more").Count);
    }

    #endregion

    #region Component base surface

    [TestMethod]
    public void BitFullCalendarShouldRenderTheStandardRootAttributes()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Id, "my-calendar");
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.Style, "--bit-bfc-height:400px");
        });

        var root = component.Find(".bit-bfc");
        Assert.AreEqual("my-calendar", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-bfc-height:400px");
    }

    [TestMethod]
    public void BitFullCalendarShouldHonourVisibility()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        StringAssert.Contains(component.Find(".bit-bfc").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitFullCalendarShouldFollowTheExplicitDirection()
    {
        var ltr = RenderCalendar();
        Assert.AreEqual("ltr", ltr.Find(".bit-bfc").GetAttribute("dir"));

        var rtl = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        Assert.AreEqual("rtl", rtl.Find(".bit-bfc").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitFullCalendarShouldFollowARightToLeftCultureWithoutBeingTold()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.CultureName, "fa-IR");
        });

        Assert.AreEqual("rtl", component.Find(".bit-bfc").GetAttribute("dir"));
    }

    [TestMethod]
    public void BitFullCalendarShouldTreatADisabledCalendarAsReadOnly()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.IsEnabled, false);
        });

        Assert.IsTrue(component.Instance.State.ReadOnly);
        Assert.AreEqual(0, component.FindAll(AddButtonSelector).Count);
    }

    #endregion

    #region Public navigation API and callbacks

    [TestMethod]
    public void BitFullCalendarShouldRaiseTheInitialDateRangeOnFirstRender()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.OnDateChange, EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, ranges.Add));
        });

        Assert.AreEqual(1, ranges.Count, "consumers need the first range to fetch the events for it");
        Assert.AreEqual(BitFullCalendarView.Month, ranges[0].View);
        Assert.AreEqual(1, ranges[0].Start.Day);
    }

    [TestMethod]
    public void BitFullCalendarNavigationMethodsShouldMoveTheCalendar()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);
        var start = component.Instance.State.SelectedDate;

        component.InvokeAsync(() => component.Instance.NavigateNext()).GetAwaiter().GetResult();
        Assert.AreEqual(start.AddDays(1), component.Instance.State.SelectedDate);

        component.InvokeAsync(() => component.Instance.NavigatePrevious()).GetAwaiter().GetResult();
        Assert.AreEqual(start, component.Instance.State.SelectedDate);

        component.InvokeAsync(() => component.Instance.GoToDate(start.AddDays(10))).GetAwaiter().GetResult();
        Assert.AreEqual(start.AddDays(10), component.Instance.State.SelectedDate);

        component.InvokeAsync(() => component.Instance.GoToToday()).GetAwaiter().GetResult();
        Assert.AreEqual(DateTime.Today, component.Instance.State.SelectedDate);
    }

    [TestMethod]
    public void BitFullCalendarChangeViewShouldClampIntoTheAllowedSet()
    {
        var component = RenderCalendar(views: [BitFullCalendarView.Week, BitFullCalendarView.Day]);

        component.InvokeAsync(() => component.Instance.ChangeView(BitFullCalendarView.Day)).GetAwaiter().GetResult();
        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.State.View);

        component.InvokeAsync(() => component.Instance.ChangeView(BitFullCalendarView.Year)).GetAwaiter().GetResult();
        Assert.AreEqual(BitFullCalendarView.Week, component.Instance.State.View, "an excluded view falls back into the set");
    }

    [TestMethod]
    public void BitFullCalendarGetVisibleRangeShouldReportWhatIsOnScreen()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);

        var (start, end) = component.Instance.GetVisibleRange();

        Assert.AreEqual(DateTime.Today, start);
        Assert.AreEqual(DateTime.Today, end);
    }

    #endregion

    #region Dialogs

    [TestMethod]
    public void BitFullCalendarShouldCloseTheDetailsDialogOnEscape()
    {
        var component = RenderCalendar();
        component.Find(".bit-bfc-event-badge").Click();
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count);

        component.Find(".bit-bfc-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldCloseTheAddDialogOnEscape()
    {
        var component = RenderCalendar();
        component.Find(".bit-bfc-month-cell").Click();
        Assert.IsTrue(component.FindAll(".bit-bfc-field").Count > 0);

        component.Find(".bit-bfc-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-field").Count);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldOfferAResourcePickerOnlyWithResources()
    {
        var without = RenderCalendar();
        without.Find(".bit-bfc-month-cell").Click();
        Assert.AreEqual(0, without.FindAll("select[id^='bfc-resource-']").Count);

        var with = RenderCalendar(resources: true);
        with.Find(".bit-bfc-month-cell").Click();

        var picker = with.Find("select[id^='bfc-resource-']");
        // The "(none)" option plus one per resource.
        Assert.AreEqual(2, picker.QuerySelectorAll("option").Length);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldNotRequireADescriptionByDefault()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(".bit-bfc-month-cell").Click();
        component.Find("input[id^='bfc-title-']").Change("Planning");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Add, changes[0].Kind);
        Assert.AreEqual("Planning", changes[0].Event.Title);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldRequireADescriptionWhenAskedTo()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { RequireEventDescription = true });
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(".bit-bfc-month-cell").Click();
        component.Find("input[id^='bfc-title-']").Change("Planning");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(0, changes.Count);
        Assert.IsTrue(component.FindAll(".bit-bfc-field-error").Count > 0);
    }

    #endregion

    #region Keyboard navigation

    [TestMethod]
    public void BitFullCalendarDayGridShouldBeASingleTabStop()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 },
            BitFullCalendarView.Day);

        var slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual(8, slots.Count);
        Assert.AreEqual(1, slots.Count(s => s.GetAttribute("tabindex") == "0"), "a grid must not flood the tab order");
        Assert.AreEqual(7, slots.Count(s => s.GetAttribute("tabindex") == "-1"));
    }

    [TestMethod]
    public void BitFullCalendarDayGridArrowKeysShouldMoveTheTabbableSlot()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 },
            BitFullCalendarView.Day);

        var first = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half")[0];
        Assert.AreEqual("0", first.GetAttribute("tabindex"));

        first.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual("-1", slots[0].GetAttribute("tabindex"));
        Assert.AreEqual("0", slots[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFullCalendarDayGridHomeAndEndShouldJumpToTheGridEdges()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 },
            BitFullCalendarView.Day);

        component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half")[0].KeyDown(new KeyboardEventArgs { Key = "End" });
        var slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual("0", slots[^1].GetAttribute("tabindex"));

        slots[^1].KeyDown(new KeyboardEventArgs { Key = "Home" });
        slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual("0", slots[0].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFullCalendarDayGridArrowKeysShouldStopAtTheGridEdge()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 },
            BitFullCalendarView.Day);

        component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        var slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual("0", slots[0].GetAttribute("tabindex"), "the first slot stays the tab stop");
    }

    [TestMethod]
    public void BitFullCalendarWeekGridShouldBeASingleTabStop()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 10 },
            BitFullCalendarView.Week);

        var slots = component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");
        Assert.AreEqual(7 * 4, slots.Count);
        Assert.AreEqual(1, slots.Count(s => s.GetAttribute("tabindex") == "0"));
    }

    [TestMethod]
    public void BitFullCalendarWeekGridArrowKeysShouldMoveAcrossDays()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 10 },
            BitFullCalendarView.Week);

        var firstColumn = component.FindAll(".bit-bfc-week-day-col")[0];
        firstColumn.QuerySelector(".bit-bfc-hour-slot")!.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var columns = component.FindAll(".bit-bfc-week-day-col");
        Assert.AreEqual(0, columns[0].QuerySelectorAll("[tabindex='0']").Length);
        Assert.AreEqual(1, columns[1].QuerySelectorAll("[tabindex='0']").Length);
    }

    [TestMethod]
    public void BitFullCalendarMonthGridShouldBeASingleTabStop()
    {
        var component = RenderCalendar();

        var hints = component.FindAll(".bit-bfc-cell-add-hint-month");
        Assert.IsTrue(hints.Count > 20);
        Assert.AreEqual(1, hints.Count(h => h.GetAttribute("tabindex") == "0"));
    }

    [TestMethod]
    public void BitFullCalendarMonthGridArrowKeysShouldMoveTheTabbableCell()
    {
        var component = RenderCalendar();
        var hints = component.FindAll(".bit-bfc-cell-add-hint-month");
        var startIndex = hints.ToList().FindIndex(h => h.GetAttribute("tabindex") == "0");

        hints[startIndex].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        hints = component.FindAll(".bit-bfc-cell-add-hint-month");
        Assert.AreEqual("-1", hints[startIndex].GetAttribute("tabindex"));
        Assert.AreEqual("0", hints[startIndex + 1].GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitFullCalendarMonthGridArrowDownShouldMoveAWholeWeek()
    {
        var component = RenderCalendar();
        var hints = component.FindAll(".bit-bfc-cell-add-hint-month");
        var startIndex = hints.ToList().FindIndex(h => h.GetAttribute("tabindex") == "0");

        hints[startIndex].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        hints = component.FindAll(".bit-bfc-cell-add-hint-month");
        Assert.AreEqual("0", hints[startIndex + 7].GetAttribute("tabindex"));
    }

    #endregion

    #region Keyboard move and resize

    private static IRenderedComponent<BitFullCalendar> RenderOneEventCalendar(
        List<BitFullCalendarChangeEventArgs> changes,
        BitFullCalendarTests owner,
        BitFullCalendarView view = BitFullCalendarView.Day,
        BitFullCalendarSettings? settings = null)
    {
        var today = DateTime.Today;
        return owner.RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(10) }
            ]);
            parameters.Add(p => p.DefaultView, view);
            parameters.Add(p => p.Settings, settings ?? new BitFullCalendarSettings());
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(owner, changes.Add));
        });
    }

    [TestMethod]
    public void BitFullCalendarAltArrowShouldMoveAnEventBlockByOneSlot()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderOneEventCalendar(changes, this);

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeSource.Drag, changes[0].Source);
        Assert.AreEqual(DateTime.Today.AddHours(9).AddMinutes(30), changes[0].Event.StartDate);
        Assert.AreEqual(DateTime.Today.AddHours(10).AddMinutes(30), changes[0].Event.EndDate);
    }

    [TestMethod]
    public void BitFullCalendarShiftArrowShouldResizeAnEventBlockByOneSlot()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderOneEventCalendar(changes, this);

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeSource.Resize, changes[0].Source);
        Assert.AreEqual(DateTime.Today.AddHours(9), changes[0].Event.StartDate, "a resize leaves the start alone");
        Assert.AreEqual(DateTime.Today.AddHours(10).AddMinutes(30), changes[0].Event.EndDate);
    }

    [TestMethod]
    public void BitFullCalendarShiftArrowShouldNotShrinkAnEventBelowOneSlot()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(30) }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", ShiftKey = true });

        Assert.AreEqual(0, changes.Count, "one slot is already the minimum length");
    }

    [TestMethod]
    public void BitFullCalendarKeyboardEditShouldObeyTheOverlapRule()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var refusals = new List<BitFullCalendarChangeRefusal>();
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(10) },
                new BitFullCalendarEvent { Id = "2", Title = "Retro", StartDate = today.AddHours(10), EndDate = today.AddHours(11) }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { AllowEventOverlap = false });
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
            parameters.Add(p => p.OnRefused, EventCallback.Factory.Create<BitFullCalendarChangeRefusal>(this, refusals.Add));
        });

        // Sliding the 09:00-10:00 event down a slot would run it into the 10:00 one; the two merely
        // touching today is exactly what makes the move the first real conflict.
        component.FindAll(".bit-bfc-event-block")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(0, changes.Count);
        CollectionAssert.AreEqual(new[] { BitFullCalendarChangeRefusal.Overlap }, refusals.ToArray());
    }

    [TestMethod]
    public void BitFullCalendarKeyboardEditShouldDoNothingOnALockedEvent()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Payroll", StartDate = today.AddHours(9), EndDate = today.AddHours(10), IsReadOnly = true }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitFullCalendarAltArrowShouldMoveAMonthBadgeByADay()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderOneEventCalendar(changes, this, BitFullCalendarView.Month);

        component.Find(".bit-bfc-event-badge").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(DateTime.Today.AddDays(1).AddHours(9), changes[0].Event.StartDate);
    }

    [TestMethod]
    public void BitFullCalendarPlainArrowOnAnEventShouldChangeNothing()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderOneEventCalendar(changes, this);

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(0, changes.Count, "the modifier is what turns an arrow into an edit");
    }

    #endregion

    #region Range selection

    /// <summary>
    /// Every slot of the rendered time grid, re-queried on each call: running a handler re-renders
    /// the component, which invalidates the handler ids an earlier query captured.
    /// </summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement>Slots(IRenderedComponent<BitFullCalendar> component)
        => component.FindAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half");

    /// <summary>The slots of one week-view day column, re-queried for the same reason.</summary>
    private static IReadOnlyList<AngleSharp.Dom.IElement>ColumnSlots(IRenderedComponent<BitFullCalendar> component, int columnIndex)
        => component.FindAll($".bit-bfc-week-day-col:nth-of-type({columnIndex + 1}) .bit-bfc-hour-slot, .bit-bfc-week-day-col:nth-of-type({columnIndex + 1}) .bit-bfc-hour-row-half");

    [TestMethod]
    public void BitFullCalendarShouldHighlightADraggedRangeOfSlots()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 },
            BitFullCalendarView.Day);

        Slots(component)[0].MouseDown();
        Slots(component)[2].MouseEnter();

        var slots = Slots(component);
        Assert.AreEqual(3, slots.Count(s => s.ClassList.Contains("bit-bfc-slot-selected")));
        Assert.IsTrue(slots[0].ClassList.Contains("bit-bfc-slot-selected"));
        Assert.IsTrue(slots[2].ClassList.Contains("bit-bfc-slot-selected"));
        Assert.IsFalse(slots[3].ClassList.Contains("bit-bfc-slot-selected"));
    }

    [TestMethod]
    public void BitFullCalendarShouldOpenTheDraftSpanningTheSelectedRange()
    {
        var drafts = new List<BitFullCalendarEvent?>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 });
            parameters.Add(p => p.OnAddClick, EventCallback.Factory.Create<BitFullCalendarEvent?>(this, drafts.Add));
        });

        // 08:00 through 09:30, four half-hour slots.
        Slots(component)[0].MouseDown();
        Slots(component)[3].MouseEnter();
        Slots(component)[3].MouseUp();

        Assert.AreEqual(1, drafts.Count);
        Assert.AreEqual(DateTime.Today.AddHours(8), drafts[0]!.StartDate);
        Assert.AreEqual(DateTime.Today.AddHours(10), drafts[0]!.EndDate);
    }

    [TestMethod]
    public void BitFullCalendarShouldSelectUpwardsToo()
    {
        var drafts = new List<BitFullCalendarEvent?>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 });
            parameters.Add(p => p.OnAddClick, EventCallback.Factory.Create<BitFullCalendarEvent?>(this, drafts.Add));
        });

        Slots(component)[3].MouseDown();
        Slots(component)[1].MouseEnter();
        Slots(component)[1].MouseUp();

        Assert.AreEqual(1, drafts.Count);
        Assert.AreEqual(DateTime.Today.AddHours(8).AddMinutes(30), drafts[0]!.StartDate);
        Assert.AreEqual(DateTime.Today.AddHours(10), drafts[0]!.EndDate);
    }

    [TestMethod]
    public void BitFullCalendarShouldLeaveASingleSlotPressToTheClickHandler()
    {
        var drafts = new List<BitFullCalendarEvent?>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12 });
            parameters.Add(p => p.OnAddClick, EventCallback.Factory.Create<BitFullCalendarEvent?>(this, drafts.Add));
        });

        Slots(component)[0].MouseDown();
        Slots(component)[0].MouseUp();

        Assert.AreEqual(0, drafts.Count, "a press and release on one slot is a click, not a range");

        Slots(component)[0].Click();

        Assert.AreEqual(1, drafts.Count);
        Assert.AreEqual(DateTime.Today.AddHours(8).AddMinutes(30), drafts[0]!.EndDate, "a click still covers one slot");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotSelectARangeWhenTheFeatureIsOff()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 12, AllowRangeSelection = false },
            BitFullCalendarView.Day);

        Slots(component)[0].MouseDown();
        Slots(component)[2].MouseEnter();

        Assert.AreEqual(0, component.FindAll(".bit-bfc-slot-selected").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldNotSelectARangeWhileReadOnly()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.ReadOnly, true);
        });

        Slots(component)[0].MouseDown();
        Slots(component)[2].MouseEnter();

        Assert.AreEqual(0, component.FindAll(".bit-bfc-slot-selected").Count);
    }

    [TestMethod]
    public void BitFullCalendarWeekRangeSelectionShouldStayInsideOneDayColumn()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { VisibleStartHour = 8, VisibleEndHour = 10 },
            BitFullCalendarView.Week);

        ColumnSlots(component, 0)[0].MouseDown();
        ColumnSlots(component, 1)[2].MouseEnter();

        var columns = component.FindAll(".bit-bfc-week-day-col");
        Assert.AreEqual(1, columns[0].QuerySelectorAll(".bit-bfc-slot-selected").Length, "the anchor slot stays selected on its own");
        Assert.AreEqual(0, columns[1].QuerySelectorAll(".bit-bfc-slot-selected").Length);
    }

    #endregion

    #region Agenda

    [TestMethod]
    public void BitFullCalendarAgendaSearchShouldMatchAnAttendeeName()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "1",
                    Title = "Standup",
                    StartDate = today.AddHours(9),
                    EndDate = today.AddHours(10),
                    Attendees = [new BitFullCalendarAttendee { FirstName = "Zainab", LastName = "Khan" }]
                },
                new BitFullCalendarEvent { Id = "2", Title = "Retro", StartDate = today.AddHours(11), EndDate = today.AddHours(12) }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Agenda);
        });

        Assert.AreEqual(2, component.FindAll(".bit-bfc-agenda-item").Count);

        component.Find(".bit-bfc-agenda-search input").Input("Zainab");

        var items = component.FindAll(".bit-bfc-agenda-item");
        Assert.AreEqual(1, items.Count);
        StringAssert.Contains(items[0].TextContent, "Standup");
    }

    [TestMethod]
    public void BitFullCalendarAgendaShouldLabelAnAllDayEvent()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Holiday", StartDate = today, EndDate = today.AddDays(1), IsAllDay = true }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Agenda);
        });

        Assert.AreEqual("All day", component.Find(".bit-bfc-agenda-time").TextContent.Trim());
    }

    #endregion

    #region Recurrence

    [TestMethod]
    public void BitFullCalendarShouldRenderARecurringSeriesAcrossTheMonth()
    {
        // One fixed mid-month date drives both the calendar and the series: on the real today the
        // month grid can run out of cells after the start date, leaving fewer occurrences than this
        // asserts.
        var start = new DateTime(2024, 6, 10);
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultDate, start);
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "series",
                    Title = "Standup",
                    StartDate = start.AddHours(9),
                    EndDate = start.AddHours(10),
                    Recurrence = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
                }
            ]);
        });

        // One badge per day of the visible month grid from the start date onwards.
        Assert.IsTrue(component.FindAll(".bit-bfc-event-badge").Count > 5);
        Assert.IsTrue(component.Instance.State.Events.All(e => e.IsOccurrence));
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderARecurringOccurrenceReadOnly()
    {
        // A fixed mid-month date, so the single occurrence always has a cell in the rendered grid.
        var start = new DateTime(2024, 6, 10);
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultDate, start);
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "series",
                    Title = "Standup",
                    StartDate = start.AddHours(9),
                    EndDate = start.AddHours(10),
                    Recurrence = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 1 }
                }
            ]);
        });

        var badge = component.Find(".bit-bfc-event-badge");
        Assert.AreEqual("false", badge.GetAttribute("draggable"));

        badge.Click();

        // Reading the details keeps working; the mutating actions are gone.
        Assert.IsNotNull(component.Find(".bit-bfc-dialog"));
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog-footer button").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldHandOnTheSeriesIdentityOnClick()
    {
        // A fixed mid-month date, so the three occurrences always land in the rendered grid and the
        // second badge is the day after the start.
        var start = new DateTime(2024, 6, 10);
        var clicked = new List<BitFullCalendarEvent>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultDate, start);
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "series",
                    Title = "Standup",
                    StartDate = start.AddHours(9),
                    EndDate = start.AddHours(10),
                    Recurrence = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 3 }
                }
            ]);
            parameters.Add(p => p.OnEventClick, EventCallback.Factory.Create<BitFullCalendarEvent>(this, clicked.Add));
        });

        component.FindAll(".bit-bfc-event-badge")[1].Click();

        Assert.AreEqual(1, clicked.Count);
        Assert.AreEqual("series", clicked[0].SeriesId);
        Assert.AreEqual(start.AddDays(1), clicked[0].OccurrenceDate);
    }

    #endregion

    #region More dialogs

    [TestMethod]
    public void BitFullCalendarAddDialogShouldRefuseAnOverlappingSaveWhenOverlapsAreDisallowed()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(8), EndDate = today.AddHours(10) }
            ]);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { AllowEventOverlap = false, StartOfDayHour = 8 });
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        // The toolbar's add button seeds the draft on the selected date at the start-of-day hour,
        // which the existing event already covers.
        component.Find(AddButtonSelector).Click();
        component.Find("input[id^='bfc-title-']").Change("Conflicting");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(0, changes.Count);
        Assert.IsTrue(component.FindAll(".bit-bfc-field-error").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldRefuseASaveOutsideTheBusinessHours()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            // The draft opens at the start-of-day hour, which sits before the business day.
            parameters.Add(p => p.Settings, new BitFullCalendarSettings
            {
                StartOfDayHour = 6,
                BusinessStartHour = 9,
                BusinessEndHour = 17,
                RestrictToBusinessHours = true
            });
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });

        component.Find(AddButtonSelector).Click();
        component.Find("input[id^='bfc-title-']").Change("Too early");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(0, changes.Count);
        Assert.IsTrue(component.FindAll(".bit-bfc-field-error").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldCarryTheEndWhenTheStartMoves()
    {
        var component = RenderCalendar();

        component.Find(AddButtonSelector).Click();

        // Both pickers report the range through their trigger button, so the rendered text is what
        // the form is holding. Moving the start has to take the end with it, not shorten the event.
        var triggersBefore = component.FindAll(".bit-bfc-dtp-trigger");
        Assert.AreEqual(2, triggersBefore.Count);

        // Open the start picker and pick the first day of the rendered grid.
        triggersBefore[0].Click();
        component.FindAll(".bit-bfc-dtp-panel .bit-bfc-dtp-day")[0].Click();

        var triggers = component.FindAll(".bit-bfc-dtp-trigger");
        Assert.AreNotEqual(
            triggersBefore[1].TextContent.Trim(),
            triggers[1].TextContent.Trim(),
            "the end followed the start instead of staying put");
    }

    #endregion

    #region Toolbar

    [TestMethod]
    public void BitFullCalendarShouldRenderTheToolbarByDefault()
    {
        var component = RenderCalendar();

        Assert.AreEqual(1, component.FindAll(".bit-bfc-header").Count);
    }

    [TestMethod]
    public void BitFullCalendarHideHeaderShouldRemoveTheWholeToolbar()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.HideHeader, true);
        });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-header").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-view-tabs").Count);
        Assert.AreEqual(0, component.FindAll(AddButtonSelector).Count);
        // The grid itself is untouched.
        Assert.AreEqual(1, component.FindAll(".bit-bfc-month").Count);
    }

    [TestMethod]
    public void BitFullCalendarViewTabsShouldBeASingleTabStop()
    {
        var component = RenderCalendar();

        var tabs = component.FindAll(".bit-bfc-view-tab");
        Assert.AreEqual(5, tabs.Count);
        Assert.AreEqual(1, tabs.Count(t => t.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("true", tabs.Single(t => t.GetAttribute("tabindex") == "0").GetAttribute("aria-selected"));
    }

    [TestMethod]
    public void BitFullCalendarViewTabsArrowKeysShouldMoveTheSelection()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);

        // Day is first in the strip, so the right arrow lands on Week.
        component.FindAll(".bit-bfc-view-tab")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual(BitFullCalendarView.Week, component.Instance.State.View);

        component.FindAll(".bit-bfc-view-tab")[1].KeyDown(new KeyboardEventArgs { Key = "End" });

        Assert.AreEqual(BitFullCalendarView.Agenda, component.Instance.State.View);
    }

    [TestMethod]
    public void BitFullCalendarViewTabsArrowKeysShouldStopAtTheStripEdge()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);

        component.FindAll(".bit-bfc-view-tab")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.State.View);
    }

    #endregion

    #region Business hours

    [TestMethod]
    public void BitFullCalendarShouldNotShadeAnyHourByDefault()
    {
        var component = RenderCalendar(defaultView: BitFullCalendarView.Day);

        Assert.AreEqual(0, component.FindAll(".bit-bfc-slot-off").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldShadeTheHoursOutsideTheBusinessWindow()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            // A Monday, so the day itself is a business day and only the hours decide.
            parameters.Add(p => p.DefaultDate, new DateTime(2025, 5, 12));
            parameters.Add(p => p.Settings, new BitFullCalendarSettings
            {
                HighlightBusinessHours = true,
                BusinessStartHour = 9,
                BusinessEndHour = 17,
                VisibleStartHour = 8,
                VisibleEndHour = 18,
                SlotDurationMinutes = 60
            });
        });

        var slots = component.FindAll(".bit-bfc-hour-slot");
        Assert.AreEqual(10, slots.Count);
        // 08:00 and 17:00 fall outside the 09:00-17:00 window; the eight hours between them do not.
        Assert.AreEqual(2, slots.Count(s => s.ClassList.Contains("bit-bfc-slot-off")));
    }

    [TestMethod]
    public void BitFullCalendarShouldShadeAWholeNonBusinessDay()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            // A Saturday: no hour of it is a business hour.
            parameters.Add(p => p.DefaultDate, new DateTime(2025, 5, 17));
            parameters.Add(p => p.Settings, new BitFullCalendarSettings
            {
                HighlightBusinessHours = true,
                VisibleStartHour = 8,
                VisibleEndHour = 18,
                SlotDurationMinutes = 60
            });
        });

        var slots = component.FindAll(".bit-bfc-hour-slot");
        Assert.AreEqual(10, slots.Count);
        Assert.AreEqual(10, slots.Count(s => s.ClassList.Contains("bit-bfc-slot-off")));
    }

    [TestMethod]
    public void BitFullCalendarShouldShadeTheNonBusinessMonthCells()
    {
        var component = RenderWithSettings(new BitFullCalendarSettings { HighlightBusinessHours = true });

        // Every rendered week contributes its Saturday and Sunday.
        var cells = component.FindAll(".bit-bfc-month-cell");
        Assert.IsTrue(cells.Count(c => c.ClassList.Contains("bit-bfc-day-off")) >= 8);
    }

    [TestMethod]
    public void BitFullCalendarShouldExposeTheBusinessHoursToggleInTheSettingsPanel()
    {
        var component = RenderCalendar();

        var toggles = OpenSettingsMenu(component);
        var businessToggle = toggles.Single(t => t.TextContent.Contains("business hours", StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual("false", businessToggle.GetAttribute("aria-pressed"));

        businessToggle.Click();

        Assert.IsTrue(component.Instance.State.HighlightBusinessHours);
    }

    #endregion

    #region Month grid shape

    [TestMethod]
    public void BitFullCalendarShouldRenderSixWeekRowsWhenAskedTo()
    {
        // February 2026 starts on a Sunday and is 28 days long, so it otherwise fits in four rows.
        var february = new DateTime(2026, 2, 10);

        var natural = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, []);
            parameters.Add(p => p.DefaultDate, february);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { FirstDayOfWeek = DayOfWeek.Sunday });
        });

        var padded = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, []);
            parameters.Add(p => p.DefaultDate, february);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { FirstDayOfWeek = DayOfWeek.Sunday, FixedWeekCount = true });
        });

        Assert.AreEqual(28, natural.FindAll(".bit-bfc-month-cell").Count);
        Assert.AreEqual(42, padded.FindAll(".bit-bfc-month-cell").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldBlankTheNeighbouringMonthDaysWhenAskedTo()
    {
        var may = new DateTime(2025, 5, 12);

        var shown = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, []);
            parameters.Add(p => p.DefaultDate, may);
        });

        var blanked = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, []);
            parameters.Add(p => p.DefaultDate, may);
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { ShowNonCurrentDates = false });
        });

        Assert.AreEqual(0, shown.FindAll(".bit-bfc-month-cell-blank").Count);
        // The blanked cells hold their columns open, so the grid keeps its shape.
        Assert.AreEqual(
            shown.FindAll(".bit-bfc-month-cell").Count,
            blanked.FindAll(".bit-bfc-month-cell").Count);
        Assert.IsTrue(blanked.FindAll(".bit-bfc-month-cell-blank").Count > 0);
        // A blanked cell offers neither the add affordance nor a day number.
        Assert.AreEqual(
            blanked.FindAll(".bit-bfc-month-cell").Count - blanked.FindAll(".bit-bfc-month-cell-blank").Count,
            blanked.FindAll(".bit-bfc-month-cell-day").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldMarkTheTrailingColumnOfEveryMonthRow()
    {
        // The separator between columns is dropped on the trailing one, and the grid is not always
        // seven wide - a work week narrows it - so the marker has to follow the real column count.
        var full = RenderCalendar();
        var workWeek = RenderWithSettings(
            new BitFullCalendarSettings { HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday] });

        Assert.AreEqual(
            full.FindAll(".bit-bfc-month-cell").Count / 7,
            full.FindAll(".bit-bfc-month-cell-last-col").Count);

        Assert.AreEqual(
            workWeek.FindAll(".bit-bfc-month-cell").Count / 5,
            workWeek.FindAll(".bit-bfc-month-cell-last-col").Count);
    }

    #endregion

    #region Navigation links

    [TestMethod]
    public void BitFullCalendarShouldNotRenderNavLinksByDefault()
    {
        var component = RenderCalendar();

        Assert.AreEqual(0, component.FindAll(".bit-bfc-navlink").Count);
    }

    [TestMethod]
    public void BitFullCalendarNavLinkShouldOpenTheClickedDay()
    {
        var component = RenderWithSettings(new BitFullCalendarSettings { NavLinks = true });

        var dayLinks = component.FindAll(".bit-bfc-month-cell-day.bit-bfc-navlink");
        Assert.IsTrue(dayLinks.Count > 0);

        dayLinks[10].Click();

        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.State.View);
    }

    [TestMethod]
    public void BitFullCalendarNavLinkShouldOnlyMoveTheDateWhenTheDayViewIsExcluded()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { NavLinks = true });
            parameters.Add(p => p.Views, new[] { BitFullCalendarView.Month, BitFullCalendarView.Agenda });
        });

        var dayLinks = component.FindAll(".bit-bfc-month-cell-day.bit-bfc-navlink");
        var clicked = int.Parse(dayLinks[10].TextContent.Trim());
        dayLinks[10].Click();

        // The view stays put because Day was excluded, but the date still followed the link.
        Assert.AreEqual(BitFullCalendarView.Month, component.Instance.State.View);
        Assert.AreEqual(clicked, component.Instance.State.SelectedDate.Day);
    }

    [TestMethod]
    public void BitFullCalendarNavLinkShouldOpenTheClickedWeek()
    {
        var component = RenderWithSettings(new BitFullCalendarSettings { NavLinks = true, ShowWeekNumbers = true });

        var weekLinks = component.FindAll(".bit-bfc-weeknum-cell.bit-bfc-navlink");
        Assert.IsTrue(weekLinks.Count > 0);

        weekLinks[1].Click();

        Assert.AreEqual(BitFullCalendarView.Week, component.Instance.State.View);
    }

    [TestMethod]
    public void BitFullCalendarNavLinkShouldOpenADayFromTheWeekViewHeader()
    {
        var component = RenderWithSettings(
            new BitFullCalendarSettings { NavLinks = true },
            BitFullCalendarView.Week);

        var headers = component.FindAll(".bit-bfc-week-header-day-link");
        Assert.AreEqual(7, headers.Count);

        headers[3].Click();

        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.State.View);
    }

    #endregion

    #region Event surface

    [TestMethod]
    public void BitFullCalendarShouldGiveAMonthBadgeATooltip()
    {
        var component = RenderCalendar();

        var badge = component.Find(".bit-bfc-event-badge");
        var tooltip = badge.GetAttribute("title");

        Assert.IsFalse(string.IsNullOrWhiteSpace(tooltip));
        StringAssert.Contains(tooltip!, "Standup");
    }

    [TestMethod]
    public void BitFullCalendarShouldNotPutAClockRangeOnAnAllDayTooltip()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Holiday", StartDate = today, EndDate = today.AddDays(1), IsAllDay = true }
            ]);
        });

        var tooltip = component.Find(".bit-bfc-event-badge").GetAttribute("title")!;

        StringAssert.Contains(tooltip, "All day");
        Assert.IsFalse(tooltip.Contains("00:00 - 00:00"));
    }

    [TestMethod]
    public void BitFullCalendarAgendaShouldListADayInClockOrder()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            // Supplied out of order, and with an all-day event that heads the date it covers.
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Afternoon", StartDate = today.AddHours(15), EndDate = today.AddHours(16) },
                new BitFullCalendarEvent { Id = "2", Title = "Morning", StartDate = today.AddHours(9), EndDate = today.AddHours(10) },
                new BitFullCalendarEvent { Id = "3", Title = "Holiday", StartDate = today, EndDate = today.AddDays(1), IsAllDay = true }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Agenda);
        });

        var titles = component.FindAll(".bit-bfc-agenda-item .bit-bfc-agenda-title")
            .Select(t => t.TextContent.Trim())
            .ToList();

        CollectionAssert.AreEqual(new[] { "Holiday", "Morning", "Afternoon" }, titles);
    }

    [TestMethod]
    public void BitFullCalendarShouldPlaceAnEventBlockAgainstTheHourHeightVariable()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent { Id = "1", Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(10) }
            ]);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
        });

        // Both the offset and the height read the custom property the hour rows are sized from, so a
        // consumer who re-scales the grid keeps the block aligned with the hour it sits on.
        var anchor = component.Find(".bit-bfc-event-stack-item").GetAttribute("style")!;
        var block = component.Find(".bit-bfc-event-block").GetAttribute("style")!;

        StringAssert.Contains(anchor, BitFullCalendarHelpers.HourHeightVariableName);
        StringAssert.Contains(block, BitFullCalendarHelpers.HourHeightVariableName);
    }

    [TestMethod]
    public void BitFullCalendarShouldReportTheVisibleRangeOnlyWhenItMoves()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.OnDateChange, EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, ranges.Add));
        });

        // The initial range is reported once, so a consumer can fetch the events it needs.
        Assert.AreEqual(1, ranges.Count);

        // "Today" while today is already on screen lands on the range that was just reported.
        component.Instance.GoToToday();
        Assert.AreEqual(1, ranges.Count);

        component.Instance.NavigateNext();
        Assert.AreEqual(2, ranges.Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldTintTodaysWeekColumn()
    {
        var thisWeek = RenderCalendar(defaultView: BitFullCalendarView.Week);
        Assert.AreEqual(1, thisWeek.FindAll(".bit-bfc-week-day-col.bit-bfc-today-col").Count);

        var anotherWeek = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Week);
            parameters.Add(p => p.DefaultDate, DateTime.Today.AddDays(30));
        });

        Assert.AreEqual(0, anotherWeek.FindAll(".bit-bfc-today-col").Count);
    }

    [TestMethod]
    public void BitFullCalendarDetailsDialogShouldDescribeTheRepeatRuleOfAnOccurrence()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "series",
                    Title = "Standup",
                    StartDate = today.AddHours(9),
                    EndDate = today.AddHours(10),
                    Recurrence = new BitFullCalendarRecurrence
                    {
                        Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                        Interval = 2,
                        Count = 6
                    }
                }
            ]);
        });

        // An occurrence carries no rule of its own, so the dialog has to look it up through the
        // series it was expanded from - otherwise nothing would say it is part of one.
        component.Find(".bit-bfc-event-badge").Click();

        var labels = component.FindAll(".bit-bfc-event-detail-label").Select(l => l.TextContent.Trim()).ToList();
        CollectionAssert.Contains(labels, "Repeats");

        var summary = component.FindAll(".bit-bfc-event-detail-value")
            .Select(v => v.TextContent.Trim())
            .Single(v => v.StartsWith("Weekly", StringComparison.Ordinal));

        StringAssert.Contains(summary, "every 2");
        StringAssert.Contains(summary, "6 times");
    }

    [TestMethod]
    public void BitFullCalendarDetailsDialogShouldStaySilentAboutAOneOffEvent()
    {
        var component = RenderCalendar();

        component.Find(".bit-bfc-event-badge").Click();

        var labels = component.FindAll(".bit-bfc-event-detail-label").Select(l => l.TextContent.Trim()).ToList();
        CollectionAssert.DoesNotContain(labels, "Repeats");
    }

    [TestMethod]
    public void BitFullCalendarShouldMakeAnOutOfRangeWeekColumnInert()
    {
        var today = DateTime.Today;
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Week);
            // Only today onwards is reachable, so the days before it are inert columns.
            parameters.Add(p => p.MinDate, today);
        });

        var columns = component.FindAll(".bit-bfc-week-day-col");
        var inert = columns.Where(c => c.ClassList.Contains("bit-bfc-out-of-range")).ToList();
        Assert.IsTrue(inert.Count > 0, "the week has to contain at least one unreachable day for this test");

        foreach (var column in inert)
        {
            foreach (var slot in column.QuerySelectorAll(".bit-bfc-hour-slot, .bit-bfc-hour-row-half"))
            {
                Assert.IsNull(slot.GetAttribute("role"), "an inert slot is not a button");
                Assert.IsNull(slot.GetAttribute("tabindex"), "an inert slot is not in the tab order");
                Assert.IsNull(slot.GetAttribute("aria-label"));
            }
        }
    }

    #endregion
}
