namespace Bit.Butil;

/// <summary>
/// The result of <see cref="ProofreaderSession.Proofread"/>: the corrected text, plus each change
/// located in the original so a UI can show what was wrong rather than just replacing it.
/// </summary>
public class ProofreadResult
{
    /// <summary>The whole input with every correction applied.</summary>
    public string CorrectedInput { get; set; } = string.Empty;

    /// <summary>Each change, positioned in the original input. Empty when nothing needed correcting.</summary>
    public ProofreadCorrection[] Corrections { get; set; } = [];
}
