using System.Text.Json;
using Bit.Bonboarding.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// The default browser store in the situations a real browser puts it in: several people on one device, the same
/// site open in two tabs, and progress kept for one visit only.
/// </summary>
[TestClass]
public class StorageTests
{
    private static BonboardingHarness BrowserStoreHarness(Action<BonboardingOptions>? configure = null)
        => new(configure, configureServices: services => services.AddBitBonboardingStore<BonboardingBrowserStore>());

    private static BonboardingTourDefinition Tour(string id = "shared") => new()
    {
        Id = id,
        Steps = [new BonboardingStepDefinition { Id = "a" }, new BonboardingStepDefinition { Id = "b" }],
    };

    private static string Json(BonboardingRecord record) => JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    // ---- Several people on one device ---------------------------------------------

    [TestMethod]
    public async Task Each_User_Has_Their_Own_Progress()
    {
        var user = "alice";
        await using var harness = BrowserStoreHarness(options => options.StorageScope = (_, _) => ValueTask.FromResult<string?>(user));

        var alice = await harness.Service.StartAsync(Tour());
        await alice.CompleteAsync();

        Assert.IsTrue(harness.Interop.Storage.ContainsKey("bit-bonboarding:@alice/shared"), "The key says whose progress it is.");

        user = "bob";
        Assert.IsFalse(await harness.Service.IsCompletedAsync("shared"), "Bob has not taken the tour.");
        Assert.IsNotNull(await harness.Service.StartOnceAsync(Tour()));

        user = "alice";
        Assert.IsTrue(await harness.Service.IsCompletedAsync("shared"));
    }

    [TestMethod]
    public async Task A_User_Id_That_Starts_Another_Is_Still_Another_User()
    {
        var user = "1";
        await using var harness = BrowserStoreHarness(options => options.StorageScope = (_, _) => ValueTask.FromResult<string?>(user));

        await (await harness.Service.StartAsync(Tour())).CompleteAsync();

        user = "12";
        await harness.Service.ResetAllAsync();

        user = "1";
        Assert.IsTrue(await harness.Service.IsCompletedAsync("shared"), "Resetting user 12 left user 1 alone.");
    }

    [TestMethod]
    public async Task Resetting_The_Device_Leaves_Signed_In_Users_Alone_And_The_Other_Way_Round()
    {
        string? user = null;
        await using var harness = BrowserStoreHarness(options => options.StorageScope = (_, _) => ValueTask.FromResult(user));

        await (await harness.Service.StartAsync(Tour("device"))).CompleteAsync();
        user = "alice";
        await (await harness.Service.StartAsync(Tour("personal"))).CompleteAsync();

        await harness.Service.ResetAllAsync();
        Assert.IsTrue(harness.Interop.Storage.ContainsKey("bit-bonboarding:device"));
        Assert.IsFalse(harness.Interop.Storage.ContainsKey("bit-bonboarding:@alice/personal"));

        user = null;
        await harness.Service.ResetAllAsync();
        Assert.IsFalse(harness.Interop.Storage.ContainsKey("bit-bonboarding:device"));
    }

    [TestMethod]
    public async Task A_Scope_That_Throws_Falls_Back_To_The_Device()
    {
        await using var harness = BrowserStoreHarness(options => options.StorageScope = (_, _) => throw new InvalidOperationException("No auth state."));

        await (await harness.Service.StartAsync(Tour())).CompleteAsync();

        Assert.IsTrue(harness.Interop.Storage.ContainsKey("bit-bonboarding:shared"));
    }

    // ---- The same site in two tabs ---------------------------------------------------

    [TestMethod]
    public async Task A_Tour_Dismissed_In_Another_Tab_Stops_Here_Without_Writing_Over_It()
    {
        await using var harness = BrowserStoreHarness();

        var session = await harness.Service.StartAsync(Tour());
        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);

        var dismissed = new BonboardingRecord { TourId = "shared", Dismissed = true, CurrentStepId = "a", UpdatedAt = DateTimeOffset.UtcNow };
        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:shared", Json(dismissed));

        Assert.AreEqual(BonboardingTourStatus.Cancelled, session.Status);

        var record = await harness.Service.GetRecordAsync("shared");
        Assert.IsTrue(record!.Dismissed, "The other tab's decision stands; cancelling wrote nothing.");
        Assert.IsTrue(await harness.Service.IsClosedAsync("shared"));
    }

    [TestMethod]
    public async Task Progress_Made_In_Another_Tab_Does_Not_Stop_The_Tour_Here()
    {
        await using var harness = BrowserStoreHarness();

        var session = await harness.Service.StartAsync(Tour());

        var progress = new BonboardingRecord { TourId = "shared", CurrentStepId = "b", CurrentIndex = 1 };
        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:shared", Json(progress));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
        Assert.AreEqual("b", (await harness.Service.GetRecordAsync("shared"))!.CurrentStepId, "The cached copy was dropped.");
    }

    [TestMethod]
    public async Task A_Replay_Here_Is_Not_Stopped_By_An_Old_Completion()
    {
        await using var harness = BrowserStoreHarness();

        await (await harness.Service.StartAsync(Tour())).CompleteAsync();
        var replay = await harness.Service.StartAsync(Tour(), new BonboardingStartOptions { Force = true });

        // The other tab writes progress of its own; the completion it carries is the old, sticky one.
        var old = (await harness.Service.GetRecordAsync("shared"))! with { CurrentStepId = "b", Dismissed = false };
        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:shared", Json(old));

        Assert.AreEqual(BonboardingTourStatus.Running, replay.Status);
    }

    [TestMethod]
    public async Task A_Checklist_Item_Ticked_In_Another_Tab_Is_Ticked_Here()
    {
        await using var harness = BrowserStoreHarness();

        var raised = new List<string>();
        harness.Services.GetRequiredService<BonboardingGuidanceRegistry>().ChecklistChanged += (_, id) => raised.Add(id);

        // Read once, so a stale copy would be cached.
        Assert.IsEmpty(await harness.Service.GetCompletedChecklistItemsAsync("start"));

        var ticked = new BonboardingRecord { TourId = "checklist:start", Data = new Dictionary<string, string> { ["invite"] = "2026-10-25T09:00:00Z" } };
        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:checklist:start", Json(ticked));

        CollectionAssert.AreEqual(new[] { "start" }, raised);
        CollectionAssert.AreEqual(new[] { "invite" }, (await harness.Service.GetCompletedChecklistItemsAsync("start")).ToArray());
    }

    [TestMethod]
    public async Task A_Storage_Cleared_In_Another_Tab_Is_Forgotten_Here()
    {
        await using var harness = BrowserStoreHarness();

        await (await harness.Service.StartAsync(Tour())).CompleteAsync();
        Assert.IsTrue(await harness.Service.IsCompletedAsync("shared"));

        await harness.Interop.ChangeStorageInAnotherTabAsync(null, null);

        Assert.IsFalse(await harness.Service.IsCompletedAsync("shared"));
    }

    [TestMethod]
    public async Task Another_Users_Change_In_Another_Tab_Is_Not_News_Here()
    {
        await using var harness = BrowserStoreHarness(options => options.StorageScope = (_, _) => ValueTask.FromResult<string?>("alice"));

        var session = await harness.Service.StartAsync(Tour());

        var bobs = new BonboardingRecord { TourId = "shared", Dismissed = true };
        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:@bob/shared", Json(bobs));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }

    [TestMethod]
    public async Task Session_Storage_Is_Not_Watched_Since_Tabs_Do_Not_Share_It()
    {
        await using var harness = BrowserStoreHarness(options => options.StorageLocation = BonboardingStorageLocation.Session);

        var session = await harness.Service.StartAsync(Tour());

        await harness.Interop.ChangeStorageInAnotherTabAsync("bit-bonboarding:shared", Json(new BonboardingRecord { TourId = "shared", Dismissed = true }));

        Assert.AreEqual(BonboardingTourStatus.Running, session.Status);
    }
}
