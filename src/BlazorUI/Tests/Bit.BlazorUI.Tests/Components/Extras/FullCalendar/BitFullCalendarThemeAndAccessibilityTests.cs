using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the theme surface (BitFullCalendarParams, Classes/Styles, IsLoading) and the accessibility contract of the
/// calendar's chrome: a named root, labelled tab strips controlling a labelled panel, a settings popup of real form
/// semantics, today marked as the current date, form errors tied to their fields, and keys that follow the rendered
/// direction rather than the culture's.
/// </summary>
[TestClass]
public class BitFullCalendarThemeAndAccessibilityTests : BunitTestContext
{
    private static List<BitFullCalendarEvent> Events()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = "1", Title = "Standup", Description = "Daily sync", StartDate = today.AddHours(9), EndDate = today.AddHours(10) }
        ];
    }

    #region BitParams

    [TestMethod]
    public void BitFullCalendarShouldTakeTheCascadedParametersItDoesNotSetItself()
    {
        var component = RenderComponent<BitFullCalendarCascadingParamsTest>();

        var first = component.Find("#first-calendar");

        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.AreEqual(2, first.QuerySelectorAll(".bit-bfc-view-tab").Length);
        Assert.IsNull(first.QuerySelector(".bit-bfc-filter-row"));
        Assert.IsTrue(first.QuerySelector(".bit-bfc-header")!.ClassList.Contains("cascaded-header"));
        Assert.AreEqual("outline: 1px solid red", first.QuerySelector(".bit-bfc-body")!.GetAttribute("style"));
        Assert.AreEqual("Now", first.QuerySelector(".bit-bfc-header-left > .bit-bfc-btn")!.TextContent.Trim());
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepItsOwnParametersOverTheCascadedOnes()
    {
        var component = RenderComponent<BitFullCalendarCascadingParamsTest>();

        var second = component.Find("#second-calendar");

        Assert.AreEqual(5, second.QuerySelectorAll(".bit-bfc-view-tab").Length);
        Assert.IsNotNull(second.QuerySelector(".bit-bfc-filter-row"));
    }

    [TestMethod]
    public void BitFullCalendarShouldShareTheCascadedSettingsObject()
    {
        var component = RenderComponent<BitFullCalendarCascadingParamsTest>();

        var calendars = component.FindComponents<BitFullCalendar>();

        Assert.AreEqual(2, calendars.Count);
        Assert.IsTrue(calendars.All(c => ReferenceEquals(c.Instance.Settings, BitFullCalendarCascadingParamsTest.SharedSettings)));
        Assert.IsTrue(calendars.All(c => c.Instance.State.Use24HourFormat is false));
    }

    #endregion

    #region Classes & Styles

    [TestMethod]
    public void BitFullCalendarShouldApplyClassesAndStylesToEveryPart()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Week);
            parameters.Add(p => p.Classes, new BitFullCalendarClassStyles { Root = "c-root", Header = "c-header", Body = "c-body", Event = "c-event", Dialog = "c-dialog" });
            parameters.Add(p => p.Styles, new BitFullCalendarClassStyles { Root = "color: red", Header = "color: green", Body = "color: blue", Event = "color: pink", Dialog = "color: gray" });
        });

        var root = component.Find(".bit-bfc");
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");

        Assert.IsTrue(component.Find(".bit-bfc-header").ClassList.Contains("c-header"));
        Assert.AreEqual("color: green", component.Find(".bit-bfc-header").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-bfc-body").ClassList.Contains("c-body"));

        var block = component.Find(".bit-bfc-event-block");
        Assert.IsTrue(block.ClassList.Contains("c-event"));
        StringAssert.Contains(block.GetAttribute("style"), "color: pink");

        block.Click();

        var dialog = component.Find(".bit-bfc-dialog");
        Assert.IsTrue(dialog.ClassList.Contains("c-dialog"));
        Assert.AreEqual("color: gray", dialog.GetAttribute("style"));
    }

    #endregion

    #region Root

    [TestMethod]
    public void BitFullCalendarShouldBeARegionOnlyWhenItIsNamed()
    {
        var unnamed = RenderComponent<BitFullCalendar>(parameters => parameters.Add(p => p.Events, Events()));
        Assert.IsNull(unnamed.Find(".bit-bfc").GetAttribute("role"));

        var named = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.AriaLabel, "Team schedule");
        });
        var root = named.Find(".bit-bfc");
        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Team schedule", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFullCalendarShouldShowTheLoadingBarAndMarkTheBodyBusyWhileLoading()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.IsLoading, true);
        });

        Assert.AreEqual("true", component.Find(".bit-bfc-body").GetAttribute("aria-busy"));
        Assert.AreEqual(1, component.FindAll(".bit-bfc-loading").Count);

        component.Render(parameters => parameters.Add(p => p.IsLoading, false));

        Assert.IsNull(component.Find(".bit-bfc-body").GetAttribute("aria-busy"));
        Assert.AreEqual(0, component.FindAll(".bit-bfc-loading").Count);
    }

    #endregion

    #region Tabs

    [TestMethod]
    public void BitFullCalendarShouldLabelTheViewTabsAndTheirPanel()
    {
        var component = RenderComponent<BitFullCalendar>(parameters => parameters.Add(p => p.Events, Events()));

        var tablist = component.Find(".bit-bfc-view-tabs");
        Assert.AreEqual("tablist", tablist.GetAttribute("role"));
        Assert.AreEqual("Views", tablist.GetAttribute("aria-label"));

        var body = component.Find(".bit-bfc-body");
        var active = component.Find(".bit-bfc-view-tab[aria-selected='true']");

        Assert.AreEqual("tabpanel", body.GetAttribute("role"));
        Assert.AreEqual(body.Id, active.GetAttribute("aria-controls"));
        Assert.AreEqual(active.Id, body.GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitFullCalendarBodyShouldNotClaimToBeATabPanelWithoutTabs()
    {
        var hidden = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.HideHeader, true);
        });
        Assert.IsNull(hidden.Find(".bit-bfc-body").GetAttribute("role"));

        var single = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Views, new[] { BitFullCalendarView.Month });
        });
        Assert.IsNull(single.Find(".bit-bfc-body").GetAttribute("role"));
    }

    [TestMethod]
    public void BitFullCalendarTabArrowsShouldFollowTheRenderedDirection()
    {
        // An explicit Dir="Rtl" on a left-to-right culture: ArrowLeft is the next tab, the way it is drawn.
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.Dir, BitDir.Rtl);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Week);
        });

        Assert.IsTrue(component.Instance.State.IsRtl);

        component.Find(".bit-bfc-view-tab[aria-selected='true']").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

        Assert.AreEqual(BitFullCalendarView.Month, component.Instance.State.View);
    }

    #endregion

    #region Settings panel

    [TestMethod]
    public void BitFullCalendarSettingsShouldBeADialogOfSwitchesAndARadioGroup()
    {
        var component = RenderComponent<BitFullCalendar>(parameters => parameters.Add(p => p.Events, Events()));

        var gear = component.Find(".bit-bfc-dropdown > button");
        Assert.AreEqual("dialog", gear.GetAttribute("aria-haspopup"));
        Assert.AreEqual("Settings", gear.GetAttribute("aria-label"));

        gear.Click();

        var panel = component.Find(".bit-bfc-dropdown-menu");
        Assert.AreEqual("dialog", panel.GetAttribute("role"));
        Assert.IsNotNull(panel.GetAttribute("aria-labelledby"));
        Assert.AreEqual(0, component.FindAll("[role='menu']").Count);
        Assert.AreEqual(7, component.FindAll(".bit-bfc-dropdown-menu [role='switch']").Count);

        var radios = component.FindAll(".bit-bfc-dropdown-menu [role='radiogroup'] [role='radio']");
        Assert.AreEqual(2, radios.Count);
        Assert.AreEqual("true", radios[0].GetAttribute("aria-checked"));

        radios[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.AreEqual(BitFullCalendarAgendaGroupBy.Color, component.Instance.State.AgendaModeGroupBy);

        component.Find(".bit-bfc-dropdown").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, component.FindAll(".bit-bfc-dropdown-menu").Count);
    }

    #endregion

    #region Dates and events

    [TestMethod]
    public void BitFullCalendarShouldMarkTodayAsTheCurrentDate()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { NavLinks = true });
        });

        var current = component.FindAll("[aria-current='date']");

        Assert.IsTrue(current.Count > 0);
        Assert.IsTrue(current.All(e => (e.GetAttribute("aria-label") ?? "").Contains(DateTime.Today.ToString("D", component.Instance.State.Culture))));
    }

    [TestMethod]
    public void BitFullCalendarEventBlockShouldBeNamedByItsTitleAndTime()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.DayEventTemplate, (RenderFragment<BitFullCalendarEvent>)(_ => builder => builder.AddContent(0, "custom")));
        });

        var label = component.Find(".bit-bfc-event-block").GetAttribute("aria-label");

        StringAssert.StartsWith(label, "Standup, ");
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldTieItsErrorsToTheirFields()
    {
        var component = RenderComponent<BitFullCalendar>(parameters => parameters.Add(p => p.Events, Events()));

        component.Find(".bit-bfc-header-right .bit-bfc-btn-primary").Click();
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        var title = component.Find("input[id^='bfc-title-']");
        Assert.AreEqual("true", title.GetAttribute("aria-required"));
        Assert.AreEqual("true", title.GetAttribute("aria-invalid"));

        var describedBy = title.GetAttribute("aria-describedby");
        Assert.IsNotNull(describedBy);
        Assert.AreEqual("Title is required", component.Find($"#{describedBy}").TextContent.Trim());

        title.Change("Planning");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog").Count);
    }

    #endregion

    #region Templates

    [TestMethod]
    public void BitFullCalendarResourceTemplateShouldReplaceTheResourceHeader()
    {
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.Resources, new List<BitFullCalendarResource> { new() { Id = "r1", Title = "Room 1", Subtitle = "HQ" } });
            parameters.Add(p => p.DefaultMode, BitFullCalendarMode.Timeline);
            parameters.Add(p => p.ResourceTemplate, (RenderFragment<BitFullCalendarResource>)(r => builder =>
            {
                builder.OpenElement(0, "b");
                builder.AddAttribute(1, "class", "custom-resource");
                builder.AddContent(2, r.Title.ToUpperInvariant());
                builder.CloseElement();
            }));
        });

        var cells = component.FindAll(".bit-bfc-tl-resource-cell");

        Assert.AreEqual("ROOM 1", cells[0].QuerySelector(".custom-resource")!.TextContent);
        Assert.IsNull(cells[0].QuerySelector(".bit-bfc-tl-resource-title"));
        // The unassigned row keeps its built-in label.
        Assert.IsNotNull(cells[^1].QuerySelector(".bit-bfc-tl-resource-title"));
    }

    #endregion

    #region OnChanging

    private IRenderedComponent<BitFullCalendar> RenderVetoing(List<BitFullCalendarChangingEventArgs> asked, List<BitFullCalendarChangeEventArgs> changes, bool cancel, BitFullCalendarView view = BitFullCalendarView.Day)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultView, view);
            parameters.Add(p => p.OnChanging, EventCallback.Factory.Create<BitFullCalendarChangingEventArgs>(this, args =>
            {
                asked.Add(args);
                args.Cancel = cancel;
            }));
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
        });
    }

    [TestMethod]
    public void BitFullCalendarShouldPutAnEventBackWhenOnChangingCancelsAMove()
    {
        var asked = new List<BitFullCalendarChangingEventArgs>();
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderVetoing(asked, changes, cancel: true);

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(1, asked.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, asked[0].Kind);
        Assert.AreEqual(DateTime.Today.AddHours(9), asked[0].OldEvent!.StartDate);
        Assert.AreNotEqual(asked[0].OldEvent!.StartDate, asked[0].Event.StartDate);
        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(DateTime.Today.AddHours(9), component.Instance.State.AllEvents.Single().StartDate);
    }

    [TestMethod]
    public void BitFullCalendarShouldCommitAChangeOnChangingAllows()
    {
        var asked = new List<BitFullCalendarChangingEventArgs>();
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderVetoing(asked, changes, cancel: false);

        component.Find(".bit-bfc-event-block").KeyDown(new KeyboardEventArgs { Key = "ArrowDown", AltKey = true });

        Assert.AreEqual(1, asked.Count);
        Assert.AreEqual(1, changes.Count);
        Assert.AreNotEqual(DateTime.Today.AddHours(9), component.Instance.State.AllEvents.Single().StartDate);
    }

    [TestMethod]
    public void BitFullCalendarAddDialogShouldStayOpenWhenOnChangingCancels()
    {
        var asked = new List<BitFullCalendarChangingEventArgs>();
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderVetoing(asked, changes, cancel: true, view: BitFullCalendarView.Month);

        component.Find(".bit-bfc-header-right .bit-bfc-btn-primary").Click();
        component.Find("input[id^='bfc-title-']").Change("Vetoed");
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

        Assert.AreEqual(1, asked.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Add, asked[0].Kind);
        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count);
        Assert.AreEqual("Vetoed", component.Find("input[id^='bfc-title-']").GetAttribute("value"));
        Assert.AreEqual(1, component.Instance.State.AllEvents.Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepAnEventWhoseDeleteOnChangingCancels()
    {
        var asked = new List<BitFullCalendarChangingEventArgs>();
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderVetoing(asked, changes, cancel: true, view: BitFullCalendarView.Month);

        component.Find(".bit-bfc-event-badge").Click();
        component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();

        Assert.AreEqual(1, asked.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Delete, asked[0].Kind);
        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(1, component.Instance.State.AllEvents.Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count);
    }

    #endregion
}
