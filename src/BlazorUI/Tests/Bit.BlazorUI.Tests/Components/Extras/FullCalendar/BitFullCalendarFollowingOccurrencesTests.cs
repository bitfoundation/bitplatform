using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// "This and following events", the scope prompt's choice between one occurrence and the whole series: a delete ends the
/// series before the occurrence, and an edit ends it there and starts a new series from the occurrence with the edited
/// fields - reported as an edit of the master and an add, approved together.
/// </summary>
[TestClass]
public class BitFullCalendarFollowingOccurrencesTests : BunitTestContext
{
    private static readonly DateTime Start = new(2024, 6, 10);

    private const string Scope = ".bit-bfc-scope-dialog";

    private IRenderedComponent<BitFullCalendar> RenderSeries(BitFullCalendarRecurrence rule,
                                                             List<BitFullCalendarChangeEventArgs> changes,
                                                             Action<ComponentParameterCollectionBuilder<BitFullCalendar>>? configure = null)
        => RenderComponent<BitFullCalendar>(parameters =>
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
                    Recurrence = rule
                }
            ]);
            parameters.Add(p => p.OnChange, EventCallback.Factory.Create<BitFullCalendarChangeEventArgs>(this, changes.Add));
            configure?.Invoke(parameters);
        });

    private static BitFullCalendarRecurrence Daily(int? count = null, DateTime? until = null)
        => new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Count = count, Until = until };

    // Opens the badge's details, then its edit or delete, and picks "This and following events".
    private static void ChooseFollowing(IRenderedComponent<BitFullCalendar> component, int badge, bool edit)
    {
        component.FindAll(".bit-bfc-event-badge")[badge].Click();
        if (edit)
            component.FindAll(".bit-bfc-dialog-footer .bit-bfc-btn")[0].Click();
        else
            component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-danger").Click();

        var following = component.FindAll($"{Scope} .bit-bfc-scope-option").Single(o => o.TextContent.Trim() == "This and following events");
        following.QuerySelector("input")!.Change(true);
        component.Find($"{Scope} .bit-bfc-btn-primary").Click();
    }

    private static void Save(IRenderedComponent<BitFullCalendar> component)
        => component.Find(".bit-bfc-dialog-footer .bit-bfc-btn-primary").Click();

    private static string[] ShownDays(IEnumerable<BitFullCalendarEvent> events)
        => [.. BitFullCalendarHelpers.ExpandRecurrences([.. events], new DateTime(2024, 6, 1), new DateTime(2024, 6, 30))
                .OrderBy(e => e.StartDate).Select(e => $"{e.StartDate:MM-dd} {e.Title}")];

    [TestMethod]
    public void BitFullCalendarShouldEndTheSeriesBeforeAnOccurrenceDeletedWithTheFollowingOnes()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count: 3), changes);

        ChooseFollowing(component, badge: 1, edit: false);

        var master = component.Instance.State.AllEvents.Single();
        Assert.AreEqual(Start, master.Recurrence!.Until, "the series ends the day before the occurrence");
        Assert.AreEqual(3, master.Recurrence.Count);
        Assert.AreEqual(1, component.FindAll(".bit-bfc-event-badge").Count);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);
        Assert.AreEqual("series", changes[0].Event.Id);
        Assert.IsNull(changes[0].OldEvent!.Recurrence!.Until, "the snapshot keeps the rule as it was");
    }

    [TestMethod]
    public void BitFullCalendarShouldDeleteTheWholeSeriesWhenItsFirstOccurrenceGoesWithTheFollowingOnes()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count: 3), changes);

        ChooseFollowing(component, badge: 0, edit: false);

        Assert.AreEqual(0, component.Instance.State.AllEvents.Count);
        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Delete, changes[0].Kind);
    }

    // The new series takes the occurrence's place and every later one's: a count closing the series hands on what it
    // had left, an end date is kept, and an open-ended series stays open.
    [TestMethod]
    [DataRow(3, null, 2, null, DisplayName = "Closed by a count")]
    [DataRow(null, "2024-06-12", null, "2024-06-12", DisplayName = "Closed by a date")]
    [DataRow(null, null, null, null, DisplayName = "Open-ended")]
    public void BitFullCalendarShouldStartANewSeriesFromAnOccurrenceEditedWithTheFollowingOnes(int? count, string? until, int? newCount, string? newUntil)
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count, until is null ? null : DateTime.Parse(until)), changes);
        var before = ShownDays(component.Instance.State.AllEvents);

        ChooseFollowing(component, badge: 1, edit: true);

        // The form opens on the occurrence with the series' rule, its count down to what is left from there.
        Assert.AreEqual(1, component.FindAll("select[id^='bfc-repeat-']").Count);
        if (newCount is not null)
            Assert.AreEqual($"{newCount}", component.Find("input[id^='bfc-count-']").GetAttribute("value"));

        component.Find("input[id^='bfc-title-']").Change("Standup v2");
        Save(component);

        var events = component.Instance.State.AllEvents;
        Assert.AreEqual(2, events.Count);

        var master = events.Single(e => e.Id == "series");
        Assert.AreEqual("Standup", master.Title);
        Assert.AreEqual(Start, master.Recurrence!.Until);

        var following = events.Single(e => e.Id != "series");
        Assert.AreEqual("Standup v2", following.Title);
        Assert.AreEqual(Start.AddDays(1).AddHours(9), following.StartDate);
        Assert.AreEqual(Start.AddDays(1).AddHours(10), following.EndDate);
        Assert.AreEqual(BitFullCalendarRecurrenceFrequency.Daily, following.Recurrence!.Frequency);
        Assert.AreEqual(newCount, following.Recurrence.Count);
        Assert.AreEqual(newUntil is null ? null : DateTime.Parse(newUntil), following.Recurrence.Until);

        // Every date the series had stays, the first under the old title and the rest under the new one.
        var after = ShownDays(events);
        CollectionAssert.AreEqual(before.Select(d => d[..5]).ToArray(), after.Select(d => d[..5]).ToArray());
        Assert.AreEqual("06-10 Standup", after[0]);
        Assert.IsTrue(after.Skip(1).All(d => d.EndsWith("Standup v2")));

        CollectionAssert.AreEqual(
            new[] { BitFullCalendarChangeKind.Edit, BitFullCalendarChangeKind.Add },
            changes.Select(c => c.Kind).ToArray());
        Assert.AreEqual("series", changes[0].Event.Id);
        Assert.AreEqual(following.Id, changes[1].Event.Id);
    }

    // The dates a series skips or adds go with the part of it they fall in, and a count is a position in the series:
    // the skipped date before the split still used up its place.
    [TestMethod]
    public void BitFullCalendarShouldSplitTheSkippedAndAddedDatesAtTheOccurrence()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var rule = Daily(count: 5);
        rule.ExceptionDates = [new(2024, 6, 11), new(2024, 6, 13)];
        rule.AdditionalDates = [new(2024, 6, 7), new(2024, 6, 20)];
        var component = RenderSeries(rule, changes);
        var before = ShownDays(component.Instance.State.AllEvents);
        CollectionAssert.AreEqual(new[] { "06-07", "06-10", "06-12", "06-14", "06-20" }, before.Select(d => d[..5]).ToArray());

        ChooseFollowing(component, badge: 2, edit: true);
        Save(component);

        var events = component.Instance.State.AllEvents;
        var master = events.Single(e => e.Id == "series").Recurrence!;
        Assert.AreEqual(new DateTime(2024, 6, 11), master.Until);
        Assert.AreEqual(5, master.Count);
        CollectionAssert.AreEqual(new[] { new DateTime(2024, 6, 11) }, master.ExceptionDates!.ToArray());
        CollectionAssert.AreEqual(new[] { new DateTime(2024, 6, 7) }, master.AdditionalDates!.ToArray());

        var following = events.Single(e => e.Id != "series");
        Assert.AreEqual(new DateTime(2024, 6, 12, 9, 0, 0), following.StartDate);
        Assert.AreEqual(3, following.Recurrence!.Count, "June 10 and the skipped June 11 used up two of the five");
        CollectionAssert.AreEqual(new[] { new DateTime(2024, 6, 13) }, following.Recurrence.ExceptionDates!.ToArray());
        CollectionAssert.AreEqual(new[] { new DateTime(2024, 6, 20) }, following.Recurrence.AdditionalDates!.ToArray());

        CollectionAssert.AreEqual(before, ShownDays(events), "the split shows every date it showed before");
    }

    [TestMethod]
    public void BitFullCalendarShouldEditTheWholeSeriesWhenItsFirstOccurrenceIsEditedWithTheFollowingOnes()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count: 3), changes);

        ChooseFollowing(component, badge: 0, edit: true);
        component.Find("input[id^='bfc-title-']").Change("Standup v2");
        Save(component);

        var master = component.Instance.State.AllEvents.Single();
        Assert.AreEqual("series", master.Id);
        Assert.AreEqual("Standup v2", master.Title);
        Assert.IsNull(master.Recurrence!.Until);
        Assert.AreEqual(3, master.Recurrence.Count);

        Assert.AreEqual(1, changes.Count);
        Assert.AreEqual(BitFullCalendarChangeKind.Edit, changes[0].Kind);
    }

    // The occurrences the new series takes over are its series' own, so landing on one does not count as an overlap when
    // overlaps are not allowed: here the start moves from June 11 onto June 12, an occurrence the split replaces.
    [TestMethod]
    public void BitFullCalendarShouldNotTakeTheSeriesOwnOccurrencesForAnOverlap()
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count: 3), changes, parameters =>
            parameters.Add(p => p.Settings, new BitFullCalendarSettings { AllowEventOverlap = false }));

        ChooseFollowing(component, badge: 1, edit: true);
        component.FindAll(".bit-bfc-dtp-trigger")[0].Click();
        component.Find("button[id$='-20240612']").Click();
        Save(component);

        var following = component.Instance.State.AllEvents.Single(e => e.Id != "series");
        Assert.AreEqual(Start.AddDays(2).AddHours(9), following.StartDate);
        Assert.AreEqual(2, changes.Count);
    }

    // The end of the series and the new one stand or fall together: refusing either puts the series back as it was.
    [TestMethod]
    [DataRow(BitFullCalendarChangeKind.Edit)]
    [DataRow(BitFullCalendarChangeKind.Add)]
    public void BitFullCalendarShouldPutTheSeriesBackWhenTheSplitIsRefused(BitFullCalendarChangeKind refused)
    {
        var changes = new List<BitFullCalendarChangeEventArgs>();
        var component = RenderSeries(Daily(count: 3), changes, parameters =>
            parameters.Add(p => p.OnChanging, (BitFullCalendarChangingEventArgs args) => args.Cancel = args.Kind == refused));

        ChooseFollowing(component, badge: 1, edit: true);
        component.Find("input[id^='bfc-title-']").Change("Standup v2");
        Save(component);

        var master = component.Instance.State.AllEvents.Single();
        Assert.AreEqual("Standup", master.Title);
        Assert.IsNull(master.Recurrence!.Until);
        Assert.AreEqual(0, changes.Count);
        Assert.AreEqual(1, component.FindAll("input[id^='bfc-title-']").Count, "the form stays open with what was typed");
    }
}
