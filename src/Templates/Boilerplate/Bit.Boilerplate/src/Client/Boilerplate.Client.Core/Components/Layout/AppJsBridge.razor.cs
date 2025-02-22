namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppJsBridge
{
    private DotNetObjectReference<AppJsBridge>? dotnetObj;
    /// <summary>
    /// at the rendering time of this component (the component is added to the `RootLayout`)
    /// it registers an instance of the `DotNetObjectReference` into the js code (look at the `app.ts` file), 
    /// so we can later use it to call .net methods.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        dotnetObj = DotNetObjectReference.Create(this);

        await JSRuntime.FastInvokeVoidAsync("App.registerJsBridge", dotnetObj);

        await base.OnAfterFirstRenderAsync();
    }


    /// <summary>
    /// you can add any other method like this to utilize the bridge between js and .net code.
    /// </summary>
    [JSInvokable(nameof(ShowDiagnostic))]
    public async Task ShowDiagnostic()
    {
        PubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }

    [JSInvokable(nameof(PublishMessage))]
    public async Task PublishMessage(string message, string? payload)
    {
        PubSubService.Publish(message, payload);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        dotnetObj?.Dispose();
        await base.DisposeAsync(disposing);
    }
}
