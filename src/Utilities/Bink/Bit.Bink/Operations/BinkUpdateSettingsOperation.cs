namespace Bit.Bink;

/// <summary>Changes the document settings (size, background).</summary>
/// <param name="Settings">New settings.</param>
public sealed record BinkUpdateSettingsOperation(BinkDocumentSettings Settings) : BinkDrawingOperation
{
    /// <inheritdoc />
    public override string Label => "Change document";

    /// <inheritdoc />
    public override bool CanApply(BinkDocument document) => true;

    internal override BinkDrawingOperation ApplyCore(BinkDocument document, out BinkDrawingOperation applied)
    {
        applied = this;
        BinkDocumentSettings previous = document.UpdateSettingsCore(Settings);
        return new BinkUpdateSettingsOperation(previous);
    }
}
