namespace Bit.BlazorUI;

public partial class BitProModalContainer
{
    [Inject] private BitProModalService _modalService { get; set; } = default!;



    protected override BitModalServiceBase<BitProModalReference, BitProModalParameters> ModalService => _modalService;

    protected override BitProModalParameters? MergeParameters(BitProModalParameters? modalParameters, BitProModalParameters? containerParameters)
    {
        return BitProModalParameters.Merge(modalParameters, containerParameters);
    }
}
