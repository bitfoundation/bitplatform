using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Bit.Cli.Infrastructure;

public enum SignatureState
{
    Valid,
    Unsigned,
    Invalid
}

public sealed record FileSignature(SignatureState State, string? Signer = null);

public enum AttestationState
{
    Found,
    Missing,
    Unavailable
}

public static class Provenance
{
    private const int TrustNoSignature = unchecked((int)0x800B0100);

    private static readonly Guid verifyAction = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public static string AttestationsApiUrl(string sha256) => $"https://api.github.com/repos/bitfoundation/bitplatform/attestations/sha256:{sha256}";

    public static async Task<AttestationState> FindAttestationAsync(HttpMessageHandler handler, string path, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(HttpMethod.Get, AttestationsApiUrl(Sha256(path)));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("bit-cli", BuildInfo.Version));
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotFound)
                return AttestationState.Missing;
            if (response.IsSuccessStatusCode is false)
                return AttestationState.Unavailable;

            return CountAttestations(await response.Content.ReadAsStringAsync(cancellationToken)) > 0 ? AttestationState.Found : AttestationState.Missing;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or JsonException)
        {
            return AttestationState.Unavailable;
        }
    }

    public static int CountAttestations(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind is JsonValueKind.Object
            && document.RootElement.TryGetProperty("attestations", out var attestations)
            && attestations.ValueKind is JsonValueKind.Array ? attestations.GetArrayLength() : 0;
    }

    public static FileSignature WindowsSignature(string path)
    {
        if (OperatingSystem.IsWindows() is false)
            throw new PlatformNotSupportedException();

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

            var result = WinVerifyTrust(IntPtr.Zero, ref action, ref data);
            if (result == TrustNoSignature)
                return new(SignatureState.Unsigned);
            if (result != 0)
                return new(SignatureState.Invalid);

#pragma warning disable SYSLIB0057
            using var signer = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            using var certificate = new X509Certificate2(signer);
            return new(SignatureState.Valid, certificate.GetNameInfo(X509NameType.SimpleName, false));
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
