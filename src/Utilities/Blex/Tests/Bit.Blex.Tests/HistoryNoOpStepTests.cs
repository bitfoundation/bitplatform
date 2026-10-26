using System.Text.Json.Nodes;

namespace Bit.Blex.Tests;

/// <summary>
/// An undo click always changes something: a step another history already undid, or one recorded for a store
/// that has since left the manager, is not spent on restoring what is already showing -- and the button keeps
/// naming the step it will actually revert.
/// </summary>
[TestClass]
public class HistoryNoOpStepTests
{
    private static (BlexManager Manager, CounterStore Counter, SettingsStore Settings) Stores()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var settings = new SettingsStore();
        manager.Register(counter);
        manager.Register(settings);
        return (manager, counter, settings);
    }

    [TestMethod]
    public void After_a_narrower_history_undoes_the_wider_one_reverts_the_step_before()
    {
        var (manager, counter, settings) = Stores();
        var all = new BlexHistory(manager);
        var counterOnly = new BlexHistory<CounterStore>(manager);
        all.Start();
        counterOnly.Start();

        counter.Increment();
        settings.SetTheme("dark");
        counter.Increment();

        counterOnly.Undo();
        Assert.AreEqual(1, counter.Count);

        // The counter history already took back the last Increment, so the next app-wide step is the theme.
        Assert.AreEqual("settings/SetTheme", all.NextUndoLabel);
        all.Undo();

        Assert.AreEqual("light", settings.Theme, "the first click must revert something");
        Assert.AreEqual(1, counter.Count);
    }

    [TestMethod]
    public void After_the_wider_history_undoes_the_narrower_one_is_not_left_with_a_dead_step()
    {
        var (manager, counter, _) = Stores();
        var all = new BlexHistory(manager);
        var counterOnly = new BlexHistory<CounterStore>(manager);
        all.Start();
        counterOnly.Start();

        counter.Increment();
        counter.Increment();
        all.Undo();
        Assert.AreEqual(1, counter.Count);

        Assert.AreEqual("counter/Increment", counterOnly.NextUndoLabel);
        counterOnly.Undo();

        Assert.AreEqual(0, counter.Count);
        Assert.IsFalse(counterOnly.CanUndo);
    }

    [TestMethod]
    public void Steps_of_a_store_that_left_are_skipped()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();

        counter.Increment();
        var dialog = new SettingsStore();
        manager.Register(dialog);
        dialog.SetTheme("dark");
        manager.Unregister(dialog);

        history.Undo();

        Assert.AreEqual(0, counter.Count, "one click reaches the Increment; the theme step has nothing left to restore");
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void Steps_of_a_store_that_left_wait_for_it_when_nothing_else_is_left_to_undo()
    {
        var manager = new BlexManager();
        manager.Register(new CounterStore());
        var document = new SettingsStore();
        manager.Register(document);
        var history = new BlexHistory(manager);
        history.Start();
        document.SetTheme("dark");
        document.SetTheme("blue");
        manager.Unregister(document);

        // Another document is open: there is nothing on screen to undo, and the closed one's steps must not be used up.
        history.Undo();
        Assert.AreEqual(2, history.UndoCount);
        Assert.IsFalse(history.CanRedo);

        // The document is reopened (and hydrated from storage): its steps are undoable again.
        var reopened = new SettingsStore();
        manager.Register(reopened);
        reopened.RestoreState(new JsonObject { ["Theme"] = "blue", ["FontSize"] = 14 });
        history.Undo();
        Assert.AreEqual("dark", reopened.Theme);
    }

    [TestMethod]
    public void A_store_restored_as_it_joins_keeps_the_undo_label()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();
        counter.Increment();

        // A scoped instance hydrating from storage as its dialog opens: a restore of a store the present has never seen.
        var late = new SettingsStore();
        manager.Register(late);
        late.RestoreState(new JsonObject { ["Theme"] = "dark", ["FontSize"] = 14 });

        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
        history.Undo();
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("dark", late.Theme, "undo must not touch a store the step never held");
    }

    [TestMethod]
    public void The_first_action_of_a_store_that_registered_late_is_undoable()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();

        var late = new SettingsStore();
        manager.Register(late);
        late.SetTheme("dark");

        history.Undo();

        Assert.AreEqual("light", late.Theme);
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void A_late_store_hydrating_after_another_action_is_not_reverted_by_its_undo()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager);
        history.Start();

        // The read from storage is still on its way when another store acts.
        var late = new SettingsStore();
        manager.Register(late);
        counter.Increment();
        late.RestoreState(new JsonObject { ["Theme"] = "dark", ["FontSize"] = 14 });

        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
        history.Undo();
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("dark", late.Theme, "undo must not put the declared defaults back over the hydrated state");
    }

    [TestMethod]
    public void A_store_registering_closes_the_open_group()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var history = new BlexHistory(manager, groupBy: _ => "all");
        history.Start();
        counter.Increment();

        var late = new SettingsStore();
        manager.Register(late);
        late.SetTheme("dark");

        // The open step was taken before the store arrived: folded into it, the theme change could not be undone.
        history.Undo();
        Assert.AreEqual("light", late.Theme);
        Assert.AreEqual(1, counter.Count);
    }
}
