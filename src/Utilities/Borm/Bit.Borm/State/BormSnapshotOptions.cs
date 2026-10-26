namespace Bit.Borm;

/// <summary>What <see cref="BormState.Snapshot(BormSnapshotOptions)"/> leaves out.</summary>
public sealed class BormSnapshotOptions
{
    /// <summary>
    /// Leave out the fields the form is hiding — a branch of the form the user abandoned, a wizard step
    /// a condition took away — so the payload holds only what the user could see.
    /// </summary>
    public bool ExcludeHidden { get; init; }

    /// <summary>Leave out sensitive values (<see cref="BormFieldDefinition.IsSensitive"/>): passwords and fields marked <c>Sensitive()</c>.</summary>
    public bool ExcludeSensitive { get; init; }
}
