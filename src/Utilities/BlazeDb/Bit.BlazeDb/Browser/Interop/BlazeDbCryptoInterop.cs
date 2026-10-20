using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Bit.BlazeDb.Browser;

/// <summary>Bindings to the bit-blazedb-crypto.js ES module.</summary>
[SupportedOSPlatform("browser")]
internal static partial class BlazeDbCryptoInterop
{
    private const string ModuleName = BlazeDbBrowserModules.Crypto;

    public static Task EnsureModuleAsync(string? moduleUrl) => BlazeDbBrowserModules.EnsureAsync(ModuleName, moduleUrl);

    [JSImport("importKey", ModuleName)]
    public static partial Task<JSObject> ImportKey(byte[] rawKey);

    [JSImport("encrypt", ModuleName)]
    public static partial Task<JSObject> Encrypt(JSObject key, byte[] nonce, byte[] plaintext, byte[] associatedData);

    [JSImport("decrypt", ModuleName)]
    public static partial Task<JSObject> Decrypt(JSObject key, byte[] nonce, byte[] ciphertext, byte[] associatedData);

    [JSImport("copyBytes", ModuleName)]
    public static partial void CopyBytes(JSObject source, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);
}
