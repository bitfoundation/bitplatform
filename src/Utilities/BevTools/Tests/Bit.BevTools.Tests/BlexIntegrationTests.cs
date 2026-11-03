using System.Text.Json;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[BlexStore(Name = "cart")]
public partial class BevCartStore
{
    [BlexState] private int _items;

    [BlexState] private string? _coupon;

    [BlexAction] private void OnAdd(int count, string password) => Items += count;

    [BlexAction] private void OnApplyCoupon(string code) => Coupon = code;
}

/// <summary>
/// bit Blex through BevTools' State panel: a change per action with its payload redacted, and restoring an earlier state
/// through Blex's own time travel.
/// </summary>
[TestClass]
public class BlexIntegrationTests
{
    [TestMethod]
    public async Task Each_action_is_a_change_and_an_earlier_state_can_be_restored()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var manager = new BlexManager();
            var store = new BevCartStore();
            manager.Register(store);
            using var provider = new BevBlexStateProvider(manager);
            session.State.Register(provider);

            store.Add(2, "hunter2");
            store.ApplyCoupon("SPRING");
            store.Add(3, "hunter2");

            var entry = session.State.Entries.Single(e => e.Name == "Blex");
            Assert.AreEqual("1 store: cart", entry.Description);
            var changes = entry.Changes;
            Assert.HasCount(3, changes);
            Assert.AreEqual("cart/Add", changes[0].Action);
            Assert.AreEqual("count=2, password=«redacted»", changes[0].Detail);
            Assert.AreEqual("cart/ApplyCoupon", changes[1].Action);
            Assert.IsNotNull(changes[0].RestoreToken);
            Assert.Contains(d => d.Path.EndsWith("items", StringComparison.OrdinalIgnoreCase) && d.After == "2", changes[0].Changes, string.Join("; ", changes[0].Changes.Select(c => c.Path + "=" + c.After)));

            Assert.AreEqual("Restored Blex to after cart/Add.", await session.State.RestoreAsync(changes[0].Id));
            Assert.AreEqual(2, store.Items);
            Assert.IsNull(store.Coupon);
            var restored = session.State.Entries.Single(e => e.Name == "Blex").Changes[^1];
            Assert.AreEqual("(state restored)", restored.Action);
            Assert.IsNotNull(restored.RestoreToken, "a restored state can be gone back to as well");

            var snapshot = (JsonElement)provider.GetSnapshot()!;
            Assert.AreEqual(2, snapshot.GetProperty("cart").EnumerateObject().First(p => p.Name.Equals("items", StringComparison.OrdinalIgnoreCase)).Value.GetInt32());
        }
    }

    [TestMethod]
    public void Registered_through_dependency_injection_it_is_in_the_state_panel()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices();
        services.AddBitBlexStore<BevCartStore>();
        services.AddBitBevToolsServices(o => o.Enabled = true);
        services.AddBevToolsBlex();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();
        session.EnsureActivated();
        scope.ServiceProvider.GetRequiredService<BevCartStore>().Add(1, "x");

        var entry = session.State.Entries.Single(e => e.Name == "Blex");
        Assert.AreEqual("cart/Add", Assert.ContainsSingle(entry.Changes).Action);
    }

    [TestMethod]
    public void An_actions_arguments_are_shown_at_the_sessions_value_access()
    {
        string?[] Details(BevValueAccess access)
        {
            var (session, scope) = TestHelpers.CreateSession(o => o.ValueAccess = access);
            using (scope)
            {
                var manager = new BlexManager();
                var store = new BevCartStore();
                manager.Register(store);
                using var provider = new BevBlexStateProvider(manager);
                session.State.Register(provider);
                store.Add(2, "hunter2");
                store.ApplyCoupon("SPRING");
                return session.State.Entries.Single(e => e.Name == "Blex").Changes.Select(c => c.Detail).ToArray();
            }
        }

        CollectionAssert.AreEqual(new[] { "count=2, password=«redacted»", "code=\"SPRING\"" }, Details(BevValueAccess.Full));
        CollectionAssert.AreEqual(new[] { "count=2, password=«redacted»", "code=\"SPRING\"" }, Details(BevValueAccess.Shallow));
        CollectionAssert.AreEqual(new[] { "count, password", "code" }, Details(BevValueAccess.None), "names only, as for every other value");
    }

    [TestMethod]
    public async Task A_store_blex_could_not_restore_fails_the_restore_and_the_apps_handler_still_hears_of_it()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var manager = new BlexManager();
            var heard = new List<BlexError>();
            manager.OnError = heard.Add;
            var store = new BevCartStore();
            manager.Register(store);
            using var provider = new BevBlexStateProvider(manager);
            session.State.Register(provider);
            store.Add(1, "x");
            var change = Assert.ContainsSingle(session.State.Entries.Single().Changes);

            // A state the store's slice cannot be read back from, under the token of a change BevTools recorded.
            var states = (System.Collections.IDictionary)typeof(BevBlexStateProvider).GetField("_states", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var broken = manager.CaptureGlobalState();
            var cart = (System.Text.Json.Nodes.JsonObject)broken["cart"]!;
            cart[cart.First(p => p.Key.Equals("items", StringComparison.OrdinalIgnoreCase)).Key] = "not a number";
            states[change.RestoreToken!] = JsonSerializer.SerializeToElement(broken);

            var outcome = await session.State.RestoreAsync(change.Id);

            StringAssert.StartsWith(outcome, "Blex could not restore it: InvalidOperationException: Blex could not restore every store: cart");
            Assert.Contains(e => e.Source == "restore", heard, "the app's own OnError still gets it");
            Assert.AreSame(heard, manager.OnError!.Target, "and is put back as it was");
            Assert.DoesNotContain(e => e.Category == "state-restore", session.Timeline.Snapshot(), "nothing restored is said to be");
        }
    }

    [TestMethod]
    public void Registered_through_dependency_injection_it_keeps_a_state_for_every_change_the_history_keeps()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices();
        services.AddBitBlexStore<BevCartStore>();
        services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.MaxStateChangesPerProvider = 150;
        });
        services.AddBevToolsBlex();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var blex = scope.ServiceProvider.GetServices<IBevStateProvider>().OfType<BevBlexStateProvider>().Single();
        Assert.AreEqual(150, typeof(BevBlexStateProvider).GetField("_maxStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(blex));
    }

    [TestMethod]
    public async Task While_capture_is_paused_no_state_is_kept_for_a_restore()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var manager = new BlexManager();
            var store = new BevCartStore();
            manager.Register(store);
            using var provider = new BevBlexStateProvider(manager);
            session.State.Register(provider);

            session.PauseCapture();
            store.Add(1, "x");
            Assert.IsEmpty(session.State.Entries.Single().Changes);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => provider.RestoreAsync("1").AsTask(), "nothing recorded, nothing kept");

            session.ResumeCapture();
            store.Add(1, "x");
            var change = Assert.ContainsSingle(session.State.Entries.Single().Changes);
            store.Add(5, "x");
            Assert.AreEqual("Restored Blex to after cart/Add.", await session.State.RestoreAsync(change.Id));
            Assert.AreEqual(2, store.Items);
        }
    }
}
