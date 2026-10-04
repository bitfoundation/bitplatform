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
/// Covers the event colors an event can carry of its own, the month-cell and agenda templates, and the keyboard
/// contract of the month grid, the year view, the date-time picker and the multi-day events: one tab stop per grid,
/// paging by month, arrows that carry on past the grid's edge, and one stop per run of a multi-day event.
/// </summary>
[TestClass]
public class BitFullCalendarKeyboardAndTemplateTests : BunitTestContext
{
    private static readonly DateTime Anchor = new(2026, 3, 18);

    private static List<BitFullCalendarEvent> Events() =>
    [
        new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) },
        new() { Id = "2", Title = "Review", StartDate = Anchor.AddDays(2).AddHours(14), EndDate = Anchor.AddDays(2).AddHours(15) },
    ];

    private IRenderedComponent<BitFullCalendar> RenderCalendar(Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null,
                                                               List<BitFullCalendarEvent>? events = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, events ?? Events());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            configure?.Invoke(parameters);
        });
    }

    #region Event colors

    [TestMethod]
    public void BitFullCalendarColorSchemeShouldResolveOptionsRawColorsAndTheDefault()
    {
        var scheme = new BitFullCalendarColorScheme([new() { Id = "work", Title = "Work", Value = "#2563eb" }]);

        Assert.AreEqual("#2563eb", scheme.GetCssValue("work"));
        Assert.AreEqual("#db2777", scheme.GetCssValue("#db2777"));
        Assert.AreEqual("oklch(0.7 0.1 200)", scheme.GetCssValue("oklch(0.7 0.1 200)"));
        Assert.AreEqual("rgb(0 0 0 / 50%)", scheme.GetCssValue("rgb(0 0 0 / 50%)"));
        Assert.AreEqual("color-mix(in srgb, red 40%, blue)", scheme.GetCssValue("color-mix(in srgb, red 40%, blue)"));
        Assert.AreEqual("var(--brand-color, #333)", scheme.GetCssValue("var(--brand-color, #333)"));
        Assert.AreEqual(BitFullCalendarColorScheme.DefaultCssValue, scheme.GetCssValue("legacy"));
    }

    [TestMethod]
    [DataRow("#12")]
    [DataRow("#12345g")]
    [DataRow("red;background:url(x)")]
    [DataRow("rgb(0,0,0);color:red")]
    [DataRow("url(evil)")]
    [DataRow("expression(alert(1))")]
    [DataRow("rgb(0 0 0) url(https://example.com/t.png)")]
    [DataRow("var(--x, url(a))")]
    [DataRow("rgb(1)/*)")]
    [DataRow("rgb(1 2 3")]
    [DataRow("color-mix(in srgb, red, image(x))")]
    [DataRow("var(--x, linear-gradient(red, blue))")]
    [DataRow("var(--x, -moz-element(#id))")]
    public void BitFullCalendarColorSchemeShouldRefuseValuesThatAreNotSafeColors(string value)
    {
        var scheme = new BitFullCalendarColorScheme(null);

        Assert.AreEqual(BitFullCalendarColorScheme.DefaultCssValue, scheme.GetCssValue(value));
    }

    [TestMethod]
    public void BitFullCalendarColorSchemeShouldDrawTheDefaultIdInTheFirstSwatchOfAPaletteWithoutIt()
    {
        var scheme = new BitFullCalendarColorScheme([new() { Id = "work", Title = "Work", Value = "#2563eb" }]);

        // An event that never set a color carries "blue"; a custom palette without it keeps drawing it in its first swatch.
        Assert.AreEqual("#2563eb", scheme.GetCssValue(new BitFullCalendarEvent().Color));
        Assert.AreEqual("Work", scheme.GetLabel(new BitFullCalendarEvent().Color));
        Assert.AreEqual("work", scheme.GetCanonicalId(new BitFullCalendarEvent().Color));
        Assert.AreSame(scheme.Options, scheme.GetEditorOptions(new BitFullCalendarEvent().Color));
    }

    [TestMethod]
    public void BitFullCalendarAgendaShouldGroupADefaultColoredEventWithTheSwatchItIsDrawnIn()
    {
        var component = RenderCalendar(p =>
        {
            p.Add(c => c.DefaultView, BitFullCalendarView.Agenda);
            p.Add(c => c.EventColorOptions, [new BitFullCalendarColorOption { Id = "teal", Title = "Teal", Value = "#0d9488" }]);
            p.Add(c => c.Settings, new BitFullCalendarSettings { AgendaModeGroupBy = BitFullCalendarAgendaGroupBy.Color });
        },
        [
            new() { Id = "1", Title = "Default", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) },
            new() { Id = "2", Title = "Teal", StartDate = Anchor.AddHours(11), EndDate = Anchor.AddHours(12), Color = "teal" },
        ]);

        var groups = component.FindAll(".bit-bfc-agenda-group");
        Assert.AreEqual(1, groups.Count);
        Assert.AreEqual(2, groups[0].QuerySelectorAll(".bit-bfc-agenda-item").Length);
    }

    [TestMethod]
    public void BitFullCalendarShouldDrawAnUnknownColorInTheDefaultEventColor()
    {
        var component = RenderCalendar(events:
        [
            new() { Id = "1", Title = "Imported", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10), Color = "legacy" },
            new() { Id = "2", Title = "Branded", StartDate = Anchor.AddHours(11), EndDate = Anchor.AddHours(12), Color = "#db2777" },
        ]);

        var badges = component.FindAll(".bit-bfc-event-badge");
        StringAssert.Contains(badges.Single(b => b.GetAttribute("data-bit-bfc-event") == "1").GetAttribute("style"), "--bit-bfc-evt-color:var(--bit-bfc-event-default);");
        StringAssert.Contains(badges.Single(b => b.GetAttribute("data-bit-bfc-event") == "2").GetAttribute("style"), "--bit-bfc-evt-color:#db2777;");
    }

    #endregion

    #region Templates

    [TestMethod]
    public void BitFullCalendarShouldRenderTheMonthCellTemplateInEveryShownDay()
    {
        RenderFragment<BitFullCalendarCell> template = cell => builder =>
        {
            if (cell.Date == Anchor)
            {
                builder.AddContent(0, "Holiday");
            }
        };

        var component = RenderCalendar(p => p.Add(c => c.MonthCellTemplate, template));

        var contents = component.FindAll(".bit-bfc-month-cell-content");
        Assert.IsTrue(contents.Count >= 28);
        Assert.AreEqual(1, contents.Count(c => c.TextContent == "Holiday"));
    }

    [TestMethod]
    public void BitFullCalendarShouldRenderTheAgendaEventTemplateInsideTheRowButton()
    {
        RenderFragment<BitFullCalendarEvent> template = ev => builder => builder.AddContent(0, $"Row: {ev.Title}");

        var component = RenderCalendar(p =>
        {
            p.Add(c => c.DefaultView, BitFullCalendarView.Agenda);
            p.Add(c => c.AgendaEventTemplate, template);
        });

        var rows = component.FindAll("button.bit-bfc-agenda-item");
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("Row: Standup", rows[0].TextContent.Trim());
        Assert.IsNull(rows[0].QuerySelector(".bit-bfc-agenda-avatar"));
    }

    [TestMethod]
    public void BitFullCalendarShouldTakeTheNewTemplatesFromBitParams()
    {
        RenderFragment<BitFullCalendarCell> cellTemplate = _ => builder => builder.AddContent(0, "x");
        var calendarParams = new BitFullCalendarParams { MonthCellTemplate = cellTemplate };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new IBitComponentParams[] { calendarParams });
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitFullCalendar>(0);
                builder.AddComponentParameter(1, nameof(BitFullCalendar.DefaultDate), (DateTime?)Anchor);
                builder.CloseComponent();
            }));
        });

        Assert.IsTrue(component.FindAll(".bit-bfc-month-cell-content").Count >= 28);
    }

    #endregion

    #region Month grid keyboard

    private static void PressOnRovingCell(IRenderedComponent<BitFullCalendar> component, string key, bool shift = false)
        => component.Find(".bit-bfc-cell-add-hint-month[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

    [TestMethod]
    public void BitFullCalendarMonthGridShouldBeOneTabStop()
    {
        var component = RenderCalendar();

        Assert.AreEqual(1, component.FindAll(".bit-bfc-cell-add-hint-month[tabindex='0']").Count);
    }

    [TestMethod]
    public void BitFullCalendarMonthGridShouldPageByMonthAndByYearWithShift()
    {
        var component = RenderCalendar();

        PressOnRovingCell(component, "PageDown");
        Assert.AreEqual(4, component.Instance.State.SelectedDate.Month);

        PressOnRovingCell(component, "PageUp", shift: true);
        Assert.AreEqual(2025, component.Instance.State.SelectedDate.Year);
    }

    [TestMethod]
    public void BitFullCalendarMonthGridArrowShouldCarryOnIntoTheNextMonth()
    {
        var component = RenderCalendar(p => p.Add(c => c.Settings, new BitFullCalendarSettings { ShowNonCurrentDates = false }));

        // End of the month: the next day is a blanked April cell, so the arrow carries on into April.
        PressOnRovingCell(component, "End");
        for (var i = 0; i < 6; i++)
        {
            PressOnRovingCell(component, "ArrowDown");
        }

        Assert.AreEqual(4, component.Instance.State.SelectedDate.Month);
    }

    [TestMethod]
    public void BitFullCalendarMonthGridShouldNotPagePastTheMaxDate()
    {
        var component = RenderCalendar(p => p.Add(c => c.MaxDate, new DateTime(2026, 3, 31)));

        PressOnRovingCell(component, "PageDown");

        Assert.AreEqual(3, component.Instance.State.SelectedDate.Month);
    }

    [TestMethod]
    public void BitFullCalendarMultiDayEventShouldBeOneTabStopPerWeekRow()
    {
        // Monday 16 March to Wednesday 25 March 2026: two week rows of the en-US grid (Sunday first).
        var component = RenderCalendar(events:
        [
            new() { Id = "trip", Title = "Trip", StartDate = new DateTime(2026, 3, 16), EndDate = new DateTime(2026, 3, 25, 18, 0, 0) },
        ]);

        var segments = component.FindAll("[data-bit-bfc-event='trip']");
        var stops = segments.Where(s => s.GetAttribute("tabindex") == "0").ToList();

        Assert.AreEqual(10, segments.Count);
        Assert.AreEqual(2, stops.Count);
        Assert.IsTrue(segments.Except(stops).All(s => s.GetAttribute("aria-hidden") == "true" && s.GetAttribute("role") is null));
    }

    [TestMethod]
    public void BitFullCalendarMultiDayEventStartingOnAHiddenDayShouldStillHaveAStop()
    {
        // Wednesday 18 to Thursday 19 March 2026 with Wednesday hidden: Thursday's segment is the only one drawn.
        var component = RenderCalendar(p => p.Add(c => c.Settings, new BitFullCalendarSettings { HiddenDays = [DayOfWeek.Wednesday] }),
        [
            new() { Id = "offsite", Title = "Offsite", StartDate = new DateTime(2026, 3, 18), EndDate = new DateTime(2026, 3, 19, 18, 0, 0) },
        ]);

        var segments = component.FindAll("[data-bit-bfc-event='offsite']");

        Assert.AreEqual(1, segments.Count);
        Assert.AreEqual("0", segments[0].GetAttribute("tabindex"));
    }

    #endregion

    #region Year view

    [TestMethod]
    public void BitFullCalendarYearViewShouldBeOneTabStopTheArrowKeysWalk()
    {
        var component = RenderCalendar(p => p.Add(c => c.DefaultView, BitFullCalendarView.Year));

        var days = component.FindAll(".bit-bfc-year-day.has-events");
        Assert.AreEqual(2, days.Count);
        Assert.AreEqual(1, days.Count(d => d.GetAttribute("tabindex") == "0"));

        days.Single(d => d.GetAttribute("tabindex") == "0").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        var focused = component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.GetAttribute("tabindex") == "0");
        StringAssert.EndsWith(focused.Id, "20260320");
    }

    [TestMethod]
    public void BitFullCalendarYearViewEventListShouldFollowTheEvents()
    {
        var events = Events();
        var component = RenderCalendar(p => p.Add(c => c.DefaultView, BitFullCalendarView.Year), events);

        component.FindAll(".bit-bfc-year-day.has-events")[0].Click();
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog .bit-bfc-agenda-item").Count);

        events.RemoveAt(0);
        component.Render(p => p.Add(c => c.Events, events));

        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog .bit-bfc-agenda-item").Count);
    }

    #endregion

    #region Timeline bounds

    [TestMethod]
    public void BitFullCalendarTimelineShouldNotOfferAnAddOutsideTheDateWindow()
    {
        BitFullCalendarEvent? draft = null;
        var component = RenderCalendar(p =>
        {
            p.Add(c => c.Resources, [new BitFullCalendarResource { Id = "r1", Title = "Room 1" }]);
            p.Add(c => c.DefaultMode, BitFullCalendarMode.Timeline);
            p.Add(c => c.DefaultView, BitFullCalendarView.Month);
            p.Add(c => c.MinDate, Anchor);
            p.Add(c => c.OnAddClick, (BitFullCalendarEvent? e) => draft = e);
        });

        var outside = component.FindAll(".bit-bfc-tl-cell-day.bit-bfc-out-of-range");
        Assert.IsTrue(outside.Count > 0);
        Assert.AreEqual("true", outside[0].GetAttribute("aria-disabled"));

        outside[0].Click();
        Assert.IsNull(draft);

        component.FindAll(".bit-bfc-tl-cell-day").First(c => c.ClassList.Contains("bit-bfc-out-of-range") is false).Click();
        Assert.IsNotNull(draft);
    }

    #endregion

    #region Date-time picker

    [TestMethod]
    public void BitFullCalendarDateTimePickerShouldBeADialogWithOneDayTabStop()
    {
        var component = RenderCalendar();

        component.Find(".bit-bfc-header-right .bit-bfc-btn-primary").Click();
        var trigger = component.FindAll(".bit-bfc-dtp-trigger")[0];
        trigger.Click();

        var panel = component.Find(".bit-bfc-dtp-panel");
        Assert.AreEqual("dialog", panel.GetAttribute("role"));
        Assert.IsFalse(string.IsNullOrEmpty(panel.GetAttribute("aria-label")));
        Assert.AreEqual(panel.Id, component.FindAll(".bit-bfc-dtp-trigger")[0].GetAttribute("aria-controls"));
        Assert.AreEqual(1, panel.QuerySelectorAll(".bit-bfc-dtp-day[tabindex='0']").Length);

        component.Find(".bit-bfc-dtp-day[tabindex='0']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });

        var label = component.Find(".bit-bfc-dtp-month-label").TextContent;
        StringAssert.Contains(label, "April");
    }

    #endregion

    #region Add dialog overlay

    [TestMethod]
    public void BitFullCalendarAddDialogShouldCloseOnlyForAPressThatBeganOnTheOverlay()
    {
        var component = RenderCalendar();

        component.Find(".bit-bfc-header-right .bit-bfc-btn-primary").Click();

        // A text selection dragged out of a field and released over the overlay: a click without its press.
        component.Find(".bit-bfc-overlay").Click();
        Assert.AreEqual(1, component.FindAll(".bit-bfc-overlay").Count);

        component.Find(".bit-bfc-overlay").MouseDown();
        component.Find(".bit-bfc-overlay").Click();
        Assert.AreEqual(0, component.FindAll(".bit-bfc-overlay").Count);
    }

    #endregion

    #region Helpers

    [TestMethod]
    [DataRow("en-US", "3/18")]
    [DataRow("en-GB", "18/03")]
    [DataRow("de-DE", "18.03")]
    [DataRow("ja-JP", "03/18")]
    public void BitFullCalendarFormatShortMonthDayShouldFollowTheCultureOrder(string culture, string expected)
    {
        Assert.AreEqual(expected, BitFullCalendarHelpers.FormatShortMonthDay(Anchor, new CultureInfo(culture)));
    }

    #endregion
}
