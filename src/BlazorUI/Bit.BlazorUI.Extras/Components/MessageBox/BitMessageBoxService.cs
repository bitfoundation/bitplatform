namespace Bit.BlazorUI;

/// <summary>
/// A wrapper service around the <see cref="BitProModalService"/> and <see cref="BitModalService"/> to enhance showing message boxes.
/// It works with either of these services when available and prefers the <see cref="BitProModalService"/> when both are available.
/// </summary>
public class BitMessageBoxService(BitProModalService? proModalService = null, BitModalService? modalService = null)
{
    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitProModal"/> using the <see cref="BitProModalService"/> when available,
    /// otherwise inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    public async Task Show(string title, string body)
    {
        Func<Task> closeModal = () => Task.CompletedTask;
        Dictionary<string, object> parameters = new()
        {
            { nameof(BitMessageBox.Title), title },
            { nameof(BitMessageBox.Body), body },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => closeModal()) }
        };

        if (proModalService is not null)
        {
            var modalRef = await proModalService.Show<BitMessageBox>(parameters);
            closeModal = modalRef.Close;
        }
        else if (modalService is not null)
        {
            var modalRef = await modalService.Show<BitMessageBox>(parameters);
            closeModal = modalRef.Close;
        }
        else
        {
            throw new InvalidOperationException("Neither BitProModalService nor BitModalService is available. Register at least one of them to use BitMessageBoxService.");
        }
    }
}
