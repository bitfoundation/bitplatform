namespace Boilerplate.Client.Core.Components.Layout;

public partial class JsBridge
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        var dotnetObj = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("App.registerJsBridge", dotnetObj);

        await base.OnAfterFirstRenderAsync();
    }


    [JSInvokable(nameof(ShowDiagnostic))]
    public async Task ShowDiagnostic()
    {
        PubSubService.Publish(PubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
}
