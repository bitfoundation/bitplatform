namespace AdminPanel.Client.Core.Services;

public partial class MessageBoxData(string message, string title, TaskCompletionSource<bool> taskCompletionSource)
{
    public string Message { get; set; } = message;

    public string Title { get; set; } = title;

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; } = taskCompletionSource;
}
