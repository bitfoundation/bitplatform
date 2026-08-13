using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class MainLayout : IDisposable
{
    private bool _isHomePage;
    private bool _isNavPanelOpen;
    private BitAppShell? _appShellRef;



    [AutoInject] private IExceptionHandler _exceptionHandler = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IPrerenderStateService _prerenderStateService = default!;
    [AutoInject] private BitAccentColorService _accentColorService = default!;


    /// <summary>
    /// The accent the server read from its cookie while prerendering (see App.razor in the server
    /// project). Null on the clients, which have no such cookie to cascade - they restore the accent
    /// from their own stores once interactive.
    /// </summary>
    [CascadingParameter(Name = nameof(PrerenderedAccentColor))] public string? PrerenderedAccentColor { get; set; }



    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Before the first render, so the swatches - which render below this layout - mark the
            // visitor's accent in the prerendered markup rather than flipping to it on hydration.
            // Round-tripped through the prerender state because the interactive client runs in its
            // own service scope, where the cascaded (HttpContext-backed) value is gone: without it
            // the swatches would drop back to Blue for the renders between hydration and the point
            // where AppAccentColorService can reach localStorage.
            _accentColorService.SeedFromPrerender(await _prerenderStateService.GetValue(
                nameof(PrerenderedAccentColor), () => Task.FromResult(PrerenderedAccentColor)));

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    protected override void OnInitialized()
    {
        try
        {
            SetCurrentUrl();
            _navigationManager.LocationChanged += OnLocationChanged;

            base.OnInitialized();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    // The accent is restored from the layout rather than from the home page so a refresh that lands
    // anywhere in the docs comes back with the visitor's color, and so the home page finds it already
    // applied when they navigate back. localStorage is unreachable until the client is live, hence
    // the wait for the first render.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await _accentColorService.InitializeAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }



    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        var url = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
        _isHomePage = url == "/";
    }



    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
