namespace Bit.BlazorUI;

public partial class BitModalContainer
{
    [Inject] private BitModalService _modalService { get; set; } = default!;



    protected override BitModalServiceBase<BitModalReference, BitModalParameters> ModalService => _modalService;

    protected override BitModalParameters? MergeParameters(BitModalParameters? modalParameters, BitModalParameters? containerParameters)
    {
        return BitModalParameters.Merge(modalParameters, containerParameters);
    }
}
