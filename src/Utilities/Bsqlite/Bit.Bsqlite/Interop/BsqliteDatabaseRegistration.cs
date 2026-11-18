namespace Bit.Bsqlite;

/// <summary>
/// One per <see cref="BsqliteBuilder.AddDatabase"/> call, so the registered names can be listed
/// without opening - or even constructing - any of the databases.
/// </summary>
internal sealed record BsqliteDatabaseRegistration(string DatabaseName);
