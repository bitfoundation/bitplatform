using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage.FileSystem;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Storage.Tests;

[TestClass]
public sealed class FileSystemStorageProviderTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private readonly TemporaryDirectory _root = new();

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();

    private BsqliteFileSystemStorageProvider Provider() => new(new() { RootDirectory = _root.Path });

    [TestMethod]
    public void Capabilities_DescribeANativeFile()
    {
        var provider = Provider();
        var capabilities = provider.Capabilities;

        Assert.AreEqual("file-system", provider.Name);
        Assert.AreEqual(BsqliteExecutionContexts.NativeProcess, capabilities.SupportedContexts);
        Assert.IsTrue(capabilities.IsPersistent);
        Assert.IsTrue(capabilities.SupportsMultipleConnections);
        Assert.IsTrue(capabilities.SupportsMultiDatabaseTransactions);
        Assert.IsTrue(capabilities.CanChangePageSize);
        Assert.IsNull(provider.VfsModule);
        Assert.IsInstanceOfType<IBsqliteFileStorageProvider>(provider);
    }

    [TestMethod]
    public void DefaultRoot_IsTheAppsOwnFolder_UnderLocalApplicationData()
    {
        var root = new BsqliteFileSystemStorageProvider().RootDirectory;

        Assert.StartsWith(
            Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify)),
            root);
        Assert.AreEqual("bit-bsqlite", Path.GetFileName(root));
    }

    [TestMethod]
    public void DatabaseName_IsAPathUnderTheRoot()
    {
        var provider = Provider();

        Assert.AreEqual(Path.Combine(provider.RootDirectory, "app.db"), provider.GetDatabasePath("app.db"));
        Assert.AreEqual(
            Path.Combine(provider.RootDirectory, "App_Data", "AppOffline.db"),
            provider.GetDatabasePath("App_Data/AppOffline.db"));
        Assert.AreEqual(
            Path.Combine(provider.RootDirectory, "App_Data", "AppOffline.db"),
            provider.GetDatabasePath("/App_Data\\AppOffline.db"));
    }

    [TestMethod]
    [DataRow("../app.db")]
    [DataRow("data/../../app.db")]
    [DataRow("./app.db")]
    [DataRow("/")]
    public void DatabaseName_CannotLeaveTheRoot(string name)
    {
        var error = Assert.ThrowsExactly<ArgumentException>(() => Provider().GetDatabasePath(name));

        Assert.Contains("relative to", error.Message);
    }

    [TestMethod]
    public void DatabaseName_CannotNameADrive()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Only Windows has drives; elsewhere a leading slash is trimmed like any other.");
        }

        Assert.ThrowsExactly<ArgumentException>(() => Provider().GetDatabasePath(@"C:\elsewhere.db"));
        Assert.ThrowsExactly<ArgumentException>(() => Provider().GetDatabasePath("data/C:/elsewhere.db"));
    }

    /// <summary>
    /// Held to what every platform keeps as written: on Windows a ':' names an alternate data stream of another
    /// file, and a trailing dot or space is dropped, so two names would share one file.
    /// </summary>
    [TestMethod]
    [DataRow("tenant:1.db")]
    [DataRow("data/tenant:1.db")]
    [DataRow("what?.db")]
    [DataRow("all*.db")]
    [DataRow("a|b.db")]
    [DataRow("a\"b.db")]
    [DataRow("a<b>.db")]
    [DataRow("tab\t.db")]
    [DataRow("app.db.")]
    [DataRow("data /app.db")]
    public void DatabaseName_IsAFileNameOnEveryPlatform(string name)
    {
        var error = Assert.ThrowsExactly<ArgumentException>(() => Provider().GetDatabasePath(name));

        Assert.Contains("relative to", error.Message);
    }

    [TestMethod]
    public void Root_CanBeADriveOrTheFileSystemRoot()
    {
        var root = Path.GetPathRoot(_root.Path)!;
        var provider = new BsqliteFileSystemStorageProvider(new() { RootDirectory = root });

        Assert.AreEqual(Path.Combine(root, "app.db"), provider.GetDatabasePath("app.db"));
        Assert.AreEqual(Path.Combine(root, "data", "app.db"), provider.GetDatabasePath("data/app.db"));
    }

    [TestMethod]
    public async Task Probe_IsAvailable_WhereDotNetRunsNatively_AndNamesTheEngine()
    {
        var probe = await Provider().ProbeAsync(Ct);

        Assert.IsTrue(probe.IsAvailable, probe.UnavailableReason);
        Assert.AreEqual(_root.Path, probe.Environment["rootDirectory"]);
        Assert.StartsWith("3.", probe.Environment["sqliteVersion"]);
        Assert.IsTrue(Directory.Exists(_root.Path));
    }

    [TestMethod]
    public async Task List_ReportsDatabasesOnly_WithNestedNamesInForwardSlashes()
    {
        var provider = Provider();
        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);
        await provider.Admin.ImportAsync("App_Data/AppOffline.db", SqliteImage(), Ct);

        // Files a shared root may hold besides databases: a journal, and a file of the app's own.
        await File.WriteAllTextAsync(provider.GetDatabasePath("app.db") + "-journal", "journal", Ct);
        await File.WriteAllTextAsync(Path.Combine(_root.Path, "settings.json"), "{}", Ct);

        CollectionAssert.AreEqual(
            new[] { "App_Data/AppOffline.db", "app.db" },
            (await provider.Admin.ListAsync(Ct)).ToArray());
    }

    /// <summary>
    /// An empty file cannot be told from any other empty file of the app, and the bindings a Blazor Hybrid app keeps
    /// in the root are not a database either.
    /// </summary>
    [TestMethod]
    public async Task List_LeavesOutEmptyFilesAndTheBindings()
    {
        var provider = Provider();
        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);
        await File.WriteAllBytesAsync(Path.Combine(_root.Path, "placeholder.txt"), [], Ct);

        var bindings = new BsqliteFileStorageBindingStore(Path.Combine(_root.Path, BsqliteFileStorageBindingStore.FileName));
        await bindings.SetProviderNameAsync("app.db", "file-system", Ct);
        CollectionAssert.AreEqual(new[] { "app.db" }, (await provider.Admin.ListAsync(Ct)).ToArray());

        await bindings.RemoveAsync("app.db", Ct);
        CollectionAssert.AreEqual(new[] { "app.db" }, (await provider.Admin.ListAsync(Ct)).ToArray());
    }

    [TestMethod]
    public async Task Delete_SucceedsQuietly_WhenTheFolderWasNeverCreated()
    {
        var provider = Provider();

        await provider.Admin.DeleteAsync("App_Data/AppOffline.db", Ct);

        Assert.IsFalse(Directory.Exists(_root.Path));
    }

    /// <summary>
    /// A file a stock EF Core app - Bit.Besql's, on a device - wrote in WAL mode may hold committed rows in its log
    /// alone. Exporting the file without them, and then deleting the source with its log, would lose them.
    /// </summary>
    [TestMethod]
    public async Task Export_FoldsTheWriteAheadLogIntoTheImage()
    {
        var provider = Provider();
        var path = provider.GetDatabasePath("app.db");
        Directory.CreateDirectory(_root.Path);

        await using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(Ct);

            // What a process killed before its last connection closed leaves behind: the log, not checkpointed.
            Assert.AreEqual(SQLitePCL.raw.SQLITE_OK, SQLitePCL.raw.sqlite3_db_config(connection.Handle!, 1006 /* SQLITE_DBCONFIG_NO_CKPT_ON_CLOSE */, 1, out _));

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = WAL; CREATE TABLE t (x); INSERT INTO t VALUES (42);";
            await command.ExecuteNonQueryAsync(Ct);
        }

        Assert.IsTrue(new FileInfo(path + "-wal").Length > 0, "The test needs the rows in the log.");

        var image = await provider.Admin.ExportAsync("app.db", Ct);

        var copy = Path.Combine(_root.Path, "copy.db");
        await File.WriteAllBytesAsync(copy, image, Ct);
        await using var reader = new SqliteConnection($"Data Source={copy};Pooling=False");
        await reader.OpenAsync(Ct);
        await using var query = reader.CreateCommand();
        query.CommandText = "SELECT x FROM t";
        Assert.AreEqual(42L, await query.ExecuteScalarAsync(Ct));
    }

    /// <summary>
    /// Two windows of a Blazor Hybrid app share the file. Replacing or deleting it under the one still open would
    /// send its writes to a file that is no longer the database - or, on Windows, fail half way.
    /// </summary>
    [TestMethod]
    public async Task Admin_WaitsForEveryWindowToCloseTheFile()
    {
        var provider = Provider();
        await provider.Admin.ImportAsync("app.db", ReadOnlyMemory<byte>.Empty, Ct);

        var otherWindow = new BsqliteNativeTransport(provider);
        await otherWindow.OpenAsync("app.db", Ct);

        await Assert.ThrowsExactlyAsync<IOException>(async () => await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct));
        await Assert.ThrowsExactlyAsync<IOException>(async () => await provider.Admin.ExportAsync("app.db", Ct));
        await Assert.ThrowsExactlyAsync<IOException>(async () => await provider.Admin.DeleteAsync("app.db", Ct));
        Assert.IsTrue(await provider.Admin.ExistsAsync("app.db", Ct));

        await otherWindow.DisposeAsync();

        await provider.Admin.DeleteAsync("app.db", Ct);
        Assert.IsFalse(await provider.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>A journal left by a crash would be rolled back into whatever file next takes the name.</summary>
    [TestMethod]
    public async Task Import_RemovesAJournalLeftNextToTheOldFile()
    {
        var provider = Provider();
        var path = provider.GetDatabasePath("app.db");
        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);
        await File.WriteAllTextAsync(path + "-journal", "stale", Ct);

        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);

        Assert.IsFalse(File.Exists(path + "-journal"));
    }

    /// <summary>
    /// The old file's journal and log hold committed writes of its own until the new file has taken the name:
    /// an import that cannot finish leaves the old database as it was, log included.
    /// </summary>
    [TestMethod]
    public async Task Import_ThatCannotTakeTheName_KeepsTheOldFilesLog()
    {
        var provider = Provider();
        var path = provider.GetDatabasePath("app.db");

        // A folder where the file goes: the new file cannot be moved over it, on any platform.
        Directory.CreateDirectory(path);
        await File.WriteAllTextAsync(path + "-wal", "committed", Ct);

        await Assert.ThrowsAsync<Exception>(async () => await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct));

        Assert.AreEqual("committed", await File.ReadAllTextAsync(path + "-wal", Ct));
        Assert.HasCount(1, Directory.GetFiles(_root.Path), "Something was left beside the database.");
    }

    /// <summary>
    /// The file and its journal leave the name together or not at all: a journal left behind would be rolled back
    /// into whatever file next takes the name.
    /// </summary>
    [TestMethod]
    public async Task Delete_ThatCannotMoveTheJournal_LeavesTheFileInPlace()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Only Windows refuses to rename a file another handle holds without sharing it.");
        }

        var provider = Provider();
        var path = provider.GetDatabasePath("app.db");
        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);
        await File.WriteAllTextAsync(path + "-journal", "hot", Ct);

        await using (new FileStream(path + "-journal", FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await Assert.ThrowsAsync<IOException>(async () => await provider.Admin.DeleteAsync("app.db", Ct));
        }

        Assert.IsTrue(await provider.Admin.ExistsAsync("app.db", Ct));
        Assert.AreEqual("hot", await File.ReadAllTextAsync(path + "-journal", Ct));
        Assert.HasCount(2, Directory.GetFiles(_root.Path));
    }

    [TestMethod]
    public async Task List_LeavesOutFilesMovedAside()
    {
        var provider = Provider();
        await provider.Admin.ImportAsync("app.db", SqliteImage(), Ct);
        await File.WriteAllBytesAsync(provider.GetDatabasePath($"app.db.{Guid.NewGuid():N}.aside"), SqliteImage(), Ct);

        CollectionAssert.AreEqual(new[] { "app.db" }, (await provider.Admin.ListAsync(Ct)).ToArray());
    }

    [TestMethod]
    public async Task Delete_RemovesTheFileAndItsJournal()
    {
        var provider = Provider();
        var path = provider.GetDatabasePath("data/app.db");
        await provider.Admin.ImportAsync("data/app.db", SqliteImage(), Ct);
        await File.WriteAllTextAsync(path + "-journal", "stale", Ct);

        await provider.Admin.DeleteAsync("data/app.db", Ct);

        Assert.IsFalse(File.Exists(path));
        Assert.IsFalse(File.Exists(path + "-journal"));
    }

    /// <summary>The files are plain SQLite databases: what Microsoft.Data.Sqlite wrote, the backend exports as is.</summary>
    [TestMethod]
    public async Task Export_IsTheFileMicrosoftDataSqliteWrote()
    {
        var provider = Provider();
        var path = provider.GetDatabasePath("app.db");
        Directory.CreateDirectory(_root.Path);

        await using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(Ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE t (x); INSERT INTO t VALUES (42);";
            await command.ExecuteNonQueryAsync(Ct);
        }

        CollectionAssert.AreEqual(await File.ReadAllBytesAsync(path, Ct), await provider.Admin.ExportAsync("app.db", Ct));
        Assert.IsTrue(await provider.Admin.ExistsAsync("app.db", Ct));
    }

    private static byte[] SqliteImage()
    {
        var image = new byte[4096];
        "SQLite format 3\0"u8.CopyTo(image);
        return image;
    }
}
