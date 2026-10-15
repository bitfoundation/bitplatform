using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers <see cref="BitFcRecurrenceScopeDialog"/>, the "this event or the whole series" prompt an edit or a delete
/// of a recurring occurrence opens, through the calendar that renders it: its markup and accessible names, its
/// texts and parts, the modal setup, the choice it confirms and every way it backs out.
/// </summary>
[TestClass]
public class BitFullCalendarRecurrenceScopeDialogTests : BunitTestContext
{
    private static readonly DateTime Start = new(2024, 6, 10);

    private const string Scope = ".bit-bfc-scope-dialog";

    /// <summary>
    /// A month calendar over one daily series of three occurrences starting June 10 at 09:00, so the second badge is
    /// June 11.
    /// </summary>
    private IRenderedComponent<BitFullCalendar> RenderSeries(List<BitFullCalendarChangeEventArgs>? changes = null,
                                                             Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultDate, Start);
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.Events,
            [
                new BitFullCalendarEvent
                {
                    Id = "series",
                    Title = "Standup",
                    StartDate = Start.AddHours(9),
                    EndDate = Start.AddHours(10),
                    Recurrence = new BitFullCalendarRecurrence { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = 3 }
                }
            ]);

            if (changes is not null)
            {
                parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
            }

            configure?.Invoke(parameters);
        });
    }

    private static void OpenDelete(IRenderedComponent<BitFullCalendar> component)
    {
        component.FindAll(".bit-bfc-event-badge")[1].Click();
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();
    }

    private static void OpenEdit(IRenderedComponent<BitFullCalendar> component)
    {
        component.FindAll(".bit-bfc-event-badge")[1].Click();
        component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn")[0].Click();
    }

    private static List<AngleSharp.Dom.IElement> Radios(IRenderedComponent<BitFullCalendar> component)
        => [.. component.FindAll($"{Scope} input[type=radio]")];

    #region Markup

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldNotRenderForAOneOffEvent()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.DefaultDate, Start);
            parameters.Add(p => p.Events, [new BitFullCalendarEvent { Id = "one", Title = "One", StartDate = Start.AddHours(9), EndDate = Start.AddHours(10) }]);
        });

        component.Find(".bit-bfc-event-badge").Click();
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();

        Assert.AreEqual(0, component.FindAll(Scope).Count, "only an occurrence of a series asks which part is meant");
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldBeAModalNamedByItsTitle()
    {
        var component = RenderSeries();
        OpenDelete(component);

        var dialog = component.Find(Scope);
        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        Assert.AreEqual("-1", dialog.GetAttribute("tabindex"));

        var title = dialog.QuerySelector(".bit-bfc-dialog-header h3")!;
        Assert.AreEqual("Delete recurring event", title.TextContent.Trim());
        Assert.AreEqual(title.Id, dialog.GetAttribute("aria-labelledby"));

        var group = dialog.QuerySelector("[role=radiogroup]")!;
        Assert.AreEqual(title.Id, group.GetAttribute("aria-labelledby"));

        // It sits on an overlay of its own, on top of the details dialog.
        Assert.IsTrue(dialog.ParentElement!.ClassList.Contains("bit-bfc-overlay"));
        Assert.AreEqual(2, component.FindAll(".bit-bfc-dialog").Count);
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldBeTitledForAnEdit()
    {
        var component = RenderSeries();
        OpenEdit(component);

        Assert.AreEqual("Edit recurring event", component.Find($"{Scope} h3").TextContent.Trim());
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldOfferTwoChoicesWithThisEventPreselected()
    {
        var component = RenderSeries();
        OpenDelete(component);

        var radios = Radios(component);
        Assert.AreEqual(2, radios.Count);
        Assert.IsTrue(radios[0].HasAttribute("checked"));
        Assert.IsFalse(radios[1].HasAttribute("checked"));

        // One group, so the arrow keys move between them and only one is ever checked.
        Assert.IsFalse(string.IsNullOrEmpty(radios[0].GetAttribute("name")));
        Assert.AreEqual(radios[0].GetAttribute("name"), radios[1].GetAttribute("name"));

        CollectionAssert.AreEqual(
            new[] { "This event", "All events in the series" },
            component.FindAll($"{Scope} .bit-bfc-scope-option").Select(o => o.TextContent.Trim()).ToArray());
        Assert.IsTrue(component.FindAll($"{Scope} .bit-bfc-scope-option").All(o => o.TagName == "LABEL"), "a click on the text checks the radio");
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldRenderCancelAndOkButtons()
    {
        var component = RenderSeries();
        OpenDelete(component);

        var buttons = component.FindAll($"{Scope} .bit-bfc-dialog-footer button");
        CollectionAssert.AreEqual(new[] { "Cancel", "OK" }, buttons.Select(b => b.TextContent.Trim()).ToArray());
        Assert.IsTrue(buttons.All(b => b.GetAttribute("type") == "button"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-bfc-btn-primary"));
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldTakeItsWordsFromTheTexts()
    {
        var component = RenderSeries(configure: p => p.Add(c => c.Texts, new BitFullCalendarTexts
        {
            RecurringDeleteTitle = "Remove a repeating meeting",
            ThisOccurrenceOption = "Only this one",
            AllOccurrencesOption = "Every one",
            CancelButton = "Keep",
            OkButton = "Go",
        }));
        OpenDelete(component);

        Assert.AreEqual("Remove a repeating meeting", component.Find($"{Scope} h3").TextContent.Trim());
        CollectionAssert.AreEqual(
            new[] { "Only this one", "Every one" },
            component.FindAll($"{Scope} .bit-bfc-scope-option").Select(o => o.TextContent.Trim()).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Keep", "Go" },
            component.FindAll($"{Scope} .bit-bfc-dialog-footer button").Select(b => b.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldCarryTheDialogClassAndStyle()
    {
        var component = RenderSeries(configure: p =>
        {
            p.Add(c => c.Classes, new BitFullCalendarClassStyles { Dialog = "custom-dialog" });
            p.Add(c => c.Styles, new BitFullCalendarClassStyles { Dialog = "max-width:20rem" });
        });
        OpenDelete(component);

        var dialog = component.Find(Scope);
        Assert.IsTrue(dialog.ClassList.Contains("custom-dialog"));
        Assert.AreEqual("max-width:20rem", dialog.GetAttribute("style"));
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldTrapTheFocusWhileOpen()
    {
        var component = RenderSeries();
        var before = Context.JSInterop.Invocations["BitBlazorUI.FullCalendar.setupDialog"].Count;

        OpenDelete(component);

        Assert.AreEqual(before + 2, Context.JSInterop.Invocations["BitBlazorUI.FullCalendar.setupDialog"].Count, "the details dialog and the prompt on top of it");

        var teardowns = Context.JSInterop.Invocations["BitBlazorUI.FullCalendar.teardownDialog"].Count;
        component.Find($"{Scope} .bit-bfc-dialog-footer .bit-bfc-btn:not(.bit-bfc-btn-primary)").Click();

        Assert.AreEqual(teardowns + 1, Context.JSInterop.Invocations["BitBlazorUI.FullCalendar.teardownDialog"].Count);
    }

    #endregion

    #region Choosing

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldConfirmTheLastChoiceMade()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(changes);
        OpenDelete(component);

        Radios(component)[1].Change(true);
        Assert.IsTrue(Radios(component)[1].HasAttribute("checked"));
        Assert.IsFalse(Radios(component)[0].HasAttribute("checked"));

        Radios(component)[0].Change(true);
        component.Find($"{Scope} .bit-bfc-btn-primary").Click();

        // Back on "this event": the date is skipped and the series stays.
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);
        Assert.AreEqual(1, component.Instance.State.AllEvents.Count);
        Assert.AreEqual(2, component.FindAll(".bit-bfc-event-badge").Count);
        Assert.AreEqual(0, component.FindAll(Scope).Count);
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldOpenTheSeriesRuleWhenEveryEventIsEdited()
    {
        var component = RenderSeries();
        OpenEdit(component);

        Radios(component)[1].Change(true);
        component.Find($"{Scope} .bit-bfc-btn-primary").Click();

        Assert.AreEqual(0, component.FindAll(Scope).Count);
        Assert.AreEqual(1, component.FindAll("select[id^='bfc-repeat-']").Count, "the whole series is edited through its rule");
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldStartFromThisEventEachTimeItOpens()
    {
        var component = RenderSeries();
        OpenDelete(component);
        Radios(component)[1].Change(true);
        component.Find($"{Scope} .bit-bfc-dialog-footer .bit-bfc-btn:not(.bit-bfc-btn-primary)").Click();

        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();

        Assert.IsTrue(Radios(component)[0].HasAttribute("checked"));
        Assert.IsFalse(Radios(component)[1].HasAttribute("checked"));
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldNameEachDialogsGroupApart()
    {
        var component = RenderSeries();
        OpenDelete(component);
        var first = Radios(component)[0].GetAttribute("name");
        var firstTitle = component.Find($"{Scope} h3").Id;
        component.Find($"{Scope} .bit-bfc-dialog-footer .bit-bfc-btn:not(.bit-bfc-btn-primary)").Click();

        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();

        Assert.AreNotEqual(first, Radios(component)[0].GetAttribute("name"));
        Assert.AreNotEqual(firstTitle, component.Find($"{Scope} h3").Id);
    }

    #endregion

    #region Backing out

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldCancelFromItsOverlay()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(changes);
        OpenDelete(component);

        component.Find(Scope).ParentElement!.Click();

        Assert.AreEqual(0, component.FindAll(Scope).Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count, "the details dialog stays open");
        Assert.AreEqual(0, changes.Count);
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldStayOpenOnAClickInsideIt()
    {
        var component = RenderSeries();
        OpenDelete(component);

        try
        {
            component.Find($"{Scope} h3").Click();
        }
        catch (MissingEventHandlerException)
        {
            // The dialog stops the click before it reaches any handler, which is exactly the point.
        }

        Assert.AreEqual(1, component.FindAll(Scope).Count);
    }

    [TestMethod]
    [DataRow(false, false, false, false, true)]
    [DataRow(true, false, false, false, false)]
    [DataRow(false, true, false, false, false)]
    [DataRow(false, false, true, false, false)]
    [DataRow(false, false, false, true, false)]
    public void BitFullCalendarScopeDialogShouldCancelOnAPlainEscapeOnly(bool shift, bool ctrl, bool alt, bool meta, bool closes)
    {
        var component = RenderSeries();
        OpenDelete(component);
        Assert.AreEqual("claim", component.Find(Scope).GetAttribute("data-bit-esc"));

        // A modified Escape is left to the surface the calendar sits in.
        component.Find(Scope).KeyDown(new KeyboardEventArgs { Key = "Escape", ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta });

        Assert.AreEqual(closes ? 0 : 1, component.FindAll(Scope).Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog:not(.bit-bfc-scope-dialog)").Count, "the details dialog stays either way");
    }

    [TestMethod]
    public void BitFullCalendarScopeDialogShouldIgnoreOtherKeys()
    {
        var component = RenderSeries();
        OpenDelete(component);

        component.Find(Scope).KeyDown(new KeyboardEventArgs { Key = "Enter" });
        component.Find(Scope).KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.AreEqual(1, component.FindAll(Scope).Count);
        Assert.AreEqual(3, component.FindAll(".bit-bfc-event-badge").Count);
    }

    #endregion
}
