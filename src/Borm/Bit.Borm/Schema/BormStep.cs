namespace Bit.Borm;

/// <summary>
/// One step of a multi-step (wizard) form. References top-level fields by name and may carry
/// a condition controlling whether the step is shown at all.
/// </summary>
public sealed class BormStep
{
    public BormStep(string id, string? title = null)
    {
        Id = id;
        Title = title;
    }

    /// <summary>Stable identifier for the step.</summary>
    public string Id { get; set; }

    /// <summary>Title shown in the stepper.</summary>
    public string? Title { get; set; }

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Names of the top-level fields belonging to this step, in order.</summary>
    public IList<string> Fields { get; set; } = new List<string>();

    /// <summary>When set and evaluates false, the step is skipped.</summary>
    public IBormCondition? VisibleWhen { get; set; }
}
