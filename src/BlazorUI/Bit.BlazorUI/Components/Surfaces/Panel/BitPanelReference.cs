namespace Bit.BlazorUI;

/// <summary>
/// A reference to a <see cref="BitPanel"/> shown with the <see cref="BitPanelService"/>: what closes it, what it was
/// closed with, and the content it shows.
/// </summary>
public class BitPanelReference : BitModalReferenceBase<BitPanelReference, BitPanelParameters>
{
    public BitPanelReference(BitPanelService panelService, bool persistent) : base(panelService, persistent)
    {
    }
}
