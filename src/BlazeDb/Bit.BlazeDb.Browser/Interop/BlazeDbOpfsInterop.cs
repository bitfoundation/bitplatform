using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>Generated bindings to the bit-blazedb-opfs.js ES module.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BlazeDbOpfsInterop
{
    private const string ModuleName = BlazeDbBrowserModules.Opfs;

    public static Task EnsureModuleAsync(string? moduleUrl) => BlazeDbBrowserModules.EnsureAsync(ModuleName, moduleUrl);

    [JSImport("hasWebLocks", ModuleName)]
    public static partial bool HasWebLocks();

    [JSImport("acquireLock", ModuleName)]
    public static partial Task<bool> AcquireLock(string lockName, bool allowWithoutWebLocks);

    [JSImport("releaseLock", ModuleName)]
    public static partial void ReleaseLock(string lockName);

    [JSImport("isLockHeld", ModuleName)]
    public static partial bool IsLockHeld(string lockName);

    [JSImport("openDatabaseDirectory", ModuleName)]
    public static partial Task<JSObject> OpenDatabaseDirectory(string databaseName);

    [JSImport("openExistingDatabaseDirectory", ModuleName)]
    public static partial Task<JSObject?> OpenExistingDatabaseDirectory(string databaseName);

    [JSImport("readFile", ModuleName)]
    public static partial Task<JSObject?> ReadFile(JSObject directory, string name);

    [JSImport("copyBytes", ModuleName)]
    public static partial void CopyBytes(
        JSObject source,
        [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);

    [JSImport("writeAtomic", ModuleName)]
    public static partial Task WriteAtomic(JSObject directory, string name, byte[] bytes);

    [JSImport("appendFile", ModuleName)]
    public static partial Task AppendFile(JSObject directory, string name, byte[] bytes);

    [JSImport("deleteFile", ModuleName)]
    public static partial Task DeleteFile(JSObject directory, string name);

    [JSImport("storageEstimate", ModuleName)]
    public static partial Task<JSObject?> StorageEstimate();

    [JSImport("requestPersistence", ModuleName)]
    public static partial Task<bool> RequestPersistence();
}
