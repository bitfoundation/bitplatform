namespace Bit.Websites.Platform.Client.Services;
public partial class MessageBoxService
{
    [AutoInject] IPubSubService pubSubService = default!;

    public async Task Show(string message, string title = "")
    {
        TaskCompletionSource<object?> tcs = new();
        pubSubService.Publish(PubSubMessages.SHOW_MESSAGE, (message, title, tcs));
        await tcs.Task;
    }
}
