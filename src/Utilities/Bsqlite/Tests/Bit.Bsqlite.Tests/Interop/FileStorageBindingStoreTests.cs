using Bit.Bsqlite.Storage.Tests;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>The bindings a Blazor Hybrid app keeps in a file next to its databases.</summary>
[TestClass]
public sealed class FileStorageBindingStoreTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private readonly TemporaryDirectory _root = new();

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();

    private string FilePath => Path.Combine(_root.Path, BsqliteFileStorageBindingStore.FileName);

    [TestMethod]
    public async Task Bindings_SurviveTheStore()
    {
        await new BsqliteFileStorageBindingStore(FilePath).SetProviderNameAsync("data/app.db", "file-system", Ct);

        Assert.AreEqual("file-system", await new BsqliteFileStorageBindingStore(FilePath).GetProviderNameAsync("data/app.db", Ct));
    }

    /// <summary>An empty file in the databases' folder would read as a database that was never written.</summary>
    [TestMethod]
    public async Task RemovingTheLastBinding_RemovesTheFile()
    {
        var store = new BsqliteFileStorageBindingStore(FilePath);
        await store.SetProviderNameAsync("app.db", "file-system", Ct);

        await store.RemoveAsync("app.db", Ct);

        Assert.IsFalse(File.Exists(FilePath));
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// A binding the app left in its web view's localStorage, from when it ran Bsqlite there, is found - and kept in
    /// the file, so the next open finds it even before the web view is up.
    /// </summary>
    [TestMethod]
    public async Task BindingLeftInTheWebView_IsFoundAndKept()
    {
        var webView = new BsqliteInMemoryStorageBindingStore();
        await webView.SetProviderNameAsync("app.db", "opfs", Ct);

        Assert.AreEqual("opfs", await new BsqliteFileStorageBindingStore(FilePath, webView).GetProviderNameAsync("app.db", Ct));
        Assert.AreEqual("opfs", await new BsqliteFileStorageBindingStore(FilePath).GetProviderNameAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task BindingInTheFile_OutranksTheWebViews()
    {
        var webView = new BsqliteInMemoryStorageBindingStore();
        await webView.SetProviderNameAsync("app.db", "opfs", Ct);
        var store = new BsqliteFileStorageBindingStore(FilePath, webView);
        await store.SetProviderNameAsync("app.db", "file-system", Ct);

        Assert.AreEqual("file-system", await store.GetProviderNameAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task WebViewThatCannotAnswerYet_HoldsNoBinding()
    {
        var store = new BsqliteFileStorageBindingStore(FilePath, new UnreachableStore());

        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>What a web view's localStorage is before the web view is up.</summary>
    private sealed class UnreachableStore : IBsqliteStorageBindingStore
    {
        public ValueTask<string?> GetProviderNameAsync(string databaseName, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Cannot invoke JavaScript outside of a WebView context.");

        public ValueTask SetProviderNameAsync(string databaseName, string providerName, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Cannot invoke JavaScript outside of a WebView context.");

        public ValueTask RemoveAsync(string databaseName, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Cannot invoke JavaScript outside of a WebView context.");
    }
}
