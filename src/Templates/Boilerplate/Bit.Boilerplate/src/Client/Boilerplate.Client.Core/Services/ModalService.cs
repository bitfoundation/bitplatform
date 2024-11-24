namespace Boilerplate.Client.Core.Services;

public partial class ModalService
{
    private bool isRunning = false;
    private readonly ConcurrentQueue<ModalData> queue = new();


    [AutoInject] private readonly PubSubService pubSubService = default!;


    public Task<bool> Show(Type type, IDictionary<string, object>? parameters = null, string title = "")
    {
        TaskCompletionSource<bool> tcs = new();

        queue.Enqueue(new(type, parameters, title, tcs));

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
            pubSubService.Publish(ClientPubSubMessages.SHOW_MODAL, data, persistent: true);

            await data.TaskCompletionSource.Task;
        }

        _ = ProcessQueue();
    }
}
