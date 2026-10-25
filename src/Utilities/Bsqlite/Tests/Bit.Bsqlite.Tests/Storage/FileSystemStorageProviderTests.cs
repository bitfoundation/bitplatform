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
