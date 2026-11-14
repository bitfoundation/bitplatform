namespace Bit.BlazorUI;

/// <summary>
/// Renders the panels shown with the <see cref="BitPanelService"/>. Mount a single one in the layout, in the same
/// interactive render mode as the components that show panels.
/// </summary>
/// <remarks>
/// Its <c>ModalParameters</c> are the defaults of every panel it renders: a panel's own parameters win over them, one by
/// one.
/// </remarks>
public class BitPanelContainer : BitModalContainerBase<BitPanelReference, BitPanelParameters>
{
    // Resolved through the provider rather than injected: a container handed a Service of its own has no need of the
    // registered one, and an app that registered none is still an app it works in.
    [Inject] private IServiceProvider _services { get; set; } = default!;



    /// <summary>
    /// The service whose panels this container renders, in place of the registered one.
    /// </summary>
    [Parameter] public BitPanelService? Service { get; set; }



    protected override BitModalServiceBase<BitPanelReference, BitPanelParameters> ModalService =>
        Service ??
        (_services.GetService(typeof(BitPanelService)) as BitPanelService) ??
        throw new InvalidOperationException(
            $"No {nameof(BitPanelService)} is registered: call AddBitBlazorUIServices, or hand the container a {nameof(Service)} of its own.");

    protected override BitPanelParameters? MergeParameters(BitPanelParameters? panelParameters, BitPanelParameters? containerParameters)
    {
        return BitPanelParameters.Merge(panelParameters, containerParameters);
    }

    // Read off the merged parameters rather than the panel's own, so that a container can set the policy for every
    // panel it renders and a single panel can still say otherwise.
    protected override bool? GetCloseOnNavigation(BitPanelReference panelReference)
    {
        return GetMergedParameters(panelReference)?.CloseOnNavigation;
    }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.AddContent(0, RenderModals());
    }
}
