using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.LandingPage.Client.Components.Tour;

public partial class LandingTour : IDisposable
{
    // The same file the project file marks with BlazorWebAssemblyLazyLoad
    private static readonly string[] lazyAssemblies = ["Bit.Bonboarding.wasm"];

    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;
    [AutoInject] private LandingTourLauncher launcher = default!;

    private int run;
    private bool isLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            launcher.Requested += Start;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void Start() => InvokeAsync(StartAsync);

    private async Task StartAsync()
    {
        if (isLoaded is false)
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("landing.loadAssets", (string[])["_content/Bit.Bonboarding/bit-bonboarding.css"], (string[])[]);
            }
            catch (JSException)
            {
                return;
            }

            if (OperatingSystem.IsBrowser() && (AppContext.TryGetSwitch("Boilerplate.IsAot", out var isAot) && isAot) is false)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(lazyAssemblies);
            }

            isLoaded = true;
        }

        run++;
        StateHasChanged();
    }

    public void Dispose() => launcher.Requested -= Start;
}
