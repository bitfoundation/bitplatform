namespace Bit.Bsqlite.Storage.FileSystem;

/// <summary>Options for <see cref="BsqliteFileSystemStorageProvider"/>.</summary>
public sealed class BsqliteFileSystemStorageOptions
{
    /// <summary>
    /// The folder databases are kept in. A database name maps to a path under it, so
    /// <c>AddDatabase("data/app.db")</c> is <c>{RootDirectory}/data/app.db</c>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="BsqliteFileSystemStorageProvider.DefaultRootDirectory"/>: a folder of the
    /// application's own under local application data, which on Android, iOS and Mac Catalyst is inside the
    /// app's sandbox and on Windows and macOS is per user. Set it to keep using the files an app already
    /// has - a database Bit.Besql kept on the device, say.
    /// </remarks>
    public string? RootDirectory { get; set; }
}
