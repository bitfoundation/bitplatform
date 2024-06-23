namespace Boilerplate.Client.Core.Services;

public class MessageBoxData(string message, string title, TaskCompletionSource<bool> taskCompletionSource)
{
    public string Message { get; set; } = message;

    public string Title { get; set; } = title;

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = taskCompletionSource;
}
