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
    public void Payloads_show_scalars_hide_objects_and_redact_credentials()
    {
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl";
        Assert.IsNull(BevBlexStateProvider.Payload([]));
        Assert.AreEqual("id=42, when=2026-01-02, item=BlexIntegrationTests", BevBlexStateProvider.Payload([new("id", 42), new("when", new DateOnly(2026, 1, 2)), new("item", this)]));
        Assert.AreEqual("note=«redacted»", BevBlexStateProvider.Payload([new("note", token)]));
        Assert.AreEqual("apiKey=«redacted»", BevBlexStateProvider.Payload([new("apiKey", "abc")]));
    }
}
