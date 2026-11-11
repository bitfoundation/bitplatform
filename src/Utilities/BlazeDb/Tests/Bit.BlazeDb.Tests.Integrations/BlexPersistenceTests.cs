using Bit.BlazeDb.Blex;
using Bit.BlazeDb.Storage;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazeDb.Tests.Integrations;

[BlexStore(Name = "settings", Persist = true)]
public partial class SettingsStore
{
    [BlexState] private string _theme = "light";
    [BlexState] private int _fontSize = 14;

    [BlexAction]
    private void OnSetTheme(string theme) => Theme = theme;
}

[TestClass]
public class BlexPersistenceTests
{
    private static BlazeDbDatabaseOptions Options(BlazeDbInMemoryStorage storage, bool readOnly = false) =>
        new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1), ReadOnly = readOnly }.AddBlexState();

    [TestMethod]
    public async Task Values_Are_Stored_Removed_And_Read_Back_After_A_Restart()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            await using var blex = new BlazeDbBlexStorage(db);
            Assert.IsNull(await blex.GetAsync("blex:settings"));
            await blex.SetAsync("blex:settings", "{\"Theme\":\"dark\"}");
            await blex.SetAsync("blex:other", "x");
            await blex.RemoveAsync("blex:other");
            Assert.AreEqual("{\"Theme\":\"dark\"}", await blex.GetAsync("blex:settings"));
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        await using var again = new BlazeDbBlexStorage(reopened);
        Assert.AreEqual("{\"Theme\":\"dark\"}", await again.GetAsync("blex:settings"));
        Assert.IsNull(await again.GetAsync("blex:other"));
    }

    [TestMethod]
    public async Task A_Save_Is_On_Storage_When_It_Completes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var db = await BlazeDbDatabase.OpenAsync(Options(storage));
        await using var blex = new BlazeDbBlexStorage(db);
        await blex.SetAsync("blex:settings", "saved");

        // No flush and no dispose: what storage holds now is what a closed tab would leave.
        await using var crashed = await BlazeDbDatabase.OpenAsync(Options(storage.Clone()));
        Assert.AreEqual("saved", crashed.GetTable(BlazeDbBlexEntry.Table).Get("blex:settings")!.Value);
    }

    [TestMethod]
    public async Task An_Unchanged_Value_Is_Not_Written_Again()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBlexState());
        await using var blex = new BlazeDbBlexStorage(db);
        await blex.SetAsync("k", "v");
        var version = db.GetTable(BlazeDbBlexEntry.Table).Version;
        await blex.SetAsync("k", "v");
        Assert.AreEqual(version, db.GetTable(BlazeDbBlexEntry.Table).Version);
    }

    [TestMethod]
    public async Task A_Database_Without_The_Table_Is_Reported_With_The_Fix()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());
        await using var blex = new BlazeDbBlexStorage(db);
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await blex.GetAsync("k"));
        StringAssert.Contains(ex.Message, "AddBlexState()");
    }

    [TestMethod]
    public async Task A_Database_That_Fails_To_Open_Is_Opened_Again_On_The_Next_Use()
    {
        var attempts = 0;
        await using var blex = new BlazeDbBlexStorage(async _ =>
        {
            if (++attempts == 1)
            {
                throw new IOException("not yet");
            }
            return await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBlexState());
        }) { FlushOnSave = false };
        await Assert.ThrowsExactlyAsync<IOException>(async () => await blex.GetAsync("k"));
        Assert.IsNull(await blex.GetAsync("k"));
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public async Task A_Replica_Reload_Is_Reported_As_An_External_Change_And_Its_Own_Writes_Are_Not()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writerDb = await BlazeDbDatabase.OpenAsync(Options(storage));
        await using var writer = new BlazeDbBlexStorage(writerDb);
        var writerChanges = 0;
        writer.ExternalChange += _ => writerChanges++;
        await writer.StartWatchingAsync();
        await writer.SetAsync("blex:settings", "one");

        await using var replicaDb = await BlazeDbDatabase.OpenAsync(Options(storage, readOnly: true));
        await using var replica = new BlazeDbBlexStorage(replicaDb);
        var keys = new List<string?>();
        replica.ExternalChange += keys.Add;
        await replica.StartWatchingAsync();

        await writer.SetAsync("blex:settings", "two");
        await writerDb.CheckpointAsync();
        await replicaDb.ReloadAsync();

        Assert.AreEqual(0, writerChanges);
        CollectionAssert.AreEqual(new string?[] { null }, keys);
        Assert.AreEqual("two", await replica.GetAsync("blex:settings"));
        // The writer tab owns the database: a replica cannot save.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await replica.SetAsync("blex:settings", "three"));
    }

    [TestMethod]
    public async Task A_Write_By_Another_Instance_Over_The_Same_Database_Is_Reported_By_Key()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBlexState());
        await using var first = new BlazeDbBlexStorage(db);
        await using var second = new BlazeDbBlexStorage(db);
        var heardByFirst = new List<string?>();
        var heardBySecond = new List<string?>();
        first.ExternalChange += heardByFirst.Add;
        second.ExternalChange += heardBySecond.Add;
        await first.StartWatchingAsync();
        await second.StartWatchingAsync();

        await second.SetAsync("blex:settings", "from second");
        await second.RemoveAsync("blex:settings");

        CollectionAssert.AreEqual(new string?[] { "blex:settings", "blex:settings" }, heardByFirst);
        Assert.IsEmpty(heardBySecond);
    }

    [TestMethod]
    public async Task A_Persisted_Store_Is_Restored_From_The_Database_Through_Blex()
    {
        var storage = new BlazeDbInMemoryStorage();
        var db = await BlazeDbDatabase.OpenAsync(Options(storage));
        await using (var services = Services(db))
        {
            var persistor = services.GetRequiredService<BlexStatePersistor>();
            await persistor.StartAsync();
            services.GetRequiredService<SettingsStore>().SetTheme("dark");
            await persistor.FlushAsync();
        }
        await db.DisposeAsync();

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        await using var restored = Services(reopened);
        var settings = restored.GetRequiredService<SettingsStore>();
        await restored.GetRequiredService<BlexStatePersistor>().StartAsync();
        Assert.AreEqual("dark", settings.Theme);
        Assert.AreEqual(14, settings.FontSize);
    }

    private static ServiceProvider Services(BlazeDbDatabase db)
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices(lifetime: ServiceLifetime.Singleton);
        services.AddBitBlexStore<SettingsStore>(ServiceLifetime.Singleton);
        services.AddBitBlexBlazeDbPersistence((_, _) => ValueTask.FromResult(db), lifetime: ServiceLifetime.Singleton);
        return services.BuildServiceProvider();
    }
}
