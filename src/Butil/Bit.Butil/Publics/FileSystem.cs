using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/File_System_API#file_system_access_api">File System Access API</see>:
/// lets the user pick a real file or folder on their machine, and lets your app read it, write
/// back to it, and come back to it later.
/// </summary>
/// <remarks>
/// This is the difference between a download and a save: <see cref="FileReader"/> reads what the
/// user hands you once, while a handle from here can be written to repeatedly - a document editor
/// with a working "Save" rather than "Download a copy".
/// <br/>
/// Chromium-only at the time of writing; Firefox and Safari implement neither picker. Check
/// <see cref="IsSupported"/> and keep a download-based fallback.
/// <br/>
/// Every picker must be called from a user-gesture handler, and each returns null when the user
/// cancels rather than throwing - dismissing a dialog is not an error.
/// </remarks>
public class FileSystem(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>window.showOpenFilePicker</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.fileSystem.isSupported");

    /// <summary>True when the runtime exposes <c>window.showDirectoryPicker</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsDirectorySupported() => js.Invoke<bool>("BitButil.fileSystem.isDirectorySupported");

    /// <summary>
    /// Shows the open-file picker.
    /// </summary>
    /// <param name="multiple">When true, the user can select more than one file.</param>
    /// <param name="accept">
    /// File-type groups to offer in the picker's filter dropdown. A group with no extensions is
    /// dropped rather than sent, because the underlying API rejects an empty filter.
    /// </param>
    /// <param name="excludeAcceptAllOption">When true, hides the picker's "All files" choice, forcing one of <paramref name="accept"/>.</param>
    /// <param name="startIn">
    /// Where to open: a well-known directory name (<c>"desktop"</c>, <c>"documents"</c>,
    /// <c>"downloads"</c>, <c>"music"</c>, <c>"pictures"</c>, <c>"videos"</c>) or empty for the
    /// browser's default.
    /// </param>
    /// <returns>The picked files, or null when the user cancelled or the API is unavailable.</returns>
    /// <remarks>Must be called from a user-gesture handler.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FilePickerType))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemHandleInfo))]
    public ValueTask<FileSystemHandleInfo[]?> ShowOpenFilePicker(bool multiple = false,
                                                                 FilePickerType[]? accept = null,
                                                                 bool excludeAcceptAllOption = false,
                                                                 string startIn = "")
        => js.Invoke<FileSystemHandleInfo[]?>("BitButil.fileSystem.openFilePicker",
            multiple, excludeAcceptAllOption, accept ?? [], startIn);

    /// <summary>
    /// Shows the save-file picker. The returned handle can be written to as often as you like -
    /// this is what a real "Save" is built on.
    /// </summary>
    /// <param name="suggestedName">The file name to pre-fill, e.g. <c>"notes.txt"</c>.</param>
    /// <param name="accept">File-type groups to offer, as in <see cref="ShowOpenFilePicker"/>.</param>
    /// <param name="excludeAcceptAllOption">When true, hides the picker's "All files" choice.</param>
    /// <param name="startIn">Where to open - see <see cref="ShowOpenFilePicker"/>.</param>
    /// <returns>The chosen file, or null when the user cancelled or the API is unavailable.</returns>
    /// <remarks>
    /// Must be called from a user-gesture handler. Nothing is written until you call
    /// <see cref="WriteText"/> or <see cref="WriteBytes"/>; picking a name only reserves it.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FilePickerType))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemHandleInfo))]
    public ValueTask<FileSystemHandleInfo?> ShowSaveFilePicker(string suggestedName = "",
                                                               FilePickerType[]? accept = null,
                                                               bool excludeAcceptAllOption = false,
                                                               string startIn = "")
        => js.Invoke<FileSystemHandleInfo?>("BitButil.fileSystem.saveFilePicker",
            suggestedName, excludeAcceptAllOption, accept ?? [], startIn);

    /// <summary>
    /// Shows the directory picker, granting access to a whole folder.
    /// </summary>
    /// <param name="write">When true, asks for write access up front instead of read-only.</param>
    /// <param name="startIn">Where to open - see <see cref="ShowOpenFilePicker"/>.</param>
    /// <returns>The chosen directory, or null when the user cancelled or the API is unavailable.</returns>
    /// <remarks>Must be called from a user-gesture handler.</remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemHandleInfo))]
    public ValueTask<FileSystemHandleInfo?> ShowDirectoryPicker(bool write = false, string startIn = "")
        => js.Invoke<FileSystemHandleInfo?>("BitButil.fileSystem.directoryPicker", write ? "readwrite" : "read", startIn);

    /// <summary>
    /// Lists a directory's immediate children. Not recursive - descend by calling this again on a
    /// child whose <see cref="FileSystemHandleInfo.IsDirectory"/> is true.
    /// </summary>
    /// <param name="directory">A handle from <see cref="ShowDirectoryPicker"/> or a nested listing.</param>
    /// <returns>The entries, or an empty array when the handle is gone or permission was revoked.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemHandleInfo))]
    public ValueTask<FileSystemHandleInfo[]> ListDirectory(FileSystemHandleInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return js.Invoke<FileSystemHandleInfo[]>("BitButil.fileSystem.listDirectory", directory.Id);
    }

    /// <summary>Reads a file's contents as text.</summary>
    /// <param name="file">A file handle from one of the pickers or a directory listing.</param>
    /// <returns>The text, or null when the file is unreadable - moved, deleted, or permission revoked.</returns>
    public ValueTask<string?> ReadText(FileSystemHandleInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return js.Invoke<string?>("BitButil.fileSystem.readText", file.Id);
    }

    /// <summary>Reads a file's contents as bytes.</summary>
    /// <param name="file">A file handle from one of the pickers or a directory listing.</param>
    /// <returns>The bytes, or null when the file is unreadable.</returns>
    public ValueTask<byte[]?> ReadBytes(FileSystemHandleInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return js.Invoke<byte[]?>("BitButil.fileSystem.readBytes", file.Id);
    }

    /// <summary>Reads a file's name, size, MIME type and last-modified time without reading its contents.</summary>
    /// <param name="file">A file handle from one of the pickers or a directory listing.</param>
    /// <returns>The metadata, or null when the file is gone.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemFileInfo))]
    public ValueTask<FileSystemFileInfo?> GetFileInfo(FileSystemHandleInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return js.Invoke<FileSystemFileInfo?>("BitButil.fileSystem.getInfo", file.Id);
    }

    /// <summary>Overwrites a file with text.</summary>
    /// <param name="file">A writable file handle - typically from <see cref="ShowSaveFilePicker"/>.</param>
    /// <param name="text">The contents to write.</param>
    /// <returns>False when the write was refused, e.g. the handle is read-only or permission lapsed.</returns>
    /// <remarks>
    /// Writing to a handle picked in a previous session needs the permission re-granted first -
    /// see <see cref="RequestPermission"/>.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteText(FileSystemHandleInfo file, string text)
    {
        ArgumentNullException.ThrowIfNull(file);
        return js.Invoke<bool>("BitButil.fileSystem.write", file.Id, text, null, false);
    }

    /// <summary>Overwrites a file with bytes.</summary>
    /// <param name="file">A writable file handle - typically from <see cref="ShowSaveFilePicker"/>.</param>
    /// <param name="data">The contents to write.</param>
    /// <returns>False when the write was refused.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteBytes(FileSystemHandleInfo file, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(file);
        return js.Invoke<bool>("BitButil.fileSystem.write", file.Id, null, data, false);
    }

    /// <summary>Checks the current permission for a handle without prompting.</summary>
    /// <param name="handle">A file or directory handle.</param>
    /// <param name="write">When true, asks about write access rather than read.</param>
    public async ValueTask<FileSystemPermission> QueryPermission(FileSystemHandleInfo handle, bool write = false)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return Parse(await js.Invoke<string>("BitButil.fileSystem.queryPermission", handle.Id, write));
    }

    /// <summary>
    /// Prompts for permission on a handle, which is what makes a handle restored from a previous
    /// session usable again.
    /// </summary>
    /// <param name="handle">A file or directory handle.</param>
    /// <param name="write">When true, asks for write access rather than read.</param>
    /// <remarks>Must be called from a user-gesture handler.</remarks>
    public async ValueTask<FileSystemPermission> RequestPermission(FileSystemHandleInfo handle, bool write = false)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return Parse(await js.Invoke<string>("BitButil.fileSystem.requestPermission", handle.Id, write));
    }

    /// <summary>Gets a named file inside a directory, optionally creating it.</summary>
    /// <param name="directory">A directory handle.</param>
    /// <param name="name">The file name, without a path - this API has no path traversal.</param>
    /// <param name="create">When true, creates the file if it doesn't exist.</param>
    /// <returns>The file, or null when it doesn't exist and <paramref name="create"/> is false.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FileSystemHandleInfo))]
    public ValueTask<FileSystemHandleInfo?> GetFile(FileSystemHandleInfo directory, string name, bool create = false)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return js.Invoke<FileSystemHandleInfo?>("BitButil.fileSystem.getFileInDirectory", directory.Id, name, create);
    }

    /// <summary>Deletes an entry from a directory.</summary>
    /// <param name="directory">A directory handle with write permission.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="recursive">When true, a non-empty subdirectory is removed along with its contents.</param>
    /// <returns>False when the entry is missing or the removal was refused.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Remove(FileSystemHandleInfo directory, string name, bool recursive = false)
    {
        ArgumentNullException.ThrowIfNull(directory);
        return js.Invoke<bool>("BitButil.fileSystem.removeFromDirectory", directory.Id, name, recursive);
    }

    /// <summary>
    /// Drops a handle you're finished with. Handles are held on the JS side for as long as your app
    /// might use them, so releasing the ones you no longer need keeps that set small.
    /// </summary>
    /// <param name="handle">The handle to forget.</param>
    public ValueTask Release(FileSystemHandleInfo handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        return js.InvokeVoid("BitButil.fileSystem.release", handle.Id);
    }

    private static FileSystemPermission Parse(string? state) => state switch
    {
        "granted" => FileSystemPermission.Granted,
        "denied" => FileSystemPermission.Denied,
        "prompt" => FileSystemPermission.Prompt,
        _ => FileSystemPermission.Unsupported,
    };

    /// <summary>
    /// On scope/circuit teardown, drops every handle this app was holding.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.fileSystem.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        GC.SuppressFinalize(this);
    }
}
