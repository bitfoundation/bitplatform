namespace Boilerplate.Client.Core.Services;

public partial class MessageBoxService
{
    private bool isRunning = false;
    private readonly ConcurrentQueue<MessageBoxData> queue = new();


    [AutoInject] private readonly IPubSubService pubSubService = default!;


    public Task<bool> Show(string message, string title = "")
    {
        TaskCompletionSource<bool> tcs = new();

        queue.Enqueue(new(message, title, tcs));

        if (isRunning is false)
        {
            isRunning = true;
            Task.Run(ProcessQueue);
        }

        return tcs.Task;
    }

    private void ProcessQueue()
    {
        while (true)
        {
            if (queue.IsEmpty)
            {
                isRunning = false;
                break;
            }

            if (queue.TryDequeue(out var data))
            {
                pubSubService.Publish(PubSubMessages.SHOW_MESSAGE, data);
            }
        }
    }

}
