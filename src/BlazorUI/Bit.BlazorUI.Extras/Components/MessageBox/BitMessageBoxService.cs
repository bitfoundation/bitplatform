namespace Bit.BlazorUI;

/// <summary>
/// A wrapper service around the <see cref="BitModalService"/> to enhance showing message boxes.
/// </summary>
/// <remarks>
/// A message box only appears if the <see cref="BitModalContainer"/> of the service is mounted in the layout:
/// a modal shown while no container is mounted is silently not rendered. Use
/// <see cref="BitModalServiceBase{TReference, TParameters}.IsContainerAvailable"/> to check for one before showing.
/// </remarks>
public class BitMessageBoxService(BitModalService modalService)
{
    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    public async Task Show(string title, string body)
    {
        // The parameters are built from the modal reference the service hands back, so the OnClose callback
        // closes this very modal without a window where the reference isn't assigned yet.
        await modalService.Show<BitMessageBox>(modalRef => BuildParameters(title, body, modalRef.Close));
    }

    private Dictionary<string, object> BuildParameters(string title, string body, Func<Task> onClose) => new()
    {
        { nameof(BitMessageBox.Title), title },
        { nameof(BitMessageBox.Body), body },
        { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, onClose) }
    };
}
