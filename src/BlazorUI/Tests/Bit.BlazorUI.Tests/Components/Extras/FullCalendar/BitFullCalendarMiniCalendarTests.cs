using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers <see cref="BitFcMiniCalendar"/>, the month grid in the sidebar of the day view, through the calendar that
/// renders it: its title, headers and day buttons, browsing the months without moving the selected date, picking a
/// day, following a date changed elsewhere, the date bounds and the roving tab stop the keys walk.
/// </summary>
[TestClass]
public class BitFullCalendarMiniCalendarTests : BunitTestContext
{
    // A Wednesday of a month that starts on a Sunday; the calendar's clock is pinned to it.
    private static readonly DateTime Anchor = new(2026, 3, 18);

    private IRenderedComponent<BitFullCalendar> RenderDay(Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null, DateTime? date = null)
    {
        return RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, date ?? Anchor);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(11)));
            configure?.Invoke(parameters);
        });
    }

    private static string Title(IRenderedComponent<BitFullCalendar> component)
        => component.Find(".bit-bfc-mini-calendar-title").TextContent.Trim();

    private static List<AngleSharp.Dom.IElement> Days(IRenderedComponent<BitFullCalendar> component)
        => [.. component.FindAll(".bit-bfc-mini-calendar-day")];

    private static AngleSharp.Dom.IElement Day(IRenderedComponent<BitFullCalendar> component, DateTime date)
        => Days(component).Single(d => d.Id.EndsWith(date.ToString("yyyyMMdd")));

    private static string RovingDay(IRenderedComponent<BitFullCalendar> component)
        => Days(component).Single(d => d.GetAttribute("tabindex") == "0").Id[^8..];

    private static AngleSharp.Dom.IElement PrevButton(IRenderedComponent<BitFullCalendar> component)
        => component.FindAll(".bit-bfc-mini-calendar-header .bit-bfc-btn-nav")[0];

    private static AngleSharp.Dom.IElement NextButton(IRenderedComponent<BitFullCalendar> component)
        => component.FindAll(".bit-bfc-mini-calendar-header .bit-bfc-btn-nav")[1];

    #region Rendering

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldShowTheMonthOfTheSelectedDate()
    {
        var component = RenderDay();

        Assert.AreEqual(1, component.FindAll(".bit-bfc-mini-calendar").Count);
        Assert.AreEqual("March 2026", Title(component));

        var title = component.Find(".bit-bfc-mini-calendar-title");
        Assert.AreEqual("polite", title.GetAttribute("aria-live"));
        Assert.AreEqual(title.Id, component.Find(".bit-bfc-mini-calendar table").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldRenderTheWeekdayHeaders()
    {
        var component = RenderDay();

        var headers = component.FindAll(".bit-bfc-mini-calendar th");
        CollectionAssert.AreEqual(new[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" }, headers.Select(h => h.TextContent.Trim()).ToArray());
        Assert.IsTrue(headers.All(h => h.GetAttribute("scope") == "col"));
        // The abbreviation keeps the full short name a screen reader announces.
        Assert.AreEqual("Sun", headers[0].GetAttribute("abbr"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldRenderTheDaysOfTheMonthInWeekRows()
    {
        var component = RenderDay();

        // March 2026 runs Sunday to Tuesday, padded with April 1 - 4: five rows of seven.
        Assert.AreEqual(5, component.FindAll(".bit-bfc-mini-calendar tbody tr").Count);
        var days = Days(component);
        Assert.AreEqual(35, days.Count);
        Assert.AreEqual("1", days[0].TextContent.Trim());
        Assert.IsTrue(days.All(d => d.GetAttribute("type") == "button"));

        var others = days.Where(d => d.ClassList.Contains("other-month")).Select(d => d.TextContent.Trim()).ToArray();
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4" }, others);
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldMarkTheSelectedDayAndToday()
    {
        var component = RenderDay(date: Anchor.AddDays(2));

        var selected = Day(component, Anchor.AddDays(2));
        Assert.IsTrue(selected.ClassList.Contains("selected"));
        Assert.AreEqual("Friday, March 20, 2026, selected", selected.GetAttribute("aria-label"));
        Assert.IsNull(selected.GetAttribute("aria-current"));

        var today = Day(component, Anchor);
        Assert.IsTrue(today.ClassList.Contains("today"));
        Assert.AreEqual("date", today.GetAttribute("aria-current"));
        Assert.AreEqual("Wednesday, March 18, 2026", today.GetAttribute("aria-label"));

        Assert.AreEqual(1, Days(component).Count(d => d.ClassList.Contains("selected")));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldNameTheSelectedDayThroughTheTexts()
    {
        var component = RenderDay(p => p.Add(c => c.Texts, new BitFullCalendarTexts
        {
            PickerSelectedDayAriaLabel = "chosen",
            PreviousMonthAriaLabel = "Back",
            NextMonthAriaLabel = "Forward",
        }));

        Assert.AreEqual("Wednesday, March 18, 2026, chosen", Day(component, Anchor).GetAttribute("aria-label"));
        Assert.AreEqual("Back", PrevButton(component).GetAttribute("aria-label"));
        Assert.AreEqual("Forward", NextButton(component).GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldNameItsNavigationButtons()
    {
        var component = RenderDay();

        Assert.AreEqual("Previous month", PrevButton(component).GetAttribute("aria-label"));
        Assert.AreEqual("Next month", NextButton(component).GetAttribute("aria-label"));
        Assert.AreEqual("button", PrevButton(component).GetAttribute("type"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldFollowTheWeekSettings()
    {
        var component = RenderDay(p => p.Add(c => c.Settings, new BitFullCalendarSettings
        {
            FirstDayOfWeek = DayOfWeek.Monday,
            HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday]
        }));

        CollectionAssert.AreEqual(
            new[] { "Mo", "Tu", "We", "Th", "Fr" },
            component.FindAll(".bit-bfc-mini-calendar th").Select(h => h.TextContent.Trim()).ToArray());
        Assert.IsTrue(component.FindAll(".bit-bfc-mini-calendar tbody tr").All(r => r.QuerySelectorAll("td").Length == 5));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldBeLeftOutWhenTheSettingsTurnItOff()
    {
        var component = RenderDay(p => p.Add(c => c.Settings, new BitFullCalendarSettings { ShowDayViewCalendar = false }));

        Assert.AreEqual(0, component.FindAll(".bit-bfc-mini-calendar").Count);
    }

    #endregion

    #region Browsing and picking

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldBrowseTheMonthsWithoutMovingTheSelectedDate()
    {
        var component = RenderDay();

        NextButton(component).Click();
        Assert.AreEqual("April 2026", Title(component));
        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate.Date);
        Assert.AreEqual(0, Days(component).Count(d => d.ClassList.Contains("selected")));

        PrevButton(component).Click();
        PrevButton(component).Click();
        Assert.AreEqual("February 2026", Title(component));
        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate.Date);
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldBrowseAcrossTheYear()
    {
        var component = RenderDay(date: new DateTime(2026, 12, 10));

        NextButton(component).Click();

        Assert.AreEqual("January 2027", Title(component));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldSelectAPickedDay()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderDay(p => p.Add(c => c.OnDateChange, EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, ranges.Add)));
        ranges.Clear();

        Day(component, new DateTime(2026, 3, 24)).Click();

        Assert.AreEqual(new DateTime(2026, 3, 24), component.Instance.State.SelectedDate.Date);
        Assert.IsTrue(Day(component, new DateTime(2026, 3, 24)).ClassList.Contains("selected"));
        Assert.IsFalse(Day(component, Anchor).ClassList.Contains("selected"));
        Assert.AreEqual(new DateTime(2026, 3, 24), ranges.Last().Start.Date, "the day view moves with it");
        Assert.AreEqual(BitFullCalendarView.Day, component.Instance.State.View);
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldTurnToTheMonthOfAPickedPaddingDay()
    {
        var component = RenderDay();

        Day(component, new DateTime(2026, 4, 2)).Click();

        Assert.AreEqual(new DateTime(2026, 4, 2), component.Instance.State.SelectedDate.Date);
        Assert.AreEqual("April 2026", Title(component));
        Assert.IsFalse(Day(component, new DateTime(2026, 4, 2)).ClassList.Contains("other-month"));
    }

    [TestMethod]
    public async Task BitFullCalendarMiniCalendarShouldFollowADateChangedElsewhere()
    {
        var component = RenderDay();
        NextButton(component).Click();
        Assert.AreEqual("April 2026", Title(component));

        await component.InvokeAsync(() => component.Instance.State.SetSelectedDate(new DateTime(2026, 7, 7)));

        Assert.AreEqual("July 2026", Title(component));
        Assert.IsTrue(Day(component, new DateTime(2026, 7, 7)).ClassList.Contains("selected"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldFollowACalendarSwitchedOnACultureOfTheSameName()
    {
        // DateTimeFormat.Calendar is the documented way of switching the calendar of a culture: the name stays, and so
        // does CultureInfo.Calendar (the culture's default), so the month shown has to follow the calendar the format
        // reads rather than either of those.
        var hijri = CultureInfo.GetCultureInfo("ar-SA");
        var gregorian = (CultureInfo)hijri.Clone();
        gregorian.DateTimeFormat.Calendar = new GregorianCalendar();

        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.Culture, hijri);
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            parameters.Add(p => p.TimeProvider, new FixedTimeProvider(Anchor.AddHours(11)));
        });

        var umAlQura = hijri.DateTimeFormat.Calendar;
        Assert.AreEqual($"{hijri.DateTimeFormat.GetMonthName(umAlQura.GetMonth(Anchor))} {umAlQura.GetYear(Anchor)}", Title(component));
        Assert.AreNotEqual(31, Days(component).Count(d => d.ClassList.Contains("other-month") is false));

        component.Render(parameters => parameters.Add(p => p.Culture, gregorian));

        // March 2026 of the Gregorian calendar: its own month name and its thirty-one days.
        Assert.AreEqual($"{gregorian.DateTimeFormat.GetMonthName(3)} 2026", Title(component));
        Assert.AreEqual(31, Days(component).Count(d => d.ClassList.Contains("other-month") is false));
        Assert.IsTrue(Day(component, Anchor).ClassList.Contains("selected"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldFollowTheBoundDate()
    {
        var component = RenderDay();

        component.Render(p => p.Add(c => c.Date, new DateTime(2026, 9, 9)));

        Assert.AreEqual("September 2026", Title(component));
        Assert.AreEqual("09", RovingDay(component)[^2..]);
    }

    #endregion

    #region Date bounds

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldDisableTheDaysOutsideTheBounds()
    {
        var component = RenderDay(p =>
        {
            p.Add(c => c.MinDate, new DateTime(2026, 3, 10));
            p.Add(c => c.MaxDate, new DateTime(2026, 3, 25));
        });

        var days = Days(component);
        var disabled = days.Where(d => d.HasAttribute("disabled")).ToList();
        // March 1 - 9, March 26 - 31 and the four April padding days.
        Assert.AreEqual(9 + 6 + 4, disabled.Count);
        Assert.IsTrue(disabled.All(d => d.GetAttribute("aria-disabled") == "true"));
        Assert.IsTrue(days.Where(d => d.HasAttribute("disabled") is false).All(d => d.HasAttribute("aria-disabled") is false));
        Assert.IsFalse(Day(component, new DateTime(2026, 3, 10)).HasAttribute("disabled"));
        Assert.IsTrue(Day(component, new DateTime(2026, 3, 26)).HasAttribute("disabled"));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldNeverPutTheTabStopOnADisabledDay()
    {
        var component = RenderDay(p =>
        {
            p.Add(c => c.MinDate, new DateTime(2026, 4, 10));
        }, new DateTime(2026, 5, 15));

        PrevButton(component).Click();

        // The browsed month holds no selected day, and its first nine days are out of reach: a disabled button
        // cannot take the focus, so the stop goes to the first day that can.
        Assert.AreEqual("April 2026", Title(component));
        Assert.AreEqual("20260410", RovingDay(component));
    }

    [TestMethod]
    [DataRow("ArrowLeft")]
    [DataRow("ArrowUp")]
    [DataRow("PageUp")]
    public void BitFullCalendarMiniCalendarKeysShouldStopAtTheLowerBound(string key)
    {
        var component = RenderDay(p => p.Add(c => c.MinDate, Anchor));

        Day(component, Anchor).KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual("20260318", RovingDay(component));
        Assert.AreEqual("March 2026", Title(component));
    }

    #endregion

    #region Keyboard

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldBeOneTabStopOnTheSelectedDay()
    {
        var component = RenderDay();

        Assert.AreEqual(1, Days(component).Count(d => d.GetAttribute("tabindex") == "0"));
        Assert.AreEqual("20260318", RovingDay(component));
        Assert.IsTrue(Days(component).All(d => d.HasAttribute("data-bit-bfc-roving")));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldPutTheTabStopOnTheFirstDayOfABrowsedMonth()
    {
        var component = RenderDay();

        NextButton(component).Click();

        Assert.AreEqual("20260401", RovingDay(component));
    }

    [TestMethod]
    [DataRow("ArrowRight", "20260319")]
    [DataRow("ArrowLeft", "20260317")]
    [DataRow("ArrowDown", "20260325")]
    [DataRow("ArrowUp", "20260311")]
    [DataRow("Home", "20260315")]
    [DataRow("End", "20260321")]
    [DataRow("Enter", "20260318")]
    [DataRow("x", "20260318")]
    public void BitFullCalendarMiniCalendarKeysShouldWalkTheDays(string key, string expected)
    {
        var component = RenderDay();

        Day(component, Anchor).KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expected, RovingDay(component));
        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate.Date, "walking the days does not pick one");
    }

    [TestMethod]
    [DataRow("PageDown", false, "20260418", "April 2026")]
    [DataRow("PageUp", false, "20260218", "February 2026")]
    [DataRow("PageDown", true, "20270318", "March 2027")]
    [DataRow("PageUp", true, "20250318", "March 2025")]
    public void BitFullCalendarMiniCalendarPageKeysShouldTurnTheMonthOrTheYear(string key, bool shift, string expected, string title)
    {
        var component = RenderDay();

        Day(component, Anchor).KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        Assert.AreEqual(title, Title(component));
        Assert.AreEqual(expected, RovingDay(component));
        Assert.AreEqual(Anchor, component.Instance.State.SelectedDate.Date);
    }

    [TestMethod]
    [DataRow("ArrowRight", "20260317")]
    [DataRow("ArrowLeft", "20260319")]
    public void BitFullCalendarMiniCalendarArrowsShouldFollowTheReadingDirectionInRtl(string key, string expected)
    {
        var component = RenderDay(p => p.Add(c => c.Dir, BitDir.Rtl));

        Day(component, Anchor).KeyDown(new KeyboardEventArgs { Key = key });

        Assert.AreEqual(expected, RovingDay(component));
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarArrowsShouldWalkIntoThePaddingDays()
    {
        var component = RenderDay(date: new DateTime(2026, 3, 31));

        Day(component, new DateTime(2026, 3, 31)).KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.AreEqual("20260401", RovingDay(component));
        Assert.AreEqual("March 2026", Title(component), "the month only turns when a day is picked or paged to");
    }

    [TestMethod]
    public void BitFullCalendarMiniCalendarShouldMoveTheFocusWithTheTabStop()
    {
        var component = RenderDay();

        Day(component, Anchor).KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var focus = Context.JSInterop.VerifyInvoke("BitBlazorUI.FullCalendar.focusElement");
        Assert.AreEqual(Day(component, new DateTime(2026, 3, 25)).Id, focus.Arguments[^1]);
    }

    #endregion

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}
