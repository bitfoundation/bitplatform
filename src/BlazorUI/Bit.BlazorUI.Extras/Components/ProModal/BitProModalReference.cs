namespace Bit.BlazorUI;

/// <summary>
/// A reference to the <see cref="BitProModal"/> instance that is shown using the <see cref="BitProModalService"/>.
/// </summary>
public class BitProModalReference : BitModalReferenceBase<BitProModalReference, BitProModalParameters>
{
    public BitProModalReference(BitProModalService modalService, bool persistent) : base(modalService, persistent)
    {
    }
}
