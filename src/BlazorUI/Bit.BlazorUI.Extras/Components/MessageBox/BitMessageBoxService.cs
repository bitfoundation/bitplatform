namespace Bit.BlazorUI;

/// <summary>
/// A wrapper serive around the <see cref="BitModalService"/> to enhance showing message boxes.
/// </summary>
public class BitMessageBoxService(BitModalService modalService)
{
    /// <summary>
    /// Shows a <see cref="BitMessageBox"/> inside a <see cref="BitModal"/> using the <see cref="BitModalService"/>.
    /// </summary>
    public async Task Show(string title, string body)
    {
        BitModalReference modalRef = default!;
        Dictionary<string, object> parameters = new()
        {
            { nameof(BitMessageBox.Title), title },
            { nameof(BitMessageBox.Body), body },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => modalRef.Close()) }
        };

        modalRef = await modalService.Show<BitMessageBox>(parameters);
    }
}
