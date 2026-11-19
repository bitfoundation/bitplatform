using System.Formats.Tar;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tools;

public static class Oasdiff
{
    public const string Version = "1.39.0";

    private static readonly Dictionary<string, string> archiveSha256 = new(StringComparer.Ordinal)
    {
        ["windows_amd64"] = "956a73df88e03efdf3fcc5f327446cc176fefe4adcea1978b0fef464a1ee8901",
        ["windows_arm64"] = "5699ac9dc74b432f78990cd369ff2bec3a79302c58e923c1ec823e47a86ddb82",
        ["linux_amd64"] = "71a85930728f367a2086077b7ae1411319421549d18473a3e8258b6451520433",
        ["linux_arm64"] = "481db7d891d916f13df277b2ef8bbb95477702827710e49fe22cef69c359c780",
        ["darwin_all"] = "fe88d9950324d3b0ebeaf8e0d3a210b64cb099a0f55fce5ee5a22b276df0eac4"
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
