namespace Boilerplate.Client.Core.Services;

public partial class MessageBoxService
{
    [AutoInject] private ModalService modalService = default!;

    public Task<bool> Show(string message, string title = "")
    {
        TaskCompletionSource<bool> tcs = new();
        Dictionary<string, object> parameters = new()
        {
            { nameof(MessageBox.Body), message },
            { nameof(MessageBox.OnOk), () => { tcs.SetResult(true); modalService.Close(); } }
        };
        modalService.Show<MessageBox>(parameters, title).ContinueWith(async task =>
        {
            await task;
            tcs.TrySetResult(false);
        });
        return tcs.Task;
    }
}
