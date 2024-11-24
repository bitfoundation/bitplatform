namespace Boilerplate.Client.Core.Services;

public partial class PromptService
{
    [AutoInject] private ModalService modalService = default!;

    public Task<string?> Show(string message, string title = "")
    {
        TaskCompletionSource<string?> tcs = new();
        Dictionary<string, object> parameters = new()
        {
            { nameof(Prompt.Body), message },
            { nameof(Prompt.OnOk), (string value) => { tcs.SetResult(value); modalService.Close(); } }
        };
        modalService.Show<Prompt>(parameters, title).ContinueWith(async task =>
        {
            await task;
            tcs.SetResult(null);
        });
        return tcs.Task;
    }
}
