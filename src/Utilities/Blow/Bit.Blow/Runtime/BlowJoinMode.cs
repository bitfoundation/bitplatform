namespace Bit.Blow;

/// <summary>How a node with several incoming edges decides whether it runs, once everything before it has settled.</summary>
public enum BlowJoinMode
{
    /// <summary>
    /// Runs when at least one incoming edge carried a value; skipped when none did. The join after a branch: only one
    /// of the branches reaches it.
    /// </summary>
    Any,

    /// <summary>Runs only when every incoming edge carried a value; skipped otherwise. The join after a parallel split.</summary>
    All,
}
