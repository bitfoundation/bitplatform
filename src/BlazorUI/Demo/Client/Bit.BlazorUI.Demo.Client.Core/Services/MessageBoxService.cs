namespace Bit.BlazorUI.Demo.Client.Core.Services;

public partial class MessageBoxService
{
    [AutoInject] private BitModalService modalService { get; set; } = default!;

    public async Task Show(string message, string title = "")
    {
        BitModalReference modalRef = default!;
        Dictionary<string, object> parameters = new()
        {
            { nameof(BitMessageBox.Title), title },
            { nameof(BitMessageBox.Body), message },
            { nameof(BitMessageBox.OnClose), EventCallback.Factory.Create(this, () => modalRef.Close()) }
        };
        modalRef = await modalService.Show<BitMessageBox>(parameters);
    }
}
