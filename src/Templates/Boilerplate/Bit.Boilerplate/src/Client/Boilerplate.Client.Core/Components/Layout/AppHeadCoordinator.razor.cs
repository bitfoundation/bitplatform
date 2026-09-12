namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppHeadCoordinator
{
    /// <summary>Set by App.razor only: this instance is being rendered inside the document's head tag already.</summary>
    [Parameter] public bool InHead { get; set; }

    /// <summary>
    /// True when Server.Web renders this app (Blazor Server, WebAssembly and Auto), which is exactly when App.razor
    /// has already rendered <see cref="AppHead"/> into the head.
    /// </summary>
    private bool RenderedInHeadByServerWeb => AppPlatform.IsBlazorHybrid is false && AppPlatform.IsWasmStandalone is false;
}
