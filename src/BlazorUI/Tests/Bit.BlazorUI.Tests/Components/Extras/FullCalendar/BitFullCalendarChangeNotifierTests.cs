using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers how <see cref="BitFullCalendarChangeNotifier"/> puts the state back after a refused or failed change.
/// </summary>
[TestClass]
public class BitFullCalendarChangeNotifierTests
{
    private static readonly DateTime Anchor = new(2031, 6, 18);

    [TestMethod]
    public async Task RefusedDeleteShouldNotRestoreAnEventAConsumerResyncAlreadyBroughtBack()
    {
        var consumerEvents = new List<BitFullCalendarEvent>
        {
            new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) }
        };
        var state = new BitFullCalendarState();
        state.Initialize(consumerEvents);
        state.SetSelectedDate(Anchor);

        var notifier = new BitFullCalendarChangeNotifier(state, _ => Task.CompletedTask)
        {
            // An asynchronous OnChanging lets the parent re-render, and the calendar re-syncs the consumer's list,
            // which still holds the event, before the refusal is read.
            ApprovalHandler = _ =>
            {
                state.SyncEvents(consumerEvents);
                return Task.FromResult(false);
            }
        };

        var target = state.AllEvents.Single();
        var snapshot = BitFullCalendarChangeNotifier.CloneEvent(target);
        state.RemoveEvent(target.Id);

        var reported = await notifier.TryNotifyAsync(new BitFullCalendarChangeEventArgs
        {
            Event = snapshot,
            OldEvent = snapshot,
            Kind = BitFullCalendarChangeKind.Delete,
            Source = BitFullCalendarChangeSource.Dialog
        });

        Assert.IsFalse(reported);
        Assert.AreEqual(1, state.AllEvents.Count, "the event is shown once, not restored on top of the re-synced one");
        Assert.AreEqual("1", state.AllEvents.Single().Id);
    }

    [TestMethod]
    public async Task RefusedDeleteShouldRestoreTheEventWhenNothingBroughtItBack()
    {
        var state = new BitFullCalendarState();
        state.Initialize([new() { Id = "1", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10) }]);
        var notifier = new BitFullCalendarChangeNotifier(state, _ => Task.CompletedTask)
        {
            ApprovalHandler = _ => Task.FromResult(false)
        };

        var snapshot = BitFullCalendarChangeNotifier.CloneEvent(state.AllEvents.Single());
        state.RemoveEvent("1");

        await notifier.TryNotifyAsync(new BitFullCalendarChangeEventArgs { Event = snapshot, OldEvent = snapshot, Kind = BitFullCalendarChangeKind.Delete, Source = BitFullCalendarChangeSource.Dialog });

        Assert.AreEqual(1, state.AllEvents.Count);
    }

    [TestMethod]
    public async Task DetachedOccurrenceShouldKeepWhatWasReportedWhenTheSecondReportThrows()
    {
        var state = new BitFullCalendarState();
        state.Initialize(
        [
            new()
            {
                Id = "series",
                Title = "Standup",
                StartDate = Anchor.AddHours(9),
                EndDate = Anchor.AddHours(10),
                Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily }
            }
        ]);
        state.SetView(BitFullCalendarView.Day);
        state.SetSelectedDate(Anchor.AddDays(2));

        var reported = new List<BitFullCalendarChangeKind>();
        var notifier = new BitFullCalendarChangeNotifier(state, args =>
        {
            if (args.Kind is BitFullCalendarChangeKind.Add)
                throw new InvalidOperationException("consumer failure");

            reported.Add(args.Kind);
            return Task.CompletedTask;
        });

        var occurrence = state.Events.Single(e => e.OccurrenceDate == Anchor.AddDays(2));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => notifier.CommitEditAsync(occurrence, occurrence.StartDate.AddHours(1), occurrence.EndDate.AddHours(1), null, BitFullCalendarChangeSource.Drag));

        CollectionAssert.AreEqual(new[] { BitFullCalendarChangeKind.Edit }, reported, "the skip reached the consumer");
        var master = state.AllEvents.Single(e => e.Id == "series");
        CollectionAssert.Contains(master.Recurrence!.ExceptionDates!.ToList(), Anchor.AddDays(2),
                                  "the series keeps the skip the consumer stored, rather than being rolled back under it");
    }
}
