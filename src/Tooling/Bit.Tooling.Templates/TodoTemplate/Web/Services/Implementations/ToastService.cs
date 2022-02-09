namespace TodoTemplate.App.Services.Implementations;

public class ToastService : IToastService
{
    private readonly IJSRuntime _jsRuntime;

    public ToastService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ShowToast(string message)
    {
        await _jsRuntime.InvokeVoidAsync("window.alert", message);
    }
}
