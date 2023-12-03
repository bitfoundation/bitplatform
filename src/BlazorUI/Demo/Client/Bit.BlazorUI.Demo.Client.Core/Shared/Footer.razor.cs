using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class Footer
{
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    private IExceptionHandler exceptionHandler { get; set; } = default!;

    public string CurrentUrl { get; set; }

    protected override async Task OnInitAsync()
    {
        try
        {
            SetCurrentUrl();
            NavigationManager.LocationChanged += OnLocationChanged;

            await base.OnInitAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        base.Dispose();
    }
}
