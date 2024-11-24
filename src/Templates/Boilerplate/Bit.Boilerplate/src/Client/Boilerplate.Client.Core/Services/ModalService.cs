using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Boilerplate.Client.Core.Services;

public partial class ModalService
{
    private bool isRunning = false;
    private readonly ConcurrentQueue<ModalData> queue = new();


    [AutoInject] private readonly PubSubService pubSubService = default!;


    public void Close()
    {
        pubSubService.Publish(ClientPubSubMessages.CLOSE_MODAL);
    }

    public Task Show<T>(IDictionary<string, object>? parameters = null, string title = "")
    {
        TaskCompletionSource tcs = new();

        queue.Enqueue(new(typeof(T), parameters, title, tcs));

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
