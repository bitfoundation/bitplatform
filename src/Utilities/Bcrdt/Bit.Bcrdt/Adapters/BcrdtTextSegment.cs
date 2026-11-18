namespace Bit.Bcrdt;

/// <summary>
/// A run of displayed characters and the document range they occupy.
/// </summary>
/// <param name="DisplayLength">How many displayed characters the run covers.</param>
/// <param name="DocumentStart">Where the run starts in the document.</param>
/// <param name="DocumentLength">
/// How many document units the run covers: equal to <paramref name="DisplayLength"/> for ordinary
/// text, zero for characters whose content was deleted by somebody else, and two for one normalized
/// <c>"\r\n"</c>.
/// </param>
internal readonly record struct BcrdtTextSegment(int DisplayLength, int DocumentStart, int DocumentLength)
{
    public bool IsOneToOne => DisplayLength == DocumentLength;

    public bool IsCollapsed => DocumentLength == 0;

    public int DocumentEnd => DocumentStart + DocumentLength;
}
