using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>Bindings to the bit-blazedb-idb.js ES module.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BlazeDbIndexedDbInterop
{
    private const string ModuleName = BlazeDbBrowserModules.IndexedDb;

    public static Task EnsureModuleAsync(string? moduleUrl) => BlazeDbBrowserModules.EnsureAsync(ModuleName, moduleUrl);

    [JSImport("openDatabase", ModuleName)]
    public static partial Task<JSObject> OpenDatabase(string databaseName);

    [JSImport("openExistingDatabase", ModuleName)]
    public static partial Task<JSObject?> OpenExistingDatabase(string databaseName);

    [JSImport("readFile", ModuleName)]
    public static partial Task<JSObject?> ReadFile(JSObject db, string name);

    [JSImport("copyBytes", ModuleName)]
    public static partial void CopyBytes(JSObject source, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);

    [JSImport("writeAtomic", ModuleName)]
    public static partial Task WriteAtomic(JSObject db, string name, byte[] bytes);

    [JSImport("appendFile", ModuleName)]
    public static partial Task AppendFile(JSObject db, string name, byte[] bytes);

    [JSImport("deleteFile", ModuleName)]
    public static partial Task DeleteFile(JSObject db, string name);

    [JSImport("closeDatabase", ModuleName)]
    public static partial void CloseDatabase(JSObject db);

    [JSImport("isOpfsAvailable", ModuleName)]
    public static partial Task<bool> IsOpfsAvailable();
}
