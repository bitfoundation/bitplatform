namespace Bit.BlazorUI;

public partial class BitModalContainer
{
    // Resolved through the provider rather than injected: a container handed a Service of its own has no need
    // of the registered one, and an app that registered none is still an app it works in.
    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// The modal service this container renders the modals of, in place of the one registered in DI - for a
    /// region of the app with modals of its own, which the modals of the rest of the app never mix with.
    /// </summary>
    /// <remarks>
    /// Read when the container initializes and not to be changed afterwards: give the container a <c>@key</c> of
    /// the service to mount a new one for another service instead.
    /// </remarks>
    [Parameter] public BitModalService? Service { get; set; }



    protected override BitModalServiceBase<BitModalReference, BitModalParameters> ModalService =>
        Service ??
        (_services.GetService(typeof(BitModalService)) as BitModalService) ??
        throw new InvalidOperationException(
            $"No {nameof(BitModalService)} is registered: call AddBitBlazorUIServices, or hand the container a {nameof(Service)} of its own.");

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
