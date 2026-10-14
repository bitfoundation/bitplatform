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
/// Covers <see cref="BitFcYearView"/> through the calendar that renders it: the twelve month grids and their headers,
/// which days are marked busy and how they are named, the bullets, the drill-down of a month title, the event list a
/// busy day opens, and the roving tab stop the keys walk across the busy days of the year.
/// </summary>
[TestClass]
public class BitFullCalendarYearViewTests : BunitTestContext
{
    // A Wednesday; the calendar's clock is pinned to it, so it is "today" in every test.
    private static readonly DateTime Anchor = new(2026, 3, 18);

    private static BitFullCalendarEvent Timed(string id, DateTime day, int hour = 9, string? color = null)
        => new() { Id = id, Title = id, StartDate = day.AddHours(hour), EndDate = day.AddHours(hour + 1), Color = color };

    /// <summary>Busy days spread over the year: Jan 5, Jan 20, Mar 18 (today), Mar 19, Mar 30 and May 2.</summary>
    private static List<BitFullCalendarEvent> SpreadEvents() =>
    [
        Timed("jan5", new DateTime(2026, 1, 5)),
        Timed("jan20", new DateTime(2026, 1, 20)),
        Timed("mar18", Anchor),
        Timed("mar19", new DateTime(2026, 3, 19)),
        Timed("mar30", new DateTime(2026, 3, 30)),
        Timed("may2", new DateTime(2026, 5, 2)),
    ];

    private IRenderedComponent<BitFullCalendar> RenderYear(List<BitFullCalendarEvent>? events = null,
                                                           Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, events ?? SpreadEvents());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Year);
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(11)));
            configure?.Invoke(parameters);
        });
    }

    private static string RovingDay(IRenderedComponent<BitFullCalendar> component)
        => component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.GetAttribute("tabindex") == "0").Id[^8..];

    #region Months and headers

    [TestMethod]
    public void BitFullCalendarYearViewShouldRenderTheTwelveMonthsOfTheSelectedYear()
    {
        var component = RenderYear();

        var titles = component.FindAll(".bit-bfc-year-month-title").Select(t => t.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(
            new[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" },
            titles);

        // Every title is a native button, so a month is reachable from the keyboard.
        Assert.IsTrue(component.FindAll(".bit-bfc-year-month-title").All(t => t.TagName == "BUTTON" && t.GetAttribute("type") == "button"));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldRenderTheWeekdayHeadersOfEachMonth()
    {
        var component = RenderYear();

        var headers = component.FindAll(".bit-bfc-year-weekdays");
        Assert.AreEqual(12, headers.Count);
        Assert.IsTrue(headers.All(h => h.GetAttribute("aria-hidden") == "true"), "the busy days name their own dates");

        CollectionAssert.AreEqual(
            new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" },
            headers[0].QuerySelectorAll(".bit-bfc-year-weekday").Select(d => d.TextContent.Trim()).ToArray());
        StringAssert.Contains(headers[0].GetAttribute("style"), "repeat(7, minmax(0, 1fr))");
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldStartTheWeekOnTheConfiguredDay()
    {
        var component = RenderYear(configure: p => p.Add(c => c.Settings, new BitFullCalendarSettings { FirstDayOfWeek = DayOfWeek.Monday }));

        var first = component.FindAll(".bit-bfc-year-weekdays")[0].QuerySelectorAll(".bit-bfc-year-weekday");
        Assert.AreEqual("Mo", first[0].TextContent.Trim());
        Assert.AreEqual("Su", first[^1].TextContent.Trim());
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldDropTheHiddenDays()
    {
        var component = RenderYear(configure: p => p.Add(c => c.Settings, new BitFullCalendarSettings { HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday] }));

        var headers = component.FindAll(".bit-bfc-year-weekdays")[0];
        CollectionAssert.AreEqual(
            new[] { "Mo", "Tu", "We", "Th", "Fr" },
            headers.QuerySelectorAll(".bit-bfc-year-weekday").Select(d => d.TextContent.Trim()).ToArray());
        StringAssert.Contains(headers.GetAttribute("style"), "repeat(5, minmax(0, 1fr))");
        StringAssert.Contains(component.FindAll(".bit-bfc-year-days")[0].GetAttribute("style"), "repeat(5, minmax(0, 1fr))");

        // May 2, 2026 is a Saturday, so it is not drawn at all.
        Assert.IsFalse(component.FindAll(".bit-bfc-year-day.has-events").Any(d => d.Id.EndsWith("20260502")));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldLayTheYearOutInTheCalendarOfTheDateTimeFormat()
    {
        // DateTimeFormat.Calendar is the documented way of switching the calendar of a culture; CultureInfo.Calendar
        // stays the culture's default (Um Al Qura for ar-SA), so the year has to be read off the former: the twelve
        // Gregorian months of 2026, named by the culture, rather than the Hijri year the anchor falls in.
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ar-SA").Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();

        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, SpreadEvents());
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Year);
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(11)));
        });

        Assert.AreEqual("2026", component.Find(".bit-bfc-header-title").TextContent.Trim());
        CollectionAssert.AreEqual(
            Enumerable.Range(1, 12).Select(m => culture.DateTimeFormat.GetMonthName(m)).ToArray(),
            component.FindAll(".bit-bfc-year-month-title").Select(t => t.TextContent.Trim()).ToArray());
        // March is the third grid, and holds today.
        Assert.IsNotNull(component.FindAll(".bit-bfc-year-month")[2].QuerySelector(".bit-bfc-year-day.has-events[aria-current='date']"));
        Assert.AreEqual(31, component.FindAll(".bit-bfc-year-month")[2].QuerySelectorAll(".bit-bfc-year-day:not(.other-month)").Length);
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldHideTheDaysOfTheNeighbouringMonths()
    {
        var component = RenderYear();

        // March 2026 starts on a Sunday and ends on a Tuesday, so its grid is padded with April 1 - 4 only.
        var march = component.FindAll(".bit-bfc-year-month")[2];
        var others = march.QuerySelectorAll(".bit-bfc-year-day.other-month");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4" }, others.Select(d => d.TextContent.Trim()).ToArray());
        Assert.IsTrue(others.All(d => d.GetAttribute("aria-hidden") == "true"));
        Assert.IsTrue(others.All(d => d.TagName == "DIV"));

        // A quiet day of the month itself is plain text, read in the context of its grid.
        var quiet = march.QuerySelectorAll(".bit-bfc-year-day:not(.has-events):not(.other-month)");
        Assert.AreEqual(31 - 3, quiet.Length);
        Assert.IsTrue(quiet.All(d => d.HasAttribute("aria-hidden") is false));
    }

    #endregion

    #region Busy days

    [TestMethod]
    public void BitFullCalendarYearViewShouldMarkEveryBusyDayOnceAsAButton()
    {
        var component = RenderYear();

        var days = component.FindAll(".bit-bfc-year-day.has-events");
        CollectionAssert.AreEqual(
            new[] { "20260105", "20260120", "20260318", "20260319", "20260330", "20260502" },
            days.Select(d => d.Id[^8..]).ToArray());
        Assert.IsTrue(days.All(d => d.TagName == "BUTTON" && d.GetAttribute("type") == "button"));
        Assert.IsTrue(days.All(d => d.HasAttribute("data-bit-bfc-roving")));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldNameABusyDayByItsDateAndEventCount()
    {
        var events = SpreadEvents();
        events.Add(Timed("mar18b", Anchor, hour: 14));
        var component = RenderYear(events);

        var today = component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260318"));
        Assert.AreEqual("Wednesday, March 18, 2026, 2 event(s)", today.GetAttribute("aria-label"));
        Assert.AreEqual("date", today.GetAttribute("aria-current"));
        Assert.IsTrue(today.QuerySelector(".bit-bfc-year-day-number")!.ClassList.Contains("today"));

        var other = component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260319"));
        Assert.AreEqual("Thursday, March 19, 2026, 1 event(s)", other.GetAttribute("aria-label"));
        Assert.IsNull(other.GetAttribute("aria-current"));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldFormatTheCountThroughTheTexts()
    {
        var component = RenderYear(configure: p => p.Add(c => c.Texts, new BitFullCalendarTexts { EventListCountFormat = "{0} meetings" }));

        var today = component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260318"));
        Assert.AreEqual("Wednesday, March 18, 2026, 1 meetings", today.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldDrawAtMostThreeBullets()
    {
        var events = Enumerable.Range(0, 5).Select(i => Timed($"e{i}", Anchor, hour: 8 + i)).ToList();
        var component = RenderYear(events);

        var day = component.Find(".bit-bfc-year-day.has-events");
        Assert.AreEqual(3, day.QuerySelectorAll(".bit-bfc-year-day-bullets .bit-bfc-bullet").Length);
        Assert.AreEqual("true", day.QuerySelector(".bit-bfc-year-day-bullets")!.GetAttribute("aria-hidden"));

        // The name still counts every event of the day.
        StringAssert.EndsWith(day.GetAttribute("aria-label"), "5 event(s)");
    }

    [TestMethod]
    public void BitFullCalendarYearViewBulletsShouldBeDrawnInTheirEventsColors()
    {
        var component = RenderYear(
        [
            Timed("brand", Anchor, color: "#2563eb"),
            Timed("work", Anchor, hour: 11, color: "green"),
        ]);

        var bullets = component.FindAll(".bit-bfc-year-day.has-events .bit-bfc-bullet");
        Assert.AreEqual(2, bullets.Count);
        StringAssert.Contains(bullets[0].GetAttribute("style"), "--bit-bfc-evt-color:#2563eb;");
        Assert.AreNotEqual(bullets[0].GetAttribute("style"), bullets[1].GetAttribute("style"));
        Assert.IsFalse(bullets[1].GetAttribute("style")!.Contains(BitFullCalendarColorScheme.DefaultCssValue));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldMarkEveryDayAMultiDayEventSpans()
    {
        var component = RenderYear(
        [
            new() { Id = "trip", Title = "Trip", StartDate = new DateTime(2026, 6, 8, 9, 0, 0), EndDate = new DateTime(2026, 6, 10, 17, 0, 0) },
        ]);

        CollectionAssert.AreEqual(
            new[] { "20260608", "20260609", "20260610" },
            component.FindAll(".bit-bfc-year-day.has-events").Select(d => d.Id[^8..]).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldNotMarkTheDayAMidnightEndFallsOn()
    {
        var component = RenderYear(
        [
            new() { Id = "offsite", Title = "Offsite", StartDate = new DateTime(2026, 6, 8), EndDate = new DateTime(2026, 6, 10) },
        ]);

        // A 00:00 end is exclusive, so June 10 itself is free.
        CollectionAssert.AreEqual(
            new[] { "20260608", "20260609" },
            component.FindAll(".bit-bfc-year-day.has-events").Select(d => d.Id[^8..]).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldLeaveOutTheEventsOfOtherYears()
    {
        var component = RenderYear(
        [
            Timed("last-year", new DateTime(2025, 12, 31)),
            Timed("next-year", new DateTime(2027, 1, 1)),
            Timed("this-year", new DateTime(2026, 7, 1)),
        ]);

        // Dec 31, 2025 and Jan 1, 2027 are drawn as padding of January and December, never as busy days.
        CollectionAssert.AreEqual(new[] { "20260701" }, component.FindAll(".bit-bfc-year-day.has-events").Select(d => d.Id[^8..]).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldRenderNoBusyDayForAnEmptyYear()
    {
        var component = RenderYear([]);

        Assert.AreEqual(12, component.FindAll(".bit-bfc-year-month").Count);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-year-day.has-events").Count);
        Assert.AreEqual(0, component.FindAll("[data-bit-bfc-roving]").Count);
    }

    #endregion

    #region Event list

    [TestMethod]
    public void BitFullCalendarYearViewShouldListTheEventsOfAClickedDay()
    {
        var events = SpreadEvents();
        events.Add(Timed("mar18b", Anchor, hour: 14));
        var component = RenderYear(events);

        component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260318")).Click();

        var dialog = component.Find(".bit-bfc-dialog");
        Assert.AreEqual("dialog", dialog.GetAttribute("role"));
        StringAssert.Contains(dialog.QuerySelector("h3")!.TextContent, "Wednesday, March 18, 2026");
        CollectionAssert.AreEqual(
            new[] { "mar18", "mar18b" },
            dialog.QuerySelectorAll(".bit-bfc-agenda-item").Select(i => i.GetAttribute("data-bit-bfc-event")).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldCloseTheEventListFromTheOverlay()
    {
        var component = RenderYear();
        component.FindAll(".bit-bfc-year-day.has-events")[0].Click();
        Assert.AreEqual(1, component.FindAll(".bit-bfc-dialog").Count);

        component.Find(".bit-bfc-overlay").Click();

        Assert.AreEqual(0, component.FindAll(".bit-bfc-dialog").Count);
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldMoveTheTabStopToTheClickedDay()
    {
        var component = RenderYear();
        Assert.AreEqual("20260318", RovingDay(component));

        component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260502")).Click();

        Assert.AreEqual("20260502", RovingDay(component));
    }

    #endregion

    #region Month drill-down

    [TestMethod]
    public void BitFullCalendarYearViewShouldOpenAMonthFromItsTitle()
    {
        BitFullCalendarView? changedTo = null;
        var component = RenderYear(configure: p => p.Add(c => c.OnViewChange, (BitFullCalendarView v) => changedTo = v));

        component.FindAll(".bit-bfc-year-month-title")[4].Click();

        Assert.AreEqual(BitFullCalendarView.Month, component.Instance.State.View);
        Assert.AreEqual(BitFullCalendarView.Month, changedTo);
        Assert.AreEqual(new DateTime(2026, 5, 1), component.Instance.State.SelectedDate.Date);
        Assert.AreEqual(0, component.FindAll(".bit-bfc-year").Count);
        Assert.IsTrue(component.FindAll(".bit-bfc-month-cell").Count > 0);
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldOpenAClippedMonthOnItsFirstAllowedDay()
    {
        var component = RenderYear(configure: p => p.Add(c => c.MinDate, new DateTime(2026, 4, 10)));

        component.FindAll(".bit-bfc-year-month-title")[3].Click();

        Assert.AreEqual(BitFullCalendarView.Month, component.Instance.State.View);
        Assert.AreEqual(new DateTime(2026, 4, 10), component.Instance.State.SelectedDate.Date);
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldStayPutOnAMonthTheBoundsPutOutOfReach()
    {
        var component = RenderYear(configure: p =>
        {
            p.Add(c => c.MinDate, new DateTime(2026, 1, 1));
            p.Add(c => c.MaxDate, new DateTime(2026, 6, 15));
        });

        component.FindAll(".bit-bfc-year-month-title")[8].Click();

        Assert.AreEqual(BitFullCalendarView.Year, component.Instance.State.View);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-year").Count);
    }

    #endregion

    #region Keyboard

    [TestMethod]
    public void BitFullCalendarYearViewShouldPutTheTabStopOnTheFirstBusyDayWhenTodayIsFree()
    {
        var component = RenderYear(
        [
            Timed("feb", new DateTime(2026, 2, 3)),
            Timed("aug", new DateTime(2026, 8, 3)),
        ]);

        Assert.AreEqual("20260203", RovingDay(component));
        Assert.AreEqual(1, component.FindAll(".bit-bfc-year-day.has-events").Count(d => d.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("-1", component.FindAll(".bit-bfc-year-day.has-events")[1].GetAttribute("tabindex"));
    }

    [TestMethod]
    [DataRow("ArrowRight", "20260319")]
    [DataRow("ArrowLeft", "20260120")]
    [DataRow("ArrowDown", "20260330")]
    [DataRow("ArrowUp", "20260120")]
    [DataRow("PageDown", "20260502")]
    [DataRow("PageUp", "20260105")]
    [DataRow("Home", "20260105")]
    [DataRow("End", "20260502")]
    [DataRow("Enter", "20260318")]
    [DataRow("a", "20260318")]
    public void BitFullCalendarYearViewKeysShouldWalkTheBusyDays(string key, string expected)
    {
        var component = RenderYear();

        component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.GetAttribute("tabindex") == "0")
                 .KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expected, RovingDay(component));
    }

    [TestMethod]
    [DataRow("ArrowRight", "20260120")]
    [DataRow("ArrowLeft", "20260319")]
    public void BitFullCalendarYearViewArrowsShouldFollowTheReadingDirectionInRtl(string key, string expected)
    {
        var component = RenderYear(configure: p => p.Add(c => c.Dir, BitDir.Rtl));

        component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.GetAttribute("tabindex") == "0")
                 .KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expected, RovingDay(component));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldStopAtTheEndsOfTheYear()
    {
        var component = RenderYear();
        void Press(string key) => component.FindAll(".bit-bfc-year-day.has-events")
                                           .Single(d => d.GetAttribute("tabindex") == "0")
                                           .KeyDown(new KeyboardEventArgs { Key = key });

        Press("End");
        Press("ArrowRight");
        Press("ArrowDown");
        Press("PageDown");
        Assert.AreEqual("20260502", RovingDay(component));

        Press("Home");
        Press("ArrowLeft");
        Press("ArrowUp");
        Press("PageUp");
        Assert.AreEqual("20260105", RovingDay(component));
    }

    [TestMethod]
    public void BitFullCalendarYearViewShouldMoveTheFocusWithTheTabStop()
    {
        var component = RenderYear();

        component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.GetAttribute("tabindex") == "0")
                 .KeyDown(new KeyboardEventArgs { Key = "End" });

        var target = component.FindAll(".bit-bfc-year-day.has-events").Single(d => d.Id.EndsWith("20260502"));
        var focus = Context.JSInterop.VerifyInvoke("BitBlazorUI.FullCalendar.focusElement");
        Assert.AreEqual(target.Id, focus.Arguments[^1]);
    }

    #endregion

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}
