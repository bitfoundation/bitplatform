namespace Boilerplate.Client.Core.Services;

public partial class MessageBoxService
{
    private bool isRunning = false;
    private readonly ConcurrentQueue<MessageBoxData> queue = new();


    [AutoInject] private readonly PubSubService pubSubService = default!;


    public Task<bool> Show(string message, string title = "")
    {
        TaskCompletionSource<bool> tcs = new();

        queue.Enqueue(new(message, title, tcs));

        if (isRunning is false)
        {
            isRunning = true;
            _ = ProcessQueue();
        }

        return tcs.Task;
    }

    private async Task ProcessQueue()
    {
        if (queue.IsEmpty)
        {
            isRunning = false;
            return;
        }

        if (queue.TryDequeue(out var data))
        {
            pubSubService.Publish(PubSubMessages.SHOW_MESSAGE, data);

            await data.TaskCompletionSource.Task;
        }

        _ = ProcessQueue();
    }
}
