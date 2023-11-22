namespace Boilerplate.Client.Core.Services;
public partial class MessageBoxService
{
    [AutoInject] IPubSubService _pubSubService = default!;

    public async Task Show(string message, string title = "")
    {
        TaskCompletionSource<object?> tsc = new();
        _pubSubService.Publish(PubSubMessages.SHOW_MESSAGE, (message, title, tsc));
        await tsc.Task;
    }
}
