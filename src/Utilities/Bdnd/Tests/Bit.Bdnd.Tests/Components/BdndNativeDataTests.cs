using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests.Components;

/// <summary>
/// The data of native drags: dropped files' contents streamed from the module, their metadata, the file types and
/// file limit a target applies, and the data a draggable puts on its native drags for destinations outside the page.
/// </summary>
[TestClass]
public class BdndNativeDataTests : BdndComponentTestBase
{
    private readonly List<BdndNativeDropEventArgs> _drops = [];

    private IRenderedComponent<BdndDropTarget<object>> RenderTarget(Action<ComponentParameterCollectionBuilder<BdndDropTarget<object>>>? parameters = null)
        => Render<BdndDropTarget<object>>(p =>
        {
            p.AddCascadingValue(Manager);
            p.Add(x => x.Accept, BdndNativeTypes.File);
            p.Add(x => x.OnNativeDrop, _drops.Add);
            parameters?.Invoke(p);
        });

    private static Task DropFilesAsync(IRenderedComponent<BdndDropTarget<object>> target, string? dropId, int total, params BdndNativeFile[] files)
        => target.InvokeAsync(() => target.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["Files"],
            Files = files,
            DropId = dropId,
            TotalFiles = total,
        }));

    [TestMethod]
    public void TheTargetTellsTheScriptItsFileTypesFolderExpansionAndLimit()
    {
        RenderTarget(p => p
            .Add(x => x.AcceptFileTypes, ["image/*", " .pdf ", ""])
            .Add(x => x.ExpandDirectories, true)
            .Add(x => x.MaxFiles, 3));

        var options = TargetRegistrations.Last();
        Assert.AreSequenceEqual(new[] { "image/*", ".pdf" }, options.FileTypes);
        Assert.IsTrue(options.ExpandDirectories);
        Assert.AreEqual(3, options.MaxFiles);
    }

    [TestMethod]
    public void UnchangedFileTypesAreNotPushedAgain()
    {
        var cut = RenderTarget(p => p.Add(x => x.AcceptFileTypes, ["image/*"]));

        cut.Render(p => p.Add(x => x.AcceptFileTypes, ["image/*"]));

        Assert.IsEmpty(TargetUpdates);
    }

    [TestMethod]
    public async Task ADroppedFileCarriesItsMetadataAndTheTotal()
    {
        var cut = RenderTarget();
        var modified = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

        await DropFilesAsync(cut, "7", 5, new BdndNativeFile
        {
            Name = "beach.jpg",
            Size = 1200,
            Type = "image/jpeg",
            LastModified = modified,
            RelativePath = "photos/beach.jpg",
        });

        var drop = Assert.ContainsSingle(_drops);
        var file = Assert.ContainsSingle(drop.Files);
        Assert.AreEqual("photos/beach.jpg", file.RelativePath);
        Assert.AreEqual(modified, file.LastModified);
        Assert.IsTrue(file.CanRead);
        Assert.AreEqual(5, drop.TotalFileCount);
    }

    [TestMethod]
    public async Task AFileIsStreamedFromTheModule()
    {
        var cut = RenderTarget();
        var content = "hello"u8.ToArray();
        Module.Setup<IJSStreamReference>("readNativeFile", i => (string)i.Arguments[0]! == "7" && (int)i.Arguments[1]! == 1)
            .SetResult(new FakeStreamReference(content));
        await DropFilesAsync(cut, "7", 2, new BdndNativeFile { Name = "a.txt", Size = 1 }, new BdndNativeFile { Name = "b.txt", Size = 5 });

        await using var stream = await _drops[0].Files[1].OpenReadStreamAsync();
        using var reader = new StreamReader(stream);

        Assert.AreEqual("hello", await reader.ReadToEndAsync());
    }

    [TestMethod]
    public async Task AFileLargerThanAllowedIsRefusedBeforeAnythingIsRead()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, "7", 1, new BdndNativeFile { Name = "big.bin", Size = 600_000 });

        await Assert.ThrowsExactlyAsync<IOException>(() => _drops[0].Files[0].OpenReadStreamAsync());
        Assert.IsEmpty(Module.Invocations["readNativeFile"]);

        // with a larger limit it is read
        Module.Setup<IJSStreamReference>("readNativeFile", _ => true).SetResult(new FakeStreamReference(new byte[3]));
        await using var stream = await _drops[0].Files[0].OpenReadStreamAsync(maxAllowedSize: 1_000_000);
        Assert.IsNotNull(stream);
    }

    [TestMethod]
    public async Task AFolderCannotBeRead()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, "7", 1, new BdndNativeFile { Name = "photos", IsDirectory = true });

        var file = _drops[0].Files[0];
        Assert.IsFalse(file.CanRead);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => file.OpenReadStreamAsync());
    }

    [TestMethod]
    public async Task AFileTheModuleNoLongerKeepsCannotBeRead()
    {
        var cut = RenderTarget();
        Module.Setup<IJSStreamReference>("readNativeFile", _ => true).SetException(new JSException("The file is no longer available"));
        await DropFilesAsync(cut, "7", 1, new BdndNativeFile { Name = "a.txt", Size = 1 });

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _drops[0].Files[0].OpenReadStreamAsync());
        StringAssert.Contains(error.Message, "last drop");
    }

    [TestMethod]
    public async Task FilesOfADropWithoutAnIdCannotBeRead()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, null, 1, new BdndNativeFile { Name = "a.txt", Size = 1 });

        Assert.IsFalse(_drops[0].Files[0].CanRead);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _drops[0].Files[0].OpenReadStreamAsync());
    }

    // ---- data for destinations outside the page ----

    private IRenderedComponent<BdndDraggable<string>> RenderSource(IReadOnlyDictionary<string, string>? data)
        => Render<BdndDraggable<string>>(p => p
            .AddCascadingValue(Manager)
            .Add(x => x.Type, "card")
            .Add(x => x.Item, "a")
            .Add(x => x.NativeData, data));

    [TestMethod]
    public void TheSourceTellsTheScriptItsNativeData()
    {
        RenderSource(new Dictionary<string, string> { ["text/plain"] = "Write the docs" });

        var data = SourceRegistrations.Last().NativeData;
        Assert.IsNotNull(data);
        Assert.AreEqual("Write the docs", data["text/plain"]);
    }

    [TestMethod]
    public void TheSameNativeDataInANewDictionaryIsNotPushedAgain()
    {
        var cut = RenderSource(new Dictionary<string, string> { ["text/plain"] = "a" });

        cut.Render(p => p.Add(x => x.NativeData, new Dictionary<string, string> { ["text/plain"] = "a" }));
        Assert.IsEmpty(SourceUpdates);

        cut.Render(p => p.Add(x => x.NativeData, new Dictionary<string, string> { ["text/plain"] = "b" }));
        Assert.AreEqual("b", Assert.ContainsSingle(SourceUpdates).NativeData!["text/plain"]);
    }

    [TestMethod]
    public void NoNativeDataSendsNone()
    {
        RenderSource(new Dictionary<string, string>());

        Assert.IsNull(SourceRegistrations.Last().NativeData);
    }

    [TestMethod]
    public async Task TheFilesOfTheLastDropAreNoLongerReadableOnceTheNextDropArrives()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, "1", 1, new BdndNativeFile { Name = "first.txt", Size = 1 });
        var first = _drops[0].Files[0];
        Assert.IsTrue(first.CanRead);

        await DropFilesAsync(cut, "2", 1, new BdndNativeFile { Name = "second.txt", Size = 1 });

        Assert.IsFalse(first.CanRead);
        Assert.IsTrue(_drops[1].Files[0].CanRead);
        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => first.OpenReadStreamAsync());
        StringAssert.Contains(error.Message, "no longer available");
        Assert.IsEmpty(Module.Invocations["readNativeFile"], "nothing was asked of the module");
    }

    [TestMethod]
    public async Task TheFilesOfTheLastDropAreNoLongerReadableOnceTheTargetIsDisposed()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, "1", 1, new BdndNativeFile { Name = "a.txt", Size = 1 });
        var file = _drops[0].Files[0];

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        Assert.IsFalse(file.CanRead);
    }

    [TestMethod]
    public async Task TheRestOfAManyFileDropIsReadFromTheModulePageByPage()
    {
        var cut = RenderTarget();
        Module.Setup<BdndNativeFile[]>("nativeDropFiles", i => (string)i.Arguments[0]! == "9" && (int)i.Arguments[1]! == 1)
            .SetResult([new BdndNativeFile { Name = "b" }, new BdndNativeFile { Name = "c" }]);
        Module.Setup<BdndNativeFile[]>("nativeDropFiles", i => (string)i.Arguments[0]! == "9" && (int)i.Arguments[1]! == 3)
            .SetResult([new BdndNativeFile { Name = "d" }]);

        await cut.InvokeAsync(() => cut.Instance.HandleNativeDrop(new BdndNativeDropPayload
        {
            Types = ["Files"],
            Files = [new BdndNativeFile { Name = "a" }],
            FileCount = 4,
            DropId = "9",
            TotalFiles = 6,
            Incomplete = true,
        }));

        var drop = Assert.ContainsSingle(_drops);
        Assert.AreSequenceEqual(new[] { "a", "b", "c", "d" }, drop.Files.Select(f => f.Name).ToArray());
        Assert.IsTrue(drop.Files.All(f => f.CanRead));
        Assert.AreEqual(6, drop.TotalFileCount);
        Assert.IsTrue(drop.FileListIncomplete);
    }

    [TestMethod]
    public async Task DisposingTheStreamReleasesTheModulesReference()
    {
        var cut = RenderTarget();
        var reference = new FakeStreamReference("x"u8.ToArray());
        Module.Setup<IJSStreamReference>("readNativeFile", _ => true).SetResult(reference);
        await DropFilesAsync(cut, "7", 1, new BdndNativeFile { Name = "a.txt", Size = 1 });

        await (await _drops[0].Files[0].OpenReadStreamAsync()).DisposeAsync();

        Assert.IsTrue(reference.Disposed);
    }

    [TestMethod]
    public async Task ACancelledReadAsksTheModuleForNothing()
    {
        var cut = RenderTarget();
        await DropFilesAsync(cut, "7", 1, new BdndNativeFile { Name = "a.txt", Size = 1 });

        await Assert.ThrowsAsync<OperationCanceledException>(() => _drops[0].Files[0].OpenReadStreamAsync(cancellationToken: new CancellationToken(true)));

        Assert.IsEmpty(Module.Invocations["readNativeFile"]);
    }

    private sealed class FakeStreamReference(byte[] content) : IJSStreamReference
    {
        public bool Disposed { get; private set; }

        public long Length => content.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Stream>(new MemoryStream(content));

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
