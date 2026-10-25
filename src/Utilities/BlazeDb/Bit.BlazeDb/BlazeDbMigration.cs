namespace Bit.BlazeDb;

/// <summary>
/// A data migration registered with <see cref="BlazeDbDatabaseOptions.AddMigration"/>: what to run to bring
/// a database to <see cref="Version"/>.
/// </summary>
/// <param name="Version">The <see cref="BlazeDbDatabase.UserVersion"/> the database has once it ran.</param>
/// <param name="Migrate">The migration, given the open database; its writes all join one transaction.</param>
public sealed record BlazeDbMigration(long Version, Action<BlazeDbDatabase> Migrate);
