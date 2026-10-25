using Bit.Blex.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests;

/// <summary>
/// The generated <c>Actions</c> descriptors: an action named by a symbol rather than a string, with a
/// payload read back as the type the action takes.
/// </summary>
[TestClass]
public class TypedActionTests
{
    [TestMethod]
    public void A_descriptor_names_its_store_its_action_and_its_parameters()
    {
        var place = OrderBookStore.Actions.Place;

        Assert.AreEqual(typeof(OrderBookStore), place.StoreType);
        Assert.AreEqual("Place", place.Name);
        CollectionAssert.AreEqual(new[] { "name", "quantity" }, place.ParameterNames.ToArray());
        Assert.IsEmpty(OrderBookStore.Actions.Clear.ParameterNames);
        Assert.AreEqual("OrderBookStore.Place", place.ToString());
    }

    [TestMethod]
    public void A_display_label_is_the_name_it_dispatches_under()
        => Assert.AreEqual("Apply discount", OrderBookStore.Actions.ApplyDiscount.Name);

    [TestMethod]
    public void Effects_get_descriptors_without_their_cancellation_token()
        => CollectionAssert.AreEqual(new[] { "reason" }, OrderBookStore.Actions.Sync.ParameterNames.ToArray());

    [TestMethod]
    public void Matches_tells_the_action_apart_by_store_type_and_name()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        var counter = harness.Register(new CounterStore());

        var contexts = new List<BlexActionContext>();
        using (harness.Manager.Subscribe(contexts.Add))
        {
            harness.Store.AddUnits(1);
            counter.Add(1);
        }

        Assert.IsTrue(OrderBookStore.Actions.AddUnits.Matches(contexts[0]));
        Assert.IsFalse(OrderBookStore.Actions.AddUnits.Matches(contexts[1]), "Same action name, another store.");
        Assert.IsFalse(OrderBookStore.Actions.Place.Matches(contexts[0]));
    }

    [TestMethod]
    public void A_single_argument_payload_is_the_argument_itself()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        BlexActionContext? seen = null;
        using var sub = harness.Manager.Subscribe(ctx => seen = ctx);

        harness.Store.AddUnits(5);

        Assert.IsTrue(OrderBookStore.Actions.AddUnits.TryGetPayload(seen!, out var units));
        Assert.AreEqual(5, units);
        Assert.AreEqual(5, OrderBookStore.Actions.AddUnits.GetPayload(seen!));
    }

    [TestMethod]
    public void Several_arguments_make_a_tuple_named_after_the_parameters()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        BlexActionContext? seen = null;
        using var sub = harness.Manager.Subscribe(ctx => seen = ctx);

        harness.Store.Place("widget", 3);

        Assert.IsTrue(OrderBookStore.Actions.Place.TryGetPayload(seen!, out var order));
        Assert.AreEqual("widget", order.Name);
        Assert.AreEqual(3, order.Quantity);
    }

    [TestMethod]
    public void A_parameter_that_cannot_name_a_tuple_element_leaves_the_tuple_unnamed()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        BlexActionContext? seen = null;
        using var sub = harness.Manager.Subscribe(ctx => seen = ctx);

        harness.Store.Move("north", 4);

        Assert.IsTrue(OrderBookStore.Actions.Move.TryGetPayload(seen!, out var move));
        Assert.AreEqual(("north", 4), move);
        Assert.AreEqual("north", move.Item1);
    }

    [TestMethod]
    public void TryGetPayload_is_false_for_another_action()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        BlexActionContext? seen = null;
        using var sub = harness.Manager.Subscribe(ctx => seen = ctx);

        harness.Store.AddUnits(5);

        Assert.IsFalse(OrderBookStore.Actions.Place.TryGetPayload(seen!, out _));
        Assert.ThrowsExactly<InvalidOperationException>(() => OrderBookStore.Actions.Place.GetPayload(seen!));
    }

    [TestMethod]
    public void A_failed_action_reports_a_payload_too()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        Assert.ThrowsExactly<InvalidOperationException>(() => harness.Store.Fail("out of stock"));

        var failure = Assert.ContainsSingle(harness.Failures);
        Assert.IsTrue(OrderBookStore.Actions.Fail.TryGetPayload(failure, out var why));
        Assert.AreEqual("out of stock", why);
    }

    [TestMethod]
    public async Task InvokeAsync_dispatches_through_the_generated_wrapper()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await OrderBookStore.Actions.Place.InvokeAsync(harness.Store, ("lamp", 2));
        await OrderBookStore.Actions.Clear.InvokeAsync(harness.Store);
        await OrderBookStore.Actions.AddUnits.InvokeAsync(harness.Store, 7);

        Assert.AreEqual(7, harness.Store.Units);
        CollectionAssert.AreEqual(new[] { "Place", "Clear", "AddUnits" }, harness.Log.Names.ToArray());
    }

    [TestMethod]
    public async Task An_async_action_descriptor_completes_when_the_body_does()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await OrderBookStore.Actions.ImportAsync.InvokeAsync(harness.Store, 4);

        Assert.AreEqual(4, harness.Store.Units);
    }

    [TestMethod]
    public async Task An_effect_descriptor_runs_the_effect_lifecycle()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await OrderBookStore.Actions.Sync.InvokeAsync(harness.Store, "manual");

        Assert.AreEqual("manual", harness.Store.LastSync);
        Assert.IsFalse(harness.Store.SyncIsLoading);
        Assert.IsNull(harness.Store.SyncError);
    }

    [TestMethod]
    public async Task A_params_payload_is_the_array()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await OrderBookStore.Actions.Tag.InvokeAsync(harness.Store, ["new", "rush"]);

        CollectionAssert.AreEqual(new[] { "new", "rush" }, harness.Store.Tags);
        CollectionAssert.AreEqual(new[] { "new", "rush" }, harness.Log.PayloadsOf(OrderBookStore.Actions.Tag)[0]);
    }

    [TestMethod]
    public void InvokeAsync_rejects_a_store_of_another_type()
        => Assert.ThrowsExactly<ArgumentException>(() => OrderBookStore.Actions.AddUnits.InvokeAsync(new CounterStore(), [new BlexActionArg("units", 1)]));

    [TestMethod]
    public void SubscribeToAction_hands_over_the_store_and_the_typed_payload()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        var placed = new List<(OrderBookStore Store, string Name, int Quantity)>();
        var cleared = 0;

        using var onPlace = harness.Manager.SubscribeToAction(OrderBookStore.Actions.Place,
            (store, order) => placed.Add((store, order.Name, order.Quantity)));
        using var onClear = harness.Manager.SubscribeToAction(OrderBookStore.Actions.Clear, _ => cleared++);

        harness.Store.Place("cup", 1);
        harness.Store.AddUnits(9);
        harness.Store.Clear();

        var only = Assert.ContainsSingle(placed);
        Assert.AreSame(harness.Store, only.Store);
        Assert.AreEqual(("cup", 1), (only.Name, only.Quantity));
        Assert.AreEqual(1, cleared);
    }

    [TestMethod]
    public void A_typed_filter_vetoes_only_the_action_it_names()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices(options =>
        {
            options.UseFilter(OrderBookStore.Actions.AddUnits, (_, units) => units <= 10);
            options.UseFilter(OrderBookStore.Actions.Clear, store => store.Units < 100);
        });
        services.AddBitBlexStore<OrderBookStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<OrderBookStore>();

        store.AddUnits(50);   // vetoed
        store.AddUnits(5);
        store.Place("bulk", 200);   // another action: unaffected
        store.Clear();        // vetoed: 205 units

        Assert.AreEqual(205, store.Units);
    }
}
