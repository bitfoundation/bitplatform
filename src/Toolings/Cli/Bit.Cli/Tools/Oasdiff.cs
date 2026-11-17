using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public static class Oasdiff
{
    public const string Version = "1.38.0";

    private static readonly Dictionary<string, string> archiveSha256 = new(StringComparer.Ordinal)
    {
        ["windows_amd64"] = "4509a5d838849ab39c759b6ed95a0b9193b80a69e55b88c6c9e72d7bde21bbcf",
        ["windows_arm64"] = "2e562ef56a6447fef0fc35158c390455fff843a4e10e862aded3fb7102bc9b89",
        ["linux_amd64"] = "613037fd6a1d3362a1b742d6d86c338c0eb5ab3511f4942ce3948e78720ea2da",
        ["linux_arm64"] = "a5ebb16d90f729f6b436a92d3054c11ba5633ddfb639dfdfb64b0023cd03680c",
        ["darwin_all"] = "8a85428df3b00e76e7d41c49d222533c9beec813e9cbdc8162f3a6e776604c18"
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
