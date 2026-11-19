namespace Bit.Borm;

/// <summary>What <see cref="BormDraft.Restore"/> did.</summary>
/// <param name="Restored">Whether the document was a draft and its answers were put back.</param>
/// <param name="Version">The form version the draft was saved against.</param>
/// <param name="SavedAt">When it was saved.</param>
/// <param name="Migrated">Whether <see cref="BormDraftOptions.Migrate"/> was asked to bring it up to date.</param>
/// <param name="Skipped">Saved paths the form has no field for, or whose field can no longer hold the saved value.</param>
/// <param name="Error">Why the document was not read as a draft, when it was not.</param>
public sealed record BormDraftRestoreResult(
    bool Restored,
    string? Version,
    DateTimeOffset? SavedAt,
    bool Migrated,
    IReadOnlyList<string> Skipped,
    string? Error)
{
    internal static BormDraftRestoreResult NotADraft(string error) => new(false, null, null, false, [], error);
}
