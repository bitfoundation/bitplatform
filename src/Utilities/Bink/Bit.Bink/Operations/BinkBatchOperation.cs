namespace Bit.Bink;

/// <summary>A group of operations applied atomically and undone as one step.</summary>
/// <param name="Operations">Operations in application order.</param>
/// <param name="Label">Optional label; defaults to the first operation's label.</param>
public sealed record BinkBatchOperation(IReadOnlyList<BinkDrawingOperation> Operations, string? Label = null) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label { get; } = Label ?? (Operations.Count > 0 ? Operations[0].Label : "Batch");

    /// <summary>True when the batch contains nothing.</summary>
    public bool IsEmpty => Operations.Count == 0;

    /// <inheritdoc />
    /// <remarks>
    /// Only the first operation is checked: the later ones depend on what the earlier ones change, so whether they
    /// apply is only known by applying them, and a batch that fails part way is rolled back. A collaboration session
    /// therefore applies a remote batch one operation at a time instead, skipping the ones that no longer apply.
    /// </remarks>
    public override bool CanApply(BinkDocument document) => Operations.Count == 0 || Operations[0].CanApply(document);

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        var inverses = new BinkDrawingOperation[Operations.Count];
        BinkDrawingOperation[]? resolved = null;
        int count = 0;
        try
        {
            for (int i = 0; i < Operations.Count; i++)
            {
                inverses[Operations.Count - 1 - i] = Operations[i].ApplyCore(document, out BinkDrawingOperation done);
                if (!ReferenceEquals(done, Operations[i]) && resolved is null)
                {
                    resolved = new BinkDrawingOperation[Operations.Count];
                    for (int j = 0; j < i; j++) resolved[j] = Operations[j];
                }
                if (resolved is not null) resolved[i] = done;
                count++;
            }
        }
        catch
        {
            // Roll back the partially applied batch so the document is never left half-changed. The rollback is
            // never reported, so it has to restore elements exactly: a replacement undone the ordinary way gets a
            // newer version, and this replica would then out-rank every collaborator's next edit of the element.
            bool restoring = document.Restoring;
            document.Restoring = true;
            try
            {
                for (int i = 0; i < count; i++) inverses[Operations.Count - count + i].ApplyCore(document, out _);
            }
            finally
            {
                document.Restoring = restoring;
            }
            throw;
        }
        applied = resolved is null ? this : new BinkBatchOperation(resolved, Label);
        return new BinkBatchOperation(inverses, Label);
    }
}
