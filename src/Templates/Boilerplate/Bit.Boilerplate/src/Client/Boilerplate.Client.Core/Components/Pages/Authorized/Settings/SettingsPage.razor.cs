namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class SettingsPage
{
    [Parameter] public string? Section { get; set; }




    private bool isLoading;
    private string? openedAccordion;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        openedAccordion = Section?.ToLower();
    }
}
