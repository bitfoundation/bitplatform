namespace Bit.Blex;

/// <summary>Something a failed mutation undoes: an optimistic cache update, or any change of your own.</summary>
public interface IBlexRollback
{
    /// <summary>Undoes the change. Called at most once.</summary>
    void Rollback();
}
