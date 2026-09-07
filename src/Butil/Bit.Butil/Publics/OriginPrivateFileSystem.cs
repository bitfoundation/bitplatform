using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system">origin private file system</see>
/// (OPFS): a real file system, private to the origin, that needs no picker and no permission.
/// </summary>
/// <remarks>
/// <see cref="FileSystem"/> wraps the other half of the File System API - the pickers - where every
/// handle is something the user chose and can revoke. This half is the opposite: nothing is shown to
/// the user, nothing is granted, and nothing here is visible outside the origin. It is the place for
/// data your app owns rather than data the user owns - a cache, a database file, a working copy -
/// and it is the only browser storage with real seek/truncate semantics.
/// <br/>
/// Everything is addressed by path (<c>"logs/today.txt"</c>), rooted at the origin's private
/// directory. Missing directories are created for a write and never for a read.
/// <br/>
/// The <c>Sync*</c> members are the fast path: <c>createSyncAccessHandle</c> only exists on a worker
/// thread, so those calls are relayed to a dedicated worker this service starts on first use (and
/// terminates on disposal). They read and write at an offset without rewriting the whole file, which
/// is what makes OPFS a viable backing store for something like SQLite. An access handle takes an
/// exclusive lock on the file for the duration of one call, so two overlapping <c>Sync*</c> calls on
/// the same path are serialized rather than run together.
/// <br/>
/// Storage here counts against the origin's quota (see <see cref="StorageManager.Estimate"/>) and is
/// evictable unless <see cref="StorageManager.Persist"/> was granted.
/// </remarks>
[ButilService(typeof(OriginPrivateFileSystem))]
public class OriginPrivateFileSystem(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>navigator.storage.getDirectory</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.originPrivateFileSystem.isSupported");

    /// <summary>
    /// True when the <c>Sync*</c> members can work: OPFS, workers and
    /// <c>FileSystemFileHandle.createSyncAccessHandle</c> all present.
    /// </summary>
    /// <remarks>
    /// The last of those cannot be checked from the page at all - the method is exposed only on
    /// worker threads - so this starts the worker and asks it, which also proves the worker can be
    /// created and reached.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSyncAccessSupported() => js.Invoke<bool>("BitButil.originPrivateFileSystem.isSyncAccessSupported");

    /// <summary>
    /// Lists a directory's immediate children. Not recursive - descend by calling this again with a
    /// child's <see cref="OpfsEntry.Path"/>.
    /// </summary>
    /// <param name="path">A directory path, or empty for the root.</param>
    /// <returns>The entries, or an empty array when the directory doesn't exist.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OpfsEntry))]
    public ValueTask<OpfsEntry[]> List(string path = "")
        => js.Invoke<OpfsEntry[]>("BitButil.originPrivateFileSystem.list", path);

    /// <summary>Creates a directory, and every missing directory above it.</summary>
    /// <param name="path">The directory path, e.g. <c>"projects/drafts"</c>.</param>
    /// <returns>True when the directory exists afterwards - including when it already did.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> CreateDirectory(string path) => js.Invoke<bool>("BitButil.originPrivateFileSystem.createDirectory", path);

    /// <summary>True when a file or directory exists at <paramref name="path"/>.</summary>
    /// <remarks>
    /// The root always exists, so an empty path is always true.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Exists(string path) => js.Invoke<bool>("BitButil.originPrivateFileSystem.exists", path);

    /// <summary>Reads a file's name, size, MIME type and last-modified time without reading its contents.</summary>
    /// <param name="path">A file path.</param>
    /// <returns>The metadata, or null when there is no file there.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OpfsFileInfo))]
    public ValueTask<OpfsFileInfo?> GetFileInfo(string path)
        => js.Invoke<OpfsFileInfo?>("BitButil.originPrivateFileSystem.getInfo", path);

    /// <summary>Reads a whole file as text.</summary>
    /// <param name="path">A file path.</param>
    /// <returns>The text, or null when there is no file there.</returns>
    public ValueTask<string?> ReadText(string path) => js.Invoke<string?>("BitButil.originPrivateFileSystem.readText", path);

    /// <summary>Reads a whole file as bytes.</summary>
    /// <param name="path">A file path.</param>
    /// <returns>The bytes, or null when there is no file there.</returns>
    public ValueTask<byte[]?> ReadBytes(string path) => js.Invoke<byte[]?>("BitButil.originPrivateFileSystem.readBytes", path);

    /// <summary>Writes text to a file, replacing whatever was there and creating what's missing.</summary>
    /// <param name="path">A file path. Missing directories along it are created.</param>
    /// <param name="text">The contents to write.</param>
    /// <returns>False when the write failed - a path whose parent is a file, or the quota being full.</returns>
    /// <remarks>
    /// The write goes through a swap file and is committed on close, so a failure leaves the
    /// previous contents intact rather than a half-written file.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteText(string path, string text)
        => js.Invoke<bool>("BitButil.originPrivateFileSystem.write", path, text, null);

    /// <summary>Writes bytes to a file, replacing whatever was there and creating what's missing.</summary>
    /// <param name="path">A file path. Missing directories along it are created.</param>
    /// <param name="data">The contents to write.</param>
    /// <returns>False when the write failed.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteBytes(string path, byte[] data)
        => js.Invoke<bool>("BitButil.originPrivateFileSystem.write", path, null, data);

    /// <summary>Deletes a file or directory.</summary>
    /// <param name="path">The path to delete.</param>
    /// <param name="recursive">Required to delete a directory that isn't empty.</param>
    /// <returns>False when there was nothing there, or a non-empty directory was passed without <paramref name="recursive"/>.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Remove(string path, bool recursive = false)
        => js.Invoke<bool>("BitButil.originPrivateFileSystem.remove", path, recursive);

    /// <summary>Moves or renames a file.</summary>
    /// <param name="path">The file to move.</param>
    /// <param name="destination">Its new path. Missing directories along it are created.</param>
    /// <returns>False when the source is missing or the destination path is unusable.</returns>
    /// <remarks>
    /// <c>FileSystemHandle.move()</c> is Chromium-only; elsewhere this copies and deletes, which
    /// costs the file's size in time and memory but produces the same result.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Move(string path, string destination)
        => js.Invoke<bool>("BitButil.originPrivateFileSystem.move", path, destination);

    /// <summary>Deletes everything in the origin's private file system.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Clear() => js.Invoke<bool>("BitButil.originPrivateFileSystem.clear");

    /// <summary>
    /// Reads part of a file through a sync access handle on the worker - without loading the rest.
    /// </summary>
    /// <param name="path">A file path.</param>
    /// <param name="offset">Where to start, in bytes.</param>
    /// <param name="length">How many bytes to read; 0 reads to the end of the file.</param>
    /// <returns>The bytes read, or null when the file is missing or the worker is unavailable.</returns>
    /// <remarks>Requires <see cref="IsSyncAccessSupported"/>.</remarks>
    public ValueTask<byte[]?> SyncRead(string path, long offset = 0, int length = 0)
        => js.Invoke<byte[]?>("BitButil.originPrivateFileSystem.syncRead", path, offset, length);

    /// <summary>
    /// Writes bytes at an offset through a sync access handle on the worker, creating the file if
    /// it isn't there.
    /// </summary>
    /// <param name="path">A file path. Missing directories along it are created.</param>
    /// <param name="data">The bytes to write.</param>
    /// <param name="offset">Where to start, in bytes.</param>
    /// <param name="truncate">
    /// When true (the default) the file is cut back to <paramref name="offset"/> first, so the
    /// result is exactly what was written. Pass false to patch bytes in place and leave the rest.
    /// </param>
    /// <returns>The number of bytes written, or -1 when the write failed.</returns>
    /// <remarks>Requires <see cref="IsSyncAccessSupported"/>.</remarks>
    public ValueTask<long> SyncWrite(string path, byte[] data, long offset = 0, bool truncate = true)
        => js.Invoke<long>("BitButil.originPrivateFileSystem.syncWrite", path, data, offset, truncate);

    /// <summary>
    /// Appends bytes at the end of a file through a sync access handle - the cheap way to keep a
    /// log, since nothing before the new bytes is read or rewritten.
    /// </summary>
    /// <param name="path">A file path. Missing directories along it are created.</param>
    /// <param name="data">The bytes to append.</param>
    /// <returns>The number of bytes written, or -1 when the write failed.</returns>
    /// <remarks>Requires <see cref="IsSyncAccessSupported"/>.</remarks>
    public ValueTask<long> SyncAppend(string path, byte[] data)
        => js.Invoke<long>("BitButil.originPrivateFileSystem.syncAppend", path, data);

    /// <summary>Cuts a file to <paramref name="size"/> bytes, or pads it with zeros when it is shorter.</summary>
    /// <param name="path">A file path.</param>
    /// <param name="size">The size the file should have afterwards.</param>
    /// <remarks>
    /// Requires <see cref="IsSyncAccessSupported"/>.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SyncTruncate(string path, long size)
        => js.Invoke<bool>("BitButil.originPrivateFileSystem.syncTruncate", path, size);

    /// <summary>Reads a file's size through a sync access handle.</summary>
    /// <param name="path">A file path.</param>
    /// <returns>The size in bytes, or -1 when the file is missing or the worker is unavailable.</returns>
    /// <remarks>Requires <see cref="IsSyncAccessSupported"/>.</remarks>
    public ValueTask<long> SyncSize(string path) => js.Invoke<long>("BitButil.originPrivateFileSystem.syncSize", path);

    /// <summary>
    /// On scope/circuit teardown, terminates the sync worker. The files themselves are untouched -
    /// this storage outlives the app, which is the point of it.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.originPrivateFileSystem.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
