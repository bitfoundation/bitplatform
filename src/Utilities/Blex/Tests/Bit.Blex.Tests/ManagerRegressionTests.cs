using Bit.Blex.Blazor;

namespace Bit.Blex.Tests;

[BlexStoreAttribute(Name = "shared")]
public partial class SharedFailureStore
{
    [BlexStateAttribute] private int _runs;

    /// <summary>One task both effects await: a cached request, a lazily started load.</summary>
    public Task Shared { get; set; } = Task.CompletedTask;

    [BlexEffectAttribute]
    private async Task OnLoadPrices()
    {
        Runs++;
        await Shared;
    }

    [BlexEffectAttribute]
    private async Task OnLoadCatalog()
    {
        Runs++;
        await Shared;
    }

    [BlexActionAttribute]
    private void OnOuter() => InnerThrows();

    [BlexActionAttribute]
    private void OnInnerThrows()
    {
        Runs++;
        throw new InvalidOperationException("inner");
    }
}

/// <summary>
/// Regressions found auditing the manager: generated scope keys, undo steps of reactor follow-ups, DevTools bridges
/// of several providers, failure reports of one exception instance, and subscriptions disposed mid-dispatch.
/// </summary>
[TestClass]
public class ManagerRegressionTests
{
    [TestMethod]
    public void A_generated_scope_key_never_collides_with_an_id_given_as_a_key()
    {
        var manager = new BlexManager();
        var errors = new List<BlexError>();
        manager.OnError = errors.Add;

        using var document = manager.CreateScope(new CounterStore(), "1");
        using var draft = manager.CreateScope(new CounterStore());

        Assert.AreEqual("counter:1", document.Store.Name);
        Assert.AreEqual("counter:#1", draft.Store.Name);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void A_reactor_follow_up_is_its_own_undo_step_whoever_subscribed_first()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var profile = new ProfileStore();
        manager.Register(counter);
        manager.Register(profile);

        // Subscribed before the history starts, as components are when <BlexProvider> starts it after hydration.
        using var reactor = manager.SubscribeToAction(CounterStore.Actions.Increment, _ => profile.SignIn("ada"));
        using var history = new BlexHistory(manager);
        history.Start();

        counter.Increment();

        Assert.AreEqual(2, history.UndoCount);
        Assert.AreEqual("profile/SignIn", history.NextUndoLabel);

        history.Undo();
        Assert.AreEqual(1, counter.Count, "undoing the follow-up leaves its cause");
        Assert.AreEqual("anonymous", profile.UserName);

        history.Undo();
        Assert.AreEqual(0, counter.Count);
    }

    private sealed class RecordingDevTools : IBlexDevTools
    {
        public List<string> Sent { get; } = [];

        public void Init(System.Text.Json.Nodes.JsonObject globalState) { }

        public void Send(string actionName, System.Text.Json.Nodes.JsonObject globalState) => Sent.Add(actionName);
    }

    [TestMethod]
    public void A_bridge_going_away_does_not_disconnect_the_one_connected_after_it()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        var first = new RecordingDevTools();
        var second = new RecordingDevTools();
        manager.ConnectDevTools(first);
        manager.ConnectDevTools(second);

        manager.DisconnectDevTools(first);
        counter.Increment();

        Assert.AreSequenceEqual(["counter/Increment"], second.Sent);

        manager.DisconnectDevTools(second);
        counter.Increment();
        Assert.HasCount(1, second.Sent);
    }

    [TestMethod]
    public async Task A_connector_disposed_while_connecting_closes_the_connection_it_was_handed()
    {
        var js = new FakeJsRuntime();
        var connect = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        js.Module.Returns["connect"] = connect.Task;
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var connector = new BlexReduxDevToolsConnector(js);
        var connecting = connector.ConnectAsync(manager, "Blex");
        await connector.DisposeAsync();
        connect.SetResult(5);
        await connecting;

        Assert.AreEqual(5, js.Module.ArgsOf("disconnect")?[0]);
        Assert.IsTrue(js.Module.Disposed);

        counter.Increment();
        Assert.AreEqual(0, js.Module.CountOf("send"), "the manager was never connected to it");
    }

    [TestMethod]
    public async Task A_connector_disposed_while_a_failing_connect_is_pending_still_releases_the_module()
    {
        var js = new FakeJsRuntime();
        var connect = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        js.Module.Returns["connect"] = connect.Task;
        var manager = new BlexManager();

        var connector = new BlexReduxDevToolsConnector(js);
        var connecting = connector.ConnectAsync(manager, "Blex");
        await connector.DisposeAsync();
        connect.SetException(new Microsoft.JSInterop.JSException("the extension threw"));
        await connecting;

        Assert.IsTrue(js.Module.Disposed);
    }

    [TestMethod]
    public async Task Two_actions_failing_with_one_exception_instance_are_both_reported()
    {
        var manager = new BlexManager();
        var store = new SharedFailureStore();
        manager.Register(store);
        var failures = new List<BlexActionErrorContext>();
        manager.ActionFailed += failures.Add;

        var shared = new TaskCompletionSource();
        store.Shared = shared.Task;
        var prices = store.LoadPrices();
        var catalog = store.LoadCatalog();
        shared.SetException(new InvalidOperationException("down"));
        await prices;
        await catalog;

        Assert.AreSequenceEqual(["shared/LoadPrices", "shared/LoadCatalog"], failures.Select(f => f.QualifiedName).ToList());
        Assert.AreSame(failures[0].Exception, failures[1].Exception);
    }

    [TestMethod]
    public void A_nested_failure_is_still_reported_once_by_the_action_that_threw()
    {
        var manager = new BlexManager();
        var store = new SharedFailureStore();
        manager.Register(store);
        var failures = new List<BlexActionErrorContext>();
        manager.ActionFailed += failures.Add;

        Assert.ThrowsExactly<InvalidOperationException>(store.Outer);
        Assert.ThrowsExactly<InvalidOperationException>(store.Outer);

        Assert.AreSequenceEqual(["shared/InnerThrows", "shared/InnerThrows"], failures.Select(f => f.QualifiedName).ToList());
    }

    [TestMethod]
    public void A_subscription_disposed_by_an_earlier_handler_does_not_run_for_that_action()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        IDisposable later = null!;
        IDisposable laterSelector = null!;
        var calls = 0;
        var selections = 0;

        using var first = manager.Subscribe(_ =>
        {
            later.Dispose();
            laterSelector.Dispose();
        });
        later = manager.Subscribe(_ => calls++);
        laterSelector = manager.Subscribe(() => counter.Count, _ => selections++);

        counter.Increment();

        Assert.AreEqual(0, calls);
        Assert.AreEqual(0, selections);
    }
}
