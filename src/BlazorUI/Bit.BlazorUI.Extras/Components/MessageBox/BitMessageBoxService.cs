namespace Bit.BlazorUI;

/// <summary>
/// A wrapper service around the <see cref="BitProModalService"/> and <see cref="BitModalService"/> to enhance showing message boxes.
/// It works with either of these services when available and prefers the <see cref="BitProModalService"/> when both are available.
/// </summary>
/// <remarks>
/// A message box only appears if the modal container of the chosen service is mounted in the layout:
/// <see cref="BitProModalContainer"/> for the <see cref="BitProModalService"/> and <see cref="BitModalContainer"/>
/// for the <see cref="BitModalService"/>. Because the two services are usually both registered but a layout may
/// mount only one container, the selection (see <see cref="Show"/>) is driven by which container is actually
/// mounted rather than by which service is registered, so the message box is not silently swallowed.
/// </remarks>
public class BitMessageBoxService(BitProModalService? proModalService = null, BitModalService? modalService = null)
{
    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitProModal"/> using the <see cref="BitProModalService"/>,
    /// otherwise inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    /// <remarks>
    /// Both services are commonly registered together (registering the Extras services adds both), yet whether a
    /// message box actually renders depends on which modal container is mounted, not on which service is registered:
    /// a modal shown through a service whose container is absent is silently not rendered. To avoid that, the service
    /// to use is chosen from container availability (<see cref="BitModalServiceBase{TReference, TParameters}.IsContainerAvailable"/>),
    /// in this order:
    /// <list type="number">
    /// <item>the <see cref="BitProModalService"/> when its <see cref="BitProModalContainer"/> is mounted (preferred when both are);</item>
    /// <item>otherwise the <see cref="BitModalService"/> when its <see cref="BitModalContainer"/> is mounted;</item>
    /// <item>otherwise the only registered service — or, when both are registered but neither container is mounted, the
    /// preferred <see cref="BitProModalService"/> (a misconfiguration that renders nothing regardless: mount a container).</item>
    /// </list>
    /// Container availability reflects live state and is reliable at the moment of a user-triggered show, since the
    /// layout (and thus the container) has already rendered by then.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Neither <see cref="BitProModalService"/> nor <see cref="BitModalService"/> is registered.</exception>
    public async Task Show(string title, string body)
    {
        if (proModalService is null && modalService is null)
        {
            throw new InvalidOperationException("Neither BitProModalService nor BitModalService is available. Register at least one of them to use BitMessageBoxService.");
        }

        // Choose the service whose container is actually mounted (preferring the pro one), because a modal shown
        // through a service without a mounted container is silently not rendered. Only when neither container is
        // mounted do we fall back to the preferred registered service.
        bool useProModal;
        if (proModalService is null)
        {
            useProModal = false;
        }
        else if (modalService is null)
        {
            useProModal = true;
        }
        else if (modalService.IsContainerAvailable && !proModalService.IsContainerAvailable)
        {
            // Only the classic container is mounted: route to it so the message box actually renders.
            useProModal = false;
        }
        else
        {
            // The pro container is mounted, or neither is (nothing renders either way): keep the preferred pro service.
            useProModal = true;
        }

        // The parameters are built from the modal reference the service hands back, so the OnClose callback
        // closes this very modal without a window where the reference isn't assigned yet.
        if (useProModal)
        {
            await proModalService!.Show<BitMessageBox>(modalRef => BuildParameters(title, body, modalRef.Close));
        }
        else
        {
            await modalService!.Show<BitMessageBox>(modalRef => BuildParameters(title, body, modalRef.Close));
        }
    }

    private Dictionary<string, object> BuildParameters(string title, string body, Func<Task> onClose) => new()
    {
        { nameof(BitMessageBox.Title), title },
        { nameof(BitMessageBox.Body), body },
        { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, onClose) }
    };
}
