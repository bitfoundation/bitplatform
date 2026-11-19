namespace Bit.BbScanner;

/// <summary>
/// A row decoder that can piece a symbol together from more than one scan line of the same image: one whose halves
/// no single line has to cross - the two rows of a stacked symbol, or the two ends of a short one turned further off
/// a line than the line can cross it whole. It is told where each line it reads lies in the image.
/// </summary>
internal interface IBbScannerLineAwareRowDecoder
{
    /// <summary>A new image starts: everything kept from the lines of the last one is forgotten.</summary>
    void BeginImage();

    /// <summary>
    /// Where the line the next <see cref="IBbScannerRowDecoder.DecodeRow"/> call reads lies: its sample <c>i</c> is at
    /// <c>(x0 + i dx, y0 + i dy)</c>, counted from the far end when the row has been turned round; and whether it was
    /// binarised the other way round, for a light symbol on a dark ground.
    /// </summary>
    void SetLine(float x0, float y0, float dx, float dy, int length, bool reversed, bool inverted);

    /// <summary>
    /// After a line that read part of a symbol but not the whole of it, that part; <see langword="null"/> when there is
    /// nothing to follow up. Asked after every line the decoder read nothing whole on.
    /// </summary>
    BbScannerPartialRead? Partial { get; }
}
