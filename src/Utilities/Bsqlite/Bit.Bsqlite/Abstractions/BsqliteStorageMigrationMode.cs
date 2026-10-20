namespace Bit.Bsqlite;

/// <summary>
/// What to do when a database lives on one backend but a more preferred backend has since become
/// available - for example a database created on IndexedDB in a browser that now supports OPFS.
/// </summary>
public enum BsqliteStorageMigrationMode
{
    /// <summary>
    /// Keep using the backend that holds the data. A diagnostic reports that a better option exists,
    /// but nothing moves. The default, because moving a database is never something to do behind the
    /// application's back.
    /// </summary>
    KeepExisting,

    /// <summary>
    /// Keep using the existing backend, and surface the opportunity so the application can migrate
    /// deliberately - when the user is idle, or after asking.
    /// </summary>
    Manual,

    /// <summary>
    /// Migrate before the database is first opened, once the preferred backend becomes available.
    /// </summary>
    AutomaticOnOpen,
}
