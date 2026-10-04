using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the five edit permissions of <see cref="BitFullCalendarSettings"/> (add, edit, delete, drag, resize) and
/// <see cref="BitFullCalendar.ScrollToTimeAsync"/>.
/// </summary>
[TestClass]
public class BitFullCalendarPermissionsTests : BunitTestContext
{
    private static readonly DateTime Anchor = new(2031, 6, 18);

    private static List<BitFullCalendarEvent> Events() =>
    [
        new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) },
    ];

    private IRenderedComponent<BitFullCalendar> RenderCalendar(BitFullCalendarSettings settings, BitFullCalendarView view = BitFullCalendarView.Week)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, view);
            parameters.Add(p => p.Settings, settings);
        });
    }

    [TestMethod]
    public void BitFullCalendarShouldRemoveEveryAddAffordanceWhenAddingIsNotAllowed()
    {
        var component = RenderCalendar(new() { AllowAdd = false });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-btn-primary").Count, "no Add Event button");
        Assert.AreEqual(0, component.FindAll(".bit-bfc-cell-add-hint").Count, "no add hint on a slot");
        Assert.AreEqual(0, component.FindAll(".bit-bfc-hour-slot[role=button]").Count, "the slots are inert");

        // Events can still be moved.
        Assert.AreEqual("true", component.Find("[data-bit-bfc-event='1']").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepMovingAndResizingApart()
    {
        var noResize = RenderCalendar(new() { AllowResize = false });
        var block = noResize.Find("[data-bit-bfc-event='1']");
        Assert.AreEqual("true", block.GetAttribute("draggable"));
        Assert.AreEqual(0, noResize.FindAll(".bit-bfc-resize-handle").Count);

        var noDrag = RenderCalendar(new() { AllowDrag = false });
        block = noDrag.Find("[data-bit-bfc-event='1']");
        Assert.AreEqual("false", block.GetAttribute("draggable"));
        Assert.IsNull(block.GetAttribute("data-bit-bfc-move"));
        Assert.AreEqual(2, noDrag.FindAll(".bit-bfc-resize-handle").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldNameTheAxesItsKeyboardEditsUse()
    {
        // The script cancels Alt+Arrow (data-bit-bfc-move) and Shift+Arrow (data-bit-bfc-resize) defaults on these
        // axes only, so a resize still claims its key when moving is off, and the reverse.
        var noDrag = RenderCalendar(new() { AllowDrag = false }).Find("[data-bit-bfc-event='1']");
        Assert.IsNull(noDrag.GetAttribute("data-bit-bfc-move"));
        Assert.AreEqual("y", noDrag.GetAttribute("data-bit-bfc-resize"));

        var noResize = RenderCalendar(new() { AllowResize = false }).Find("[data-bit-bfc-event='1']");
        Assert.AreEqual("xy", noResize.GetAttribute("data-bit-bfc-move"));
        Assert.IsNull(noResize.GetAttribute("data-bit-bfc-resize"));
    }

    [TestMethod]
    public void BitFullCalendarShouldTurnOffDragAndResizeWithEditing()
    {
        var component = RenderCalendar(new() { AllowEdit = false });
        var state = component.Instance.State;
        var ev = state.Events.Single();

        Assert.IsFalse(state.CanDrag(ev));
        Assert.IsFalse(state.CanResize(ev));
        Assert.IsTrue(state.CanDelete(ev), "deleting is a permission of its own");
        Assert.IsTrue(state.CanAdd);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-resize-handle").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldRefuseAKeyboardMoveWhenDraggingIsNotAllowed()
    {
        var component = RenderCalendar(new() { AllowDrag = false });

        component.Find("[data-bit-bfc-event='1']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(Anchor.AddHours(9), component.Instance.State.Events.Single().StartDate);
    }

    [TestMethod]
    public void BitFullCalendarShouldStillResizeByKeyboardWhenOnlyDraggingIsOff()
    {
        var component = RenderCalendar(new() { AllowDrag = false });

        component.Find("[data-bit-bfc-event='1']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", ShiftKey = true });

        component.WaitForAssertion(() => Assert.AreEqual(Anchor.AddHours(10).AddMinutes(30), component.Instance.State.Events.Single().EndDate));
    }

    [TestMethod]
    public void BitFullCalendarShouldNotEnterADragWhenDraggingIsNotAllowed()
    {
        var state = RenderCalendar(new() { AllowDrag = false }).Instance.State;

        state.StartDrag(state.Events.Single());

        Assert.IsFalse(state.IsDragging);
    }

    [TestMethod]
    public void BitFullCalendarShouldOfferOnlyTheAllowedActionsInTheDetailsDialog()
    {
        var component = RenderCalendar(new() { AllowDelete = false });

        component.Find("[data-bit-bfc-event='1']").Click();

        var footer = component.Find(".bit-bfc-dialog-footer");
        Assert.IsFalse(footer.QuerySelectorAll(".bit-bfc-btn-danger").Any(), "no Delete");
        Assert.AreEqual(2, footer.QuerySelectorAll("button").Length, "Edit and Close");
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyAPermissionChangedOnTheSettings()
    {
        var settings = new BitFullCalendarSettings();
        var component = RenderCalendar(settings);
        Assert.IsTrue(component.Instance.State.CanAdd);

        settings.AllowAdd = false;
        component.Render(p => p.Add(x => x.Settings, settings));

        Assert.IsFalse(component.Instance.State.CanAdd);
    }

    [TestMethod]
    public async Task BitFullCalendarShouldScrollTheTimeGridFromItsFirstHour()
    {
        Context.JSInterop.Setup<bool>("BitBlazorUI.FullCalendar.scrollGridToTime", _ => true).SetResult(true);
        var component = RenderCalendar(new() { VisibleStartHour = 6 });

        var scrolled = await component.InvokeAsync(() => component.Instance.ScrollToTimeAsync(TimeSpan.FromHours(14)));

        Assert.IsTrue(scrolled);
        var call = Context.JSInterop.Invocations["BitBlazorUI.FullCalendar.scrollGridToTime"].Last();
        Assert.AreEqual(8.0, (double)call.Arguments[1]!, "14:00 is eight hours past a grid starting at 06:00");
    }

    [TestMethod]
    public async Task BitFullCalendarShouldNotScrollAViewWithoutATimeAxis()
    {
        var component = RenderCalendar(new(), BitFullCalendarView.Month);

        var scrolled = await component.InvokeAsync(() => component.Instance.ScrollToTimeAsync(TimeSpan.FromHours(14)));

        Assert.IsFalse(scrolled);
    }
}
