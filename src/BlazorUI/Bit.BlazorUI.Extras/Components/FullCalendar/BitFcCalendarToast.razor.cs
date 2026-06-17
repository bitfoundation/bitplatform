namespace Bit.BlazorUI;

public partial class BitFcCalendarToast
{
    private readonly List<ToastItem> _toasts = [];
    private int _nextId;

    public void Show(string message, bool isError = false)
    {
        var item = new ToastItem { Id = _nextId++, Message = message, IsError = isError };
        _toasts.Add(item);
        StateHasChanged();
        _ = RemoveAfterDelay(item.Id);
    }

    private async Task RemoveAfterDelay(int id)
    {
        await Task.Delay(3000);
        _toasts.RemoveAll(t => t.Id == id);
        await InvokeAsync(StateHasChanged);
    }

    private class ToastItem
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
    }
}
