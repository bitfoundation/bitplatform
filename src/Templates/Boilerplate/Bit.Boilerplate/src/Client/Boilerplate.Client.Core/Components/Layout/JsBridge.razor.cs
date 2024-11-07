namespace Boilerplate.Client.Core.Components.Layout;

public partial class JsBridge
{
    /// <summary>
    /// at the rendering time of this component (the component is added to the `RootLayout`)
    /// it registers an instance of the `DotNetObjectReference` into the js code (look at the `app.ts` file), 
    /// so we can later use it to call .net methods.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        var dotnetObj = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("App.registerJsBridge", dotnetObj);

        await base.OnAfterFirstRenderAsync();
    }


    /// <summary>
    /// you can add any other method like this to complete the bridge between js and .net code.
    /// </summary>
    [JSInvokable(nameof(ShowDiagnostic))]
    public async Task ShowDiagnostic()
    {
        PubSubService.Publish(PubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
}
