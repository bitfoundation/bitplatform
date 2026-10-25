using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Storage.FileSystem;

/// <summary>
/// Keeps databases as SQLite files on the device, opened in-process by the native SQLite engine the app
/// brings - <c>SQLitePCLRaw.bundle_e_sqlite3</c>, or the bundle it already uses. The backend of Blazor
/// Hybrid apps, where .NET runs natively.
/// </summary>
/// <remarks>
/// <para>
/// Nothing has to be registered to use it: in a Blazor Hybrid app <c>AddBitBsqliteServices</c> registers
/// it and selection puts it first, ahead of the browser backends, which need .NET running in the browser.
/// The same registration therefore serves a WebAssembly client and a MAUI, WPF or Windows Forms app. It
/// is never offered elsewhere - not to a server, whose users' databases belong in their browsers.
/// </para>
/// <para>
/// A database name is a path relative to <see cref="RootDirectory"/>. Files are plain SQLite databases in
/// rollback-journal mode, so <see cref="Admin"/> exports and imports them as they are, and an app can
/// point <see cref="BsqliteFileSystemStorageOptions.RootDirectory"/> at a folder of databases it already
/// has.
/// </para>
/// </remarks>
public sealed class BsqliteFileSystemStorageProvider : IBsqliteFileStorageProvider
{
    /// <summary>The name applications use to select this backend.</summary>
    public const string ProviderName = "file-system";

    private readonly BsqliteFileSystemStorageAdmin _admin;

    /// <summary>Creates the backend, keeping databases under <paramref name="options"/>' root, or the default one.</summary>
    public BsqliteFileSystemStorageProvider(BsqliteFileSystemStorageOptions? options = null)
    {
        RootDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(string.IsNullOrWhiteSpace(options?.RootDirectory)
            ? DefaultRootDirectory
            : options.RootDirectory));
        _admin = new BsqliteFileSystemStorageAdmin(this);
    }

    /// <summary>
    /// <c>{local application data}/{entry assembly name}/bit-bsqlite</c>: inside the app's sandbox on
    /// Android, iOS and Mac Catalyst, and per user and per app on Windows and macOS.
    /// </summary>
    public static string DefaultRootDirectory
    {
        get
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (string.IsNullOrWhiteSpace(appName))
            {
                appName = "app";
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                appName = appName.Replace(invalid, '_');
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
                appName,
                "bit-bsqlite");
        }
    }

    /// <summary>The folder every database of this backend lives under.</summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Whether <c>AddBitBsqliteServices</c> registered this instance for a Blazor Hybrid app on its own, so
    /// that one the app registers itself takes its place.
    /// </summary>
    internal bool IsDefault { get; init; }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    /// <remarks>
    /// What a native SQLite file offers: several connections, readers alongside each other, <c>ATTACH</c>,
    /// and any page size. No engine build is downloaded - <see cref="BsqliteEngineBuild.Synchronous"/>
    /// only says no asynchronous VFS is involved.
    /// </remarks>
    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.Synchronous,
        IsPersistent = true,
        SupportsMultipleConnections = true,
        SupportsConcurrentReads = true,
        SupportsRelaxedDurability = false,
        SupportsMultiDatabaseTransactions = true,
        CanChangePageSize = true,
        SupportedContexts = BsqliteExecutionContexts.NativeProcess,
    };

    /// <inheritdoc />
    /// <remarks>None: the native engine opens the file itself, so there is no JavaScript to load.</remarks>
    public BsqliteJsModule? VfsModule => null;

    /// <inheritdoc />
    public IBsqliteStorageAdmin Admin => _admin;

    /// <inheritdoc />
    /// <remarks>
    /// A name is held to what every platform's file system keeps as written, so a name that works on one
    /// device works on all of them: no <c>&lt;&gt;:"|?*</c> or control characters, and no segment ending in a
    /// dot or a space. On Windows a <c>:</c> would name an alternate data stream of another file, and a
    /// trailing dot or space is dropped, so two names would share one file.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="databaseName"/> is blank, rooted, climbs out of <see cref="RootDirectory"/>, or has a
    /// character or segment a file name cannot carry on every platform.
    /// </exception>
    public string GetDatabasePath(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var relative = databaseName.Replace('\\', '/').Trim('/');
        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // A leading slash is tolerated - Bit.Besql trimmed it too - but a drive or a parent is not.
        var path = segments.Length == 0 || Path.IsPathRooted(relative) || !segments.All(IsPortableSegment)
            ? null
            : Path.GetFullPath(Path.Combine([RootDirectory, .. segments]));

        // Checked on the result too: a segment such as "C:" is rooted on Windows and would restart the path.
        // The root keeps its separator when it is one already - a drive, or "/".
        var prefix = Path.EndsInDirectorySeparator(RootDirectory) ? RootDirectory : RootDirectory + Path.DirectorySeparatorChar;
        if (path is null || !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"'{databaseName}' is not a database name the file system backend can keep: a name is a path "
                + $"relative to {RootDirectory}, without '.' or '..' segments, without the characters <>:\"|?* or "
                + "control characters, and without a segment that ends in a dot or a space.",
                nameof(databaseName));
        }

        return path;
    }

    private static bool IsPortableSegment(string segment)
        => segment is not ("." or "..")
            && !segment.EndsWith('.')
            && !segment.EndsWith(' ')
            && !segment.Any(character => char.IsControl(character) || character is '<' or '>' or ':' or '"' or '|' or '?' or '*');

    /// <inheritdoc />
    /// <remarks>
    /// Available wherever .NET runs natively and the native engine loads; selection offers it only to
    /// Blazor Hybrid apps.
    /// </remarks>
    public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["persistent"] = "true",
            ["rootDirectory"] = RootDirectory,
        };

        if (OperatingSystem.IsBrowser())
        {
            return ValueTask.FromResult(BsqliteProbeResult.Unavailable(
                "The file system backend needs .NET running natively, as in a Blazor Hybrid app; here .NET runs "
                + "in the browser, whose backends are OPFS, IndexedDB and Cache Storage.",
                environment));
        }

        try
        {
            Directory.CreateDirectory(RootDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return ValueTask.FromResult(BsqliteProbeResult.Unavailable(
                $"The folder {RootDirectory} cannot be created: {ex.Message}",
                environment));
        }

        try
        {
            using var probe = new SqliteConnection();
            environment["sqliteVersion"] = probe.ServerVersion;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ValueTask.FromResult(BsqliteProbeResult.Unavailable(
                $"The native SQLite engine could not be loaded ({ex.GetType().Name}: {ex.Message}). The app brings "
                + "it: add SQLitePCLRaw.bundle_e_sqlite3 - or the SQLitePCLRaw bundle it already uses, such as "
                + "SQLitePCLRaw.bundle_e_sqlcipher - to the Blazor Hybrid app's project.",
                environment));
        }

        return ValueTask.FromResult(BsqliteProbeResult.Available(environment: environment));
    }
}
