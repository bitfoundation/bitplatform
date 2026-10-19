namespace Bit.Borm;

/// <summary>
/// A boolean predicate evaluated against the current form data. Conditions drive
/// visibility, enablement and conditional validation.
/// </summary>
public interface IBormCondition
{
    /// <summary>Evaluates the condition against the supplied data reader.</summary>
    bool Evaluate(IBormDataReader data);

    /// <summary>
    /// The set of field paths this condition depends on, so the engine can re-evaluate
    /// only when a relevant value changes. Empty means "depends on everything".
    /// </summary>
    IEnumerable<string> Dependencies { get; }
}
