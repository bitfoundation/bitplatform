using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public static class Oasdiff
{
    public const string Version = "1.37.0";

    private static readonly Dictionary<string, string> archiveSha256 = new(StringComparer.Ordinal)
    {
        ["windows_amd64"] = "d72f1c5c4ba4adddafa4ec3c38f1c34f09df8ddb2d2bdd75869f94145c5bcc6d",
        ["windows_arm64"] = "8d452a04078ffa2428ce303d7565d5ec01de590cb9cd01f159b10341b3dde7e1",
        ["linux_amd64"] = "eac87307d31d08c36800c58a409c247f1c5db37fbb72e2544402f22a87cbca3c",
        ["linux_arm64"] = "072a425ac647ecf2808721e91293fd644cee7935d2b32e650b3158539b2c7898",
        ["darwin_all"] = "eccf5b9f46e689f8adcd79b79d4fdbde26a28301b6a74a0b449ad6a49e187e45"
    };

    public static string? Platform(CliEnvironment environment) => (environment.Os, environment.Architecture) switch
    {
        (HostOs.Windows, Architecture.X64) => "windows_amd64",
        (HostOs.Windows, Architecture.Arm64) => "windows_arm64",
        (HostOs.Linux, Architecture.X64) => "linux_amd64",
        (HostOs.Linux, Architecture.Arm64) => "linux_arm64",
        (HostOs.MacOS, Architecture.X64 or Architecture.Arm64) => "darwin_all",
        _ => null
    };

    public static string ArchiveUrl(string platform) => $"https://github.com/oasdiff/oasdiff/releases/download/v{Version}/oasdiff_{Version}_{platform}.tar.gz";

    public static string ExecutablePath(CliEnvironment environment) => Path.Combine(environment.BitDirectory, "tools", "oasdiff", Version, environment.IsWindows ? "oasdiff.exe" : "oasdiff");

    public static async Task<string> EnsureAsync(CliEnvironment environment, HttpMessageHandler handler, CancellationToken cancellationToken)
    {
        var executable = ExecutablePath(environment);

        if (File.Exists(executable))
            return executable;

        var platform = Platform(environment) ?? throw new PlatformNotSupportedException($"oasdiff has no build for {environment.Os} on {environment.Architecture}.");
        var url = ArchiveUrl(platform);

        using var http = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromMinutes(5) };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("bit-cli", BuildInfo.Version));

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var archive = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (Convert.ToHexStringLower(SHA256.HashData(archive)) != archiveSha256[platform])
            throw new InvalidDataException($"{url} isn't the archive oasdiff {Version} released: its SHA-256 differs.");

        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        var download = executable + ".download";

        await using (var gzip = new GZipStream(new MemoryStream(archive), CompressionMode.Decompress))
        await using (var tar = new TarReader(gzip))
        {
            while (await tar.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry)
            {
                if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile && entry.Name == Path.GetFileName(executable))
                {
                    await entry.ExtractToFileAsync(download, overwrite: true, cancellationToken);
                    break;
                }
            }
        }

        if (File.Exists(download) is false)
            throw new InvalidDataException($"{url} has no {Path.GetFileName(executable)}.");

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(download, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        File.Move(download, executable, overwrite: true);
        return executable;
    }
}
