using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Boilerplate.Client.Windows.Infrastructure.Services;

/// <summary>
/// Asks Windows (WinVerifyTrust) whether a file's Authenticode signature checks out, and returns who signed it if it does,
/// so the About page can show who signed the app people run.
/// </summary>
public static class WindowsCodeSignature
{
    private static readonly Guid verifyAction = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    public static string? GetVerifiedSigner(string path)
    {
        var pathPointer = Marshal.StringToHGlobalUni(Path.GetFullPath(path));
        var filePointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
        try
        {
            Marshal.StructureToPtr(new WinTrustFileInfo { StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>(), FilePath = pathPointer }, filePointer, false);

            var data = new WinTrustData
            {
                StructSize = (uint)Marshal.SizeOf<WinTrustData>(),
                UIChoice = 2,
                UnionChoice = 1,
                File = filePointer,
                ProviderFlags = 0x10 | 0x1000
            };
            var action = verifyAction;

            if (WinVerifyTrust(IntPtr.Zero, ref action, ref data) != 0)
                return null;

#pragma warning disable SYSLIB0057
            using var signer = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            using var certificate = new X509Certificate2(signer);
            return certificate.GetNameInfo(X509NameType.SimpleName, false);
        }
        finally
        {
            Marshal.FreeHGlobal(filePointer);
            Marshal.FreeHGlobal(pathPointer);
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int WinVerifyTrust(IntPtr window, ref Guid action, ref WinTrustData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustFileInfo
    {
        public uint StructSize;
        public IntPtr FilePath;
        public IntPtr File;
        public IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint StructSize;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UIChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr File;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UIContext;
        public IntPtr SignatureSettings;
    }
}
