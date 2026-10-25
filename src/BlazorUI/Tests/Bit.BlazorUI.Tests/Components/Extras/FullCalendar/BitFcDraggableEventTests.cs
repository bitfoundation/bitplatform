using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers <see cref="BitFcDraggableEvent"/>, the drag source that wraps free-form event content. None of the
/// calendar's own views renders it, so it is rendered here on its own, under the <see cref="BitFullCalendarState"/>
/// it reads from the cascade the way every view does: its button semantics, whether it offers a drag, the drag it
/// starts and ends on the state, and its activation by pointer and keyboard.
/// </summary>
[TestClass]
public class BitFcDraggableEventTests : BunitTestContext
{
    private static BitFullCalendarEvent Standup(bool isReadOnly = false) => new()
    {
        Id = "standup",
        Title = "Standup",
        StartDate = new DateTime(2026, 3, 18, 9, 0, 0),
        EndDate = new DateTime(2026, 3, 18, 10, 0, 0),
        IsReadOnly = isReadOnly
    };

    private IRenderedComponent<BitFcDraggableEvent> RenderDraggable(BitFullCalendarState state,
                                                                    BitFullCalendarEvent ev,
                                                                    Action? onClick = null,
                                                                    Action? onOuterClick = null,
                                                                    string? cssClass = null)
    {
        var host = Context.Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "outer");
            if (onOuterClick is not null)
            {
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, onOuterClick));
            }

            builder.OpenComponent<CascadingValue<BitFullCalendarState>>(3);
            builder.AddAttribute(4, "Value", state);
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<BitFcDraggableEvent>(0);
                inner.AddAttribute(1, nameof(BitFcDraggableEvent.Event), ev);
                inner.AddAttribute(2, nameof(BitFcDraggableEvent.Class), cssClass);
                if (onClick is not null)
                {
                    inner.AddAttribute(3, nameof(BitFcDraggableEvent.OnClick), EventCallback.Factory.Create(this, onClick));
                }
                inner.AddAttribute(4, nameof(BitFcDraggableEvent.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenElement(0, "span");
                    content.AddAttribute(1, "class", "content");
                    content.AddContent(2, ev.Title);
                    content.CloseElement();
                }));
                inner.CloseComponent();
            }));
            builder.CloseComponent();

            builder.CloseElement();
        });

        return host.FindComponent<BitFcDraggableEvent>();
    }

    #region Markup

    [TestMethod]
    public void BitFcDraggableEventShouldWrapItsContentInAFocusableButton()
    {
        var component = RenderDraggable(new BitFullCalendarState(), Standup(), cssClass: "custom-event");

        var root = component.Find("[role=button]");
        // A div rather than a native button, since the content may carry interactive elements of its own.
        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual("0", root.GetAttribute("tabindex"));
        Assert.IsTrue(root.ClassList.Contains("custom-event"));
        Assert.IsFalse(root.ClassList.Contains("bit-bfc-dragging"));
        Assert.AreEqual("Standup", root.QuerySelector(".content")!.TextContent);

        // The button chrome is reset inline, so the content looks the way it would anywhere else.
        StringAssert.Contains(root.GetAttribute("style"), "cursor:pointer;");
        StringAssert.Contains(root.GetAttribute("style"), "background:none;");
    }

    [TestMethod]
    public void BitFcDraggableEventShouldOfferADragWhenTheEventCanMove()
    {
        var component = RenderDraggable(new BitFullCalendarState(), Standup());

        Assert.AreEqual("true", component.Find("[role=button]").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFcDraggableEventShouldNotOfferADragForALockedEvent()
    {
        var component = RenderDraggable(new BitFullCalendarState(), Standup(isReadOnly: true));

        Assert.AreEqual("false", component.Find("[role=button]").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFcDraggableEventShouldNotOfferADragOnAReadOnlyCalendar()
    {
        var state = new BitFullCalendarState();
        state.SetReadOnly(true);

        var component = RenderDraggable(state, Standup());

        Assert.AreEqual("false", component.Find("[role=button]").GetAttribute("draggable"));
    }

    [TestMethod]
    public void BitFcDraggableEventShouldNotOfferADragWhenDraggingIsNotAllowed()
    {
        var state = new BitFullCalendarState();
        state.SetEditPermissions(add: true, edit: true, delete: true, drag: false, resize: true);

        var component = RenderDraggable(state, Standup());

        Assert.AreEqual("false", component.Find("[role=button]").GetAttribute("draggable"));
    }

    #endregion

    #region Dragging

    [TestMethod]
    public void BitFcDraggableEventShouldStartAndEndTheDragOnTheState()
    {
        var state = new BitFullCalendarState();
        var ev = Standup();
        var component = RenderDraggable(state, ev);

        component.Find("[role=button]").DragStart();

        Assert.AreSame(ev, state.DraggedEvent);
        Assert.IsTrue(component.Find("[role=button]").ClassList.Contains("bit-bfc-dragging"));

        component.Find("[role=button]").DragEnd();

        Assert.IsFalse(state.IsDragging);
        Assert.IsFalse(component.Find("[role=button]").ClassList.Contains("bit-bfc-dragging"));
    }

    [TestMethod]
    public void BitFcDraggableEventShouldNotDragALockedEvent()
    {
        var state = new BitFullCalendarState();
        var component = RenderDraggable(state, Standup(isReadOnly: true));

        component.Find("[role=button]").DragStart();

        Assert.IsFalse(state.IsDragging);
        Assert.IsFalse(component.Find("[role=button]").ClassList.Contains("bit-bfc-dragging"));
    }

    [TestMethod]
    public void BitFcDraggableEventShouldOnlyMarkTheEventBeingDragged()
    {
        var state = new BitFullCalendarState();
        state.StartDrag(new BitFullCalendarEvent { Id = "other", Title = "Other", StartDate = DateTime.Today, EndDate = DateTime.Today.AddHours(1) });

        var component = RenderDraggable(state, Standup());

        Assert.IsTrue(state.IsDragging);
        Assert.IsFalse(component.Find("[role=button]").ClassList.Contains("bit-bfc-dragging"));
    }

    #endregion

    #region Activation

    [TestMethod]
    public void BitFcDraggableEventShouldRaiseOnClickWithoutBubbling()
    {
        var clicks = 0;
        var outer = 0;
        var component = RenderDraggable(new BitFullCalendarState(), Standup(), onClick: () => clicks++, onOuterClick: () => outer++);

        component.Find("[role=button]").Click();

        Assert.AreEqual(1, clicks);
        Assert.AreEqual(0, outer, "a click on the event must not also reach the cell it sits in");
    }

    [TestMethod]
    [DataRow("Enter", false, 1)]
    [DataRow(" ", false, 1)]
    [DataRow("Spacebar", false, 1)]
    [DataRow("Enter", true, 0)]
    [DataRow(" ", true, 0)]
    [DataRow("a", false, 0)]
    [DataRow("Escape", false, 0)]
    public void BitFcDraggableEventShouldActivateFromTheKeyboardLikeAButton(string key, bool repeat, int expected)
    {
        var clicks = 0;
        var component = RenderDraggable(new BitFullCalendarState(), Standup(), onClick: () => clicks++);

        component.Find("[role=button]").KeyDown(new KeyboardEventArgs { Key = key, Repeat = repeat });

        Assert.AreEqual(expected, clicks);
    }

    [TestMethod]
    public void BitFcDraggableEventShouldStillStopAClickWithoutAnOnClick()
    {
        var outer = 0;
        var component = RenderDraggable(new BitFullCalendarState(), Standup(), onOuterClick: () => outer++);

        // An unbound OnClick renders no handler, but the click is still kept from the cell around the event.
        try
        {
            component.Find(".content").Click();
        }
        catch (MissingEventHandlerException)
        {
        }
        component.Find("[role=button]").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.AreEqual(0, outer);
    }

    #endregion
}
