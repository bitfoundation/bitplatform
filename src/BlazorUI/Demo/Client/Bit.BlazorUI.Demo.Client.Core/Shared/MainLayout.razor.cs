using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class MainLayout : IDisposable
{
    private bool _isHomePage;
    private string? _pageTitle;
    private bool _isNavPanelOpen;
    private Action _unsubscribe = default!;
    private BitAppShell _appShellRef = default!;


    [AutoInject] private IPubSubService _pubSubService = default!;
    [AutoInject] private IExceptionHandler _exceptionHandler = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IPrerenderStateService _prerenderStateService = default!;


    protected override void OnParametersSet()
    {
        base.OnParametersSet();
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

        _unsubscribe = _pubSubService.Subscribe(PubSubMessages.PAGE_TITLE_CHANGED, async payload =>
        {
            _pageTitle = payload?.ToString();
            await Task.Yield();
            StateHasChanged();
        });
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
        _unsubscribe.Invoke();
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
