namespace Bit.Bcrdt;

/// <summary>A collaborator editing a field of the form.</summary>
/// <param name="Collaborator">Who it is.</param>
/// <param name="Path">
/// The field's path in the local form - <c>Lines[2].Notes</c> - which follows the row even when rows
/// were inserted, removed or moved since they started.
/// </param>
public sealed record BcrdtBormEditor(BcrdtCollaborator Collaborator, string Path);
