namespace Bit.Borm;

/// <summary>How <see cref="BormDraft"/> writes and reads a draft.</summary>
public sealed class BormDraftOptions
{
    /// <summary>Keep sensitive values (passwords, fields marked <c>Sensitive()</c>) in the draft. Off by default.</summary>
    public bool IncludeSensitive { get; init; }

    /// <summary>Leave out the values of fields the form is hiding when the draft is written.</summary>
    public bool ExcludeHidden { get; init; }

    /// <summary>
    /// Brings a draft saved against another version of the form up to date before it is restored:
    /// called when the draft's version differs from <see cref="BormDefinition.Version"/>, with the
    /// draft's values and row counts to rename, convert or drop.
    /// </summary>
    public Action<BormDraftMigration>? Migrate { get; init; }

    /// <summary>The clock the draft's <c>savedAt</c> is read from. The system clock when null.</summary>
    public TimeProvider? TimeProvider { get; init; }
}
