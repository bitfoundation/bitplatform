namespace Bit.BlazorUI;

public partial class BitModalContainer
{
    [Inject] private BitModalService _modalService { get; set; } = default!;



    protected override BitModalServiceBase<BitModalReference, BitModalParameters> ModalService => _modalService;

    protected override BitModalParameters? MergeParameters(BitModalParameters? modalParameters, BitModalParameters? containerParameters)
    {
        return BitModalParameters.Merge(modalParameters, containerParameters);
    }

    // Read off the merged parameters rather than the modal's own, so that a container can set the policy for
    // every modal it renders and a single modal can still say otherwise.
    protected override bool? GetCloseOnNavigation(BitModalReference modalReference)
    {
        return GetMergedParameters(modalReference)?.CloseOnNavigation;
    }
}
