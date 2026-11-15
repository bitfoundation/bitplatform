namespace Bit.Bcrdt.Borm;

/// <summary>How a <see cref="BcrdtBormBinding"/> lays a form out in its document, and what it shares.</summary>
public sealed class BcrdtBormOptions
{
    /// <summary>
    /// Where the binding says which field the local user is editing, and reads which fields everybody
    /// else is editing. Usually the document's own presence, <c>document.Presence</c>. Without it the
    /// binding still holds back remote changes to the field being edited; it just tells nobody.
    /// </summary>
    public BcrdtPresenceService? Presence { get; init; }

    /// <summary>
    /// The presence field the edited field is published under. Two bindings sharing one document's
    /// presence - two forms in one document - each need their own.
    /// </summary>
    public string PresenceField { get; init; } = "bormField";

    /// <summary>
    /// Which text fields are stored as a nested <see cref="BcrdtText"/>, so two people typing in one
    /// at once both keep what they typed. Every other field is a value in a map, where one of two
    /// concurrent writes wins.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/>, the default, merges <see cref="BormFieldType.TextArea"/> and
    /// <see cref="BormFieldType.Markdown"/> fields - the ones people write prose into. A short field (a
    /// name, an email address) is better as a value: two concurrent edits of an email address merged
    /// character by character are a third address neither person typed. Only fields that hold a
    /// <see cref="string"/> can merge. What a document already holds wins over this setting, so two
    /// versions of an application that disagree about a field still read each other's.
    /// </remarks>
    public Func<BormFieldDefinition, bool>? MergeText { get; init; }

    /// <summary>
    /// Whether passwords and fields marked <c>Sensitive()</c> are shared too. Off by default: a
    /// document is replicated to every collaborator, persisted and relayed, which is the wrong place
    /// for a secret one of them typed.
    /// </summary>
    public bool IncludeSensitive { get; init; }
}

/// <summary>A collaborator editing a field of the form.</summary>
/// <param name="Collaborator">Who it is.</param>
/// <param name="Path">
/// The field's path in the local form - <c>Lines[2].Notes</c> - which follows the row even when rows
/// were inserted, removed or moved since they started.
/// </param>
public sealed record BcrdtBormEditor(BcrdtCollaborator Collaborator, string Path);
