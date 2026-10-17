namespace Bit.BlazeDb.Storage;

/// <summary>
/// Implemented by backends that can list what they hold. It lets the engine remove a database
/// completely - see <see cref="BlazeDbDatabase.DeleteAsync"/> - rather than only the files the
/// current manifest still names, which is all a backend that cannot enumerate allows.
/// </summary>
public interface IBlazeDbEnumerableStorage : IBlazeDbStorage
{
    /// <summary>The names of every file this backend currently holds for the database.</summary>
    ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default);
}
