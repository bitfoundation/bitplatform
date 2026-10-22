using System.Text.Json.Nodes;
using Bit.Blex.Testing;

namespace Bit.Blex.Tests;

/// <summary>
/// Actions as data: <see cref="BlexManager.DispatchAsync(string, JsonObject?)"/> fires an action by its
/// qualified name with a JSON payload, through its generated wrapper -- what the DevTools dispatcher
/// and action replay are built on.
/// </summary>
[TestClass]
public class DispatchByNameTests
{
    [TestMethod]
    public async Task An_action_is_dispatched_by_its_qualified_name()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/AddUnits", new JsonObject { ["units"] = 3 });
        await harness.Manager.DispatchAsync("orders/Place", new JsonObject { ["name"] = "desk", ["quantity"] = 1 });

        Assert.AreEqual(4, harness.Store.Units);
        CollectionAssert.AreEqual(new[] { "orders/AddUnits", "orders/Place" }, harness.Log.QualifiedNames.ToArray());

        // The wrapper captured the converted value, not the JSON node it came in as.
        Assert.AreEqual(3, harness.Log.Actions[0].Args[0].Value);
    }

    [TestMethod]
    public async Task Arguments_match_by_name_in_any_order_and_case()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/Place", new JsonObject { ["Quantity"] = 2, ["NAME"] = "chair" });

        CollectionAssert.AreEqual(new[] { "2 x chair" }, harness.Store.Lines);
    }

    [TestMethod]
    public async Task Values_convert_the_way_stored_state_does()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/SetPriority", new JsonObject { ["priority"] = "High" });
        await harness.Manager.DispatchAsync("orders/Apply discount", new JsonObject { ["percent"] = 12.5 });
        await harness.Manager.DispatchAsync("orders/Tag", new JsonObject { ["tags"] = new JsonArray("a", "b") });

        Assert.AreEqual(OrderPriority.High, harness.Store.Priority);
        Assert.AreEqual(12.5m, harness.Store.Discount);
        CollectionAssert.AreEqual(new[] { "a", "b" }, harness.Store.Tags);
    }

    [TestMethod]
    public async Task A_state_assignment_and_ResetState_are_dispatchable_too()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/Set Units", new JsonObject { ["Units"] = 9 });
        Assert.AreEqual(9, harness.Store.Units);

        await harness.Manager.DispatchAsync("orders/ResetState");
        Assert.AreEqual(0, harness.Store.Units);

        CollectionAssert.AreEqual(new[] { "Set Units", "ResetState" }, harness.Log.Names.ToArray());
    }

    [TestMethod]
    public async Task An_async_action_completes_with_the_returned_task()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/ImportAsync", new JsonObject { ["count"] = 6 });

        Assert.AreEqual(6, harness.Store.Units);
    }

    [TestMethod]
    public async Task Recorded_arguments_can_be_dispatched_as_they_are()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        await harness.Manager.DispatchAsync("orders/Place", [new BlexActionArg("name", "pen"), new BlexActionArg("quantity", 5)]);

        Assert.AreEqual(5, harness.Store.Units);
    }

    [TestMethod]
    public async Task A_store_name_may_contain_the_separator()
    {
        using var harness = BlexTestHarness.For<AdminUsersStore>();

        await harness.Manager.DispatchAsync("admin/users/Invite", new JsonObject { ["count"] = 2 });

        Assert.AreEqual(2, harness.Store.Invited);
    }

    [TestMethod]
    public void An_unknown_action_is_an_error_naming_it()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        var unknownAction = Assert.ThrowsExactly<InvalidOperationException>(() => harness.Manager.DispatchAsync("orders/Teleport"));
        StringAssert.Contains(unknownAction.Message, "orders/Teleport");
        Assert.ThrowsExactly<InvalidOperationException>(() => harness.Manager.DispatchAsync("nowhere/AddUnits"));
        Assert.ThrowsExactly<InvalidOperationException>(() => harness.Manager.DispatchAsync("orders"));
    }

    [TestMethod]
    public void A_missing_argument_names_the_parameter()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        var missing = Assert.ThrowsExactly<ArgumentException>(() => harness.Manager.DispatchAsync("orders/Place", new JsonObject { ["name"] = "x" }));
        StringAssert.Contains(missing.Message, "quantity");
        Assert.AreEqual(0, harness.Store.Units);
    }

    [TestMethod]
    public void Every_store_describes_its_actions_its_assignments_and_ResetState()
    {
        var names = new OrderBookStore().ActionDescriptors.Select(a => a.Name).ToArray();

        CollectionAssert.IsSubsetOf(new[] { "Clear", "AddUnits", "Place", "Apply discount", "Sync", "Set Units", "Set Lines", "ResetState" }, names);
        Assert.AreSame(OrderBookStore.Actions.Place, new OrderBookStore().FindAction("Place"));
        Assert.IsNull(new OrderBookStore().FindAction("Teleport"));
    }

    [TestMethod]
    public void The_DevTools_dispatcher_fires_an_action_typed_into_it()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();

        Dispatch(harness.Manager, """{"type":"orders/AddUnits","payload":{"units":4}}""");
        Dispatch(harness.Manager, """{"type":"orders/Place","name":"mug","quantity":2}""");
        Dispatch(harness.Manager, """{"type":"orders/AddUnits","payload":10}""");
        Dispatch(harness.Manager, """{"type":"orders/Clear"}""");
        Dispatch(harness.Manager, """{"type":"orders/AddUnits","payload":{"units":1}}""");

        Assert.AreEqual(1, harness.Store.Units);
        CollectionAssert.AreEqual(new[] { "AddUnits", "Place", "AddUnits", "Clear", "AddUnits" }, harness.Log.Names.ToArray());
    }

    [TestMethod]
    public async Task Anything_wrong_with_a_dispatched_action_is_reported_not_thrown()
    {
        using var harness = BlexTestHarness.For<OrderBookStore>();
        var errors = new List<BlexError>();
        var allReported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Manager.OnError = error =>
        {
            lock (errors)
            {
                errors.Add(error);
                if (errors.Count == 6)
                    allReported.TrySetResult();
            }
        };

        Dispatch(harness.Manager, "{ type: 'orders/Clear' }");                         // not JSON
        Dispatch(harness.Manager, """{"payload":{}}""");                              // no type
        Dispatch(harness.Manager, """{"type":"orders/Teleport"}""");                  // unknown
        Dispatch(harness.Manager, """{"type":"orders/Place","payload":{"name":1}}""");  // missing argument
        Dispatch(harness.Manager, """{"type":"orders/Fail","payload":"boom"}""");     // the action throws
        Dispatch(harness.Manager, """{"type":"orders/FailLater","payload":"later"}"""); // ...asynchronously

        await allReported.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(errors.All(e => e.Source == "devtools"));
        StringAssert.Contains(errors[2].Exception.Message, "orders/Teleport");
    }

    [TestMethod]
    public void The_DevTools_dispatcher_hands_a_one_object_action_its_payload_as_the_argument()
    {
        using var harness = BlexTestHarness.For<ShipmentStore>();
        var errors = new List<BlexError>();
        harness.Manager.OnError = errors.Add;

        // Redux style: the payload is the Parcel itself...
        Dispatch(harness.Manager, """{"type":"shipments/Ship","payload":{"Name":"mug","Quantity":2}}""");
        // ...and naming the parameter still works.
        Dispatch(harness.Manager, """{"type":"shipments/Ship","payload":{"parcel":{"Name":"pen","Quantity":1}}}""");

        Assert.IsEmpty(errors, string.Join("; ", errors.Select(e => e.Exception.Message)));
        CollectionAssert.AreEqual(new[] { "2 x mug", "1 x pen" }, harness.Store.Shipped);
    }

    [TestMethod]
    public async Task An_optional_argument_left_out_takes_its_declared_default()
    {
        using var harness = BlexTestHarness.For<ShipmentStore>();

        await harness.Manager.DispatchAsync("shipments/Bump");
        await harness.Manager.DispatchAsync("shipments/Bump", new JsonObject { ["amount"] = 5 });
        Assert.AreEqual(6, harness.Store.Bumps);

        await harness.Manager.DispatchAsync("shipments/Annotate", new JsonObject { ["note"] = "fragile" });
        Assert.AreEqual("#fragile", harness.Store.Note);

        await harness.Manager.DispatchAsync("shipments/Annotate");
        Assert.IsNull(harness.Store.Note);
    }

    [TestMethod]
    public void The_DevTools_dispatcher_fills_in_optional_arguments_too()
    {
        using var harness = BlexTestHarness.For<ShipmentStore>();

        Dispatch(harness.Manager, """{"type":"shipments/Bump"}""");
        Dispatch(harness.Manager, """{"type":"shipments/Bump","payload":{}}""");
        Dispatch(harness.Manager, """{"type":"shipments/Bump","payload":3}""");

        Assert.AreEqual(5, harness.Store.Bumps);
    }

    [TestMethod]
    public void A_payload_read_by_hand_fills_in_the_default_too()
    {
        using var harness = BlexTestHarness.For<ShipmentStore>();
        harness.Store.Bump(4);

        Assert.AreEqual(4, harness.Log.PayloadsOf(ShipmentStore.Actions.Bump).Single());
        Assert.AreEqual(1, ShipmentStore.Actions.Bump.ReadPayload([]));
        Assert.AreEqual(((string?)null, "#"), ShipmentStore.Actions.Annotate.ReadPayload([]));
    }

    private static void Dispatch(BlexManager manager, string typed)
        => manager.HandleDevToolsMessage(new JsonObject { ["type"] = "ACTION", ["payload"] = typed }.ToJsonString());
}
