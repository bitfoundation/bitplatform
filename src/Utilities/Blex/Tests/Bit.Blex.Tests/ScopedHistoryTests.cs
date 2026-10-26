using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests;

/// <summary>
/// Undo that stays inside the part of the app being edited: a scoped <see cref="BlexHistory"/>
/// records only the stores it covers and restores only their slices.
/// </summary>
[TestClass]
public class ScopedHistoryTests
{
    private static (BlexManager Manager, CounterStore Counter, ProfileStore Profile) Build()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var profile = new ProfileStore();
        manager.Register(counter);
        manager.Register(profile);
        return (manager, counter, profile);
    }

    [TestMethod]
    public void Actions_of_stores_outside_the_scope_make_no_undo_steps()
    {
        var (manager, counter, profile) = Build();
        using var history = new BlexHistory(manager, scope: s => s is CounterStore);
        history.Start();

        counter.Increment();
        profile.SignIn("ada");
        profile.SignOut();

        Assert.AreEqual(1, history.UndoCount);
        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
        Assert.IsTrue(history.IsScoped);
    }

    [TestMethod]
    public void Undo_never_touches_a_store_outside_the_scope()
    {
        var (manager, counter, profile) = Build();
        using var history = new BlexHistory(manager, scope: s => s is CounterStore);
        history.Start();

        counter.Add(5);
        profile.SignIn("ada");   // signing in after the edit must survive undoing it
        history.Undo();

        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("ada", profile.UserName);

        history.Redo();
        Assert.AreEqual(5, counter.Count);
        Assert.AreEqual("ada", profile.UserName);
    }

    [TestMethod]
    public void An_application_wide_undo_does_revert_every_store()
    {
        // The behavior the scope exists to avoid, kept as the default.
        var (manager, counter, profile) = Build();
        using var history = new BlexHistory(manager);
        history.Start();

        counter.Add(5);
        profile.SignIn("ada");
        history.Jump(-2);

        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("anonymous", profile.UserName);
    }

    [TestMethod]
    public void A_restore_outside_the_scope_leaves_the_history_alone()
    {
        var (manager, counter, profile) = Build();
        using var history = new BlexHistory(manager, scope: s => s is CounterStore);
        history.Start();
        counter.Increment();

        var changed = 0;
        history.Changed += () => changed++;
        profile.RestoreState(new JsonObject { ["UserName"] = "from another tab" });

        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
        Assert.AreEqual(0, changed);

        history.Undo();
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual("from another tab", profile.UserName);
    }

    [TestMethod]
    public void A_restore_that_changes_nothing_keeps_the_label_of_a_whole_app_history_too()
    {
        var (manager, counter, _) = Build();
        using var history = new BlexHistory(manager);
        history.Start();
        counter.Increment();

        manager.RestoreGlobalState(manager.CaptureGlobalState());

        Assert.AreEqual("counter/Increment", history.NextUndoLabel);
    }

    [TestMethod]
    public void A_typed_history_covers_every_instance_of_its_store()
    {
        var (manager, counter, profile) = Build();
        using var scoped = manager.CreateScope(new CounterStore(), "side");
        using var history = new BlexHistory<CounterStore>(manager);
        history.Start();

        counter.Increment();
        scoped.Store.Add(3);
        profile.SignIn("ada");

        CollectionAssert.AreEqual(new[] { "counter:side/Add", "counter/Increment" }, history.UndoLabels.ToArray());

        history.Jump(-2);
        Assert.AreEqual(0, counter.Count);
        Assert.AreEqual(0, scoped.Store.Count);
        Assert.AreEqual("ada", profile.UserName);
    }

    [TestMethod]
    public void A_scope_that_throws_covers_nothing_and_is_reported()
    {
        var (manager, counter, _) = Build();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;
        using var history = new BlexHistory(manager, scope: _ => throw new InvalidOperationException("bad scope"));
        history.Start();

        counter.Increment();

        Assert.IsFalse(history.CanUndo);
        Assert.IsTrue(errors.Count > 0 && errors.All(e => e.Source == "history"));
        Assert.IsFalse(history.Covers(counter));
    }

    [TestMethod]
    public void Checkpoints_and_clear_work_on_the_scope_alone()
    {
        var (manager, counter, profile) = Build();
        using var history = new BlexHistory(manager, scope: s => s is CounterStore, filter: _ => false);
        history.Start();

        counter.Add(2);           // filtered: no step of its own
        profile.SignIn("ada");    // out of scope: nothing changes for the history
        history.Checkpoint("Edit");
        history.Checkpoint("Again");   // nothing in scope moved: collapsed

        Assert.AreEqual(1, history.UndoCount);
        Assert.AreEqual("Edit", history.NextUndoLabel);

        history.Clear();
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void The_scope_is_required()
        => Assert.ThrowsExactly<ArgumentNullException>(() => new BlexHistory(new BlexManager(), scope: null!));

    [TestMethod]
    public void DI_registers_a_scoped_history_and_a_typed_one()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices();
        services.AddBitBlexStore<CounterStore>();
        services.AddBitBlexStore<ProfileStore>();
        services.AddBitBlexHistory(scope: s => s is not ProfileStore);
        services.AddBitBlexHistory<CounterStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<CounterStore>();
        var profile = scope.ServiceProvider.GetRequiredService<ProfileStore>();
        var app = scope.ServiceProvider.GetRequiredService<BlexHistory>();
        var typed = scope.ServiceProvider.GetRequiredService<BlexHistory<CounterStore>>();
        app.Start();   // <BlexProvider> does this after hydration; the typed one starts itself

        counter.Increment();
        profile.SignIn("ada");

        Assert.IsTrue(app.IsScoped);
        Assert.AreEqual(1, app.UndoCount);
        Assert.AreEqual(1, typed.UndoCount);
        Assert.IsFalse(app.Covers(profile));
    }
}
