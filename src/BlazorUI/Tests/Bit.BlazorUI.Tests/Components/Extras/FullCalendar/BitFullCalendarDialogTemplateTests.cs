using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the consumer's own part of the built-in dialogs: <see cref="BitFullCalendar.EventDetailsTemplate"/> and
/// <see cref="BitFullCalendar.EventEditorTemplate"/>, whose draft's Data is what a save commits.
/// </summary>
[TestClass]
public class BitFullCalendarDialogTemplateTests : BunitTestContext
{
    private static readonly DateTime Anchor = new(2031, 6, 18);

    private static List<BitFullCalendarEvent> Events() =>
    [
        new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10), Data = "Room 1" },
    ];

    private RenderFragment<BitFullCalendarEvent> LocationEditor => ev => builder =>
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "class", "set-location");
        builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => ev.Data = "Room 4"));
        builder.AddContent(3, $"Location: {ev.Data}");
        builder.CloseElement();
    };

    private static RenderFragment<BitFullCalendarEvent> LocationDetails => ev => builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "location");
        builder.AddContent(2, ev.Data);
        builder.CloseElement();
    };

    private IRenderedComponent<BitFullCalendar> RenderCalendar(List<BitFullCalendarChangeEventArgs>? changes = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.EventDetailsTemplate, LocationDetails);
            parameters.Add(p => p.EventEditorTemplate, LocationEditor);
            if (changes is not null)
                parameters.Add(p => p.OnChange, (BitFullCalendarChangeEventArgs args) => changes.Add(args));
        });
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderTheDetailsTemplateUnderTheBuiltInRows()
    {
        var component = RenderCalendar();

        component.Find("[data-bit-bfc-event='1']").Click();

        Assert.AreEqual("Room 1", component.Find(".bit-bfc-dialog .bit-bfc-dialog-custom .location").TextContent);
    }

    [TestMethod]
    public void BitFullCalendarShouldSaveTheDataTheEditorTemplateSets()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderCalendar(changes);

        component.Find("[data-bit-bfc-event='1']").Click();
        component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn").First(b => b.TextContent.Trim() == "Edit").Click();
        component.Find(".set-location").Click();
        component.FindAll(".bit-bfc-btn-primary").Last().Click();

        Assert.AreEqual("Room 4", component.Instance.State.AllEvents.Single().Data);
        Assert.AreEqual("Room 4", changes.Single().Event.Data);
        Assert.AreEqual("Room 1", changes.Single().OldEvent!.Data);
    }

    [TestMethod]
    public void BitFullCalendarShouldLeaveTheDataAloneWhenTheEditIsCancelled()
    {
        var component = RenderCalendar();

        component.Find("[data-bit-bfc-event='1']").Click();
        component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn").First(b => b.TextContent.Trim() == "Edit").Click();
        component.Find(".set-location").Click();
        component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn").Last(b => b.TextContent.Trim() == "Cancel").Click();

        Assert.AreEqual("Room 1", component.Instance.State.AllEvents.Single().Data);
    }

    [TestMethod]
    public void BitFullCalendarShouldHandANewEventDraftToTheEditorTemplate()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderCalendar(changes);

        component.Find(".bit-bfc-btn-primary").Click();
        component.Find(".bit-bfc-dialog input[type=text]").Change("Retro");
        component.Find(".set-location").Click();
        component.FindAll(".bit-bfc-btn-primary").Last().Click();

        Assert.AreEqual("Room 4", changes.Single().Event.Data);
    }
}
