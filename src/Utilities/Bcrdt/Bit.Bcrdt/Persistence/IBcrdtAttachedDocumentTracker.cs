namespace Bit.Bcrdt;

/// <summary>
/// A store that needs to know which documents are attached to a <see cref="BcrdtDocumentPersistence"/>,
/// so it never forgets one that is still being written to.
/// </summary>
/// <remarks>
/// A document attached is held in memory by whoever attached it, and its later records are changes on
/// top of what the store holds: a store that dropped its log meanwhile would be left with changes
/// missing what they build on. Calls are paired; a document may be attached more than once at a time.
/// </remarks>
internal interface IBcrdtAttachedDocumentTracker
{
    /// <summary>Called before the document is first loaded through the binding.</summary>
    void OnAttached(string documentId);

    /// <summary>Called once the binding has stopped writing.</summary>
    void OnDetached(string documentId);
}
